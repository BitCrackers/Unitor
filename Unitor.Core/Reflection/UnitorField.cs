using dnlib.DotNet;
using Il2CppInspector.Reflection;

namespace Unitor.Core.Reflection
{
    public class UnitorField
    {
        private readonly UnitorModel Owner;
        public FieldInfo Il2CppField { get; set; }
        public FieldDef MonoField { get; set; }
        public string Name
        {
            get
            {
                return Il2CppField?.Name ?? MonoField?.Name;
            }
            set
            {
                if (Il2CppField != null)
                {
                    Il2CppField.Name = value;
                }
                else if (MonoField != null)
                {
                    MonoField.Name = value;
                }
            }
        }
        public string CSharpName => Il2CppField?.CSharpName ?? MonoField?.Name ?? "";
        public bool IsStatic => Il2CppField?.IsStatic ?? MonoField?.IsStatic ?? false;
        public bool IsPublic => Il2CppField?.IsPublic ?? MonoField?.IsPublic ?? false;
        public bool IsPrivate => Il2CppField?.IsPrivate ?? MonoField?.IsPrivate ?? false;
        public bool IsLiteral => Il2CppField?.IsLiteral ?? MonoField?.IsLiteral ?? false;
        public long Offset => Il2CppField?.Offset ?? 0x0;
        public bool IsEmpty => Il2CppField == null && MonoField == null;
        public UnitorType Type { get; set; }
        public UnitorType DeclaringType { get; set; }
        public UnitorField(UnitorModel lookupModel) { Owner = lookupModel; }
        public override string ToString() => CSharpName;
    }
}
