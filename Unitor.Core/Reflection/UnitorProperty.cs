﻿using dnlib.DotNet;
using Il2CppInspector.Reflection;

namespace Unitor.Core.Reflection
{
    public class UnitorProperty
    {
        private readonly UnitorModel Owner;
        public PropertyInfo Il2CppProperty { get; set; }
        public PropertyDef MonoProperty { get; set; }
        public string Name
        {
            get
            {
                return Il2CppProperty?.Name ?? MonoProperty?.Name;
            }
            set
            {
                if (Il2CppProperty != null)
                {
                    Il2CppProperty.Name = value;
                }
                else if (MonoProperty != null)
                {
                    MonoProperty.Name = value;
                }
            }
        }

        public string CSharpName => Il2CppProperty?.CSharpName ?? MonoProperty?.Name ?? "";
        public UnitorType PropertyType { get; set; }
        public UnitorMethod GetMethod { get; set; }
        public UnitorMethod SetMethod { get; set; }
        public int Index { get; set; }
        public bool IsEmpty => Il2CppProperty == null && MonoProperty == null;
        public UnitorType DeclaringType { get; set; }
        public UnitorProperty(UnitorModel lookupModel) { Owner = lookupModel; }

        public override string ToString()
        {
            return CSharpName;
        }
    }
}
