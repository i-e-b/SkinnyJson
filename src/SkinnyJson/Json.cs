using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SkinnyJson
{
    public delegate string Serialize(object data);
    public delegate object Deserialize(string data);

    public class JsonParameters
    {
// ReSharper disable RedundantDefaultFieldInitializer
        public bool UseOptimizedDatasetSchema = true;
        public bool UseFastGuid = true;
        public bool SerializeNullValues = true;
        public bool UseUtcDateTime = true;
        public bool ShowReadOnlyProperties = false;
        public bool UsingGlobalTypes = true;
        public bool IgnoreCaseOnDeserialize = false;
        public bool EnableAnonymousTypes = false;
        public bool UseExtensions = true;
// ReSharper restore RedundantDefaultFieldInitializer
    }

    public class Json
    {
		/// <summary> Turn an object into a JSON string </summary>
		public static string Freeze(object obj)
		{
            return Instance.ToJson(obj, DefaultParameters);
		}
		
		/// <summary> Turn a JSON string into a specific object </summary>
		public static object Defrost(string json)
		{
			return Instance.ToObject(json, null);
		}

		/// <summary> Turn a JSON string into a specific object </summary>
		public static T Defrost<T>(string json)
		{
			return (T)Instance.ToObject(json, typeof(T));
		}
		
		/// <summary> Create a copy of an object through serialisation </summary>
        public static T Clone<T>(T obj)
        {
            return Defrost<T>(Freeze(obj));
        }

		/// <summary>Read a JSON object into an anonymous .Net object</summary>
        public static object Parse(string json)
        {
            return new JsonParser(json, DefaultParameters.IgnoreCaseOnDeserialize).Decode();
        }

		/// <summary>Pretty print a JSON string</summary>
        public static string Beautify(string input)
        {
            return Formatter.PrettyPrint(input);
        }

		/// <summary>Fill the members of an .Net object from a JSON object string</summary>
		/// <param name="input"></param>
		/// <param name="json"></param>
		/// <returns></returns>
        public static object FillObject(object input, string json)
        {
            var ht = new JsonParser(json, DefaultParameters.IgnoreCaseOnDeserialize).Decode() as Dictionary<string, object>;
            return ht == null ? null : Instance.ParseDictionary(ht, null, input.GetType(), input);
        }


    	internal readonly static Json Instance = new Json();
        private Json(){}
        /// <summary>
        /// You can set these paramters globally for all calls
        /// </summary>
        public static JsonParameters DefaultParameters = new JsonParameters();
        private JsonParameters jsonParameters;

        internal string ToJson(object obj, JsonParameters param)
        {
            jsonParameters = param;
            // FEATURE : enable extensions when you can deserialize anon types
            if (jsonParameters.EnableAnonymousTypes) { jsonParameters.UseExtensions = false; jsonParameters.UsingGlobalTypes = false; }
            return new JsonSerializer(param).ConvertToJson(obj);
        }

        internal object ToObject(string json, Type type)
        {
            var ht = new JsonParser(json, DefaultParameters.IgnoreCaseOnDeserialize).Decode() as Dictionary<string, object>;
            return ht == null ? null : ParseDictionary(ht, null, type, null);
        }

    	readonly SafeDictionary<Type, string> tyname = new SafeDictionary<Type, string>();
        internal string GetTypeAssemblyName(Type t)
        {
            string val;
            if (tyname.TryGetValue(t, out val)) return val;

			if (t.BaseType == typeof(object))
				tyname.Add(t, (t.GetInterfaces().FirstOrDefault() ?? t).AssemblyQualifiedName);
			else
				tyname.Add(t, t.AssemblyQualifiedName);
        	
        	return tyname[t];
        }

    	readonly SafeDictionary<string, Type> typecache = new SafeDictionary<string, Type>();
        private Type GetTypeFromCache(string typename) {
			Type val;
			if (typecache.TryGetValue(typename, out val)) return val;
			var assemblyName = typename.Split(',')[1];
			var fullName = typename.Split(',')[0];
			var available = Assembly.Load(assemblyName).GetTypes();
			// ReSharper disable PossibleNullReferenceException
			var t = available.Single(type => type.FullName.ToLower() == fullName.ToLower());
			// ReSharper restore PossibleNullReferenceException
			typecache.Add(typename, t);
			return t;
		}

    	readonly SafeDictionary<Type, CreateObject> constrcache = new SafeDictionary<Type, CreateObject>();
        private delegate object CreateObject();
		private object FastCreateInstance(Type objtype)
        {
			if (objtype.IsInterface) return DynamicProxy.GetInstanceFor(objtype);
            try
            {
                CreateObject c;
                if (constrcache.TryGetValue(objtype, out c)) return c();
            	var dynMethod = new DynamicMethod("_", objtype, null);
            	var ilGen = dynMethod.GetILGenerator();

            	var constructorInfo = objtype.GetConstructor(Type.EmptyTypes);
				if (constructorInfo == null) throw new Exception("No constructor available, can't create type");
            	ilGen.Emit(OpCodes.Newobj, constructorInfo);
            	ilGen.Emit(OpCodes.Ret);
            	c = (CreateObject)dynMethod.CreateDelegate(typeof(CreateObject));
            	constrcache.Add(objtype, c);
            	return c();
            }
            catch (Exception exc)
            {
                throw new Exception(string.Format("Failed to fast create instance for type '{0}' from assemebly '{1}'",
                    objtype.FullName, objtype.AssemblyQualifiedName), exc);
            }
        }

        bool usingglobals;
        private object ParseDictionary(Dictionary<string, object> d, Dictionary<string, object> globaltypes, Type type, object input)
        {
            object tn;

            if (d.TryGetValue("$types", out tn))
            {
                usingglobals = true;
                globaltypes = ((Dictionary<string, object>) tn).ToDictionary<KeyValuePair<string, object>, string, object>(kv => (string) kv.Value, kv => kv.Key);
            }

            var found = d.TryGetValue("$type", out tn);
            if (found == false && type == typeof(Object))
            {
                return CreateDataset(d, globaltypes);
            }
            if (found)
            {
                if (usingglobals)
                {
                    object tname;
                    if (globaltypes.TryGetValue((string)tn, out tname)) tn = tname;
                }
                if (type == null || !type.IsInterface) type = GetTypeFromCache((string)tn);
            }

            var typename = type.FullName;
            var o = input ?? FastCreateInstance(type);
        	var props = GetProperties(type, typename);
            foreach (string n in d.Keys)
            {
                var name = n;
                if (jsonParameters.IgnoreCaseOnDeserialize) name = name.ToLower();
                if (name == "$map")
                {
                    ProcessMap(o, props, (Dictionary<string, object>)d[name]);
                    continue;
                }
                MyPropInfo pi;
                if (props.TryGetValue(name, out pi) == false)
                    continue;
            	if (!pi.filled) continue;
            	var v = d[name];

            	if (v == null) continue;
            	object oset;

            	if (pi.isInt) oset = (int)CreateLong((string)v);
            	else if (pi.isLong) oset = CreateLong((string)v);
            	else if (pi.isString) oset = v;
            	else if (pi.isBool) oset = (bool)v;
            	else if (pi.isGenericType && pi.isValueType == false && pi.isDictionary == false)
            		oset = CreateGenericList((ArrayList)v, pi.pt, pi.bt, globaltypes);
            	else if (pi.isByteArray)
            		oset = Convert.FromBase64String((string)v);

            	else if (pi.isArray && pi.isValueType == false)
            		oset = CreateArray((ArrayList)v, pi.bt, globaltypes);
            	else if (pi.isGuid)
            		oset = CreateGuid((string)v);
            	else if (pi.isDataSet)
            		oset = CreateDataset((Dictionary<string, object>)v, globaltypes);

            	else if (pi.isDataTable)
            		oset = CreateDataTable((Dictionary<string, object>)v, globaltypes);

            	else if (pi.isStringDictionary)
            		oset = CreateStringKeyDictionary((Dictionary<string, object>)v, pi.pt, pi.GenericTypes, globaltypes);

            	else if (pi.isDictionary || pi.isHashtable)
            		oset = CreateDictionary((ArrayList)v, pi.pt, pi.GenericTypes, globaltypes);

            	else if (pi.isEnum)
            		oset = CreateEnum(pi.pt, (string)v);

            	else if (pi.isDateTime)
            		oset = CreateDateTime((string)v);

            	else if (pi.isClass && v is Dictionary<string, object>)
            		oset = ParseDictionary((Dictionary<string, object>)v, globaltypes, pi.pt, null);

            	else if (pi.isValueType)
            		oset = ChangeType(v, pi.changeType);
            	else if (v is ArrayList)
            		oset = CreateArray((ArrayList)v, typeof(object), globaltypes);
            	else
            		oset = v;

            	if (pi.CanWrite)
            		pi.setter(o, oset);
            }
            return o;
        }

    	private struct MyPropInfo
        {
// ReSharper disable InconsistentNaming
            public bool filled;
            public Type pt;
            public Type bt;
            public Type changeType;
            public bool isDictionary;
            public bool isValueType;
            public bool isGenericType;
            public bool isArray;
            public bool isByteArray;
            public bool isGuid;
            public bool isDataSet;
            public bool isDataTable;
            public bool isHashtable;
            public GenericSetter setter;
            public bool isEnum;
            public bool isDateTime;
            public Type[] GenericTypes;
            public bool isInt;
            public bool isLong;
            public bool isString;
            public bool isBool;
            public bool isClass;
            public GenericGetter getter;
            public bool isStringDictionary;
// ReSharper restore InconsistentNaming
            public bool CanWrite;
        }

    	readonly SafeDictionary<string, SafeDictionary<string, MyPropInfo>> propertycache = new SafeDictionary<string, SafeDictionary<string, MyPropInfo>>();
        private SafeDictionary<string, MyPropInfo> GetProperties(Type type, string typename)
        {
            SafeDictionary<string, MyPropInfo> sd;
            if (propertycache.TryGetValue(typename, out sd)) return sd;
        	sd = new SafeDictionary<string, MyPropInfo>();

			var pr = new List<PropertyInfo>();

        	pr.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        	foreach (var iface in type.GetInterfaces())
        	{
        		pr.AddRange(iface.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        	}

        	foreach (var p in pr)
        	{
        		var d = CreateMyProp(p.PropertyType);
        		d.CanWrite = p.CanWrite;
        		d.setter = CreateSetMethod(p);
				if (d.setter == null) throw new Exception("Property "+p.Name+" has no setter");
        		d.getter = CreateGetMethod(p);
        		sd.Add(p.Name, d);
        	}

			
			var fi = new List<FieldInfo>();
        	fi.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
        	foreach (var iface in type.GetInterfaces())
        	{
        		fi.AddRange(iface.GetFields(BindingFlags.Public | BindingFlags.Instance));
        	}

        	foreach (var f in fi)
        	{
        		var d = CreateMyProp(f.FieldType);
        		d.setter = CreateSetField(type, f);
        		d.getter = CreateGetField(type, f);
        		sd.Add(f.Name, d);
        	}

        	propertycache.Add(typename, sd);
        	return sd;
        }

        private static MyPropInfo CreateMyProp(Type t)
        {
        	var d = new MyPropInfo {filled = true, CanWrite = true, pt = t, isDictionary = t.Name.Contains("Dictionary")};
        	if (d.isDictionary)
                d.GenericTypes = t.GetGenericArguments();
            d.isValueType = t.IsValueType;
            d.isGenericType = t.IsGenericType;
            d.isArray = t.IsArray;
            if (d.isArray) d.bt = t.GetElementType();
            if (d.isGenericType) d.bt = t.GetGenericArguments()[0];
            d.isByteArray = t == typeof(byte[]);
            d.isGuid = (t == typeof(Guid) || t == typeof(Guid?));
            d.isHashtable = t == typeof(Hashtable);
            d.isDataSet = t == typeof(DataSet);
            d.isDataTable = t == typeof(DataTable);

            d.changeType = GetChangeType(t);
            d.isEnum = t.IsEnum;
            d.isDateTime = t == typeof(DateTime) || t == typeof(DateTime?);
            d.isInt = t == typeof(int) || t == typeof(int?);
            d.isLong = t == typeof(long) || t == typeof(long?);
            d.isString = t == typeof(string);
            d.isBool = t == typeof(bool) || t == typeof(bool?);
            d.isClass = t.IsClass;

            if (d.isDictionary && d.GenericTypes.Length > 0 && d.GenericTypes[0] == typeof(string))
                d.isStringDictionary = true;
            return d;
        }

        private delegate void GenericSetter(object target, object value);

        private static GenericSetter CreateSetMethod(PropertyInfo propertyInfo)
        {
            var setMethod = propertyInfo.GetSetMethod(true);
            if (setMethod == null) return null;
        	if (propertyInfo.DeclaringType == null) return null;

            var arguments = new Type[2];
            arguments[0] = arguments[1] = typeof(object);

            var setter = new DynamicMethod("_", typeof(void), arguments, true);
            var il = setter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
        	il.Emit(OpCodes.Ldarg_1);

        	il.Emit(propertyInfo.PropertyType.IsClass ? OpCodes.Castclass : OpCodes.Unbox_Any, propertyInfo.PropertyType);

        	il.EmitCall(OpCodes.Callvirt, setMethod, null);
            il.Emit(OpCodes.Ret);

            return (GenericSetter)setter.CreateDelegate(typeof(GenericSetter));
        }

        internal delegate object GenericGetter(object obj);

        private static GenericGetter CreateGetField(Type type, FieldInfo fieldInfo)
        {
            var dynamicGet = new DynamicMethod("_", typeof(object), new[] { typeof(object) }, type, true);
            var il = dynamicGet.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fieldInfo);
            if (fieldInfo.FieldType.IsValueType) il.Emit(OpCodes.Box, fieldInfo.FieldType);
            il.Emit(OpCodes.Ret);

            return (GenericGetter)dynamicGet.CreateDelegate(typeof(GenericGetter));
        }

        private static GenericSetter CreateSetField(Type type, FieldInfo fieldInfo)
        {
            var arguments = new Type[2];
            arguments[0] = arguments[1] = typeof(object);

            var dynamicSet = new DynamicMethod("_", typeof(void), arguments, type, true);
            var il = dynamicSet.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            if (fieldInfo.FieldType.IsValueType) il.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
            il.Emit(OpCodes.Stfld, fieldInfo);
            il.Emit(OpCodes.Ret);

            return (GenericSetter)dynamicSet.CreateDelegate(typeof(GenericSetter));
        }

        private static GenericGetter CreateGetMethod(PropertyInfo propertyInfo)
        {
            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null) return null;
			if (propertyInfo.DeclaringType == null) return null;

            var arguments = new Type[1];
            arguments[0] = typeof(object);

            var getter = new DynamicMethod("_", typeof(object), arguments, true);
            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
        	il.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
        	il.EmitCall(OpCodes.Callvirt, getMethod, null);

            if (!propertyInfo.PropertyType.IsClass)
                il.Emit(OpCodes.Box, propertyInfo.PropertyType);

            il.Emit(OpCodes.Ret);

            return (GenericGetter)getter.CreateDelegate(typeof(GenericGetter));
        }

        readonly SafeDictionary<Type, List<Getters>> getterscache = new SafeDictionary<Type, List<Getters>>();
        internal List<Getters> GetGetters(Type type)
        {
            List<Getters> val;
            if (getterscache.TryGetValue(type, out val)) return val;

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var getters = (from p in props where p.CanWrite || jsonParameters.ShowReadOnlyProperties || jsonParameters.EnableAnonymousTypes
						   let att = p.GetCustomAttributes(typeof (System.Xml.Serialization.XmlIgnoreAttribute), false)
						   where att.Length <= 0 let g = CreateGetMethod(p) where g != null 
						   
						   select new Getters {Name = p.Name, Getter = g, PropertyType = p.PropertyType}).ToList();

        	FieldInfo[] fi = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var f in fi)
            {
                var att = f.GetCustomAttributes(typeof(System.Xml.Serialization.XmlIgnoreAttribute), false);
                if (att.Length > 0)
                    continue;

                var g = CreateGetField(type, f);
            	if (g == null) continue;
            	var gg = new Getters {Name = f.Name, Getter = g, PropertyType = f.FieldType};
            	getters.Add(gg);
            }

            getterscache.Add(type, getters);
            return getters;
        }

        private static object ChangeType(object value, Type conversionType)
        {
            if (conversionType == typeof(int)) return (int)CreateLong((string)value);
        	if (conversionType == typeof(long)) return CreateLong((string)value);
        	if (conversionType == typeof(string)) return value;
        	if (conversionType == typeof(Guid)) return CreateGuid((string)value);
        	if (conversionType.IsEnum) return CreateEnum(conversionType, (string)value);
        	return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
        }

    	private static void ProcessMap(object obj, SafeDictionary<string, MyPropInfo> props, Dictionary<string, object> dic)
        {
            foreach (var kv in dic)
            {
                var p = props[kv.Key];
                var o = p.getter(obj);
                var t = Type.GetType((string)kv.Value);
                if (t == typeof(Guid)) p.setter(obj, CreateGuid((string)o));
            }
        }

        private static long CreateLong(IEnumerable<char> s)
        {
            long num = 0;
            var neg = false;
            foreach (char cc in s)
            {
                switch (cc)
                {
                	case '-':
                		neg = true;
                		break;
                	case '+':
                		neg = false;
                		break;
                	default:
                		num *= 10;
                		num += cc - '0';
                		break;
                }
            }

            return neg ? -num : num;
        }

        private static object CreateEnum(Type pt, string v)
        {
            return Enum.Parse(pt, v);
        }

        private static Guid CreateGuid(string s)
        {
        	return s.Length > 30 ? new Guid(s) : new Guid(Convert.FromBase64String(s));
        }

    	private static DateTime CreateDateTime(string value)
        {
			if (value.EndsWith("Z")) return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ssZ", null).ToLocalTime();
			return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss", null);
		}

        private object CreateArray(IEnumerable data, Type bt, Dictionary<string, object> globalTypes)
        {
            var col = new ArrayList();
            foreach (var ob in data)
            {
                if (ob is IDictionary)
                    col.Add(ParseDictionary((Dictionary<string, object>)ob, globalTypes, bt, null));
                else
                    col.Add(ChangeType(ob, bt));
            }
            return col.ToArray(bt);
        }


        private object CreateGenericList(IEnumerable data, Type pt, Type bt, Dictionary<string, object> globalTypes)
        {
            var col = (IList)FastCreateInstance(pt);
            foreach (var ob in data)
            {
                if (ob is IDictionary)
                    col.Add(ParseDictionary((Dictionary<string, object>)ob, globalTypes, bt, null));
                else if (ob is ArrayList)
                    col.Add(((ArrayList)ob).ToArray());
                else
                    col.Add(ChangeType(ob, bt));
            }
            return col;
        }

        private object CreateStringKeyDictionary(Dictionary<string, object> reader, Type pt, IList<Type> types, Dictionary<string, object> globalTypes)
        {
            var col = (IDictionary)FastCreateInstance(pt);
        	Type t2 = null;
            if (types != null) t2 = types[1];

            foreach (var values in reader)
            {
                var key = values.Key;
                object val;
                if (values.Value is Dictionary<string, object>)
                    val = ParseDictionary((Dictionary<string, object>)values.Value, globalTypes, t2, null);
                else
                    val = ChangeType(values.Value, t2);
                col.Add(key, val);
            }

            return col;
        }

        private object CreateDictionary(IEnumerable reader, Type pt, IList<Type> types, Dictionary<string, object> globalTypes)
        {
            var col = (IDictionary)FastCreateInstance(pt);
            Type t1 = null;
            Type t2 = null;
            if (types != null)
            {
                t1 = types[0];
                t2 = types[1];
            }

            foreach (Dictionary<string, object> values in reader)
            {
                object key = values["k"];
                object val = values["v"];

                if (key is Dictionary<string, object>)
                    key = ParseDictionary((Dictionary<string, object>)key, globalTypes, t1, null);
                else
                    key = ChangeType(key, t1);

                if (val is Dictionary<string, object>)
                    val = ParseDictionary((Dictionary<string, object>)val, globalTypes, t2, null);
                else
                    val = ChangeType(val, t2);

                col.Add(key, val);
            }

            return col;
        }

        private static Type GetChangeType(Type conversionType)
        {
            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return conversionType.GetGenericArguments()[0];

            return conversionType;
        }
        private DataSet CreateDataset(Dictionary<string, object> reader, Dictionary<string, object> globalTypes)
        {
        	var ds = new DataSet {EnforceConstraints = false};
        	ds.BeginInit();

            // read dataset schema here
            ReadSchema(reader, ds, globalTypes);

            foreach (var pair in reader)
            {
                if (pair.Key == "$type" || pair.Key == "$schema") continue;

                var rows = (ArrayList)pair.Value;
                if (rows == null) continue;

                var dt = ds.Tables[pair.Key];
                ReadDataTable(rows, dt);
            }

            ds.EndInit();

            return ds;
        }

        private void ReadSchema(IDictionary<string, object> reader, DataSet ds, Dictionary<string, object> globalTypes)
        {
            var schema = reader["$schema"];

            if (schema is string)
            {
                TextReader tr = new StringReader((string)schema);
                ds.ReadXmlSchema(tr);
            }
            else
            {
                var ms = (DatasetSchema)ParseDictionary((Dictionary<string, object>)schema, globalTypes, typeof(DatasetSchema), null);
                ds.DataSetName = ms.Name;
                for (int i = 0; i < ms.Info.Count; i += 3)
                {
                    if (ds.Tables.Contains(ms.Info[i]) == false)
                        ds.Tables.Add(ms.Info[i]);
                	var type = Type.GetType(ms.Info[i + 2]);
					if (type == null) continue;
                	ds.Tables[ms.Info[i]].Columns.Add(ms.Info[i + 1], type);
                }
            }
        }

        private void ReadDataTable(IEnumerable rows, DataTable dt)
        {
            dt.BeginInit();
            dt.BeginLoadData();
            var guidcols = new List<int>();
            var datecol = new List<int>();

            foreach (DataColumn c in dt.Columns)
            {
                if (c.DataType == typeof(Guid) || c.DataType == typeof(Guid?))
                    guidcols.Add(c.Ordinal);
                if (jsonParameters.UseUtcDateTime && (c.DataType == typeof(DateTime) || c.DataType == typeof(DateTime?)))
                    datecol.Add(c.Ordinal);
            }

            foreach (ArrayList row in rows)
            {
                var v = new object[row.Count];
                row.CopyTo(v, 0);
                foreach (int i in guidcols)
                {
                    var s = (string)v[i];
                    if (s != null && s.Length < 36)
                        v[i] = new Guid(Convert.FromBase64String(s));
                }
                if (jsonParameters.UseUtcDateTime)
                {
                    foreach (int i in datecol)
                    {
                        var s = (string)v[i];
                        if (s != null)
                            v[i] = CreateDateTime(s);
                    }
                }
                dt.Rows.Add(v);
            }

            dt.EndLoadData();
            dt.EndInit();
        }

        DataTable CreateDataTable(Dictionary<string, object> reader, Dictionary<string, object> globalTypes)
        {
            var dt = new DataTable();

            // read dataset schema here
            var schema = reader["$schema"];

            if (schema is string)
            {
                TextReader tr = new StringReader((string)schema);
                dt.ReadXmlSchema(tr);
            }
            else
            {
                var ms = (DatasetSchema)ParseDictionary((Dictionary<string, object>)schema, globalTypes, typeof(DatasetSchema), null);
                dt.TableName = ms.Info[0];
                for (int i = 0; i < ms.Info.Count; i += 3)
                {
                	var type = Type.GetType(ms.Info[i + 2]);
					if (type == null) continue;
                	dt.Columns.Add(ms.Info[i + 1], type);
                }
            }

            foreach (var pair in reader)
            {
                if (pair.Key == "$type" || pair.Key == "$schema")
                    continue;

                var rows = (ArrayList)pair.Value;
                if (rows == null)
                    continue;

                if (!dt.TableName.Equals(pair.Key, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                ReadDataTable(rows, dt);
            }

            return dt;
        }
    }

}