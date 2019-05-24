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
        public string Html => "/HelloWorld/Expense.html";

        [RESTarMember(hide: true)] public Person Spender { get; set; }
        public string Description { get; set; }
        [RESTarMember(hide: true)] public decimal AmountValue { get; set; }

        public string Amount
        {
            get => AmountValue.ToString("N");
            set => AmountValue = decimal.Parse(value);
        }
    }

    [Database, RESTar]
    public class Person
    {
        public string Html => "/HelloWorld/Person.html";

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";

        public IEnumerable<Expense> Expenses => Db
            .SQL<Expense>("SELECT e FROM Expense e WHERE e.Spender = ?", this);

        public decimal CurrentBalance => Db
            .SQL<Expense>("SELECT e FROM Expense e WHERE e.Spender = ?", this)
            .Sum(e => e.AmountValue);

        public string NewExpenseTrigger
        {
            get => "0";
            set => new Expense {Spender = this};
        }

        public string DeleteAllTrigger
        {
            get => "0";
            set => Db.SQL("DELETE FROM Expense WHERE Spender = ?", this);
        }
    }
}