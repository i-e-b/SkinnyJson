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
        private readonly StringBuilder output = new StringBuilder();
    	const int MaxDepth = 10;
    	int currentDepth;
        private readonly Dictionary<string, int> globalTypes = new Dictionary<string, int>();
        private readonly JsonParameters jsonParameters;

        public JsonSerializer(JsonParameters param)
        {
            jsonParameters = param;
        }

        public string ConvertToJson(object obj)
        {
            WriteValue(obj);

            string str;
            if (jsonParameters.UsingGlobalTypes)
            {
                var sb = new StringBuilder();
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
                str = output.Replace("$types$", sb.ToString()).ToString();
            }
            else
                str = output.ToString();

            return str;
        }

		/// <summary>
		/// This is the root of the serialiser.
		/// </summary>
        private void WriteValue(object obj)
        {
            if (obj == null || obj is DBNull)
                output.Append("null");

            else if (obj is string || obj is char)
                WriteString((string)obj);

            else if (obj is Guid)
                WriteGuid((Guid)obj);

            else if (obj is bool)
                output.Append(((bool)obj) ? "true" : "false"); // conform to standard

            else if (isNumericPrimitive(obj))
                output.Append(((IConvertible)obj).ToString(NumberFormatInfo.InvariantInfo));

            else if (obj is DateTime)
                WriteDateTime((DateTime)obj);

            else if (obj is IDictionary && obj.GetType().IsGenericType && obj.GetType().GetGenericArguments()[0] == typeof(string))
                WriteStringDictionary((IDictionary)obj);

            else if (obj is IDictionary)
                WriteDictionary((IDictionary)obj);
            else if (obj is DataSet)
                WriteDataset((DataSet)obj);

            else if (obj is DataTable)
                WriteDataTable((DataTable)obj);
            else if (obj is byte[])
                WriteBytes((byte[])obj);

            else if (obj is Array || obj is IList || obj is ICollection)
                WriteArray((IEnumerable)obj);

            else if (obj is IEnumerable)
                WriteArray((IEnumerable)obj);

            else if (obj is Enum)
                WriteEnum((Enum)obj);
            else
                WriteObject(obj);
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
            output.Append("\"");
        	output.Append(
				jsonParameters.UseUtcDateTime
					? dateTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ssZ")
					: dateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        	output.Append("\"");

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
            output.Append('{');
            if ( jsonParameters.UseExtensions)
            {
                WritePair("$schema", jsonParameters.UseOptimizedDatasetSchema ? (object)GetSchema(ds) : ds.GetXmlSchema());
                output.Append(',');
            }
            bool tablesep = false;
            foreach (DataTable table in ds.Tables)
            {
                if (tablesep) output.Append(",");
                tablesep = true;
                WriteDataTableData(table);
            }
            output.Append('}');
        }

        private void WriteDataTableData(DataTable table)
        {
            output.Append('\"');
            output.Append(table.TableName);
            output.Append("\":[");
            DataColumnCollection cols = table.Columns;
            bool rowseparator = false;
            foreach (DataRow row in table.Rows)
            {
                if (rowseparator) output.Append(",");
                rowseparator = true;
                output.Append('[');

                bool pendingSeperator = false;
                foreach (DataColumn column in cols)
                {
                    if (pendingSeperator) output.Append(',');
                    WriteValue(row[column]);
                    pendingSeperator = true;
                }
                output.Append(']');
            }

            output.Append(']');
        }

        void WriteDataTable(DataTable dt)
        {
            output.Append('{');
            if (jsonParameters.UseExtensions)
            {
                WritePair("$schema", jsonParameters.UseOptimizedDatasetSchema ? (object)GetSchema(dt) : GetXmlSchema(dt));
                output.Append(',');
            }

            WriteDataTableData(dt);
            output.Append('}');
        }

        bool typesWritten;
        private void WriteObject(object obj)
        {
            if (jsonParameters.UsingGlobalTypes == false) output.Append('{');
			else output.Append(typesWritten == false ? "{$types$" : "{");

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
                if ((o != null && !(o is DBNull)) || jsonParameters.SerializeNullValues != false)
                {
                    if (append) output.Append(',');
                    WritePair(property.Name, o);
                    if (o != null && jsonParameters.UseExtensions)
                    {
                        var tt = o.GetType();
                        if (tt == typeof(Object))
                            map.Add(property.Name, tt.ToString());
                    }

                    append = true;
                }
            }
            if (map.Count > 0 && jsonParameters.UseExtensions)
            {
                output.Append(",\"$map\":");
                WriteStringDictionary(map);
            }
            currentDepth--;
            output.Append('}');
            currentDepth--;

        }

    	static object GetInstanceValue(object obj, Type t, Getters p) {
    		if (t.IsValueType && p.FieldInfo != null) {
				return p.FieldInfo.GetValue(obj);
			}
    		if (t.IsValueType && p.PropertyType != null) {
    			return t.GetProperty(p.Name, BindingFlags.Public | BindingFlags.Instance).GetValue(obj, null);
    		}
    		return p.Getter(obj);
    	}

    	private void WritePairFast(string name, string value)
        {
            if ((value == null) && jsonParameters.SerializeNullValues == false)
                return;
            WriteStringFast(name);

            output.Append(':');

            WriteStringFast(value);
        }

        private void WritePair(string name, object value)
        {
            if ((value == null || value is DBNull) && jsonParameters.SerializeNullValues == false)
                return;
            WriteStringFast(name);

            output.Append(':');

            WriteValue(value);
        }

        private void WriteArray(IEnumerable array)
        {
            output.Append('[');

            bool pendingSeperator = false;

            foreach (object obj in array)
            {
                if (pendingSeperator) output.Append(',');

                WriteValue(obj);

                pendingSeperator = true;
            }
            output.Append(']');
        }

        private void WriteStringDictionary(IDictionary dic)
        {
            output.Append('{');

            bool pendingSeparator = false;

            foreach (DictionaryEntry entry in dic)
            {
                if (pendingSeparator) output.Append(',');

                WritePair((string)entry.Key, entry.Value);

                pendingSeparator = true;
            }
            output.Append('}');
        }

        private void WriteDictionary(IDictionary dic)
        {
            output.Append('[');

            bool pendingSeparator = false;

            foreach (DictionaryEntry entry in dic)
            {
                if (pendingSeparator) output.Append(',');
                output.Append('{');
                WritePair("k", entry.Key);
                output.Append(",");
                WritePair("v", entry.Value);
                output.Append('}');

                pendingSeparator = true;
            }
            output.Append(']');
        }

        private void WriteStringFast(string s)
        {
            output.Append('\"');
            output.Append(s);
            output.Append('\"');
        }

        private void WriteString(string s)
        {
            output.Append('\"');

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
                    output.Append(s, runIndex, index - runIndex);
                    runIndex = -1;
                }

                switch (c)
                {
                    case '\t': output.Append("\\t"); break;
                    case '\r': output.Append("\\r"); break;
                    case '\n': output.Append("\\n"); break;
                    case '"':
                    case '\\': output.Append('\\'); output.Append(c); break;
                    default:
                        output.Append("\\u");
                        output.Append(((int)c).ToString("X4", NumberFormatInfo.InvariantInfo));
                        break;
                }
            }

            if (runIndex != -1)
            {
                output.Append(s, runIndex, s.Length - runIndex);
            }

            output.Append('\"');
        }
    }
}
