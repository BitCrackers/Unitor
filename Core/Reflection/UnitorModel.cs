using dnlib.DotNet;
using Il2CppInspector.Model;
using Il2CppInspector.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Unitor.Core.Reflection
{
    public class UnitorModel
    {
        public ConcurrentDictionary<TypeDef, UnitorType> MonoTypeMatches { get; } = new ConcurrentDictionary<TypeDef, UnitorType>();
        public ConcurrentDictionary<TypeInfo, UnitorType> Il2CppTypeMatches { get; } = new ConcurrentDictionary<TypeInfo, UnitorType>();

        public List<string> Namespaces { get; } = new List<string>();
        public List<UnitorType> Types { get; } = new List<UnitorType>();
        public TypeModel TypeModel { get; set; }
        public AppModel AppModel { get; set; }
        public ModuleDef ModuleDef { get; set; }
        public Dictionary<UnitorMethod, int> CalledMethods { get; set; }
        public ConcurrentDictionary<UnitorMethod, List<UnitorMethod>> MethodReferences { get; } = new ConcurrentDictionary<UnitorMethod, List<UnitorMethod>>();
        public Dictionary<ulong, string> StringTable { get; set; }

        public static UnitorModel FromTypeModel(TypeModel typeModel, EventHandler<string> statusCallback)
        {
            UnitorModel model = new UnitorModel();
            statusCallback?.Invoke(model, "Creating AppModel");
            model.AppModel = new AppModel(typeModel);
            model.StringTable = model.AppModel.Strings;

            model.Types.AddRange(typeModel.Types.Where(
                t => !t.Assembly.ShortName.Contains("System") &&
                !t.Assembly.ShortName.Contains("Mono") &&
                !t.Assembly.ShortName.Contains("UnityEngine") &&
                t.Assembly.ShortName != "mscorlib.dll"
                ).ToUnitorTypeList(model, true, statusCallback).Where(t => !t.IsEmpty).Where(t => !t.IsTypeRef));

            model.Namespaces.AddRange(model.Types.Select(t => t.Namespace).Distinct());
            model.TypeModel = typeModel;
            model.StringTable = model.AppModel.Strings;
            List<UnitorMethod> methods = model.Types.AsParallel().SelectMany(t => t.Methods).ToList();
            statusCallback?.Invoke(null, "Analysing all methods");
            int total = methods.Count;
            int current = 0;
            Parallel.ForEach(methods, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount / 2, 1) }, m =>
            {
                statusCallback?.Invoke(null, $"Analysed {current}/{total} methods");
                current++;
                m.Analyse();
            });
            current = 0;

            model.CalledMethods = new Dictionary<UnitorMethod, int>();
            methods.AsParallel().SelectMany(m => m.MethodCalls).ToList().ForEach((m) =>
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
        public static UnitorModel FromTypeModel(TypeModel typeModel) => FromTypeModel(typeModel, null);

        public static UnitorModel FromModuleDef(ModuleDef moduleDef, EventHandler<string> statusCallback)
        {
            UnitorModel model = new UnitorModel();
            model.Types.AddRange(moduleDef.Types.ToUnitorTypeList(model, true, statusCallback));
            model.Namespaces.AddRange(moduleDef.Types.Select(t => t.Namespace.String).Distinct());
            model.ModuleDef = moduleDef;
            List<UnitorMethod> methods = model.Types.SelectMany(t => t.Methods).ToList();
            statusCallback?.Invoke(null, "Analysing all methods");
            int total = methods.Count;
            int current = 0;
            Parallel.ForEach(methods, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount / 2, 1) }, m =>
            {
                statusCallback?.Invoke(null, $"Analysed {current}/{total} methods");
                current++;
                m.Analyse();
            });
            model.CalledMethods = new Dictionary<UnitorMethod, int>();
            List<KeyValuePair<ulong, string>> strings = new List<KeyValuePair<ulong, string>>();
            methods.SelectMany((m) =>
            {
                strings.AddRange(m.Strings);
                return m.MethodCalls;
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
                .GroupBy(x => x.Key)
                .Select(g => g.First())
                .GroupBy(x => x.Value)
                .Select(g => g.First())
                .ToDictionary(p => p.Key, p => p.Value);
            return model;
        }
        public static UnitorModel FromModuleDef(ModuleDef moduleDef) => FromModuleDef(moduleDef, null);

        public void Add(UnitorModel model)
        {
            Types.AddRange(model.Types);
            Namespaces.Clear();
            Namespaces.AddRange(Types.Select(t => t.Namespace).Distinct());
        }
    }
}
