using dnlib.DotNet;
using Il2CppInspector.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Unitor.Core.Reflection
{
    public static class ReflectionExtensions
    {
        public static UnitorType ToUnitorType(this TypeInfo type, UnitorModel lookupModel, bool recurse)
        {
            if (type == null || type.Name == "<Module>")
            {
                return new UnitorType(lookupModel);
            }

            lookupModel.Il2CppTypeMatches.GetOrAdd(type, new UnitorType(lookupModel) { Il2CppType = type, Children = new List<UnitorType>() });
            if (lookupModel.Il2CppTypeMatches[type].Resolved)
            {
                return lookupModel.Il2CppTypeMatches[type];
            }

            if (lookupModel.Il2CppTypeMatches[type].IsGenericType)
            {
                lookupModel.Il2CppTypeMatches[type].GenericTypeParameters = lookupModel.Il2CppTypeMatches[type].Il2CppType.GenericTypeArguments.Where(t => t != type).ToUnitorTypeList(lookupModel, false).ToList();
            }
            if (lookupModel.Il2CppTypeMatches[type].IsArray)
            {
                TypeInfo elementType = lookupModel.Il2CppTypeMatches[type].Il2CppType.ElementType;
                if (elementType != null && elementType != type)
                {
                    lookupModel.Il2CppTypeMatches[type].ElementType = elementType.ToUnitorType(lookupModel, false);
                }
            }
            if (lookupModel.Il2CppTypeMatches[type].IsTypeRef)
            {
                if (lookupModel.Il2CppTypeMatches.TryGetValue(type.ElementType, out UnitorType nonref))
                {
                    nonref.Ref = lookupModel.Il2CppTypeMatches[type];
                }
            }
            if (!recurse)
            {
                return lookupModel.Il2CppTypeMatches[type];
            }

            lookupModel.Il2CppTypeMatches[type].Fields = type.DeclaredFields.ToUnitorFieldList(lookupModel).ToList();
            lookupModel.Il2CppTypeMatches[type].DeclaringType = type.DeclaringType.ToUnitorType(lookupModel, false);
            lookupModel.Il2CppTypeMatches[type].Properties = type.DeclaredProperties.ToUnitorPropertyList(lookupModel).ToList();
            lookupModel.Il2CppTypeMatches[type].Methods = type.DeclaredMethods.ToUnitorMethodList(lookupModel).ToList();

            if (!lookupModel.Il2CppTypeMatches[type].DeclaringType.IsEmpty)
            {
                lookupModel.Il2CppTypeMatches[type].DeclaringType.Children.Add(lookupModel.Il2CppTypeMatches[type]);
            }
            lookupModel.Il2CppTypeMatches[type].Resolved = true;
            return lookupModel.Il2CppTypeMatches[type];
        }

        public static UnitorType ToUnitorType(this TypeDef type, UnitorModel lookupModel, bool recurse)
        {
            if (type == null)
            {
                return new UnitorType(lookupModel);
            }
            lookupModel.MonoTypeMatches.GetOrAdd(type, new UnitorType(lookupModel) { MonoType = type, Children = new List<UnitorType>() });
            if (lookupModel.MonoTypeMatches[type].Resolved)
            {
                return lookupModel.MonoTypeMatches[type];
            }

            if (lookupModel.MonoTypeMatches[type].IsGenericType)
            {
                lookupModel.MonoTypeMatches[type].GenericTypeParameters = lookupModel.MonoTypeMatches[type].MonoType.GenericParameters.Select(p => p.DeclaringType).Where(t => t != type).ToUnitorTypeList(lookupModel, false).ToList();
            }
            if (lookupModel.MonoTypeMatches[type].IsArray)
            {
                TypeDef elementType = lookupModel.MonoTypeMatches[type].MonoType.TryGetArraySig()?.TryGetTypeDef();
                if (elementType != null && elementType != type)
                {
                    lookupModel.MonoTypeMatches[type].ElementType = elementType.ToUnitorType(lookupModel, false) ?? new UnitorType(lookupModel);
                }
            }

            if (!recurse)
            {
                return lookupModel.MonoTypeMatches[type];
            }

            lookupModel.MonoTypeMatches[type].Fields = type.Fields.ToUnitorFieldList(lookupModel).ToList();
            lookupModel.MonoTypeMatches[type].DeclaringType = type.DeclaringType.ToUnitorType(lookupModel, false);
            lookupModel.MonoTypeMatches[type].Properties = type.Properties.ToUnitorPropertyList(lookupModel).ToList();
            lookupModel.MonoTypeMatches[type].Methods = type.Methods.ToUnitorMethodList(lookupModel).ToList();

            if (!lookupModel.MonoTypeMatches[type].DeclaringType.IsEmpty)
            {
                lookupModel.MonoTypeMatches[type].DeclaringType.Children.Add(lookupModel.MonoTypeMatches[type]);
            }
            lookupModel.MonoTypeMatches[type].Resolved = true;
            return lookupModel.MonoTypeMatches[type];
        }

        public static IEnumerable<UnitorType> ToUnitorTypeList(this IEnumerable<TypeDef> monoTypes, UnitorModel lookupModel, bool recurse = true, EventHandler<string> statusCallback = null)
        {
            int current = 0;
            var filteredTypes = monoTypes.Where(t => !t.IsNested);
            int total = monoTypes.Count();
            return monoTypes.AsParallel().Select(type =>
            {
                current++;
                statusCallback?.Invoke(null, $"Loaded {current}/{total} types...");
                return type.ToUnitorType(lookupModel, recurse);
            });
        }

        public static IEnumerable<UnitorType> ToUnitorTypeList(this IEnumerable<TypeInfo> il2cppTypes, UnitorModel lookupModel, bool recurse = true, EventHandler<string> statusCallback = null)
        {
            int current = 0;
            var filteredTypes = il2cppTypes.Where(t => !t.IsNested);
            int total = filteredTypes.Count();
            return filteredTypes.AsParallel().Select(type =>
            {
                current++;
                statusCallback?.Invoke(null, $"Loaded {current}/{total} types...");
                return type.ToUnitorType(lookupModel, recurse);
            });
        }

        public static UnitorField ToUnitorField(this FieldInfo field, UnitorModel lookupModel)
        {
            if (field == null)
            {
                return new UnitorField(lookupModel);
            }

            return new UnitorField(lookupModel)
            {
                Il2CppField = field,
                Type = field.FieldType.ToUnitorType(lookupModel, false),
                DeclaringType = field.DeclaringType.ToUnitorType(lookupModel, false)
            };
        }

        public static UnitorField ToUnitorField(this FieldDef field, UnitorModel lookupModel)
        {
            if (field == null)
            {
                return new UnitorField(lookupModel);
            }
            TypeDef type = field.FieldType.ToTypeDef();

            return new UnitorField(lookupModel)
            {
                MonoField = field,
                Type = type.ToUnitorType(lookupModel, false),
                DeclaringType = field.DeclaringType.ToUnitorType(lookupModel, false)
            };
        }

        public static IEnumerable<UnitorField> ToUnitorFieldList(this IReadOnlyCollection<FieldInfo> il2cppFields, UnitorModel lookupModel) =>
            il2cppFields.Select(f => f.ToUnitorField(lookupModel));

        public static IEnumerable<UnitorField> ToUnitorFieldList(this IList<FieldDef> monoFields, UnitorModel lookupModel) =>
            monoFields.Select(f => f.ToUnitorField(lookupModel));

        public static UnitorProperty ToUnitorProperty(this PropertyDef property, int index, UnitorModel lookupModel)
        {
            if (property == null)
            {
                return new UnitorProperty(lookupModel);
            }

            return new UnitorProperty(lookupModel)
            {
                PropertyType = property.PropertySig.RetType.TryGetTypeDef()?.ToUnitorType(lookupModel, false) ?? new UnitorType(lookupModel),
                GetMethod = property.GetMethod.ToUnitorMethod(lookupModel),
                SetMethod = property.SetMethod.ToUnitorMethod(lookupModel),
                Index = index,
                MonoProperty = property
            };
        }

        public static UnitorProperty ToUnitorProperty(this PropertyInfo property, UnitorModel lookupModel)
        {
            if (property == null)
            {
                return new UnitorProperty(lookupModel);
            }

            return new UnitorProperty(lookupModel)
            {
                PropertyType = property.PropertyType.ToUnitorType(lookupModel, false),
                GetMethod = property.GetMethod.ToUnitorMethod(lookupModel),
                SetMethod = property.SetMethod.ToUnitorMethod(lookupModel),
                Index = property.Index,
                Il2CppProperty = property
            };
        }

        public static IEnumerable<UnitorProperty> ToUnitorPropertyList(this IList<PropertyDef> monoProperties, UnitorModel lookupModel) =>
            monoProperties.Select((p, i) => p.ToUnitorProperty(i, lookupModel));

        public static IEnumerable<UnitorProperty> ToUnitorPropertyList(this ReadOnlyCollection<PropertyInfo> il2cppProperties, UnitorModel lookupModel) =>
            il2cppProperties.Select(p => p.ToUnitorProperty(lookupModel));

        public static UnitorMethod ToUnitorMethod(this MethodDef method, UnitorModel lookupModel)
        {
            if (method == null)
            {
                return new UnitorMethod(lookupModel);
            }

            List<UnitorType> ParameterList = new List<UnitorType>();
            foreach (Parameter param in method.Parameters)
            {
                if (param.Type.IsTypeDefOrRef)
                {
                    ParameterList.Add(param.Type.TryGetTypeDef()?.ToUnitorType(lookupModel, false) ?? new UnitorType(lookupModel));
                }
            }
            UnitorMethod m = new UnitorMethod(lookupModel)
            {
                DeclaringType = method.DeclaringType.ToUnitorType(lookupModel, false),
                ReturnType = method.ReturnType.TryGetTypeDef()?.ToUnitorType(lookupModel, false) ?? new UnitorType(lookupModel),
                ParameterList = ParameterList,
                MonoMethod = method
            };
            return m;
        }

        public static UnitorMethod ToUnitorMethod(this MethodInfo method, UnitorModel lookupModel)
        {
            if (method == null)
            {
                return new UnitorMethod(lookupModel);
            }

            List<UnitorType> ParameterList = new List<UnitorType>();
            ParameterList.AddRange(method.DeclaredParameters.Select(p => p.ParameterType.ToUnitorType(lookupModel, false)));

            UnitorMethod m = new UnitorMethod(lookupModel)
            {
                DeclaringType = method.DeclaringType.ToUnitorType(lookupModel, false),
                ParameterList = ParameterList,
                ReturnType = method.ReturnType.ToUnitorType(lookupModel, false),
                Il2CppMethod = method
            };
            return m;
        }

        public static IEnumerable<UnitorMethod> ToUnitorMethodList(this IList<MethodDef> monoMethods, UnitorModel lookupModel) =>
            monoMethods.Select(p => p.ToUnitorMethod(lookupModel));

        public static IEnumerable<UnitorMethod> ToUnitorMethodList(this ReadOnlyCollection<MethodInfo> il2cppMethods, UnitorModel lookupModel) =>
            il2cppMethods.Select(p => p.ToUnitorMethod(lookupModel));


        public static TypeDef ToTypeDef(this TypeSig type)
        {
            TypeDef typeDef = type.ToTypeDefOrRef().ResolveTypeDef();
            if (typeDef != null)
            {
                return typeDef;
            }
            if (type.IsArray)
            {
                typeDef = type.Next.ToTypeDefOrRef().ResolveTypeDef();
            }
            if (typeDef != null)
            {
                return typeDef;
            }
            if (type.IsGenericInstanceType)
            {
                typeDef = type.ToGenericInstSig().ToTypeDefOrRef().ResolveTypeDef();
            }
            return typeDef;
        }
    }
}
