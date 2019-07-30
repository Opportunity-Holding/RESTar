//
// Instruction.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2009 - 2010 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Reflection.Emit;
using System.Text;

namespace RESTar.Meta.IL
{
    internal sealed class Instruction
    {
        public int Offset { get; }
        public OpCode OpCode { get; }
        public object Operand { get; internal set; }
        public Instruction Previous { get; internal set; }
        public Instruction Next { get; internal set; }

        internal Instruction(int offset, OpCode opcode)
        {
            Offset = offset;
            OpCode = opcode;
        }

        public override string ToString()
        {
            var instruction = new StringBuilder();
            AppendLabel(instruction, this);
            instruction.Append(':');
            instruction.Append(' ');
            instruction.Append(OpCode.Name);
            if (Operand == null)
                return instruction.ToString();
            instruction.Append(' ');
            switch (OpCode.OperandType)
            {
                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    AppendLabel(instruction, (Instruction) Operand);
                    break;
                case OperandType.InlineSwitch:
                    var labels = (Instruction[]) Operand;
                    for (var i = 0; i < labels.Length; i++)
                    {
                        if (i > 0)
                            instruction.Append(',');

                        AppendLabel(instruction, labels[i]);
                    }
                    break;
                case OperandType.InlineString:
                    instruction.Append('\"');
                    instruction.Append(Operand);
                    instruction.Append('\"');
                    break;
                default:
                    instruction.Append(Operand);
                    break;
            }
            return instruction.ToString();
        }

        private static void AppendLabel(StringBuilder builder, Instruction instruction)
        {
            builder.Append("IL_");
            builder.Append(instruction.Offset.ToString("x4"));
        }
    }
}