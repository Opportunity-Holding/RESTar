using RESTar.Resources;
using Starcounter;

#pragma warning disable 1591
// ReSharper disable All

namespace RESTarExample.HelloWorld
{
    [Database, RESTar]
    public class Expense
    {
        public Person Spender { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}