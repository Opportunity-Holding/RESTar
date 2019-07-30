using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using RESTar.ContentTypeProviders.NativeJsonProtocol;
using RESTar.Meta;
using RESTar.Requests;

namespace RESTar.ContentTypeProviders
{
    /// <inheritdoc />
    public class ExcelProvider : JsonAdapter
    {
        private const string ExcelMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string RESTarSpecific = "application/restar-excel";
        private const string Brief = "excel";

        /// <inheritdoc />
        public override string Name => "Microsoft Excel";

        /// <inheritdoc />
        public override ContentType ContentType { get; } = ExcelMimeType;

        /// <inheritdoc />
        public override string[] MatchStrings { get; set; } = {ExcelMimeType, RESTarSpecific, Brief};

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override string ContentDispositionFileExtension => ".xlsx";

        /// <inheritdoc />
        public override ulong SerializeCollection<T>(IEnumerable<T> _entities, Stream stream, IRequest request = null)
        {
            if (_entities == null) return 0;
            try
            {
                using (var package = new ExcelPackage(stream))
                {
                    var currentRow = 1;
                    var worksheet = package.Workbook.Worksheets.Add(request?.Resource.Name ?? "Sheet1");

                    void writeEntities(IEnumerable<object> entities)
                    {
                        switch (entities)
                        {
                            case IEnumerable<IDictionary<string, object>> dicts:
                                var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                                foreach (var dict in dicts)
                                {
                                    currentRow += 1;
                                    foreach (var pair in dict)
                                    {
                                        if (!columns.TryGetValue(pair.Key, out var column))
                                        {
                                            column = columns.Count + 1;
                                            columns[pair.Key] = column;
                                            var cell = worksheet.Cells[1, column];
                                            cell.Style.Font.Bold = true;
                                            cell.Value = pair.Key;
                                        }
                                        WriteExcelCell(worksheet.Cells[currentRow, column], pair.Value);
                                    }
                                }
                                break;
                            case IEnumerable<JObject> jobjects:
                                var _columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                                foreach (var jobject in jobjects)
                                {
                                    currentRow += 1;
                                    foreach (var pair in jobject)
                                    {
                                        if (!_columns.TryGetValue(pair.Key, out var column))
                                        {
                                            column = _columns.Count + 1;
                                            _columns[pair.Key] = column;
                                            var cell = worksheet.Cells[1, column];
                                            cell.Style.Font.Bold = true;
                                            cell.Value = pair.Key;
                                        }
                                        WriteExcelCell(worksheet.Cells[currentRow, column], pair.Value.ToObject<object>());
                                    }
                                }
                                break;
                            case ICollection<object> objs:
                                writeEntities(objs.Select(o => o is JObject jo ? jo : JObject.FromObject(o, JsonProvider.Serializer)));
                                break;

                            default:
                                var properties = typeof(T).GetDeclaredProperties().Values.Where(p => !p.Hidden).ToList();
                                var columnIndex = 1;
                                foreach (var property in properties)
                                {
                                    var cell = worksheet.Cells[1, columnIndex];
                                    cell.Style.Font.Bold = true;
                                    cell.Value = property.Name;
                                    columnIndex += 1;
                                }
                                foreach (var entity in entities)
                                {
                                    currentRow += 1;
                                    columnIndex = 1;
                                    foreach (var property in properties)
                                    {
                                        WriteExcelCell(worksheet.Cells[currentRow, columnIndex], GetCellValue(property, entity));
                                        columnIndex += 1;
                                    }
                                }
                                break;
                        }
                    }

                    writeEntities(_entities.Cast<object>());
                    if (currentRow == 1) return 0;
                    worksheet.Cells.AutoFitColumns(0);
                    package.Save();
                    return (ulong) currentRow - 1;
                }
            }
            catch (Exception e)
            {
                throw new ExcelFormatException(e.Message, e);
            }
        }

        private static object GetCellValue(DeclaredProperty prop, object target)
        {
            switch (prop)
            {
                case var _ when prop.ExcelReducer != null: return prop.ExcelReducer((dynamic) target);
                case var _ when prop.Type.IsEnum: return prop.GetValue(target)?.ToString();
                default: return prop.GetValue(target);
            }
        }

        private static void WriteExcelCell(ExcelRange target, object value)
        {
            switch (value)
            {
                case null:
                case DBNull _:
                case bool _:
                case decimal _:
                case long _:
                case sbyte _:
                case byte _:
                case short _:
                case ushort _:
                case int _:
                case uint _:
                case ulong _:
                case float _:
                case double _:
                case string _:
                    target.Value = value;
                    break;
                case DateTime dt:
                    target.Style.Numberformat.Format = "mm-dd-yy";
                    target.Value = dt;
                    break;
                case char @char:
                    target.Value = @char.ToString();
                    break;
                case JObject _:
                    target.Value = typeof(JObject).FullName;
                    break;
                case IDictionary other:
                    target.Value = other.GetType().RESTarTypeName();
                    break;
                case IEnumerable<object> other:
                    target.Value = string.Join(", ", other.Select(o => o.ToString()));
                    break;
                case IEnumerable<DateTime> dateTimes:
                    target.Value = string.Join(", ", dateTimes.Select(o => o.ToString("O")));
                    break;
                case var valArr when value.GetType().ImplementsGenericInterface(typeof(IEnumerable<>), out var p) && p.Any() && p[0].IsValueType:
                    IEnumerable<object> objects = Enumerable.Cast<object>((dynamic) valArr);
                    target.Value = string.Join(", ", objects.Select(o => o.ToString()));
                    break;
            }
        }

        /// <inheritdoc />
        protected override void ProduceJsonArray(Stream excelStream, Stream jsonStream)
        {
            try
            {
                using (var swr = new StreamWriter(jsonStream, UTF8, 1024, true))
                using (var jwr = new RESTarFromExcelJsonWriter(swr))
                using (var package = new ExcelPackage(excelStream))
                {
                    jwr.WriteStartArray();

                    var worksheet = package.Workbook?.Worksheets?.FirstOrDefault();
                    if (worksheet?.Dimension != null)
                    {
                        var (rows, columns) = (worksheet.Dimension.Rows, worksheet.Dimension.Columns);
                        if (rows > 1)
                        {
                            var propertyNames = new string[columns + 1];
                            for (var col = 1; col <= columns; col += 1)
                                propertyNames[col] = worksheet.Cells[1, col].GetValue<string>();
                            for (var row = 2; row <= rows; row += 1)
                            {
                                jwr.WriteStartObject();
                                for (var col = 1; col <= columns; col += 1)
                                {
                                    if (propertyNames[col] is string propertyName)
                                    {
                                        jwr.WritePropertyName(propertyName);
                                        jwr.WriteValue(worksheet.Cells[row, col].Value);
                                    }
                                }
                                jwr.WriteEndObject();
                            }
                        }
                    }

                    jwr.WriteEndArray();
                }
            }
            catch (Exception e)
            {
                throw new ExcelInputException(e.Message);
            }
        }
    }
}