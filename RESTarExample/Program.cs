using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar;
using RESTar.ContentTypeProviders;
using RESTar.Linq;
using RESTar.Palindrom;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.Resources.Templates;
using Starcounter;

#pragma warning disable 1591
// ReSharper disable All

namespace RESTarExample
{
    public static class Program
    {
        public static void Main()
        {
            RESTarConfig.Init
            (
                uri: "/rest",
                port: 8080,
                // requireApiKey: true,
                allowAllOrigins: false,
                configFilePath: @"C:\Mopedo\mopedo\Mopedo.config",
                contentTypeProviders: new IContentTypeProvider[]
                {
                    new PalindromBootstrapper()
                },
                protocolProviders: new[]
                {
                    new PalindromProtocolProvider()
                }
            );
        }
    }

    internal interface I
    {
        Method Method { get; }
    }

    [RESTar, Database]
    public class EnumTest : I
    {
        public Method Method { get; set; }
        public Method? Method2 { get; set; }
        public int? Int { get; set; }
    }

    [RESTar(AllowDynamicConditions = true), Database]
    public class EnumTest2
    {
        public Method Method { get; set; }
        public Method? Method2 { get; set; }
    }

    [RESTar(Method.GET)]
    public class Test : ISelector<Test>
    {
        public int Value { get; }
        public Test(int value) => Value = value;

        public IEnumerable<Test> Select(IRequest<Test> request)
        {
            for (var i = 1; i <= 10; i += 1)
                yield return new Test(i);
        }
    }


    [RESTar, Database]
    public class Item : IValidator<Item>
    {
        public int I { get; set; }

        public bool IsValid(Item entity, out string invalidReason)
        {
            invalidReason = null;
            return false;
        }
    }

    [RESTar, Database]
    public class MyNotification
    {
        public string Title { get; }
        public string Message { get; }

        [JsonConstructor]
        public MyNotification(string title, string message)
        {
            Title = title;
            Message = message;
            new NotificationEvent(this);
        }
    }

    [RESTar(Description = "Notification!")]
    public class NotificationEvent : Event<MyNotification>
    {
        public NotificationEvent(MyNotification payload) : base(payload) => Raise();
    }

    [Database]
    public class Person
    {
        [RESTarMember(name: "PersonName")] public string Name { get; set; }
        [RESTarMember(ignore: true)] public string PersonName { get; set; }
        [RESTarMember(hide: true)] public string Thing { get; set; }

        [RESTarView]
        public class MyView : ISelector<Person>
        {
            public IEnumerable<Person> Select(IRequest<Person> request)
            {
                return null;
            }
        }
    }

    [Database, RESTar]
    public class Thing2
    {
        public int Inte { get; set; }

        public int Inte2
        {
            get => Inte;
            set => Inte = value;
        }

        public int Int { get; set; }
        public int Int2 => Int;
        public bool T { get; set; }
        public string S { get; set; }
    }

    public class Resource1
    {
        public sbyte Sbyte;
        public byte Byte;
        public short Short;
        public ushort Ushort;
        public int Int;
        public uint Uint;
        public long Long;
        public ulong Ulong;
        public float Float;
        public double Double;
        public decimal Decimal;
        public string String;
        public bool Bool;
        public DateTime DateTime;
        public JObject MyDict;
    }

    [Database, RESTar]
    public class R1
    {
        public int I { get; set; }

        [JsonConstructor]
        private R1(int i)
        {
            I = i;
        }
    }

    //[Database, RESTar]
    //public class R2 : R1
    //{
    //    public string STR;

    //    internal R2(int i) : base(i) { }
    //}

    [Database, RESTar]
    public class DateTimeTest
    {
        [RESTarMember(dateTimeFormat: "d")] public DateTime Short { get; set; }
        [RESTarMember(dateTimeFormat: "O")] public DateTime Iso { get; set; }
        [RESTarMember(dateTimeFormat: "D")] public DateTime Long { get; set; }
        [RESTarMember(dateTimeFormat: "R")] public DateTime RFC { get; set; }

        [RESTarMember(dateTimeFormat: "yyyy-MM")]
        public DateTime? Nullable { get; set; }

        [RESTarMember(dateTimeFormat: "yy-MM-dd-afooboo:HH->mm")]
        public DateTime Special { get; set; }
    }

    [RESTar(Method.GET, Description = description)]
    public class MonthlySpendingReport : ISelector<MonthlySpendingReport>
    {
        private const string description = "Provides an aggregated view of the spending for a given month.";

        /// <summary>
        /// The month for which this report was created
        /// </summary>
        [RESTarMember(dateTimeFormat: "yyyy-MM")]
        public DateTime Month { get; set; }

