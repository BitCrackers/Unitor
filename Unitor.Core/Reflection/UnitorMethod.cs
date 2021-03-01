﻿using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Il2CppInspector.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Unitor.Core.Reflection
{
    public class UnitorMethod
    {
        private readonly UnitorModel Owner;
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
        public string MethodDecl => DeclaringType + "::" + CSharpName;
        public UnitorType DeclaringType { get; set; }
        public UnitorType ReturnType { get; set; }
        public List<UnitorType> ParameterList { get; set; }
        public bool IsEmpty => Il2CppMethod == null && MonoMethod == null;
        public ulong Address => Il2CppMethod?.VirtualAddress?.Start ?? 0x0;
        public List<UnitorMethod> MethodCalls { get; } = new List<UnitorMethod>();
        public List<UnitorMethod> References
        {
            get
            {
                if (Owner.MethodReferences.TryGetValue(this, out List<UnitorMethod> references))
                {
                    return references;
                }
                else
                {
                    return new List<UnitorMethod>();
                }
            }
        }

        public List<KeyValuePair<ulong, string>> Strings { get; } = new List<KeyValuePair<ulong, string>>();
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
        public void Analyse()
        {
            return;
            //if (IsEmpty)
            //{
            //    return;
            //}
            //if (Il2CppMethod != null)
            //{
            //    if (!Il2CppMethod.VirtualAddress.HasValue)
            //    {
            //        return;
            //    }

            //    X86DisassembleMode mode = Owner.AppModel.Image.Arch == "x64" ? X86DisassembleMode.Bit64 : X86DisassembleMode.Bit32;
            //    CapstoneX86Disassembler disassembler = CapstoneDisassembler.CreateX86Disassembler(mode);
            //    disassembler.EnableInstructionDetails = true;

            //    var asm = disassembler.Disassemble(Il2CppMethod.GetMethodBody(), (long)Il2CppMethod.VirtualAddress.Value.Start);
            //    foreach (X86Instruction ins in asm)
            //    {
            //        if (Dissasembler.ShouldCheckInstruction(ins.Id))
            //        {
            //            UnitorMethod m = Dissasembler.GetMethodFromInstruction(ins, Owner);
            //            if (m != null)
            //            {
            //                MethodCalls.Add(m);
            //                Owner.MethodReferences.AddOrUpdate(m, new List<UnitorMethod>(), (key, references) => { references.Add(this); return references; });
            //            }
            //            var s = Dissasembler.GetStringFromInstruction(ins, Owner.StringTable);
            //            if (!string.IsNullOrEmpty(s.Item2))
            //            {
            //                Strings.Add(new KeyValuePair<ulong, string>(s.Item1, s.Item2));
            //            }
            //        }
            //    }
            //    disassembler.Dispose();
            //}
            //else
            //{
            //    if (!MonoMethod.HasBody)
            //    {
            //        return;
            //    }

            //    foreach (Instruction ins in MonoMethod.Body.Instructions)
            //    {
            //        if ((ins.OpCode.Code == Code.Call || ins.OpCode.Code == Code.Calli || ins.OpCode.Code == Code.Callvirt) && ins.Operand is MethodDef calledMethod)
            //        {
            //            if (Owner.MonoTypeMatches.TryGetValue(calledMethod.DeclaringType, out UnitorType type))
            //            {
            //                if (type.Methods == null)
            //                {
            //                    continue;
            //                }
            //                UnitorMethod method = type.Methods.FirstOrDefault(m => calledMethod.Name == m.Name);
            //                MethodCalls.Add(method);
            //                Owner.MethodReferences.AddOrUpdate(method, new List<UnitorMethod>(), (key, references) => { references.Add(this); return references; });
            //            }

            //        }
            //        if (ins.OpCode.Code == Code.Ldstr && ins.Operand is string s)
            //        {
            //            Strings.Add(new KeyValuePair<ulong, string>((ulong)(MonoMethod.RVA + ins.Offset), s));
            //        }
            //    }
            //}
        }

        public override string ToString() => MethodDecl;
    }
}
