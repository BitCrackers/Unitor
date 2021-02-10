using Beebyte_Deobfuscator;
using Beebyte_Deobfuscator.Lookup;
using Beebyte_Deobfuscator.MonoDecompiler;
using Il2CppInspector;
using Il2CppInspector.Cpp;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Unitor.Core
{
    public class Game : IDisposable
    {
        public string Name { get; set; }
        public string VisualName { get; set; }
        public string Developer { get; set; }
        public string GameFolder { get; set; }
        public string Version { get; set; }
        public BackendInfo ScriptingBackend { get; set; }
        public PackingInfo Packing { get; set; }
        public ObfuscationInfo Obfuscation { get; set; }
        public AntiCheatInfo AntiCheat { get; set; }
        public LookupModule Module { get; set; }
        public Dictionary<LookupMethod, int> CalledMethods { get; set; }

        public Game()
        {
        }
        public Game(string path, EventHandler<string> statusCallback)
        {
            List<string> files = Directory.GetFiles(path).ToList();
            List<string> dirs = Directory.GetDirectories(path).ToList();
            string gameName = files.Where(f => Path.GetExtension(f) == ".exe" && dirs.Select(d => Path.GetFileName(d)).Contains($"{Path.GetFileNameWithoutExtension(f)}_Data")).FirstOrDefault();
            if (string.IsNullOrEmpty(gameName)) throw new ArgumentException("Could not find game executable and Data folder pair.");

            Name = Path.GetFileNameWithoutExtension(gameName);

            var appInfo = File.ReadLines($@"{path}\{Name}_Data\app.info").ToList();
            if (appInfo.Count() != 2) throw new ArgumentException("Malformed app.info file found in Data folder.");
            Developer = appInfo[0].Length > 14 ? appInfo[0].Substring(0, 11) + "..." : appInfo[0];
            VisualName = appInfo[1].Length > 14 ? appInfo[1].Substring(0, 11) + "..." : appInfo[1];

            ScriptingBackend = BackendInfo.FromPath(path, Name, statusCallback);
            Module = ScriptingBackend.Module;
            Obfuscation = new ObfuscationInfo(Module);
            Version = UnityVersion.FromAssetFile($@"{path}\{Name}_Data\globalgamemanagers.assets").ToString();
            Packing = new PackingInfo(ScriptingBackend.Il2CppStream);
            AntiCheat = new AntiCheatInfo(Module);
        }
        public void AnalyseMethodStructure(EventHandler endCallback = null, EventHandler<string> statusCallback = null)
        {
            statusCallback?.Invoke(null, "Gathering all methods");
            List<LookupMethod> methods = Module.Types.AsParallel().SelectMany(t => t.Methods).ToList();
            int total = methods.Count;
            int current = 0;
            statusCallback?.Invoke(null, "Starting dissasembly process");
            CalledMethods = new Dictionary<LookupMethod, int>();
            methods.AsParallel().SelectMany((m, index) =>
            {
                statusCallback?.Invoke(null, $"Processed {current}/{total} methods");
                current++;
                return m.GetCalls(Module.AppModel);
            }
            ).ToList().ForEach((m) =>
            {
                if (!CalledMethods.ContainsKey(m))
                {
                    CalledMethods.Add(m, 1);
                }
                else
                {
                    CalledMethods[m]++;
                }
            });
            statusCallback?.Invoke(null, "Done");
            endCallback?.Invoke(null, null);
        }
        public void Dispose()
        {
            Module = null;
            ScriptingBackend.Dispose();
        }
    }
    public enum BackendDef
    {
        Il2Cpp,
        Mono
    }

    public struct BackendInfo : IDisposable
    {
        public string Name { get; set; }
        public string Compiler { get; set; }
        public string Version { get; set; }
        public BackendDef Def { get; set; }
        public LookupModule Module { get; set; }

        public IFileFormatStream Il2CppStream { get; set; }
        private BackendInfo(BackendDef def, CppCompilerType? compiler, string version, LookupModule module, IFileFormatStream stream)
        {
            Def = def;
            Compiler = compiler.HasValue ? " - " + compiler.ToString() : "";
            Name = def.ToString() + Compiler;
            Version = version;
            Module = module;
            List<string> nspaces = new List<string>();
            foreach (string nspace in module.Namespaces)
            {
                if (Module.Types.Any(t => t.Namespace == nspace))
                {
                    nspaces.Add(nspace);
                }
            }
            nspaces[0] = "<root>";
            Module.Namespaces = nspaces;
            Il2CppStream = stream;
        }
        public static BackendInfo FromPath(string path, string name, EventHandler<string> statusCallback = null)
        {
            List<string> files = Directory.GetFiles(path).ToList();
            BackendDef def = files.Any(f => Path.GetFileName(f) == "GameAssembly.dll") ? BackendDef.Il2Cpp : BackendDef.Mono;
            LookupModel model = new LookupModel("");

            switch (def)
            {
                case BackendDef.Il2Cpp:
                    string metapath = @$"{path}\{name}_Data\il2cpp_data\Metadata\global-metadata.dat";
                    string binarypath = files.First(f => Path.GetFileName(f) == "GameAssembly.dll");
                    statusCallback?.Invoke(null, "Loading Il2Cpp metadata");
                    PluginManager.Enabled = false;

                    Metadata metadata = Metadata.FromStream(new MemoryStream(File.ReadAllBytes(metapath)));
                    statusCallback?.Invoke(null, "Loading Il2Cpp binary");

                    IFileFormatStream stream = FileFormatStream.Load(new FileStream(binarypath, FileMode.Open, FileAccess.Read, FileShare.Read));

                    statusCallback?.Invoke(null, "Creating Il2Cpp Type model");
                    var il2cpp = Il2CppInspector.Il2CppInspector.LoadFromStream(stream, metadata);
                    TypeModel typeModel = new TypeModel(il2cpp[0]);

                    statusCallback?.Invoke(null, "Creating universal model");
                    LookupModule module = typeModel.ToLookupModule(model, statusCallback);
                    string version = il2cpp[0].Version.ToString();
                    statusCallback?.Invoke(null, "Finding Il2Cpp informtion");

                    return new BackendInfo(def, CppCompiler.GuessFromImage(il2cpp[0].BinaryImage), version, module, stream);
                case BackendDef.Mono:
                    statusCallback?.Invoke(null, "Loading Assembly-CSharp.dll");
                    MonoDecompiler monoDecompiler = MonoDecompiler.FromFile(@$"{path}\{name}_Data\Managed\Assembly-CSharp.dll");
                    statusCallback?.Invoke(null, "Loading Assembly-CSharp-firstpass.dll");
                    MonoDecompiler monoDecompilerFirstpass = MonoDecompiler.FromFile(@$"{path}\{name}_Data\Managed\Assembly-CSharp-firstpass.dll");

                    statusCallback?.Invoke(null, "Creating universal model");
                    module = monoDecompiler.GetLookupModule(model);
                    module.Namespaces.AddRange(monoDecompilerFirstpass.GetLookupModule(model, statusCallback).Namespaces);
                    return new BackendInfo(def, null, "", module, null);
                default:
                    return new BackendInfo(def, null, "", null, null);
            }
        }

        public void Dispose()
        {
            Module = null;
            Il2CppStream = null;
        }
    }
    public enum PackingDef
    {
        VMProtect,
        Themida,
        UPX,
        None
    }
    public struct PackingInfo
    {
        public string Name { get; set; }
        public bool Detected { get; }
        public PackingDef Def { get; set; }
        public PackingInfo(IFileFormatStream stream)
        {
            if (stream == null)
            {
                Def = PackingDef.None;
                Name = Def.ToString();
                Detected = Def != PackingDef.None;
                return;
            }

            List<Section> sections = stream.GetSections().ToList();
            Def = PackingDef.None;
            if (sections.Any(s => s.Name.Equals(".vmp3"))) Def = PackingDef.VMProtect;
            if (sections.Any(s => s.Name.Equals(".themida"))) Def = PackingDef.Themida;
            if (sections.Any(s => s.Name.Equals(".upx"))) Def = PackingDef.UPX;
            Name = Def.ToString();
            Detected = Def != PackingDef.None;
        }
    }
    public enum AntiCheatDef
    {
        BattleEye,
        EasyAntiCheat,
        Riot,
        CodeStage,
        Custom,
        None
    }
    public struct AntiCheatInfo
    {
        public string Name { get; set; }
        public bool Detected { get; }
        public AntiCheatDef Def { get; set; }
        public AntiCheatInfo(LookupModule lookupModule)
        {
            Def = AntiCheatDef.None;
            if (lookupModule.Namespaces.Contains("CodeStage.AntiCheat.Common")) Def = AntiCheatDef.CodeStage;
            if (lookupModule.Namespaces.Contains("Shared.Antitamper")) Def = AntiCheatDef.Riot;
            if (lookupModule.Namespaces.Contains("BattlEye")) Def = AntiCheatDef.BattleEye;

            Name = Def.ToString();
            Detected = Def != AntiCheatDef.None;
        }
    }
    public enum ObfuscatorDef
    {
        Beebyte,
        Custom,
        None
    }
    public struct ObfuscationInfo
    {
        public string Name { get; }
        public bool Detected { get; }
        public ObfuscatorDef Def { get; set; }
        public ObfuscationInfo(LookupModule module)
        {
            Def = module.Namespaces.Contains("Beebyte.Obfuscator") ? ObfuscatorDef.Beebyte : ObfuscatorDef.None;
            Name = Def.ToString();
            Detected = Def != ObfuscatorDef.None;
        }
    }
}
