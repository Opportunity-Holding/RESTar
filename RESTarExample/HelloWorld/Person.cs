using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using RESTar;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using Starcounter;

#pragma warning disable 1591
// ReSharper disable all

namespace RESTarExample.HelloWorld
{
    #region Syntax labs

    public class MemberAccessHandlerArgs<T>
    {
        public IResource Resource { get; }
        public Property Property { get; }
        public T Object { get; }
        public string PropertyName { get; }
        public object NewValue { get; set; }
        public object OldValue { get; }
        public bool IsChange { get; }
        public void Cancel() { }
        public bool IsGet { get; }
        public bool IsSet { get; }
        public bool HasTransaction { get; }
        public Transaction Transaction { get; }
    }

    public delegate void MemberAccessHandler<T>(MemberAccessHandlerArgs<T> args);

    public interface IAccessController<T> : IDynamicMetaObjectProvider where T : class
    {
        IMemberAccessHandler<T> this[string name] { get; }
    }

    public interface IMemberAccessHandler<T> where T : class
    {
        MemberAccessHandler<T> OnAccess { get; set; }
        MemberAccessHandler<T> OnGet { get; set; }
        MemberAccessHandler<T> OnSet { get; set; }
    }

    public interface IAccessHandler<T> where T : class
    {
        void ConfigureAccessHandler(IAccessController<T> controller);
    }

    public class PalindromAttribute : Attribute
    {
        public string HtmlPath { get; }

        public PalindromAttribute(string html)
        {
            HtmlPath = html;
        }
    }

    // Alternatively, using attributes for assigning handlers to members:

    internal class SaveTrigger : HandlerAttribute
    {
        public override void Handle<T>(MemberAccessHandlerArgs<T> args)
        {
            args.Transaction.Commit();
        }
    }

    public abstract class HandlerAttribute : Attribute
    {
        public abstract void Handle<T>(MemberAccessHandlerArgs<T> args);
    }

    #endregion

    // example:




    [RESTar(Method.GET, Method.PATCH)]
    public class Registration : ResourceWrapper<Whatever>, ISelector<Whatever>, IUpdater<Whatever>
    {
        internal string HtmlPath { get; set; } = "/registration.html";

        public IEnumerable<Whatever> Select(IRequest<Whatever> request)
        {
            yield return new Whatever();
        }

        public int Update(IRequest<Whatever> request)
        {
            var c = 0;
            foreach (var item in request.GetInputEntities())
            {
                // this is where we are presented with out populated form(s)


                c += 1;
            }

            return c;
        }
    }

    public class Whatever
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string Password { get; set; }
    }


    [Database, RESTar, Palindrom(html: "/person.html")]
    public class Person : IAccessHandler<Person>
    {
        void IAccessHandler<Person>.ConfigureAccessHandler(IAccessController<Person> members)
        {
            members[nameof(SaveTrigger)].OnSet = TriggerSave;
            members[nameof(FirstName)].OnSet = input =>
            {
                if (input.IsChange && input.NewValue is string stringValue)
                {
                    if (stringValue == "Albert")
                        throw new ArgumentException("Nooo!");
                }
            };
        }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        [SaveTrigger] public long SaveTrigger { get; set; }

        private void TriggerSave(MemberAccessHandlerArgs<Person> input)
        {
            input.Transaction.Commit();
        }

        [RESTarMember(hide: true)] public IEnumerable<Expense> Expenses =>
            Db.SQL<Expense>("SELECT e FROM Expense e WHERE e.Spender = ?", this);

        public decimal CurrentBalance =>
            Db.SQL<Expense>("SELECT e FROM Expense e WHERE e.Spender = ?", this).Sum(e => e.Amount);
    }
}