using System;
using System.Collections.Generic;

namespace RESTar.Internal
{
    internal class NoCaseComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y) => string.Equals(x, y, StringComparison.CurrentCultureIgnoreCase);
        public int GetHashCode(string obj) => obj.ToLower().GetHashCode();
    }
}