using RESTar.Resources;
using Starcounter;

namespace HelloWorld
{
    [Database]
    public class Expense
    {
        public Person Spender { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Html => "/HelloWorld/ExpenseJson.html";
    }
}