using dnlib.DotNet;
using Il2CppInspector.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Unitor.Core.Reflection
{
    public class UnitorType
    {
        public readonly UnitorModel Owner;
        public TypeInfo Il2CppType { get; set; }
        public TypeDef MonoType { get; set; }

        public string Name
        {
            get
            {
                return Il2CppType?.Name ?? MonoType?.Name;
            }
            set
            {
                if (Il2CppType != null)
                {
                    Il2CppType.Name = value;
                }
                else if (MonoType != null)
                {
                    MonoType.Name = value;
                }
            }
        }
        public string CSharpName => Il2CppType?.CSharpName ?? MonoType?.Name ?? "";
        public string AssemblyName => Il2CppType?.Assembly.ShortName ?? MonoType?.Module.Assembly.Name;
        public string Namespace => Il2CppType?.Namespace ?? MonoType?.Namespace;
        public bool IsGenericType => Il2CppType?.IsGenericType ?? MonoType?.HasGenericParameters ?? false;
        public List<UnitorType> GenericTypeParameters { get; set; }
        public bool IsEnum => Il2CppType?.IsEnum ?? MonoType?.IsEnum ?? false;
        public bool IsPrimitive => Il2CppType?.IsPrimitive ?? MonoType?.IsPrimitive ?? false;
        public bool IsArray => Il2CppType?.IsArray ?? MonoType?.TryGetArraySig()?.IsArray ?? false;
        public UnitorType ElementType { get; set; }
        public bool IsEmpty => Il2CppType == null && MonoType == null;
        public bool IsNested => Il2CppType?.IsNested ?? MonoType?.IsNested ?? false;
        public bool Translated { get; private set; }
        public UnitorType DeclaringType { get; set; }
        public List<UnitorField> Fields { get; set; }
        public List<UnitorProperty> Properties { get; set; }
        public List<UnitorMethod> Methods { get; set; }
        public List<UnitorType> Children { get; set; }
        public bool Resolved { get; set; }
        public UnitorType(UnitorModel lookupModel) { Owner = lookupModel; }

        public void Resolve()
        {
            if (Resolved)
            {
                return;
            }
            Fields = Il2CppType?.DeclaredFields.ToUnitorFieldList(Owner).ToList() ?? MonoType?.Fields.ToUnitorFieldList(Owner).ToList();
            DeclaringType = Il2CppType?.DeclaringType.ToUnitorType(Owner, false) ?? MonoType?.DeclaringType.ToUnitorType(Owner, false);
            Properties = Il2CppType?.DeclaredProperties.ToUnitorPropertyList(Owner).ToList() ?? MonoType?.Properties.ToUnitorPropertyList(Owner).ToList();
            Methods = Il2CppType?.DeclaredMethods.ToUnitorMethodList(Owner).ToList() ?? MonoType?.Methods.ToUnitorMethodList(Owner).ToList();

            if (!DeclaringType.IsEmpty)
            {
                DeclaringType.Children.Add(this);
            }
        }

        public override string ToString()
        {
            if (IsPrimitive)
            {
                return CSharpName;
            }

            string typename = "";
            if (!IsEmpty)
            {
                typename = CSharpName;
            }
            if (typename == "")
            {
                typename = "object";
            }

            if (IsArray)
            {
                typename = $"{ElementType?.ToString() ?? "object"}[]";
            }
            else if (IsGenericType && GenericTypeParameters.Any())
            {
                typename = Name.Split("`")[0] + "<";
                foreach (UnitorType t in GenericTypeParameters)
                {
                    typename += t.ToString() + (GenericTypeParameters[GenericTypeParameters.Count() - 1] != t ? ", " : "");
                }
                typename += ">";
            }
            else
            {
                typename = CSharpName;
            }
            return typename;
        }
    }
}
