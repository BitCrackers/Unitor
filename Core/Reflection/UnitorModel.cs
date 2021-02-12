using dnlib.DotNet;
using Il2CppInspector.Model;
using Il2CppInspector.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unitor.Core.Reflection
{
    public class UnitorModel
    {
        public List<TypeDef> ProcessedMonoTypes { get; } = new List<TypeDef>();
        public List<TypeInfo> ProcessedIl2CppTypes { get; } = new List<TypeInfo>();
        public Dictionary<TypeDef, UnitorType> MonoTypeMatches { get; } = new Dictionary<TypeDef, UnitorType>();
        public Dictionary<TypeInfo, UnitorType> Il2CppTypeMatches { get; } = new Dictionary<TypeInfo, UnitorType>();

        public List<string> Namespaces { get; } = new List<string>();
        public List<UnitorType> Types { get; } = new List<UnitorType>();
        public TypeModel TypeModel { get; set; }
        public AppModel AppModel { get; set; }
        public ModuleDef ModuleDef { get; set; }
        public Dictionary<UnitorMethod, int> CalledMethods { get; set; }
        public Dictionary<ulong, string> StringTable { get; set; }

        public static UnitorModel FromTypeModel(TypeModel typeModel, EventHandler<string> statusCallback = null)
        {
            UnitorModel model = new UnitorModel();
            model.Types.AddRange(typeModel.Types.Where(
                t => !t.Assembly.ShortName.Contains("System") &&
                !t.Assembly.ShortName.Contains("Mono") &&
                !t.Assembly.ShortName.Contains("UnityEngine") &&
                t.Assembly.ShortName != "mscorlib.dll"
                ).ToUnitorTypeList(model, statusCallback: statusCallback).Where(t => !t.IsEmpty));
            model.Namespaces.AddRange(model.Types.Select(t => t.Namespace).Distinct());
            model.TypeModel = typeModel;
            statusCallback?.Invoke(model, "Creating AppModel");
            model.AppModel = new AppModel(typeModel);
            model.StringTable = model.AppModel.Strings;
            List<UnitorMethod> methods = model.Types.AsParallel().SelectMany(t => t.Methods).ToList();
            int total = methods.Count;
            int current = 0;
            statusCallback?.Invoke(null, "Starting dissasembly process");
            model.CalledMethods = new Dictionary<UnitorMethod, int>();
            methods.AsParallel().SelectMany((m, index) =>
            {
                statusCallback?.Invoke(null, $"Processed {current}/{total} methods");
                current++;
                return m.GetCalls(model.AppModel);
            }
            ).ToList().ForEach((m) =>
            {
                if (!model.CalledMethods.ContainsKey(m))
                {
                    model.CalledMethods.Add(m, 1);
                }
                else
                {
                    model.CalledMethods[m]++;
                }
            });
            return model;
        }
        public static UnitorModel FromModuleDef(ModuleDef moduleDef, EventHandler<string> statusCallback = null)
        {
            UnitorModel model = new UnitorModel();
            model.Types.AddRange(moduleDef.Types.ToUnitorTypeList(model, statusCallback: statusCallback));
            model.Namespaces.AddRange(moduleDef.Types.Select(t => t.Namespace.String).Distinct());
            model.ModuleDef = moduleDef;
            statusCallback?.Invoke(model, "Analysing method structure");
            List<UnitorMethod> methods = model.Types.AsParallel().SelectMany(t => t.Methods).ToList();
            int total = methods.Count;
            int current = 0;
            statusCallback?.Invoke(null, "Starting dissasembly process");
            model.CalledMethods = new Dictionary<UnitorMethod, int>();
            List<(ulong, string)> strings = new List<(ulong, string)>();
            methods.AsParallel().SelectMany((m, index) =>
            {
                statusCallback?.Invoke(null, $"Processed {current}/{total} methods");
                current++;
                strings.AddRange(m.GetStrings());
                return m.GetCalls(model.AppModel);
            }
            ).ToList().ForEach((m) =>
            {
                if (!model.CalledMethods.ContainsKey(m))
                {
                    model.CalledMethods.Add(m, 1);
                }
                else
                {
                    model.CalledMethods[m]++;
                }
            });
            model.StringTable = strings
                .GroupBy(x => x.Item1)
                .Select(g => g.First())
                .GroupBy(x => x.Item2)
                .Select(g => g.First())
                .ToDictionary(p => p.Item1, p => p.Item2);
            return model;
        }
        public void Add(UnitorModel model)
        {
            Types.AddRange(model.Types);
            Namespaces.Clear();
            Namespaces.AddRange(Types.Select(t => t.Namespace).Distinct());
        }
    }
}
