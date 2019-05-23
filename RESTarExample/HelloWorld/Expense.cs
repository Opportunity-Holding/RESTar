using RESTar.Resources;
using Starcounter;

// ReSharper disable All
#pragma warning disable 1591

namespace HelloWorld
{
    [Database]
    public class Expense
    {
        [RESTarMember(hide: true)] public Person Spender { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Html => "/HelloWorld/ExpenseJson.html";
    }
}