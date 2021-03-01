using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Iced.Intel;
using Il2CppInspector.Model;
using Il2CppInspector.Reflection;
using System.Collections.Generic;
using System.Linq;
using Unitor.Core.Reflection;

namespace Unitor.Core
{
    public static class Dissasembler
    {
        sealed class SymbolResolver : ISymbolResolver
        {
            readonly Dictionary<ulong, string> symbolDict;

            public SymbolResolver(UnitorModel model)
            {
                this.symbolDict = model.SymbolDict;
            }

            public bool TryGetSymbol(in Iced.Intel.Instruction instruction, int operand, int instructionOperand, ulong address, int addressSize, out SymbolResult symbol)
            {
                if (symbolDict.TryGetValue(address, out var symbolText))
                {
                    // The 'address' arg is the address of the symbol and doesn't have to be identical
                    // to the 'address' arg passed to TryGetSymbol(). If it's different from the input
                    // address, the formatter will add +N or -N, eg. '[rax+symbol+123]'
                    symbol = new SymbolResult(address, symbolText);
                    return true;
                }
                symbol = default;
                return false;
            }
        }
        public static string DissasembleIl2CppMethod(MethodInfo method, UnitorModel module)
        {
            var output = new StringOutput();

            if (!method.VirtualAddress.HasValue)
            {
                return "";
            }
            int bitness = module.AppModel.Image.Arch == "x64" ? 64 : 32;
            byte[] methodBody = method.GetMethodBody();

            var codeReader = new ByteArrayCodeReader(methodBody);
            var decoder = Decoder.Create(bitness, codeReader);
            decoder.IP = method.VirtualAddress.Value.Start;
            ulong endRip = decoder.IP + (uint)methodBody.Length;

            var instructions = new List<Iced.Intel.Instruction>();
            while (decoder.IP < endRip)
                instructions.Add(decoder.Decode());

            var symbolResolver = new SymbolResolver(module);
            var formatter = new IntelFormatter(symbolResolver);
            foreach(var ins in instructions)
            {
                formatter.Format(ins, output);
            }
            return output.ToString();
        }

        public static string DissasembleMonoMethod(MethodDef method)
        {
            if (!method.Body.HasInstructions)
            {
                return "";
            }
            System.Text.StringBuilder output = new System.Text.StringBuilder();


            foreach (dnlib.DotNet.Emit.Instruction ins in method.Body.Instructions)
            {
                output.AppendLine(GetInsString(ins));
            }
            return output.ToString();
        }

        static string GetInsString(dnlib.DotNet.Emit.Instruction instruction)
        {
            System.Text.StringBuilder output = new System.Text.StringBuilder();
            output.Append(instruction.OpCode.Name + " ");

            switch (instruction.OpCode.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.InlineSwitch:
                    var branches = instruction.Operand as int[];
                    for (int i = 0; i < branches.Length; i++)
                    {
                        if (i > 0)
                        {
                            output.Append(", ");
                        }
                        output.Append(branches[i]);
                    }
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    output.Append(instruction.Operand);
                    break;
                case OperandType.InlineString:
                    output.Append(string.Format("\"{0}\"", instruction.Operand));
                    break;
                default:
                    output.Append(instruction.Operand);
                    break;
            }

            return output.ToString();
        }

        public static string DissasembleMethod(UnitorMethod method, UnitorModel module)
        {
            if (method.IsEmpty)
            {
                return "";
            }

            if (method.Il2CppMethod != null)
            {
                return DissasembleIl2CppMethod(method.Il2CppMethod, module);
            }
            else
            {
                return DissasembleMonoMethod(method.MonoMethod);
            }

        }

        //public static (ulong, string) GetStringFromInstruction(Iced.Intel.Instruction ins, Dictionary<ulong, string> stringTable)
        //{
  
        //    ulong address = GetAdressFromOperand(ins, operands[0]);
        //    if (address == 0x0)
        //    {
        //        return (0x0, null);
        //    }
        //    if (stringTable.TryGetValue(address, out string s))
        //    {
        //        return (address, s);
        //    }
        //    return (0x0, null);
        //}

