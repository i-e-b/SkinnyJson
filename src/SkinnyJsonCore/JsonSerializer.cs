using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace SkinnyJson
{
    internal class JsonSerializer
    {
    	const int MaxDepth = 10;
        
        private TextWriter? _output;
    	int _currentDepth;
        private readonly Dictionary<string, int> _globalTypes = new Dictionary<string, int>();
        private readonly JsonParameters _jsonParameters;

        public JsonSerializer(JsonParameters param)
        {
            _jsonParameters = param;
        }

        /// <summary>
        /// Serialise a .Net object to a writable stream.
        /// Ignores the 'globalTypes' setting, will always either write types inline or elide them.
        /// </summary>
        public void ConvertToJson(object obj, Stream target, Encoding encoding)
        {
            if (!target.CanWrite) throw new Exception("Output stream must be writable");

            _output = new StreamWriter(target, encoding);
            WriteValue(obj);
            _output.Flush();
        }

        /// <summary>
        /// Output the static fields and properties of a .Net type
        /// as a JSON string.
        /// </summary>
        public string ConvertStaticsToJson(Type type)
        {
            // Extract public static fields and properties of the type to a dictionary,
            // and pass it through the normal type pathway
            
            var container = new Dictionary<string, object>();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var fieldInfo in fields)
            {
                var value = fieldInfo.GetValue(null!);
                container.Add(fieldInfo.Name, value); // recursion should be handled by ConvertToJson()
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static);
            foreach (var propertyInfo in properties)
            {
                var value = propertyInfo.GetValue(null!);
                container.Add(propertyInfo.Name, value);
            }
            
            return ConvertToJson(container);
        }

        /// <summary>
        /// Output a .Net object as a JSON string.
        /// Supports global types
        /// </summary>
        public string ConvertToJson(object obj)
        {
            var sb = new StringBuilder();
            _output = new StringWriter(sb);
            WriteValue(obj);
            _output.Flush();

            if (!_jsonParameters.UsingGlobalTypes)
                return sb.ToString();


            var prelim = sb.ToString();
            sb.Clear();
            sb.Append("\"$types\":{");
            bool pendingSeparator = false;
            foreach (var kv in _globalTypes)
            {
                if (kv.Key == null) continue;
                
                if (pendingSeparator) sb.Append(',');
                pendingSeparator = true;
                
                sb.Append("\"");
                sb.Append(kv.Key);
                sb.Append("\":\"");
                sb.Append(kv.Value);
                sb.Append("\"");
            }

            sb.Append("},");
            return prelim.Replace("$types$", sb.ToString());
        }

        private void Append(string? s) {
            if (s == null) return;
            _output?.Write(s);
        }
        
        private void Append(string s, int start, int length) {
            _output?.Write(s.Substring(start, length));
        }

        private void Append(char c) {
            _output?.Write(c);
        }

		/// <summary>
		/// This is the root of the serialiser.
		/// </summary>
        private void WriteValue(object? obj)
		{
		    switch (obj)
		    {
		        case null:
		        case DBNull _:
		            Append("null");
		            break;
		        case string _:
		        case char _:
		            WriteString((string)obj);
		            break;
		        case Guid guid:
		            WriteGuid(guid);
		            break;
		        case bool b:
		            Append(b ? "true" : "false"); // conform to standard
		            break;
		        default:
		            if (IsNumericPrimitive(obj))
		                Append(((IConvertible)obj).ToString(NumberFormatInfo.InvariantInfo));

		            else switch (obj)
		            {
		                case DateTime time:
		                    WriteDateTime(time);
		                    break;
                        case TimeSpan timeSpan:
                            WriteTimeSpan(timeSpan);
                            break;
		                case IDictionary dictionary when dictionary.GetType().IsGenericType && dictionary.GetType().GetGenericArguments()[0] == typeof(string):
		                    WriteStringDictionary(dictionary);
		                    break;
		                case IDictionary dictionary1:
		                    WriteDictionary(dictionary1);
		                    break;
		                case DataSet set:
		                    WriteDataset(set);
		                    break;
		                case DataTable table:
		                    WriteDataTable(table);
		                    break;
		                case byte[] bytes:
		                    WriteBytes(bytes);
		                    break;
		                case Array _:
		                case IList _:
		                case ICollection _:
		                    WriteArray((IEnumerable)obj);
		                    break;
		                case IEnumerable enumerable:
		                    WriteArray(enumerable);
		                    break;
		                case Enum enumeration:
		                    WriteEnum(enumeration);
		                    break;
                        case Type typeDef:
                            WriteString(typeDef.FullName ?? typeDef.Name);
                            break;
		                default:
		                    WriteObject(obj);
		                    break;
		            }
		            break;
		    }
		}

	    static bool IsNumericPrimitive(object obj)
	    {
		    return obj is int || obj is long || obj is double ||
		           obj is decimal || obj is float ||
		           obj is byte || obj is short ||
		           obj is sbyte || obj is ushort ||
		           obj is uint || obj is ulong;
	    }

	    private void WriteEnum(Enum e)
        {
            WriteStringFast(e.ToString());
        }

        private void WriteGuid(Guid g)
        {
            if (_jsonParameters.UseFastGuid == false)
                WriteStringFast(g.ToString());
            else
                WriteBytes(g.ToByteArray());
        }

        private void WriteBytes(byte[] bytes)
        {
            WriteStringFast(Convert.ToBase64String(bytes, 0, bytes.Length, Base64FormattingOptions.None));
        }

        private void WriteDateTime(DateTime dateTime)
        {
            Append('\"');
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                Append(dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            }
            else if (_jsonParameters.UseUtcDateTime)
            {
                Append(dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
            }
            else
            {
                Append(dateTime.ToString("yyyy-MM-ddTHH:mm:ss"));
            }
        	Append('\"');

        }

        private void WriteTimeSpan(TimeSpan timespan)
        {
            Append('\"');
            Append(timespan.ToString());
            Append('\"');
        }

        private static DatasetSchema? GetSchema(DataTable? ds)
        {
            if (ds == null) return null;

        	var m = new DatasetSchema {Info = new List<string>(), Name = ds.TableName};

        	foreach (DataColumn c in ds.Columns)
            {
                m.Info.Add(ds.TableName);
                m.Info.Add(c.ColumnName);
                m.Info.Add(c.DataType?.ToString()??"");
            }
            return m;
        }

        private static DatasetSchema? GetSchema(DataSet? ds)
        {
            if (ds == null) return null;

        	var m = new DatasetSchema {Info = new List<string>(), Name = ds.DataSetName};

        	foreach (DataTable t in ds.Tables)
            {
                foreach (DataColumn c in t.Columns)
                {
                    m.Info.Add(t.TableName);
                    m.Info.Add(c.ColumnName);
                    m.Info.Add(c.DataType?.ToString() ?? "");
                }
            }

            return m;
        }

        private static string GetXmlSchema(DataTable dt)
        {
            using var writer = new StringWriter();
            dt.WriteXmlSchema(writer);
            return dt.ToString()!;
        }

        private void WriteDataset(DataSet ds)
        {
            Append('{');
            if ( _jsonParameters.UseExtensions)
            {
                WritePair("$schema", _jsonParameters.UseOptimizedDatasetSchema ? GetSchema(ds) as object : ds.GetXmlSchema());
                Append(',');
            }
            var tableSep = false;
            foreach (DataTable table in ds.Tables)
            {
                if (tableSep) Append(",");
                tableSep = true;
                WriteDataTableData(table);
            }
            Append('}');
        }

        private void WriteDataTableData(DataTable table)
        {
            Append('\"');
            Append(table.TableName);
            Append("\":[");
            DataColumnCollection cols = table.Columns;
            var rowSeparator = false;
            foreach (DataRow row in table.Rows)
            {
                if (rowSeparator) Append(",");
                rowSeparator = true;
                Append('[');

                var pendingSeparator = false;
                foreach (DataColumn column in cols)
                {
                    if (pendingSeparator) Append(',');
                    WriteValue(row[column]);
                    pendingSeparator = true;
                }
                Append(']');
            }

            Append(']');
        }

        void WriteDataTable(DataTable dt)
        {
            Append('{');
            if (_jsonParameters.UseExtensions)
            {
                WritePair("$schema", _jsonParameters.UseOptimizedDatasetSchema ? GetSchema(dt) as object : GetXmlSchema(dt));
                Append(',');
            }

            WriteDataTableData(dt);
            Append('}');
        }

        bool _typesWritten;
        private void WriteObject(object obj)
        {
            if (_jsonParameters.UsingGlobalTypes == false) Append('{');
			else Append(_typesWritten == false ? "{$types$" : "{");

			_typesWritten = true;
			_currentDepth++;
            if (_currentDepth > MaxDepth)
                throw new Exception("Serialiser encountered maximum depth of " + MaxDepth);


            var map = new Dictionary<string, string>();
            var t = obj.GetType();
            var append = false;
            if (_jsonParameters.UseExtensions)
            {
                if (_jsonParameters.UsingGlobalTypes == false)
                    WritePairFast("$type", Json.Instance.GetTypeAssemblyName(t));
                else
                {
                    var ct = Json.Instance.GetTypeAssemblyName(t);
                    if (_globalTypes.TryGetValue(ct, out var dt) == false)
                    {
                        dt = _globalTypes.Count + 1;
                        _globalTypes.Add(ct, dt);
                    }
                    WritePairFast("$type", dt.ToString());
                }
                append = true;
            }

            var readableProperties = Json.Instance.GetGetters(t);
            foreach (var property in readableProperties)
            {
                if (property.Name == null) continue;
                var o = GetInstanceValue(obj, t, property);
                if ((o == null || o is DBNull) && _jsonParameters.SerializeNullValues == false) continue;

                if (append) Append(',');
                WritePair(property.Name, o);
                if (o != null && _jsonParameters.UseExtensions)
                {
                    var tt = o.GetType();
                    if (tt == typeof(object)) map.Add(property.Name, tt.ToString());
                }

                append = true;
            }
            if (map.Count > 0 && _jsonParameters.UseExtensions)
            {
                Append(",\"$map\":");
                WriteStringDictionary(map);
            }
            _currentDepth--;
            Append('}');
            _currentDepth--;

        }

    	static object? GetInstanceValue(object obj, Type t, Getters p) {
    		if (t.IsValueType && p.FieldInfo != null) {
				return p.FieldInfo.GetValue(obj);
			}
    		if (t.IsValueType && p.PropertyType != null && p.Name != null) {
    			return t.GetProperty(p.Name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj, null!);
    		}
    		return p.Getter?.Invoke(obj);
    	}

    	private void WritePairFast(string name, string? value)
        {
            if ((value == null) && _jsonParameters.SerializeNullValues == false)
                return;
            WriteStringFast(name);

            Append(':');

            WriteStringFast(value);
        }

        private void WritePair(string name, object? value)
        {
            if ((value == null || value is DBNull) && _jsonParameters.SerializeNullValues == false) return;
            WriteStringFast(name);
            Append(':');
            WriteValue(value);
        }

        private void WriteArray(IEnumerable array)
        {
            Append('[');

            var pendingSeparator = false;

            foreach (object obj in array)
            {
                if (pendingSeparator) Append(',');

                WriteValue(obj);

                pendingSeparator = true;
            }
            Append(']');
        }

        private void WriteStringDictionary(IDictionary dic)
        {
            Append('{');

            bool pendingSeparator = false;

            foreach (DictionaryEntry entry in dic)
            {
                var entryKey = entry.Key as string;
                if (entryKey == null) continue;
                if (pendingSeparator) Append(',');

                WritePair(entryKey, entry.Value);

                pendingSeparator = true;
            }
            Append('}');
        }

        private void WriteDictionary(IDictionary dic)
        {
            Append('[');

            bool pendingSeparator = false;

            foreach (DictionaryEntry entry in dic)
            {
                if (pendingSeparator) Append(',');
                Append('{');
                WritePair("k", entry.Key);
                Append(",");
                WritePair("v", entry.Value);
                Append('}');

                pendingSeparator = true;
            }
            Append(']');
        }

        /// <summary>
        /// Directly output strings we know won't need escape sequences
        /// </summary>
        private void WriteStringFast(string? s)
        {
            if (s == null)
            {
                Append("null");
                return;
            }

            Append('\"');
            Append(s);
            Append('\"');
        }

        /// <summary>
        /// Write a string to the output, converting characters to escape sequences where needed.
        /// </summary>
        private void WriteString(string s)
        {
            Append('\"');

            int runIndex = -1;

            for (var index = 0; index < s.Length; ++index)
            {
                var c = s[index];

                if (c >= ' ' && c < 128 && c != '\"' && c != '\\')
                {
                    if (runIndex == -1)
                    {
                        runIndex = index;
                    }

                    continue;
                }

                if (runIndex != -1)
                {
                    Append(s, runIndex, index - runIndex);
                    runIndex = -1;
                }

                switch (c)
                {
                    case '\t': Append("\\t"); break;
                    case '\r': Append("\\r"); break;
                    case '\n': Append("\\n"); break;
                    case '"':
                    case '\\': Append('\\'); Append(c); break;
                    default:
                        Append("\\u");
                        Append(((int)c).ToString("X4", NumberFormatInfo.InvariantInfo));
                        break;
                }
            }

            if (runIndex != -1)
            {
                Append(s, runIndex, s.Length - runIndex);
            }

            Append('\"');
        }
    }
}
