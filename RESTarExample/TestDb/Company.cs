﻿using System.Collections.Generic;
using RESTar.Resources;
using Starcounter;

#pragma warning disable 1591

namespace RESTarExample.TestDb
{
    [Database, RESTar]
    public class Company : TestBase
    {
        public string Name;
        public Employee CEO;

        [RESTarMember(ignore: true)]
        public IEnumerable<Employee> Employees
        {
            get { return Db.SQL<Employee>($"SELECT t FROM {typeof(Employee)} t WHERE t.Company =?", this); }
            set { }
        }
    }
}