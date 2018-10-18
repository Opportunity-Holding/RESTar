using Dynamit;
using Starcounter;

// ReSharper disable UnusedMember.Global
#pragma warning disable 1591

namespace RESTar.Dynamic
{
    [Database, DynamicTable]
    public class DynamicResource01 : DDictionary, IDDictionary<DynamicResource01, DynamicResource1KeyValuePair>
    {
        public DynamicResource1KeyValuePair NewKeyPair(DynamicResource01 dict, string key, object value = null) =>
            new DynamicResource1KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource1KeyValuePair : DKeyValuePair
    {
        public DynamicResource1KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource02 : DDictionary, IDDictionary<DynamicResource02, DynamicResource2KeyValuePair>
    {
        public DynamicResource2KeyValuePair NewKeyPair(DynamicResource02 dict, string key, object value = null) =>
            new DynamicResource2KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource2KeyValuePair : DKeyValuePair
    {
        public DynamicResource2KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource03 : DDictionary, IDDictionary<DynamicResource03, DynamicResource3KeyValuePair>
    {
        public DynamicResource3KeyValuePair NewKeyPair(DynamicResource03 dict, string key, object value = null) =>
            new DynamicResource3KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource3KeyValuePair : DKeyValuePair
    {
        public DynamicResource3KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource04 : DDictionary, IDDictionary<DynamicResource04, DynamicResource4KeyValuePair>
    {
        public DynamicResource4KeyValuePair NewKeyPair(DynamicResource04 dict, string key, object value = null) =>
            new DynamicResource4KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource4KeyValuePair : DKeyValuePair
    {
        public DynamicResource4KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource05 : DDictionary, IDDictionary<DynamicResource05, DynamicResource5KeyValuePair>
    {
        public DynamicResource5KeyValuePair NewKeyPair(DynamicResource05 dict, string key, object value = null) =>
            new DynamicResource5KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource5KeyValuePair : DKeyValuePair
    {
        public DynamicResource5KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource06 : DDictionary, IDDictionary<DynamicResource06, DynamicResource6KeyValuePair>
    {
        public DynamicResource6KeyValuePair NewKeyPair(DynamicResource06 dict, string key, object value = null) =>
            new DynamicResource6KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource6KeyValuePair : DKeyValuePair
    {
        public DynamicResource6KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource07 : DDictionary, IDDictionary<DynamicResource07, DynamicResource7KeyValuePair>
    {
        public DynamicResource7KeyValuePair NewKeyPair(DynamicResource07 dict, string key, object value = null) =>
            new DynamicResource7KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource7KeyValuePair : DKeyValuePair
    {
        public DynamicResource7KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource08 : DDictionary, IDDictionary<DynamicResource08, DynamicResource8KeyValuePair>
    {
        public DynamicResource8KeyValuePair NewKeyPair(DynamicResource08 dict, string key, object value = null) =>
            new DynamicResource8KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource8KeyValuePair : DKeyValuePair
    {
        public DynamicResource8KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource09 : DDictionary, IDDictionary<DynamicResource09, DynamicResource9KeyValuePair>
    {
        public DynamicResource9KeyValuePair NewKeyPair(DynamicResource09 dict, string key, object value = null) =>
            new DynamicResource9KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource9KeyValuePair : DKeyValuePair
    {
        public DynamicResource9KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource10 : DDictionary, IDDictionary<DynamicResource10, DynamicResource10KeyValuePair>
    {
        public DynamicResource10KeyValuePair NewKeyPair(DynamicResource10 dict, string key, object value = null) =>
            new DynamicResource10KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource10KeyValuePair : DKeyValuePair
    {
        public DynamicResource10KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource11 : DDictionary, IDDictionary<DynamicResource11, DynamicResource11KeyValuePair>
    {
        public DynamicResource11KeyValuePair NewKeyPair(DynamicResource11 dict, string key, object value = null) =>
            new DynamicResource11KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource11KeyValuePair : DKeyValuePair
    {
        public DynamicResource11KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource12 : DDictionary, IDDictionary<DynamicResource12, DynamicResource12KeyValuePair>
    {
        public DynamicResource12KeyValuePair NewKeyPair(DynamicResource12 dict, string key, object value = null) =>
            new DynamicResource12KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource12KeyValuePair : DKeyValuePair
    {
        public DynamicResource12KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource13 : DDictionary, IDDictionary<DynamicResource13, DynamicResource13KeyValuePair>
    {
        public DynamicResource13KeyValuePair NewKeyPair(DynamicResource13 dict, string key, object value = null) =>
            new DynamicResource13KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource13KeyValuePair : DKeyValuePair
    {
        public DynamicResource13KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource14 : DDictionary, IDDictionary<DynamicResource14, DynamicResource14KeyValuePair>
    {
        public DynamicResource14KeyValuePair NewKeyPair(DynamicResource14 dict, string key, object value = null) =>
            new DynamicResource14KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource14KeyValuePair : DKeyValuePair
    {
        public DynamicResource14KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource15 : DDictionary, IDDictionary<DynamicResource15, DynamicResource15KeyValuePair>
    {
        public DynamicResource15KeyValuePair NewKeyPair(DynamicResource15 dict, string key, object value = null) =>
            new DynamicResource15KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource15KeyValuePair : DKeyValuePair
    {
        public DynamicResource15KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource16 : DDictionary, IDDictionary<DynamicResource16, DynamicResource16KeyValuePair>
    {
        public DynamicResource16KeyValuePair NewKeyPair(DynamicResource16 dict, string key, object value = null) =>
            new DynamicResource16KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource16KeyValuePair : DKeyValuePair
    {
        public DynamicResource16KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource17 : DDictionary, IDDictionary<DynamicResource17, DynamicResource17KeyValuePair>
    {
        public DynamicResource17KeyValuePair NewKeyPair(DynamicResource17 dict, string key, object value = null) =>
            new DynamicResource17KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource17KeyValuePair : DKeyValuePair
    {
        public DynamicResource17KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource18 : DDictionary, IDDictionary<DynamicResource18, DynamicResource18KeyValuePair>
    {
        public DynamicResource18KeyValuePair NewKeyPair(DynamicResource18 dict, string key, object value = null) =>
            new DynamicResource18KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource18KeyValuePair : DKeyValuePair
    {
        public DynamicResource18KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource19 : DDictionary, IDDictionary<DynamicResource19, DynamicResource19KeyValuePair>
    {
        public DynamicResource19KeyValuePair NewKeyPair(DynamicResource19 dict, string key, object value = null) =>
            new DynamicResource19KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource19KeyValuePair : DKeyValuePair
    {
        public DynamicResource19KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource20 : DDictionary, IDDictionary<DynamicResource20, DynamicResource20KeyValuePair>
    {
        public DynamicResource20KeyValuePair NewKeyPair(DynamicResource20 dict, string key, object value = null) =>
            new DynamicResource20KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource20KeyValuePair : DKeyValuePair
    {
        public DynamicResource20KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource21 : DDictionary, IDDictionary<DynamicResource21, DynamicResource21KeyValuePair>
    {
        public DynamicResource21KeyValuePair NewKeyPair(DynamicResource21 dict, string key, object value = null) =>
            new DynamicResource21KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource21KeyValuePair : DKeyValuePair
    {
        public DynamicResource21KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource22 : DDictionary, IDDictionary<DynamicResource22, DynamicResource22KeyValuePair>
    {
        public DynamicResource22KeyValuePair NewKeyPair(DynamicResource22 dict, string key, object value = null) =>
            new DynamicResource22KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource22KeyValuePair : DKeyValuePair
    {
        public DynamicResource22KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource23 : DDictionary, IDDictionary<DynamicResource23, DynamicResource23KeyValuePair>
    {
        public DynamicResource23KeyValuePair NewKeyPair(DynamicResource23 dict, string key, object value = null) =>
            new DynamicResource23KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource23KeyValuePair : DKeyValuePair
    {
        public DynamicResource23KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource24 : DDictionary, IDDictionary<DynamicResource24, DynamicResource24KeyValuePair>
    {
        public DynamicResource24KeyValuePair NewKeyPair(DynamicResource24 dict, string key, object value = null) =>
            new DynamicResource24KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource24KeyValuePair : DKeyValuePair
    {
        public DynamicResource24KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource25 : DDictionary, IDDictionary<DynamicResource25, DynamicResource25KeyValuePair>
    {
        public DynamicResource25KeyValuePair NewKeyPair(DynamicResource25 dict, string key, object value = null) =>
            new DynamicResource25KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource25KeyValuePair : DKeyValuePair
    {
        public DynamicResource25KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource26 : DDictionary, IDDictionary<DynamicResource26, DynamicResource26KeyValuePair>
    {
        public DynamicResource26KeyValuePair NewKeyPair(DynamicResource26 dict, string key, object value = null) =>
            new DynamicResource26KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource26KeyValuePair : DKeyValuePair
    {
        public DynamicResource26KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource27 : DDictionary, IDDictionary<DynamicResource27, DynamicResource27KeyValuePair>
    {
        public DynamicResource27KeyValuePair NewKeyPair(DynamicResource27 dict, string key, object value = null) =>
            new DynamicResource27KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource27KeyValuePair : DKeyValuePair
    {
        public DynamicResource27KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource28 : DDictionary, IDDictionary<DynamicResource28, DynamicResource28KeyValuePair>
    {
        public DynamicResource28KeyValuePair NewKeyPair(DynamicResource28 dict, string key, object value = null) =>
            new DynamicResource28KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource28KeyValuePair : DKeyValuePair
    {
        public DynamicResource28KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource29 : DDictionary, IDDictionary<DynamicResource29, DynamicResource29KeyValuePair>
    {
        public DynamicResource29KeyValuePair NewKeyPair(DynamicResource29 dict, string key, object value = null) =>
            new DynamicResource29KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource29KeyValuePair : DKeyValuePair
    {
        public DynamicResource29KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource30 : DDictionary, IDDictionary<DynamicResource30, DynamicResource30KeyValuePair>
    {
        public DynamicResource30KeyValuePair NewKeyPair(DynamicResource30 dict, string key, object value = null) =>
            new DynamicResource30KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource30KeyValuePair : DKeyValuePair
    {
        public DynamicResource30KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource31 : DDictionary, IDDictionary<DynamicResource31, DynamicResource31KeyValuePair>
    {
        public DynamicResource31KeyValuePair NewKeyPair(DynamicResource31 dict, string key, object value = null) =>
            new DynamicResource31KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource31KeyValuePair : DKeyValuePair
    {
        public DynamicResource31KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource32 : DDictionary, IDDictionary<DynamicResource32, DynamicResource32KeyValuePair>
    {
        public DynamicResource32KeyValuePair NewKeyPair(DynamicResource32 dict, string key, object value = null) =>
            new DynamicResource32KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource32KeyValuePair : DKeyValuePair
    {
        public DynamicResource32KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource33 : DDictionary, IDDictionary<DynamicResource33, DynamicResource33KeyValuePair>
    {
        public DynamicResource33KeyValuePair NewKeyPair(DynamicResource33 dict, string key, object value = null) =>
            new DynamicResource33KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource33KeyValuePair : DKeyValuePair
    {
        public DynamicResource33KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource34 : DDictionary, IDDictionary<DynamicResource34, DynamicResource34KeyValuePair>
    {
        public DynamicResource34KeyValuePair NewKeyPair(DynamicResource34 dict, string key, object value = null) =>
            new DynamicResource34KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource34KeyValuePair : DKeyValuePair
    {
        public DynamicResource34KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource35 : DDictionary, IDDictionary<DynamicResource35, DynamicResource35KeyValuePair>
    {
        public DynamicResource35KeyValuePair NewKeyPair(DynamicResource35 dict, string key, object value = null) =>
            new DynamicResource35KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource35KeyValuePair : DKeyValuePair
    {
        public DynamicResource35KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource36 : DDictionary, IDDictionary<DynamicResource36, DynamicResource36KeyValuePair>
    {
        public DynamicResource36KeyValuePair NewKeyPair(DynamicResource36 dict, string key, object value = null) =>
            new DynamicResource36KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource36KeyValuePair : DKeyValuePair
    {
        public DynamicResource36KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource37 : DDictionary, IDDictionary<DynamicResource37, DynamicResource37KeyValuePair>
    {
        public DynamicResource37KeyValuePair NewKeyPair(DynamicResource37 dict, string key, object value = null) =>
            new DynamicResource37KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource37KeyValuePair : DKeyValuePair
    {
        public DynamicResource37KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource38 : DDictionary, IDDictionary<DynamicResource38, DynamicResource38KeyValuePair>
    {
        public DynamicResource38KeyValuePair NewKeyPair(DynamicResource38 dict, string key, object value = null) =>
            new DynamicResource38KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource38KeyValuePair : DKeyValuePair
    {
        public DynamicResource38KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource39 : DDictionary, IDDictionary<DynamicResource39, DynamicResource39KeyValuePair>
    {
        public DynamicResource39KeyValuePair NewKeyPair(DynamicResource39 dict, string key, object value = null) =>
            new DynamicResource39KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource39KeyValuePair : DKeyValuePair
    {
        public DynamicResource39KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource40 : DDictionary, IDDictionary<DynamicResource40, DynamicResource40KeyValuePair>
    {
        public DynamicResource40KeyValuePair NewKeyPair(DynamicResource40 dict, string key, object value = null) =>
            new DynamicResource40KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource40KeyValuePair : DKeyValuePair
    {
        public DynamicResource40KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource41 : DDictionary, IDDictionary<DynamicResource41, DynamicResource41KeyValuePair>
    {
        public DynamicResource41KeyValuePair NewKeyPair(DynamicResource41 dict, string key, object value = null) =>
            new DynamicResource41KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource41KeyValuePair : DKeyValuePair
    {
        public DynamicResource41KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource42 : DDictionary, IDDictionary<DynamicResource42, DynamicResource42KeyValuePair>
    {
        public DynamicResource42KeyValuePair NewKeyPair(DynamicResource42 dict, string key, object value = null) =>
            new DynamicResource42KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource42KeyValuePair : DKeyValuePair
    {
        public DynamicResource42KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource43 : DDictionary, IDDictionary<DynamicResource43, DynamicResource43KeyValuePair>
    {
        public DynamicResource43KeyValuePair NewKeyPair(DynamicResource43 dict, string key, object value = null) =>
            new DynamicResource43KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource43KeyValuePair : DKeyValuePair
    {
        public DynamicResource43KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource44 : DDictionary, IDDictionary<DynamicResource44, DynamicResource44KeyValuePair>
    {
        public DynamicResource44KeyValuePair NewKeyPair(DynamicResource44 dict, string key, object value = null) =>
            new DynamicResource44KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource44KeyValuePair : DKeyValuePair
    {
        public DynamicResource44KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource45 : DDictionary, IDDictionary<DynamicResource45, DynamicResource45KeyValuePair>
    {
        public DynamicResource45KeyValuePair NewKeyPair(DynamicResource45 dict, string key, object value = null) =>
            new DynamicResource45KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource45KeyValuePair : DKeyValuePair
    {
        public DynamicResource45KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource46 : DDictionary, IDDictionary<DynamicResource46, DynamicResource46KeyValuePair>
    {
        public DynamicResource46KeyValuePair NewKeyPair(DynamicResource46 dict, string key, object value = null) =>
            new DynamicResource46KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource46KeyValuePair : DKeyValuePair
    {
        public DynamicResource46KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource47 : DDictionary, IDDictionary<DynamicResource47, DynamicResource47KeyValuePair>
    {
        public DynamicResource47KeyValuePair NewKeyPair(DynamicResource47 dict, string key, object value = null) =>
            new DynamicResource47KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource47KeyValuePair : DKeyValuePair
    {
        public DynamicResource47KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource48 : DDictionary, IDDictionary<DynamicResource48, DynamicResource48KeyValuePair>
    {
        public DynamicResource48KeyValuePair NewKeyPair(DynamicResource48 dict, string key, object value = null) =>
            new DynamicResource48KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource48KeyValuePair : DKeyValuePair
    {
        public DynamicResource48KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource49 : DDictionary, IDDictionary<DynamicResource49, DynamicResource49KeyValuePair>
    {
        public DynamicResource49KeyValuePair NewKeyPair(DynamicResource49 dict, string key, object value = null) =>
            new DynamicResource49KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource49KeyValuePair : DKeyValuePair
    {
        public DynamicResource49KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource50 : DDictionary, IDDictionary<DynamicResource50, DynamicResource50KeyValuePair>
    {
        public DynamicResource50KeyValuePair NewKeyPair(DynamicResource50 dict, string key, object value = null) =>
            new DynamicResource50KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource50KeyValuePair : DKeyValuePair
    {
        public DynamicResource50KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource51 : DDictionary, IDDictionary<DynamicResource51, DynamicResource51KeyValuePair>
    {
        public DynamicResource51KeyValuePair NewKeyPair(DynamicResource51 dict, string key, object value = null) =>
            new DynamicResource51KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource51KeyValuePair : DKeyValuePair
    {
        public DynamicResource51KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource52 : DDictionary, IDDictionary<DynamicResource52, DynamicResource52KeyValuePair>
    {
        public DynamicResource52KeyValuePair NewKeyPair(DynamicResource52 dict, string key, object value = null) =>
            new DynamicResource52KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource52KeyValuePair : DKeyValuePair
    {
        public DynamicResource52KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource53 : DDictionary, IDDictionary<DynamicResource53, DynamicResource53KeyValuePair>
    {
        public DynamicResource53KeyValuePair NewKeyPair(DynamicResource53 dict, string key, object value = null) =>
            new DynamicResource53KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource53KeyValuePair : DKeyValuePair
    {
        public DynamicResource53KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource54 : DDictionary, IDDictionary<DynamicResource54, DynamicResource54KeyValuePair>
    {
        public DynamicResource54KeyValuePair NewKeyPair(DynamicResource54 dict, string key, object value = null) =>
            new DynamicResource54KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource54KeyValuePair : DKeyValuePair
    {
        public DynamicResource54KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource55 : DDictionary, IDDictionary<DynamicResource55, DynamicResource55KeyValuePair>
    {
        public DynamicResource55KeyValuePair NewKeyPair(DynamicResource55 dict, string key, object value = null) =>
            new DynamicResource55KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource55KeyValuePair : DKeyValuePair
    {
        public DynamicResource55KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource56 : DDictionary, IDDictionary<DynamicResource56, DynamicResource56KeyValuePair>
    {
        public DynamicResource56KeyValuePair NewKeyPair(DynamicResource56 dict, string key, object value = null) =>
            new DynamicResource56KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource56KeyValuePair : DKeyValuePair
    {
        public DynamicResource56KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource57 : DDictionary, IDDictionary<DynamicResource57, DynamicResource57KeyValuePair>
    {
        public DynamicResource57KeyValuePair NewKeyPair(DynamicResource57 dict, string key, object value = null) =>
            new DynamicResource57KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource57KeyValuePair : DKeyValuePair
    {
        public DynamicResource57KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource58 : DDictionary, IDDictionary<DynamicResource58, DynamicResource58KeyValuePair>
    {
        public DynamicResource58KeyValuePair NewKeyPair(DynamicResource58 dict, string key, object value = null) =>
            new DynamicResource58KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource58KeyValuePair : DKeyValuePair
    {
        public DynamicResource58KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource59 : DDictionary, IDDictionary<DynamicResource59, DynamicResource59KeyValuePair>
    {
        public DynamicResource59KeyValuePair NewKeyPair(DynamicResource59 dict, string key, object value = null) =>
            new DynamicResource59KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource59KeyValuePair : DKeyValuePair
    {
        public DynamicResource59KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource60 : DDictionary, IDDictionary<DynamicResource60, DynamicResource60KeyValuePair>
    {
        public DynamicResource60KeyValuePair NewKeyPair(DynamicResource60 dict, string key, object value = null) =>
            new DynamicResource60KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource60KeyValuePair : DKeyValuePair
    {
        public DynamicResource60KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource61 : DDictionary, IDDictionary<DynamicResource61, DynamicResource61KeyValuePair>
    {
        public DynamicResource61KeyValuePair NewKeyPair(DynamicResource61 dict, string key, object value = null) =>
            new DynamicResource61KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource61KeyValuePair : DKeyValuePair
    {
        public DynamicResource61KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource62 : DDictionary, IDDictionary<DynamicResource62, DynamicResource62KeyValuePair>
    {
        public DynamicResource62KeyValuePair NewKeyPair(DynamicResource62 dict, string key, object value = null) =>
            new DynamicResource62KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource62KeyValuePair : DKeyValuePair
    {
        public DynamicResource62KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource63 : DDictionary, IDDictionary<DynamicResource63, DynamicResource63KeyValuePair>
    {
        public DynamicResource63KeyValuePair NewKeyPair(DynamicResource63 dict, string key, object value = null) =>
            new DynamicResource63KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource63KeyValuePair : DKeyValuePair
    {
        public DynamicResource63KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [Database, DynamicTable]
    public class DynamicResource64 : DDictionary, IDDictionary<DynamicResource64, DynamicResource64KeyValuePair>
    {
        public DynamicResource64KeyValuePair NewKeyPair(DynamicResource64 dict, string key, object value = null) =>
            new DynamicResource64KeyValuePair(dict, key, value);
    }

    [Database]
    public class DynamicResource64KeyValuePair : DKeyValuePair
    {
        public DynamicResource64KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }
}