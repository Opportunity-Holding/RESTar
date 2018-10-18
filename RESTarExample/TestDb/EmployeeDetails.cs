using System;
using RESTar.Resources;
using Starcounter;

#pragma warning disable 1591

namespace RESTarExample.TestDb
{
    [Database, RESTar]
    public class EmployeeDetails : TestBase
    {
        public int Salary { get; set; }
        public DateTime DateOfEmployment { get; set; }
    }
}