        //public static ParameterInfo GetParameterInfo(X86Instruction ins, MethodInfo method)
        //{
        //    if (!ins.HasDetails)
        //    {
        //        return null;
        //    }
        //    X86Operand[] operands = ins.Details.Operands;
        //    if (operands.Length == 0)
        //    {
        //        return null;
        //    }
        //    if (operands[0].Type == X86OperandType.Memory)
        //    {
        //        if (operands[0].Memory.Base == null)
        //        {
        //            return null;
        //        }
        //        if (operands[0].Memory.Base.Id == X86RegisterId.X86_REG_EBP || operands[0].Memory.Base.Id == X86RegisterId.X86_REG_RBP)
        //        {
        //            int paramIndex = (int)((operands[0].Memory.Displacement - 8) / 4) - 1;
        //            if (paramIndex < 0)
        //            {
        //                return null;
        //            }
        //            if (method.DeclaredParameters.Count > paramIndex)
        //            {
        //                return method.DeclaredParameters[paramIndex];
        //            }
        //        }
        //    }
        //    return null;
        //}

        //public static UnitorType GetTypeLoaded(X86Instruction ins, UnitorModel model)
        //{
        //    if (!ins.HasDetails)
        //    {
        //        return null;
        //    }
        //    X86Operand[] operands = ins.Details.Operands;
        //    if (operands.Length != 2)
        //    {
        //        return null;
        //    }
        //    X86Operand register = operands[0]; // Future use for register type/method storage
        //    X86Operand operand = operands[1];

        //    ulong address = GetAdressFromOperand(ins, operand);
        //    if (address == 0x0)
        //    {
        //        return null;
        //    }
        //    return model.Types.FirstOrDefault(t => t.TypeClassAddress == address);
        //}

        //public static string GetTooltipFromInstruction(MethodInfo method, X86Instruction ins, UnitorModel model)
        //{
        //    return GetMethodFromInstruction(ins, model) +
        //        GetStringFromInstruction(ins, model.StringTable).Item2 +
        //        GetParameterInfo(ins, method) +
        //        GetTypeLoaded(ins, model)
        //        ;
        //}

        //public static UnitorMethod GetMethodFromInstruction(X86Instruction ins, UnitorModel model)
        //{
        //    if (!ins.HasDetails)
        //    {
        //        return null;
        //    }
        //    X86Operand[] operands = ins.Details.Operands;
        //    if (operands.Length == 0)
        //    {
        //        return null;
        //    }
        //    ulong address = GetAdressFromOperand(ins, operands[0]);
        //    if (address == 0x0)
        //    {
        //        return null;
        //    }
        //    if (model.AppModel.GetAddressMap().TryGetValue(address, out object content))
        //    {
        //        if (content is AppMethod appMethod)
        //        {
        //            if (model.Il2CppTypeMatches.TryGetValue(appMethod.Method.DeclaringType, out UnitorType type))
        //            {
        //                if (type.Methods == null)
        //                {
        //                    return null;
        //                }
        //                return model.Il2CppTypeMatches[appMethod.Method.DeclaringType].Methods.FirstOrDefault(m => m.Name == appMethod.Method.Name);
        //            }
        //        }
        //    }
        //    return null;
        //}

        //public static ulong GetAdressFromOperand(Iced.Intel.Instruction ins)
        //{
        //    ulong address = 0x0;
        //    if (ins.Op0Kind == OpKind.Imm)
        //    {
        //        address = (ulong)op.Immediate;
        //    }
        //    else if (op.Type == X86OperandType.Memory)
        //    {
        //        if (op.Memory.Base == null)
        //        {
        //            address = (ulong)op.Memory.Displacement;
        //        }
        //        else if (op.Memory.Base.Id == X86RegisterId.X86_REG_RIP)
        //        {
        //            address = (ulong)(ins.Address + op.Memory.Displacement);
        //        }
        //    }
        //    return address;
        //}
    }
}
