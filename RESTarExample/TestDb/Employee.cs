﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using RESTar;
using Starcounter;

namespace RESTarExample.TestDb
{
    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Employee : TestBase
    {
        public string Name;

        [ObjectRef, DataMember(Name = "Details")]
        public ulong? DetailsObjectNo;

        [ObjectRef, DataMember(Name = "Boss")]
        public ulong? BossObjectNo;

        [ObjectRef, DataMember(Name = "Company")]
        public ulong? CompanyObjectNo;
   
        [IgnoreDataMember]
        public EmployeeDetails Details
        {
            get { return DetailsObjectNo.GetReference<EmployeeDetails>(); }
            set { DetailsObjectNo = value.GetObjectNo(); }
        }

        [IgnoreDataMember]
        public Employee Boss
        {
            get { return BossObjectNo.GetReference<Employee>(); }
            set { BossObjectNo = value.GetObjectNo(); }
        }

        [IgnoreDataMember]
        public Company Company
        {
            get { return CompanyObjectNo.GetReference<Company>(); }
            set { CompanyObjectNo = value.GetObjectNo(); }
        }

        [IgnoreDataMember]
        public IEnumerable<Employee> Subordinates
        {
            get { return Db.SQL<Employee>($"SELECT t FROM {GetType().FullName} t WHERE t.Boss =?", this); }
            set { }
        }
    }
}
