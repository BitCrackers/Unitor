using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Gee.External.Capstone;
using Gee.External.Capstone.X86;
using Il2CppInspector.Model;
using Il2CppInspector.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unitor.Core.Reflection;

namespace Unitor.Core
{
    public static class Dissasembler
    {
        public static string DissasembleIl2CppMethod(MethodInfo method, UnitorModel module)
        {
            StringBuilder output = new StringBuilder();

            if (!method.VirtualAddress.HasValue)
            {
                return "";
            }
            X86DisassembleMode mode = module.AppModel.Image.Arch == "x64" ? X86DisassembleMode.Bit64 : X86DisassembleMode.Bit32;
            CapstoneX86Disassembler disassembler = CapstoneDisassembler.CreateX86Disassembler(mode);
            disassembler.EnableInstructionDetails = true;

            Dictionary<ulong, string> stringTable = module.AppModel.Strings;

            var asm = disassembler.Disassemble(method.GetMethodBody(), (long)method.VirtualAddress.Value.Start);

            foreach (X86Instruction ins in asm)
            {
                if (ShouldCheckInstruction(ins.Id))
                {
                    output.AppendLine(ins.Mnemonic + " " + ins.Operand + " " + GetTooltipFromInstruction(method, ins, module));
                }
                else
                {
                    output.AppendLine(ins.Mnemonic + " " + ins.Operand);
                }
            }
            disassembler.Dispose();
            return output.ToString();
        }

        public static string DissasembleMonoMethod(MethodDef method)
        {
            if (!method.Body.HasInstructions)
            {
                return "";
            }
            StringBuilder output = new StringBuilder();


            foreach (Instruction ins in method.Body.Instructions)
            {
                output.AppendLine(GetInsString(ins));
            }
            return output.ToString();
        }

        static string GetInsString(Instruction instruction)
        {
            StringBuilder output = new StringBuilder();
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
                            output.Append(", ");
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

        public static bool ShouldCheckInstruction(X86InstructionId id)
        {
            return new List<X86InstructionId>() {
                X86InstructionId.X86_INS_CALL,
                X86InstructionId.X86_INS_JAE,
                X86InstructionId.X86_INS_JA,
                X86InstructionId.X86_INS_JBE,
                X86InstructionId.X86_INS_JB,
                X86InstructionId.X86_INS_JCXZ,
                X86InstructionId.X86_INS_JECXZ,
                X86InstructionId.X86_INS_JE,
                X86InstructionId.X86_INS_JGE,
                X86InstructionId.X86_INS_JG,
                X86InstructionId.X86_INS_JL,
                X86InstructionId.X86_INS_JMP,
                X86InstructionId.X86_INS_JNO,
                X86InstructionId.X86_INS_JNP,
                X86InstructionId.X86_INS_JNS,
                X86InstructionId.X86_INS_JO,
                X86InstructionId.X86_INS_JP,
                X86InstructionId.X86_INS_JRCXZ,
                X86InstructionId.X86_INS_JS,
                X86InstructionId.X86_INS_PUSH,
                X86InstructionId.X86_INS_PUSHAL,
                X86InstructionId.X86_INS_PUSHAW,
                X86InstructionId.X86_INS_PUSHF,
                X86InstructionId.X86_INS_PUSHFD,
                X86InstructionId.X86_INS_PUSHFQ,
                X86InstructionId.X86_INS_MOV,
            }.Contains(id);
        }

        public static (ulong, string) GetStringFromInstruction(X86Instruction ins, Dictionary<ulong, string> stringTable)
        {
            if (!ins.HasDetails)
            {
                return (0x0, null);
            }
            X86Operand[] operands = ins.Details.Operands;
            if (operands.Length == 0)
            {
                return (0x0, null);
            }
            ulong address = GetAdressFromOperand(ins, operands[0]);
            if (address == 0x0)
            {
                return (0x0, null);
            }
            if (stringTable.TryGetValue(address, out string s))
            {
                return (address, s);
            }
            return (0x0, null);
        }

        public static ParameterInfo GetParameterInfo(X86Instruction ins, MethodInfo method)
        {
            if (!ins.HasDetails)
            {
                return null;
            }
            X86Operand[] operands = ins.Details.Operands;
            if (operands.Length == 0)
            {
                return null;
            }
            if (operands[0].Type == X86OperandType.Memory)
            {
                if (operands[0].Memory.Base == null)
                {
                    return null;
                }
                if (operands[0].Memory.Base.Id == X86RegisterId.X86_REG_EBP || operands[0].Memory.Base.Id == X86RegisterId.X86_REG_RBP)
                {
                    int paramIndex = (int)((operands[0].Memory.Displacement - 8) / 4) - 1;
                    if (paramIndex < 0)
                    {
                        return null;
                    }
                    if (method.DeclaredParameters.Count >= paramIndex)
                    {
                        return method.DeclaredParameters[paramIndex];
                    }
                }
            }
            return null;
        }

        public static UnitorType GetTypeLoaded(X86Instruction ins, UnitorModel model)
        {
            if (!ins.HasDetails)
            {
                return null;
            }
            X86Operand[] operands = ins.Details.Operands;
            if (operands.Length != 2)
            {
                return null;
            }
            X86Operand register = operands[0];
            X86Operand operand = operands[1];

            ulong address = GetAdressFromOperand(ins, operand);
            if (address == 0x0)
            {
                return null;
            }
            return model.Types.FirstOrDefault(t => t.TypeClassAddress == address);
        }

        public static string GetTooltipFromInstruction(MethodInfo method, X86Instruction ins, UnitorModel model)
        {
            return GetMethodFromInstruction(ins, model) +
                GetStringFromInstruction(ins, model.StringTable).Item2 +
                GetParameterInfo(ins, method) +
                GetTypeLoaded(ins, model)
                ;
        }

        public static UnitorMethod GetMethodFromInstruction(X86Instruction ins, UnitorModel model)
        {
            if (!ins.HasDetails)
            {
                return null;
            }
            X86Operand[] operands = ins.Details.Operands;
            if (operands.Length == 0)
            {
                return null;
            }
            ulong address = GetAdressFromOperand(ins, operands[0]);
            if (address == 0x0)
            {
                return null;
            }
            if (model.AppModel.GetAddressMap().TryGetValue(address, out object content))
            {
                if (content is AppMethod appMethod)
                {
                    if (model.Il2CppTypeMatches.TryGetValue(appMethod.Method.DeclaringType, out UnitorType type))
                    {
                        if (type.Methods == null)
                        {
                            return null;
                        }
                        return model.Il2CppTypeMatches[appMethod.Method.DeclaringType].Methods.FirstOrDefault(m => m.Name == appMethod.Method.Name);
                    }
                }
            }
            return null;
        }

        public static ulong GetAdressFromOperand(X86Instruction ins, X86Operand op)
        {
            ulong address = 0x0;
            if (op.Type == X86OperandType.Immediate)
            {
                address = (ulong)op.Immediate;
            }
            else if (op.Type == X86OperandType.Memory)
            {
                if (op.Memory.Base == null)
                {
                    address = (ulong)op.Memory.Displacement;
                }
                else if (op.Memory.Base.Id == X86RegisterId.X86_REG_RIP)
                {
                    address = (ulong)(ins.Address + op.Memory.Displacement);
                }
            }
            return address;
        }
    }
}
