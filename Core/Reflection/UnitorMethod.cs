using dnlib.DotNet;
using Il2CppInspector.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unitor.Core.Reflection
{
    public class UnitorMethod
    {
        public readonly UnitorModel Owner;
        public MethodInfo Il2CppMethod { get; set; }
        public MethodDef MonoMethod { get; set; }

        public string Name
        {
            get
            {
                return Il2CppMethod?.Name ?? MonoMethod?.Name;
            }
            set
            {
                if (Il2CppMethod != null)
                {
                    Il2CppMethod.Name = value;
                }
                else if (MonoMethod != null)
                {
                    MonoMethod.Name = value;
                }
            }
        }
        public string CSharpName => Il2CppMethod?.ToString() ?? MonoMethod?.ToString() ?? "";
        public UnitorType DeclaringType { get; set; }
        public UnitorType ReturnType { get; set; }
        public List<UnitorType> ParameterList { get; set; }
        public bool IsEmpty => Il2CppMethod == null && MonoMethod == null;
        public ulong Address => Il2CppMethod?.VirtualAddress?.Start ?? 0x0;
        public bool IsPropertymethod
        {
            get
            {
                if (DeclaringType.IsEmpty)
                {
                    return false;
                }
                if (DeclaringType.Properties != null)
                {
                    List<UnitorMethod> ts = DeclaringType.Properties.Select(p => p.GetMethod).Union(DeclaringType.Properties.Select(p => p.SetMethod)).ToList();
                    if (ts.Any(m => m.Name == Name))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public UnitorMethod(UnitorModel lookupModel) { Owner = lookupModel; }

        public override string ToString() => CSharpName;
    }
}
