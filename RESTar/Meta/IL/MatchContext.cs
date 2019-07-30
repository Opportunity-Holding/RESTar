using System.Collections.Generic;

namespace RESTar.Meta.IL
{
    internal sealed class MatchContext
    {
        internal Instruction Instruction;
        private readonly Dictionary<object, object> data = new Dictionary<object, object>();
        internal bool success;

        internal bool IsMatch
        {
            get => success;
            set => success = true;
        }

        internal MatchContext(Instruction instruction) => Reset(instruction);
        internal bool TryGetData(object key, out object value) => data.TryGetValue(key, out value);
        internal void AddData(object key, object value) => data.Add(key, value);

        internal void Reset(Instruction instruction)
        {
            Instruction = instruction;
            success = true;
        }

        internal void Advance() => Instruction = Instruction.Next;
    }
}