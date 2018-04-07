﻿using System;
using RESTar;
using Starcounter;

// ReSharper disable All

namespace TestProject
{
    public class Program
    {
        public static void Main()
        {
            RESTarConfig.Init();
        }

        public interface IDbCustomerInterface
        {
            string myname { get; set; }
            DateTime datetime { get; set; }
        }

        [Database, RESTar(Interface = typeof(IDbCustomerInterface))]
        public class DbCustomer : IDbCustomerInterface
        {
            public string Name { get; set; }
            public int MyInt { get; set; }
            public DateTime MyDateTime { get; set; }

            string IDbCustomerInterface.myname
            {
                get => Name;
                set => Name = value;
            }

            DateTime IDbCustomerInterface.datetime
            {
                get => MyDateTime;
                set => MyDateTime = value;
            }
        }

        [Database]
        public class DbOrder
        {
            public string Name { get; set; }
            public int MyInt { get; set; }
            public DateTime MyDateTime { get; set; }
        }
    }
}