        /// <summary>
        /// The number of wins during the defined month
        /// </summary>
        public int NrOfWins { get; set; }

        /// <inheritdoc />
        public IEnumerable<MonthlySpendingReport> Select(IRequest<MonthlySpendingReport> request)
        {
            DateTime date;
            try
            {
                date = request.Conditions.Get("month", Operators.EQUALS)?.Value;
            }
            catch
            {
                throw new Exception("Invalid input date");
            }
            return new[]
            {
                new MonthlySpendingReport
                {
                    Month = date,
                    NrOfWins = 123
                }
            };
        }
    }


    [RESTar]
    public class MyBinaryResource : IBinary<MyBinaryResource>
    {
        public (Stream stream, ContentType contentType) Select(IRequest<MyBinaryResource> request)
        {
            var stream = new MemoryStream();
            using (var swr = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                swr.Write("This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data!");
            }
            return (stream, "text/plain");
        }
    }

    [Database]
    public class DbClass
    {
        public string MyString { get; set; }
        public int MyInt { get; set; }
    }

    [RESTar]
    public class DbClassWrapper : ResourceWrapper<DbClass>
    {
        [RESTarView]
        public class MyView : ISelector<DbClass>
        {
            public IEnumerable<DbClass> Select(IRequest<DbClass> request)
            {
                return StarcounterOperations<DbClass>.Select(request);
            }
        }
    }

    [RESTar(Method.POST)]
    public class OtherClass : IInserter<OtherClass>
    {
        public string MyString { get; set; }
        public int MyInt { get; set; }

        public int Insert(IRequest<OtherClass> request)
        {
            var k = 0;
            Db.TransactAsync(() =>
            {
                foreach (var i in request.GetInputEntities())
                {
                    new DbClass
                    {
                        MyInt = i.MyInt,
                        MyString = i.MyString
                    };
                    k += 1;
                }
            });
            return k;
        }
    }

    [RESTar]
    public class MyBinary : IBinary<MyBinary>
    {
        public (Stream stream, ContentType contentType) Select(IRequest<MyBinary> request)
        {
            return (new MemoryStream(Encoding.UTF8.GetBytes("Omg this is aweseom!!!")), "text/plain");
        }
    }

    [RESTar(Method.GET)]
    public class Thing : ISelector<Thing>
    {
        public IEnumerable<Thing> Select(IRequest<Thing> request)
        {
            throw new NotImplementedException();
        }

        [RESTar]
        public class MyOptionsTerminal : OptionsTerminal
        {
            protected override IEnumerable<Option> GetOptions()
            {
                return new[] {new Option("Foo", "a foo", strings => { })};
            }
        }
    }

    [Database, RESTar]
    public class MyStarcounterResource
    {
        public string MyString { get; set; }
        public int MyInteger { get; set; }
        public DateTime MyDateTime { get; set; }
        public MyStarcounterResource MyOtherStarcounterResource { get; set; }
    }

    [RESTar]
    public class MyEntityResource : ISelector<MyEntityResource>, IInserter<MyEntityResource>,
        IUpdater<MyEntityResource>, IDeleter<MyEntityResource>
    {
        public string TheString { get; set; }
        public int TheInteger { get; set; }
        public DateTime TheDateTime { get; set; }
        public MyEntityResource TheOtherEntityResource { get; set; }

        /// <summary>
        /// Private properties are not includeded in output and cannot be set in input. 
        /// This property is only used internally to determine DB object identity.
        /// </summary>
        private ulong? ObjectNo { get; set; }

        private static MyEntityResource FromDbObject(MyStarcounterResource dbObject)
        {
            if (dbObject == null) return null;
            return new MyEntityResource
            {
                TheString = dbObject.MyString,
                TheInteger = dbObject.MyInteger,
                TheDateTime = dbObject.MyDateTime,
                TheOtherEntityResource = FromDbObject(dbObject.MyOtherStarcounterResource),
                ObjectNo = dbObject.GetObjectNo()
            };
        }

        private static MyStarcounterResource ToDbObject(MyEntityResource _object)
        {
            if (_object == null) return null;
            var dbObject = _object.ObjectNo is ulong objectNo
                ? Db.FromId<MyStarcounterResource>(objectNo)
                : new MyStarcounterResource();
            dbObject.MyString = _object.TheString;
            dbObject.MyInteger = _object.TheInteger;
            dbObject.MyDateTime = _object.TheDateTime;
            dbObject.MyOtherStarcounterResource = ToDbObject(_object.TheOtherEntityResource);
            return dbObject;
        }

        public IEnumerable<MyEntityResource> Select(IRequest<MyEntityResource> request) => Db
            .SQL<MyStarcounterResource>($"SELECT t FROM {typeof(MyStarcounterResource).FullName} t")
            .Select(FromDbObject)
            .Where(request.Conditions);

        public int Insert(IRequest<MyEntityResource> request) => Db.Transact(() => request
            .GetInputEntities()
            .Select(ToDbObject)
            .Count());

        public int Update(IRequest<MyEntityResource> request) => Db.Transact(() => request
            .GetInputEntities()
            .Select(ToDbObject)
            .Count());

        public int Delete(IRequest<MyEntityResource> request) => Db.Transact(() =>
        {
            var i = 0;
            foreach (var item in request.GetInputEntities())
            {
                item.Delete();
                i += 1;
            }
            return i;
        });
    }

