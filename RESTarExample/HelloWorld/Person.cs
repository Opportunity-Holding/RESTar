using System.Collections.Generic;
using System.Linq;
using RESTar;
using RESTar.Resources;
using Starcounter;

namespace HelloWorld
{
    [Database, RESTar]
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Html => "/HelloWorld/PersonJson.html";

        [RESTarMember(hide: true)] public IEnumerable<Expense> Expenses =>
            Db.SQL<Expense>("SELECT e FROM Expense e WHERE e.Spender = ?", this);

        public decimal CurrentBalance =>
            Db.SQL<Expense>("SELECT e FROM Expense e WHERE e.Spender = ?", this).Sum(e => e.Amount);
    }
}