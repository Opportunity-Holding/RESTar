//
// ILPattern.cs
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

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RESTar.Meta.IL
{
    internal abstract class ILPattern
    {
        internal static ILPattern Optional(OpCode opcode) => Optional(OpCode(opcode));
        internal static ILPattern Optional(params OpCode[] opcodes) => Optional(Sequence(opcodes.Select(opcode => OpCode(opcode)).ToArray()));
        internal static ILPattern Optional(ILPattern pattern) => new OptionalPattern(pattern);

        private class OptionalPattern : ILPattern
        {
            private readonly ILPattern pattern;
            public OptionalPattern(ILPattern optional) => pattern = optional;
            public override void Match(MatchContext context) => pattern.TryMatch(context);
        }

        internal static ILPattern Sequence(params ILPattern[] patterns) => new SequencePattern(patterns);

        private class SequencePattern : ILPattern
        {
            private readonly ILPattern[] patterns;
            public SequencePattern(ILPattern[] patterns) => this.patterns = patterns;

            public override void Match(MatchContext context)
            {
                foreach (var pattern in patterns)
                {
                    pattern.Match(context);
                    if (!context.success)
                        break;
                }
            }
        }

        public static ILPattern OpCode(OpCode opcode) => new OpCodePattern(opcode);

        private class OpCodePattern : ILPattern
        {
            private readonly OpCode opcode;
            public OpCodePattern(OpCode opcode) => this.opcode = opcode;

            public override void Match(MatchContext context)
            {
                if (context.Instruction == null)
                {
                    context.success = false;
                    return;
                }
                context.success = context.Instruction.OpCode == opcode;
                context.Advance();
            }
        }

        public static ILPattern Either(ILPattern a, ILPattern b) => new EitherPattern(a, b);

        private class EitherPattern : ILPattern
        {
            private readonly ILPattern a;
            private readonly ILPattern b;

            public EitherPattern(ILPattern a, ILPattern b)
            {
                this.a = a;
                this.b = b;
            }

            public override void Match(MatchContext context)
            {
                if (!a.TryMatch(context))
                    b.Match(context);
            }
        }

        public abstract void Match(MatchContext context);
        protected static Instruction GetLastMatchingInstruction(MatchContext context) => context.Instruction?.Previous;

        public bool TryMatch(MatchContext context)
        {
            var instruction = context.Instruction;
            Match(context);
            if (context.success)
                return true;
            context.Reset(instruction);
            return false;
        }

        public static MatchContext Match(MethodBase method, ILPattern pattern)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));
            var instructions = method.GetInstructions();
            if (instructions.Count == 0)
                throw new ArgumentException();
            var context = new MatchContext(instructions[0]);
            pattern.Match(context);
            return context;
        }
    }
}