    #region Stuff

    #region Solution 1

    public class MyStaticConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var myStatic = (MyStatic) value;
            writer.WriteStartObject();
            writer.WritePropertyName("myString");
            writer.WriteValue(myStatic.MyString);
            writer.WritePropertyName("myInt");
            writer.WriteValue(myStatic.MyInt);
            writer.WritePropertyName("myDateTime");
            writer.WriteValue(myStatic.MyDateTime);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override bool CanRead { get; } = false;
        public override bool CanWrite { get; } = true;
        public override bool CanConvert(Type objectType) => objectType == typeof(MyStatic);
    }

    [Database, RESTar, JsonConverter(typeof(MyStaticConverter))]
    public class MyStatic
    {
        public string MyString { get; set; }
        public int MyInt { get; set; }
        public DateTime MyDateTime { get; set; }
    }

    #endregion

    #region Solution 2

    [Database, RESTar]
    public class MyStatic2 : MyStatic2.IVersion1, MyStatic2.IVersion2
    {
        public string MyString { get; set; }
        public int MyInt { get; set; }
        public DateTime MyDateTime { get; set; }

        public bool objo { get; }

        #region Version1 interface

        public interface IVersion1 : IEntityResourceInterface
        {
            string _ICanCallThisWhateverString { get; }
            int __ThisIsMyINt { get; set; }
            DateTime AndTheDateTime_ffs { get; set; }
            bool Objectbo { get; }
        }

        string IVersion1._ICanCallThisWhateverString
        {
            get => MyString;
        }

        int IVersion1.__ThisIsMyINt
        {
            get => MyInt;
            set
            {
                MyDateTime = DateTime.MaxValue;
                MyInt = value;
            }
        }

        DateTime IVersion1.AndTheDateTime_ffs
        {
            get => MyDateTime;
            set => MyDateTime = value;
        }

        public interface IVersion2 : IEntityResourceInterface
        {
            string _ICanCadsallThisWhateverString { get; }
            int __ThiasdasdsIsMyINt { get; set; }
            DateTime AndTadadheDateTime_ffs { get; set; }
            bool Obasdjectbo { get; }
        }

        string IVersion2._ICanCadsallThisWhateverString
        {
            get => MyString;
        }

        int IVersion2.__ThiasdasdsIsMyINt
        {
            get => MyInt;
            set
            {
                MyDateTime = DateTime.MaxValue;
                MyInt = value;
            }
        }

        DateTime IVersion2.AndTadadheDateTime_ffs
        {
            get => MyDateTime;
            set => MyDateTime = value;
        }

        public bool Objectbo => !objo;
        public bool Obasdjectbo => !objo;

        #endregion
    }

    #endregion

    [Database, RESTar]
    public class MyStatic3
    {
        public EE E { get; set; }
        public string Foo { get; set; }
    }

    [RESTar(Method.GET)]
    public class SemiDynamic : JObject, ISelector<SemiDynamic>
    {
        public string InputStr { get; set; } = "Goo";
        public int Int { get; set; } = 100;

        public IEnumerable<SemiDynamic> Select(IRequest<SemiDynamic> request)
        {
            return new[]
            {
                new SemiDynamic
                {
                    ["Str"] = "123",
                    ["Int"] = 0,
                    ["Count"] = -1230
                },
                new SemiDynamic
                {
                    ["Str"] = "ad123",
                    ["Int"] = 14
                },
                new SemiDynamic {["Str"] = "123"}, new SemiDynamic
                {
                    ["Str"] = "1ds23",
                    ["Int"] = 200
                }
            };
        }
    }

    [RESTar(Method.GET)]
    public class SemiDynamic2 : Dictionary<string, object>, ISelector<SemiDynamic2>
    {
        public IEnumerable<SemiDynamic2> Select(IRequest<SemiDynamic2> request)
        {
            return new[]
            {
                new SemiDynamic2
                {
                    ["Str"] = "ad123",
                    ["Int"] = 14
                },
                new SemiDynamic2 {["Str"] = "123"}, new SemiDynamic2
                {
                    ["Str"] = "1ds23",
                    ["Int"] = 200
                }
            };
        }
    }

    [RESTar(Method.GET, AllowDynamicConditions = true)]
    public class AllDynamic : JObject, ISelector<AllDynamic>
    {
        public string Str { get; set; }
        public int Int { get; set; }

        public IEnumerable<AllDynamic> Select(IRequest<AllDynamic> request)
        {
            return new[]
            {
                new AllDynamic
                {
                    ["Str"] = "123",
                    ["Int"] = 120
                },
                new AllDynamic
                {
                    ["Str"] = 232,
                    ["Int"] = 13
                },
                new AllDynamic
                {
                    ["Str"] = 232,
                    ["Int"] = -123
                },
                new AllDynamic
                {
                    ["AStr"] = "ASD",
                    ["Int"] = 5
                }
            };
        }
    }

    [RESTar]
    public class DDictThing : DDictionary, IDDictionary<DDictThing, DDictKeyValuePair>
    {
        public string Str { get; set; }
        public int Int { get; set; }

        protected override object GetDeclaredMemberValue(string key)
        {
            switch (key)
            {
                case nameof(Str): return Str;
                case nameof(Int): return Int;
                default: return null;
            }
        }

        public DDictKeyValuePair NewKeyPair(DDictThing dict, string key, object value = null)
        {
            return new DDictKeyValuePair(dict, key, value);
        }
    }

    public class DDictKeyValuePair : DKeyValuePair
    {
        public DDictKeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    #region Random resources

    [RESTar]
    public class MyThing : ResourceWrapper<Table> { }

    [Database]
    public class Table
    {
        public string STR { get; set; }
        public DateTime? DT { get; set; }
        public DateTime DT2 { get; set; }
    }

    [RESTar(Method.GET)]
    public class MyTestResource : Dictionary<string, dynamic>, ISelector<MyTestResource>
    {
        public IEnumerable<MyTestResource> Select(IRequest<MyTestResource> request)
        {
            return new[]
            {
                new MyTestResource
                {
                    ["T"] = 1,
                    ["G"] = "asd",
                    ["Goo"] = 10
                },
                new MyTestResource
                {
                    ["T"] = 5,
                    ["G"] = "asd"
                },
                new MyTestResource
                {
                    ["T"] = -1,
                    ["G"] = "asd",
                    ["Boo"] = -10,
                    ["ASD"] = 123312
                },
                new MyTestResource
                {
                    ["T"] = 10,
                    ["G"] = "asd",
                    ["Boo"] = -10,
                    ["ASD"] = 123312,
                    ["Count"] = 30
                }
            };
        }
    }

    [Database, RESTar]
    public class MyResource
    {
        public int MyId { get; set; }
        public decimal MyDecimal { get; set; }
        public string MyMember { get; set; }
        public string SomeMember { get; set; }

        [RESTar(Method.GET, Description = "Returns a fine object")]
        public class Get : JObject, ISelector<Get>
        {
            public IEnumerable<Get> Select(IRequest<Get> request) => new[] {new Get {["Soo"] = 123}};
        }
    }


    [Database, RESTar]
    public class MyClass
    {
        public int MyInt { get; set; }

        public int OtherInt { get; set; }

        public MyResource Resource { get; }
    }

    [RESTar]
    public class R : IInserter<R>, ISelector<R>, IUpdater<R>, IDeleter<R>
    {
        public string S { get; set; }
        public string[] Ss { get; set; }

        public int Insert(IRequest<R> request)
        {
            var entities = request.GetInputEntities();
            return entities.Count();
        }

        public IEnumerable<R> Select(IRequest<R> request)
        {
            return new[]
            {
                new R
                {
                    S = "Swoo",
                    Ss = new[] {"S", "Sd"}
                }
            };
        }

        public int Update(IRequest<R> request)
        {
            var entities = request.GetInputEntities();
            return entities.Count();
        }

        public int Delete(IRequest<R> request)
        {
            var entities = request.GetInputEntities();
            return entities.Count();
        }
    }

    public enum EE
    {
        A,
        B,
        C
    }

    [Database, RESTar]
    public class MyOther
    {
        public string Str { get; set; }
    }

    [RESTar(Method.GET)]
    public class MyDynamicTable : DDictionary, IDDictionary<MyDynamicTable, MyDynamicTableKvp>
    {
        public MyDynamicTableKvp NewKeyPair(MyDynamicTable dict, string key, object value = null) =>
            new MyDynamicTableKvp(dict, key, value);
    }

    public class MyDynamicTableKvp : DKeyValuePair
    {
        public MyDynamicTableKvp(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    #endregion

    #endregion
}