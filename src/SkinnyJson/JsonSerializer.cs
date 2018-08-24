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
        private TextWriter output;
    	const int MaxDepth = 10;
    	int currentDepth;
        private readonly Dictionary<string, int> globalTypes = new Dictionary<string, int>();
        private readonly JsonParameters jsonParameters;

        public JsonSerializer(JsonParameters param)
        {
            jsonParameters = param;
        }

        /// <summary>
        /// Serialise a .Net object to a writable stream.
        /// Ignores the 'globalTypes' setting, will always either write types inline or elide them.
        /// </summary>
        public void ConvertToJson(object obj, Stream target)
        {
            if (!target.CanWrite) throw new Exception("Output stream must be writable");

            output = new StreamWriter(target);
            WriteValue(obj);
            output.Flush();
        }

        /// <summary>
        /// Output a .Net object as a JSON string.
        /// Supports global types
        /// </summary>
        public string ConvertToJson(object obj)
        {
            var sb = new StringBuilder();
            output = new StringWriter(sb);
            WriteValue(obj);
            output.Flush();

            if (!jsonParameters.UsingGlobalTypes)
                return sb.ToString();


            var prelim = sb.ToString();
            sb.Clear();
            sb.Append("\"$types\":{");
            bool pendingSeparator = false;
            foreach (var kv in globalTypes)
            {
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

        private void Append(string s) {
            output.Write(s);
        }
        
        private void Append(string s, int start, int length) {
            output.Write(s.Substring(start, length));
        }

        private void Append(char c) {
            output.Write(c);
        }

		/// <summary>
		/// This is the root of the serialiser.
		/// </summary>
        private void WriteValue(object obj)
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
		            if (isNumericPrimitive(obj))
		                Append(((IConvertible)obj).ToString(NumberFormatInfo.InvariantInfo));

		            else switch (obj)
		            {
		                case DateTime time:
		                    WriteDateTime(time);
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
		                default:
		                    WriteObject(obj);
		                    break;
		            }
		            break;
		    }
		}

	    static bool isNumericPrimitive(object obj)
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
            if (jsonParameters.UseFastGuid == false)
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
        	Append(
				jsonParameters.UseUtcDateTime
					? dateTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ssZ")
					: dateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        	Append('\"');

        }
        private static DatasetSchema GetSchema(DataTable ds)
        {
            if (ds == null) return null;

        	var m = new DatasetSchema {Info = new List<string>(), Name = ds.TableName};

        	foreach (DataColumn c in ds.Columns)
            {
                m.Info.Add(ds.TableName);
                m.Info.Add(c.ColumnName);
                m.Info.Add(c.DataType.ToString());
            }
            return m;
        }

        private static DatasetSchema GetSchema(DataSet ds)
        {
            if (ds == null) return null;

        	var m = new DatasetSchema {Info = new List<string>(), Name = ds.DataSetName};

        	foreach (DataTable t in ds.Tables)
            {
                foreach (DataColumn c in t.Columns)
                {
                    m.Info.Add(t.TableName);
                    m.Info.Add(c.ColumnName);
                    m.Info.Add(c.DataType.ToString());
                }
            }

            return m;
        }

        private static string GetXmlSchema(DataTable dt)
        {
            using (var writer = new StringWriter())
            {
                dt.WriteXmlSchema(writer);
                return dt.ToString();
            }
        }

        private void WriteDataset(DataSet ds)
        {
            Append('{');
            if ( jsonParameters.UseExtensions)
            {
                WritePair("$schema", jsonParameters.UseOptimizedDatasetSchema ? (object)GetSchema(ds) : ds.GetXmlSchema());
                Append(',');
            }
            bool tablesep = false;
            foreach (DataTable table in ds.Tables)
            {
                if (tablesep) Append(",");
                tablesep = true;
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
            bool rowseparator = false;
            foreach (DataRow row in table.Rows)
            {
                if (rowseparator) Append(",");
                rowseparator = true;
                Append('[');

                bool pendingSeperator = false;
                foreach (DataColumn column in cols)
                {
                    if (pendingSeperator) Append(',');
                    WriteValue(row[column]);
                    pendingSeperator = true;
                }
                Append(']');
            }

            Append(']');
        }

        void WriteDataTable(DataTable dt)
        {
            Append('{');
            if (jsonParameters.UseExtensions)
            {
                WritePair("$schema", jsonParameters.UseOptimizedDatasetSchema ? (object)GetSchema(dt) : GetXmlSchema(dt));
                Append(',');
            }

            WriteDataTableData(dt);
            Append('}');
        }

        bool typesWritten;
        private void WriteObject(object obj)
        {
            if (jsonParameters.UsingGlobalTypes == false) Append('{');
			else Append(typesWritten == false ? "{$types$" : "{");

			typesWritten = true;
			currentDepth++;
            if (currentDepth > MaxDepth)
                throw new Exception("Serialiser encountered maximum depth of " + MaxDepth);


            var map = new Dictionary<string, string>();
            var t = obj.GetType();
            var append = false;
            if (jsonParameters.UseExtensions)
            {
                if (jsonParameters.UsingGlobalTypes == false)
                    WritePairFast("$type", Json.Instance.GetTypeAssemblyName(t));
                else
                {
                    int dt;
                    var ct = Json.Instance.GetTypeAssemblyName(t);
                    if (globalTypes.TryGetValue(ct, out dt) == false)
                    {
                        dt = globalTypes.Count + 1;
                        globalTypes.Add(ct, dt);
                    }
                    WritePairFast("$type", dt.ToString());
                }
                append = true;
            }

            var readableProperties = Json.Instance.GetGetters(t);
            foreach (var property in readableProperties)
            {
                var o = GetInstanceValue(obj, t, property);
                if ((o == null || o is DBNull) && jsonParameters.SerializeNullValues == false) continue;

                if (append) Append(',');
                WritePair(property.Name, o);
                if (o != null && jsonParameters.UseExtensions)
                {
                    var tt = o.GetType();
                    if (tt == typeof(object)) map.Add(property.Name, tt.ToString());
                }

                append = true;
            }
            if (map.Count > 0 && jsonParameters.UseExtensions)
            {
                Append(",\"$map\":");
                WriteStringDictionary(map);
            }
            currentDepth--;
            Append('}');
            currentDepth--;

        }

    	static object GetInstanceValue(object obj, Type t, Getters p) {
    		if (t.IsValueType && p.FieldInfo != null) {
				return p.FieldInfo.GetValue(obj);
			}
    		if (t.IsValueType && p.PropertyType != null) {
    			return t.GetProperty(p.Name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj, null);
    		}
    		return p.Getter(obj);
    	}

    	private void WritePairFast(string name, string value)
        {
            if ((value == null) && jsonParameters.SerializeNullValues == false)
                return;
            WriteStringFast(name);

            Append(':');

            WriteStringFast(value);
        }

        private void WritePair(string name, object value)
        {
            if ((value == null || value is DBNull) && jsonParameters.SerializeNullValues == false)
                return;
            WriteStringFast(name);

            Append(':');

            WriteValue(value);
        }

        private void WriteArray(IEnumerable array)
        {
            Append('[');

            bool pendingSeperator = false;

            foreach (object obj in array)
            {
                if (pendingSeperator) Append(',');

                WriteValue(obj);

                pendingSeperator = true;
            }
            Append(']');
        }

        private void WriteStringDictionary(IDictionary dic)
        {
            Append('{');

            bool pendingSeparator = false;

            foreach (DictionaryEntry entry in dic)
            {
                if (pendingSeparator) Append(',');

                WritePair((string)entry.Key, entry.Value);

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
        private void WriteStringFast(string s)
        {
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
