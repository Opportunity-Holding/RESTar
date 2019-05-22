using Starcounter;

namespace HelloWorld
{
    partial class PersonJson : Json
    {
        public string FullName => $"{FirstName} {LastName}";

        static PersonJson()
        {
            DefaultTemplate.Expenses.ElementType.InstanceType = typeof(ExpenseJson);
        }

        void Handle(Input.SaveTrigger action)
        {
            Transaction.Commit();
        }

        void Handle(Input.CancelTrigger action)
        {
            Transaction.Rollback();
        }

        void Handle(Input.NewExpenseTrigger action)
        {
            new Expense()
            {
                Spender = this.Data as Person,
                Amount = 1
            };
        }

        void Handle(Input.DeleteAllTrigger action)
        {
            Db.SQL("DELETE FROM Expense WHERE Spender = ?", this.Data);
            this.Expenses.Clear();
        }
    }
}