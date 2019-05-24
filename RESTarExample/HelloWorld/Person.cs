using System.Collections.Generic;
using System.Linq;
using RESTar.Resources;
using Starcounter;

// ReSharper disable All
#pragma warning disable 1591

namespace HelloWorld
{
    [Database]
    public class Expense
    {
        private const string html = "/HelloWorld/Expense.html";
        internal const string SelectBySpender = "SELECT t FROM Expense WHERE e.Spender = ?";
        internal const string DeleteBySpender = "DELETE FROM Expense WHERE e.Spender = ?";

        [RESTarMember(hide: true)] public Person Spender { get; set; }
        public string Description { get; set; }
        [RESTarMember(hide: true)] public decimal AmountValue { get; set; }

        public string Amount
        {
            get => AmountValue.ToString("N");
            set => AmountValue = decimal.Parse(value);
        }

        public string Html => html;
    }

    [Database, RESTar]
    public class Person
    {
        private const string html = "/HelloWorld/Person.html";

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public IEnumerable<Expense> Expenses => Db.SQL<Expense>(Expense.SelectBySpender, this);
        public decimal CurrentBalance => Db.SQL<Expense>(Expense.SelectBySpender, this).Sum(e => e.AmountValue);

        public string NewExpenseTrigger
        {
            get => "0";
            set => new Expense {Spender = this};
        }

        public string DeleteAllTrigger
        {
            get => "0";
            set => Db.SQL(Expense.DeleteBySpender, this);
        }

        public string Html => html;
    }
}