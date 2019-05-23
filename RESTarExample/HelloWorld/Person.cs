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
        public decimal Amount { get; set; }
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
            .Sum(e => e.Amount);

        public string NewExpenseTrigger
        {
            get => "0";
            set => Db.TransactAsync(() => new Expense {Spender = this});
        }

        public string DeleteAllTrigger
        {
            get => "0";
            set => Db.TransactAsync(() => Db.SQL("DELETE FROM Expense WHERE Spender = ?", this));
        }
    }
}