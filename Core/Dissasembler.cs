using Beebyte_Deobfuscator.Lookup;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Gee.External.Capstone;
using Gee.External.Capstone.X86;
using Il2CppInspector.Model;
using Il2CppInspector.Reflection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Unitor.Core
{
    public static class Dissasembler
    {
        public static string DissasembleIl2CppMethod(MethodInfo method, LookupModule module)
        {
            StringBuilder output = new StringBuilder();

            if (!method.VirtualAddress.HasValue)
            {
                return "";
            }
            X86DisassembleMode mode = module.AppModel.Image.Arch == "x64" ? X86DisassembleMode.Bit64 : X86DisassembleMode.Bit32;
            CapstoneX86Disassembler disassembler = CapstoneDisassembler.CreateX86Disassembler(mode);
            disassembler.EnableInstructionDetails = true;

            AddressMap map = module.AppModel.GetAddressMap();

            var asm = disassembler.Disassemble(method.GetMethodBody(), (long)method.VirtualAddress.Value.Start);

            foreach (X86Instruction ins in asm)
            {
                if (ShoudCheckInstuctions(ins.Id))
                {
                    output.AppendLine(ins.Mnemonic + " " + ins.Operand + " " + GetMethodFromInstruction2(ins, map));
                }
                else
                {
                    output.AppendLine(" " + ins.Mnemonic + " " + ins.Operand);
                }
            }
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
        public static string DissasembleMethod(LookupMethod method, LookupModule module)
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
        public static bool ShoudCheckInstuctions(X86InstructionId id)
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
                X86InstructionId.X86_INS_JS
            }.Contains(id);
        }
        public static MethodBase GetMethodFromInstruction2(X86Instruction ins, AddressMap map)
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
            ulong address = 0x0;
            if (operands[0].Type == X86OperandType.Immediate)
            {
                address = (ulong)operands[0].Immediate;
            }
            else if (operands[0].Type == X86OperandType.Memory)
            {
                if (operands[0].Memory.Base.Id == X86RegisterId.X86_REG_RIP)
                {
                    address = (ulong)(ins.Address + operands[0].Memory.Displacement);
                }
            }
            if (map.TryGetValue(address, out object content))
            {
                if (content is AppMethod appMethod)
                {
                    return appMethod.Method;
                }
            }
            return null;
        }
        public static MethodBase GetMethodFromInstruction(X86Instruction ins, AddressMap map)
        {
            if (!ins.Operand.Contains("0x") || Regex.IsMatch(ins.Operand, @"dword ptr ([a-z]{1}s:)?\[[a-z0-9]{3} ?[\+\-&\*/\^\?]"))
            {
                return null;
            }

            ulong address = 0x0;
            if (Regex.IsMatch(ins.Operand, @"qword ptr \[rip ?[\+\-&\*/\^\?]"))
            {
                ulong rip = (ulong)ins.Address;
                Match match = Regex.Match(ins.Operand, @"(?<Operator>[\+\-&\*/\^\?]) (?<Address>0x[a-z0-9]+)");
                if (match.Groups.TryGetValue("Operator", out Group op) && match.Groups.TryGetValue("Address", out Group add))
                {
                    address = (op.ToString()) switch
                    {
                        "+" => rip + Convert.ToUInt64(add.ToString(), 16),
                        "-" => rip - Convert.ToUInt64(add.ToString(), 16),
                        _ => Convert.ToUInt64(add.ToString(), 16),
                    };
                }
            }
            else if (Regex.IsMatch(ins.Operand, @"qword ptr ([a-z]{1}s:)?\[[a-z0-9]{1,3} ?[\+\-&\*/\^\?]"))
            {
                return null;
            }
            else if (Regex.IsMatch(ins.Operand, @"\[0x[a-z0-9]{1,}\]"))
            {
                address = Convert.ToUInt64(Regex.Match(ins.Operand, @"(?!\[)[a-z0-9]{1,}(?=])").Value, 16);
            }
            else
            {
                address = Convert.ToUInt64(ins.Operand, 16);
            }
            if (map.TryGetValue(address, out object content))
            {
                if (content is AppMethod appMethod)
                {
                    return appMethod.Method;
                }
            }
            return null;
        }
    }
}
