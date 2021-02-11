using Beebyte_Deobfuscator.Lookup;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Gee.External.Capstone;
using Gee.External.Capstone.X86;
using Il2CppInspector.Model;
using Il2CppInspector.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Unitor.Core
{
    public static class Extensions
    {
        public static IEnumerable<LookupMethod> GetCalls(this LookupMethod method, AppModel appModel)
        {
            if (method.IsEmpty)
            {
                yield break;
            }
            if (method.Il2CppMethod != null)
            {
                IEnumerable<MethodBase> methods = method.Il2CppMethod.GetCalls(appModel);
                foreach (MethodBase methodCall in methods)
                {
                    if (method.Owner.ProcessedIl2CppTypes.Contains(methodCall.DeclaringType))
                    {
                        LookupType type = method.Owner.Il2CppTypeMatches[methodCall.DeclaringType];
                        if (type.Methods == null)
                        {
                            continue;
                        }
                        LookupMethod methodCallMatch = type.Methods.FirstOrDefault(m => m.Name == methodCall.Name);
                        if (methodCallMatch != null)
                        {
                            yield return methodCallMatch;
                        }
                    }
                }
            }
            else
            {
                IEnumerable<MethodDef> methods = method.MonoMethod.GetCalls();
                foreach (MethodDef methodCall in methods)
                {
                    if (method.Owner.ProcessedMonoTypes.Contains(methodCall.DeclaringType))
                    {
                        LookupType type = method.Owner.MonoTypeMatches[methodCall.DeclaringType];
                        if (type.Methods == null)
                        {
                            continue;
                        }
                        LookupMethod methodCallMatch = type.Methods.FirstOrDefault(m => m.Name == methodCall.Name);
                        if (methodCallMatch != null)
                        {
                            yield return methodCallMatch;
                        }
                    }
                }
            }
        }
        public static IEnumerable<MethodBase> GetCalls(this MethodInfo method, AppModel model)
        {
            if (!method.VirtualAddress.HasValue)
            {
                yield break;
            }
            
            X86DisassembleMode mode = model.Image.Arch == "x64" ? X86DisassembleMode.Bit64 : X86DisassembleMode.Bit32;
            CapstoneX86Disassembler disassembler = CapstoneDisassembler.CreateX86Disassembler(mode);

            // disassembler.EnableInstructionDetails = true; set when Using GetMethodFromInstruction2

            AddressMap map = model.GetAddressMap();

            var asm = disassembler.Disassemble(method.GetMethodBody(), (long)method.VirtualAddress.Value.Start);
            foreach (X86Instruction ins in asm)
            {
                if (Dissasembler.ShoudCheckForMethods(ins.Id))
                {
                    // GetMethodFromInstruction2 is cleaner but GetMethodFromInstruction is faster
                    MethodBase m = Dissasembler.GetMethodFromInstruction(ins, map);
                    if (m != null)
                    {
                        yield return m;
                    }
                }
            }
            disassembler.Dispose();
        }

        public static IEnumerable<MethodDef> GetCalls(this MethodDef method)
        {
            if (method.Body == null)
            {
                yield break;
            }

            foreach (Instruction ins in method.Body.Instructions)
            {
                if ((ins.OpCode.Code == Code.Call || ins.OpCode.Code == Code.Calli || ins.OpCode.Code == Code.Callvirt) && ins.Operand is MethodDef m)
                {
                    yield return m;
                }
            }
        }
    }
}
