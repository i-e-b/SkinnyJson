using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;

namespace SkinnyJson
{
    /// <summary>
    /// Parameters for serialising and deserialising.
    /// </summary>
    public class JsonParameters
    {
// ReSharper disable RedundantDefaultFieldInitializer
        /// <summary>
        /// Use a special format for Sql Datasets. Default true
        /// </summary>
        public bool UseOptimizedDatasetSchema = true;

        /// <summary>
        /// Use Base64 encoding for Guids. If false, uses Hex.
        /// Default true
        /// </summary>
        public bool UseFastGuid = true;

        /// <summary>
        /// Insert null values into JSON output. Otherwise remove field.
        /// Default true
        /// </summary>
        public bool SerializeNullValues = true;

        /// <summary>
        /// Force datetimes to UTC. Default true
        /// </summary>
        public bool UseUtcDateTime = true;

        /// <summary>
        /// Serialise properties that can't be written on deserialise. Default false
        /// </summary>
        public bool ShowReadOnlyProperties = false;

        /// <summary>
        /// Declare types once at the start of a document. Otherwise declare in each object.
        /// Default true, but overridden by `EnableAnonymousTypes`
        /// </summary>
        public bool UsingGlobalTypes = true;

        /// <summary>
        /// Allow case insensitive matching on deserialise. Default false
        /// </summary>
        public bool IgnoreCaseOnDeserialize = false;

        /// <summary>
        /// Default true. If false, source type information will be included in serialised output.<para></para>
        /// Sets `UseExtensions` and `UsingGlobalTypes` to false.
        /// Directly serialising an anonymous type will use these settings for that call, without needing a global setting.
        /// </summary>
        public bool EnableAnonymousTypes = true;

        /// <summary>
        /// Add type and schema information to output JSON, using $type, $types, $schema and $map properties.
        /// Default true, but overridden by `EnableAnonymousTypes`
        /// </summary>
        public bool UseExtensions = true;
// ReSharper restore RedundantDefaultFieldInitializer

        internal JsonParameters Clone()
        {
            return new JsonParameters { 
                EnableAnonymousTypes = EnableAnonymousTypes,
                IgnoreCaseOnDeserialize = IgnoreCaseOnDeserialize,
                SerializeNullValues = SerializeNullValues,
                ShowReadOnlyProperties = ShowReadOnlyProperties,
                UseExtensions = UseExtensions,
                UseFastGuid = UseFastGuid,
                UseOptimizedDatasetSchema = UseOptimizedDatasetSchema,
                UseUtcDateTime = UseUtcDateTime,
                UsingGlobalTypes = UsingGlobalTypes
            };
        }
    }

    /// <summary>
    /// SkinnyJson entry point
    /// </summary>
    public class Json
    {
		/// <summary> Turn an object into a JSON string </summary>
		public static string Freeze(object obj)
		{
            if (obj is DynamicWrapper dyn) {
                return Freeze(dyn.Parsed);
            }
            if (IsAnonymousType(obj))
            { // If we are passed an anon type, turn off type information -- it will all be junk.
                var jsonParameters = DefaultParameters.Clone();
                jsonParameters.UseExtensions = false; 
                jsonParameters.UsingGlobalTypes = false;
                jsonParameters.EnableAnonymousTypes = true;

                return Instance.ToJson(obj, jsonParameters);
            }
            return Instance.ToJson(obj, DefaultParameters);
		}

        /// <summary> Turn a JSON string into a detected object </summary>
		public static object Defrost(string json)
		{
			return Instance.ToObject(json, null);
		}

		/// <summary> Return the type name that SkinnyJson will use for the serialising the object </summary>
    	public static string WrapperType(object obj)
    	{
			if (obj is Type type) return Instance.GetTypeAssemblyName(type);
    		return Instance.GetTypeAssemblyName(obj.GetType());
    	}

		/// <summary> Turn a JSON string into a specific object </summary>
		public static T Defrost<T>(string json)
		{
			return (T)Instance.ToObject(json, typeof(T));
		}

        /// <summary> Turn a JSON string into an object containing properties found </summary>
        public static dynamic DefrostDynamic(string json)
        {
            return new DynamicWrapper(Parse(json));
        }
        
        /// <summary>
        /// Turn a sub-path of a JSON document into an enumeration of values, by specific type
        /// </summary>
        /// <remarks>This is intended to extract useful fragments from repository-style files</remarks>
        /// <typeparam name="T">Type of the fragments to be returned</typeparam>
        /// <param name="path">Dotted path through document. If the path can't be found, an empty enumeration will be returned.
        /// An empty path is equivalent to `Defrost&lt;T&gt;`</param>
        /// <param name="json">The JSON document string to read</param>
        public static IEnumerable<T> DefrostFromPath<T>(string path, string json)
        {
            if (string.IsNullOrWhiteSpace(path)) {
                return new[] { Defrost<T>(json) };
            }

            return null;
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
        
        /// <summary>
        /// Deserialise a string, perform some edits then reform as a new string
        /// </summary>
        public static string Edit(string json, Action<dynamic> editAction)
        {
            DynamicWrapper dyn = DefrostDynamic(json);
            editAction(dyn);
            return Freeze(dyn);
        }

		/// <summary>Pretty print a JSON string. This is done without value parsing.</summary>
        public static string Beautify(string input)
        {
            return Formatter.PrettyPrint(input);
        }
        
        /// <summary>
        /// Pretty print a JSON data stream to another stream.
        /// This is done without value parsing or buffering, so very large streams can be processed.
        /// The input and output encodings can be the same or different.
        /// </summary>
        public static void BeautifyStream(Stream input, Encoding inputEncoding, Stream output, Encoding outputEncoding)
        {
            Formatter.PrettyStream(input, inputEncoding, output, outputEncoding);
        }

		/// <summary>Fill the members of an .Net object from a JSON object string</summary>
        public static object FillObject(object input, string json)
        {
            var ht = new JsonParser(json, DefaultParameters.IgnoreCaseOnDeserialize).Decode() as Dictionary<string, object>;
            return ht == null ? null : Instance.ParseDictionary(ht, null, input.GetType(), input);
        }

    	internal static readonly Json Instance = new Json();
        private Json(){}

        
        static bool IsAnonymousType(object obj)
        {
            return obj.GetType().Name.StartsWith("<>f");
        }

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        /// <summary>
        /// You can set these paramters globally for all calls
        /// </summary>
        public static JsonParameters DefaultParameters = new JsonParameters();
        private JsonParameters jsonParameters;

        internal string ToJson(object obj, JsonParameters param)
        {
            jsonParameters = param.Clone();
            if (jsonParameters.EnableAnonymousTypes) { jsonParameters.UseExtensions = false; jsonParameters.UsingGlobalTypes = false; }
            return new JsonSerializer(jsonParameters).ConvertToJson(obj);
        }

        internal object ToObject(string json, Type type)
        {
			jsonParameters = jsonParameters ?? DefaultParameters;
			var globalTypes = new Dictionary<string, object>();
			
			var decodedObject = new JsonParser(json, DefaultParameters.IgnoreCaseOnDeserialize).Decode();
			if (decodedObject is Dictionary<string, object> objects)
	        {
				return ParseDictionary(objects, globalTypes, type, null);
	        }

	        if (decodedObject is ArrayList arrayList)
	        {
                if (type.IsArray) {
                    return arrayList.ToArray(type.GetElementType() ?? typeof(object));
                }

	            var containedType = type.GetGenericArguments().Single();
	            var list = (IList)Activator.CreateInstance(GenericListType(containedType));
	            foreach (var obj in arrayList)
	            {
	                list.Add(ParseDictionary((Dictionary<string, object>)obj, globalTypes, containedType, null));
	            }
	            return list;
	        }

	        throw new Exception("Don't understand this JSON");
        }

	    Type GenericListType(Type containedType)
	    {
			var d1 = typeof(List<>);
			Type[] typeArgs = { containedType };
			return d1.MakeGenericType(typeArgs);
	    }

	    readonly SafeDictionary<Type, string> tyname = new SafeDictionary<Type, string>();
        internal string GetTypeAssemblyName(Type t)
        {
            string val;
            if (tyname.TryGetValue(t, out val)) return val;

        	string name;
			if (t.BaseType == typeof(object)) {
				// on Windows, this can be just "t.GetInterfaces()" but Mono doesn't return in order.
				var interfaceType = t.GetInterfaces().FirstOrDefault(i => !t.GetInterfaces().Any(i2 => i2.GetInterfaces().Contains(i)));
				name = ShortenName((interfaceType ?? t).AssemblyQualifiedName);
			} else {
				name = ShortenName(t.AssemblyQualifiedName);
			}

        	tyname.Add(t, name);

        	return tyname[t];
        }

    	static string ShortenName(string assemblyQualifiedName) {
			var one = assemblyQualifiedName.IndexOf(',');
			var two = assemblyQualifiedName.IndexOf(',', one+1);
			return assemblyQualifiedName.Substring(0, two);
    	}

    	readonly SafeDictionary<string, Type> typecache = new SafeDictionary<string, Type>();
        private Type GetTypeFromCache(string typename) {
			Type val;
			if (typecache.TryGetValue(typename, out val)) return val;

			var typeParts = typename.Split(',');

			Type t;
			if (typeParts.Length > 1) {
				var assemblyName = typename.Split(',')[1];
				var fullName = typename.Split(',')[0];
				var available = Assembly.Load(assemblyName).GetTypes();
				// ReSharper disable PossibleNullReferenceException
				t = available.SingleOrDefault(type => type.FullName.ToLower() == fullName.ToLower());
				// ReSharper restore PossibleNullReferenceException
			} else {
				// slow but robust way of finding a type fragment.
				t = AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(asm => asm.GetTypes())
					.SingleOrDefault(type => type.FullName?.StartsWith(typeParts[0]) ?? false);
			}
        	if (t != null) {
				typecache.Add(typename, t);
			}
			return t;
		}

    	readonly SafeDictionary<Type, CreateObject> constrcache = new SafeDictionary<Type, CreateObject>();
        private delegate object CreateObject();
		private object FastCreateInstance(Type objtype)
        {
            if (objtype == null) return null;
            if (objtype.IsInterface) {
                if (objtype.Namespace == "System.Collections.Generic") {
                    // Make a standard type...
                    // TODO: improve this!
                    Type d1 = typeof(Dictionary<,>);
                    Type[] typeParameters = objtype.GetGenericArguments();
                    Type constructed = d1.MakeGenericType(typeParameters);
                    object o = Activator.CreateInstance(constructed);
                    return o;
                }
                return DynamicProxy.GetInstanceFor(objtype);
            }
            if (objtype.IsValueType) return FormatterServices.GetUninitializedObject(objtype);
            if (constrcache.TryGetValue(objtype, out var cc)) return cc();
            
            try
            {
            	var constructorInfo = objtype.GetConstructor(Type.EmptyTypes);
				if (constructorInfo == null) //throw new Exception("No constructor available, can't create type");
                {
                    return SlowCreateInstance(objtype);
                }

                var dynMethod = new DynamicMethod("_", objtype, null);
            	var ilGen = dynMethod.GetILGenerator();

            	ilGen.Emit(OpCodes.Newobj, constructorInfo);
            	ilGen.Emit(OpCodes.Ret);
            	var c = (CreateObject)dynMethod.CreateDelegate(typeof(CreateObject));
            	constrcache.Add(objtype, c);
            	return c();
            }
            catch (Exception exc)
            {
                throw new Exception(string.Format("Failed to fast create instance for type '{0}' from assembly '{1}'",
                    objtype.FullName, objtype.AssemblyQualifiedName), exc);
            }
        }

        private object SlowCreateInstance(Type objtype)
        {
            var allCtor = objtype.GetConstructors();
            if (allCtor.Length < 1) {
                throw new Exception($"Failed to create instance for type '{objtype.FullName}' from assembly '{objtype.AssemblyQualifiedName}'. No constructors found.");
            }
            
            var ptypes = allCtor[0].GetParameters().Select(p=>p.ParameterType).ToArray();
            var pinsts = ptypes.Select(FastCreateInstance).ToArray();
            var constructorInfo = objtype.GetConstructor(ptypes);
            return constructorInfo?.Invoke(pinsts);
        }

        bool usingGlobals;

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

        readonly SafeDictionary<Type, List<Getters>> getterCache = new SafeDictionary<Type, List<Getters>>();
    	readonly SafeDictionary<string, SafeDictionary<string, MyPropInfo>> propertyCache = new SafeDictionary<string, SafeDictionary<string, MyPropInfo>>();
        
        internal delegate object GenericGetter(object obj);

        /// <param name="target">object instance to accept the value</param>
        /// <param name="value">value of property to set</param>
        /// <param name="key">optional key for dictionaries</param>
        private delegate void GenericSetter(object target, object value, object key);


        private object ParseDictionary(IDictionary<string, object> jsonValues, IDictionary<string, object> globaltypes, Type type, object input)
        {
            object tn;

            if (jsonValues.TryGetValue("$types", out tn))
            {
				var dic = ((Dictionary<string, object>) tn);
	            foreach (var kvp in dic)
	            {
		            globaltypes.Add((string)kvp.Value, kvp.Key);
	            }
				
				usingGlobals = true;
            }

            var found = jsonValues.TryGetValue("$type", out tn);
            if (found == false && type == typeof(object))
            {
                return CreateDataset(jsonValues, globaltypes);
            }
            if (found)
            {
                if (usingGlobals)
                {
                    object tname;
                    if (globaltypes.TryGetValue((string)tn, out tname)) tn = tname;
                }
                if (type == null || !type.IsInterface) type = GetTypeFromCache((string)tn);
            }

            var targetObject = input ?? FastCreateInstance(type);

            if (targetObject == null) return jsonValues; // can't work out what object to fill, send back the raw values

			var props = GetProperties(targetObject.GetType(), targetObject.GetType().Name);
            foreach (var key in jsonValues.Keys)
            {
                MapJsonValueToObject(key, targetObject, jsonValues, globaltypes, props);
            }
            return targetObject;
        }

    	void MapJsonValueToObject(string objectKey, object targetObject, IDictionary<string, object> jsonValues, IDictionary<string, object> globaltypes, SafeDictionary<string, MyPropInfo> props)
    	{
    		var name = objectKey;
    		if (jsonParameters.IgnoreCaseOnDeserialize) name = name.ToLower();
    		if (name == "$map")
    		{
    			ProcessMap(targetObject, props, (Dictionary<string, object>) jsonValues[name]);
    			return;
    		}
    		MyPropInfo pi;
    		if (props.TryGetValue(name, out pi) == false) {
                if (targetObject is IDictionary) {
                    var ok = props.TryGetValue("Item", out pi);
                    pi.isDictionary = true;
                    if (!ok) return;
                } else return;
            }
    		if (!pi.filled) return;
    		var v = jsonValues[name];

    		if (v == null) return;
    		object oset;

    		if (pi.isInt) oset = (int) CreateLong((string) v);
    		else if (pi.isLong) oset = CreateLong((string) v);
    		else if (pi.isString) oset = v;
    		else if (pi.isBool) oset = (bool) v;
    		else if (pi.isGenericType && pi.isValueType == false && pi.isDictionary == false)
    			oset = CreateGenericList((ArrayList) v, pi.pt, pi.bt, globaltypes);
    		else if (pi.isByteArray)
    			oset = Convert.FromBase64String((string) v);

    		else if (pi.isArray && pi.isValueType == false)
    			oset = CreateArray((ArrayList) v, pi.bt, globaltypes);
    		else if (pi.isGuid)
    			oset = CreateGuid((string) v);
    		else if (pi.isDataSet)
    			oset = CreateDataset((Dictionary<string, object>) v, globaltypes);

    		else if (pi.isDataTable)
    			oset = CreateDataTable((Dictionary<string, object>) v, globaltypes);

    		else if (pi.isStringDictionary)
    			oset = CreateStringKeyDictionary((Dictionary<string, object>) v, pi.pt, pi.GenericTypes, globaltypes);

    		else if (pi.isDictionary || pi.isHashtable)
    			oset = CreateDictionary((ArrayList) v, pi.pt, pi.GenericTypes, globaltypes);

    		else if (pi.isEnum)
    			oset = CreateEnum(pi.pt, (string) v);

    		else if (pi.isDateTime)
    			oset = CreateDateTime((string) v);

    		else if (pi.isClass && v is Dictionary<string, object> objects)
    			oset = ParseDictionary(objects, globaltypes, pi.pt, null);

    		else if (pi.isValueType)
    			oset = ChangeType(v, pi.changeType);
    		else if (v is ArrayList list)
    			oset = CreateArray(list, typeof (object), globaltypes);
    		else
    			oset = v;

    		if (pi.CanWrite) WriteValueToTypeInstance(name, targetObject, pi, oset);
    	}

    	static void WriteValueToTypeInstance(string name, object targetObject, MyPropInfo pi, object oset) {
            try
            {
                var typ = targetObject.GetType();

                if (typ.IsValueType)
                {
                    var fi = typ.GetField(name);
                    if (fi != null)
                    {
                        fi.SetValue(targetObject, oset);
                        return;
                    }
                    var pr = typ.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
                    if (pr != null)
                    {
                        pr.SetValue(targetObject, oset, null);
                        return;
                    }
                }
                pi.setter(targetObject, oset, name);
            }
            catch (System.Security.VerificationException vex)
            {
                throw new Exception("Writing value failed [co/contra]variance checks", vex);
            }
        }

        SafeDictionary<string, MyPropInfo> GetProperties(Type type, string typename)
        {
            SafeDictionary<string, MyPropInfo> sd;
            if (propertyCache.TryGetValue(typename, out sd)) return sd;
        	sd = new SafeDictionary<string, MyPropInfo>();

			var pr = new List<PropertyInfo>();

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

        	pr.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        	foreach (var prop in type.GetInterfaces()
				.SelectMany(iface => iface.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(prop => pr.All(p => p.Name != prop.Name)))) {
        		pr.Add(prop);
        	}

        	foreach (var p in pr)
        	{
        		var d = CreateMyProp(p.PropertyType);
        		d.CanWrite = p.CanWrite;
        		d.setter = CreateSetMethod(p);
                if (d.setter == null) continue; // throw new Exception("Property " + p.Name + " has no setter");
        		d.getter = CreateGetMethod(p);
        		sd.Add(p.Name, d);
        	}

        	propertyCache.Add(typename, sd);
        	return sd;
        }

        internal List<Getters> GetGetters(Type type)
        {
            List<Getters> val;
            if (getterCache.TryGetValue(type, out val)) return val;

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
            	var gg = new Getters {Name = f.Name, Getter = g, PropertyType = f.FieldType, FieldInfo = f};
            	getters.Add(gg);
            }

            getterCache.Add(type, getters);
            return getters;
        }

        static MyPropInfo CreateMyProp(Type t)
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

        static GenericSetter CreateSetMethod(PropertyInfo propertyInfo)
        {
            if (propertyInfo.GetSetMethod(true) == null) return null;

            var idxs = propertyInfo.GetIndexParameters();

            if (idxs.Length < 1) { return (a, b, k) => propertyInfo.SetValue(a, b, null); }
            if (idxs.Length < 2) { return (a, b, k) => propertyInfo.SetValue(a, b, new []{ k }); }

            return (a, b, k) => throw new Exception("Multiple index data types are not supported");
        }

        static GenericGetter CreateGetField(Type type, FieldInfo fieldInfo)
        {
            var dynamicGet = new DynamicMethod("_"+fieldInfo.Name
				, typeof(object), new[] { typeof(object) }, type, true);
            var il = dynamicGet.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fieldInfo);
            if (fieldInfo.FieldType.IsValueType) il.Emit(OpCodes.Box, fieldInfo.FieldType);
            il.Emit(OpCodes.Ret);

            return (GenericGetter)dynamicGet.CreateDelegate(typeof(GenericGetter));
        }

        static GenericSetter CreateSetField(Type type, FieldInfo fieldInfo)
        {
            var arguments = new[] { typeof(object), typeof(object), typeof(object) };

            var dynamicSet = new DynamicMethod("_" + fieldInfo.Name, typeof(void), arguments, type, true);
            var il = dynamicSet.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            if (fieldInfo.FieldType.IsValueType) il.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
            il.Emit(OpCodes.Stfld, fieldInfo);
            il.Emit(OpCodes.Ret);

            return (GenericSetter)dynamicSet.CreateDelegate(typeof(GenericSetter));
        }

        static GenericGetter CreateGetMethod(PropertyInfo propertyInfo)
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

        static object ChangeType(object value, Type conversionType)
        {
            if (conversionType == typeof(int)) return (int)CreateLong((string)value);
        	if (conversionType == typeof(long)) return CreateLong((string)value);
        	if (conversionType == typeof(string)) return value;
        	if (conversionType == typeof(Guid)) return CreateGuid((string)value);
        	if (conversionType.IsEnum) return CreateEnum(conversionType, (string)value);
        	return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
        }

    	static void ProcessMap(object obj, SafeDictionary<string, MyPropInfo> props, Dictionary<string, object> dic)
        {
            foreach (var kv in dic)
            {
                var p = props[kv.Key];
                var o = p.getter(obj);
                var t = Type.GetType((string)kv.Value);
                if (t == typeof(Guid)) p.setter(obj, CreateGuid((string)o), null);
            }
        }

        static long CreateLong(IEnumerable<char> s)
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

        static object CreateEnum(Type pt, string v)
        {
            return Enum.Parse(pt, v);
        }

        static Guid CreateGuid(string s)
        {
        	return s.Length > 30 ? new Guid(s) : new Guid(Convert.FromBase64String(s));
        }

    	static DateTime CreateDateTime(string value)
        {
			if (value.EndsWith("Z")) return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ssZ", null).ToLocalTime();
			return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss", null);
		}

        object CreateArray(IEnumerable data, Type bt, IDictionary<string, object> globalTypes)
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

        object CreateGenericList(IEnumerable data, Type pt, Type bt, IDictionary<string, object> globalTypes)
        {
            var col = (IList)FastCreateInstance(pt);
            foreach (var ob in data)
            {
                if (ob is IDictionary)
                    col.Add(ParseDictionary((Dictionary<string, object>)ob, globalTypes, bt, null));
                else if (ob is ArrayList list)
                    col.Add(list.ToArray());
                else
                    col.Add(ChangeType(ob, bt));
            }
            return col;
        }

        object CreateStringKeyDictionary(Dictionary<string, object> reader, Type pt, IList<Type> types, IDictionary<string, object> globalTypes)
        {
            var col = (IDictionary)FastCreateInstance(pt);
        	Type t2 = null;
            if (types != null) t2 = types[1];

            foreach (var values in reader)
            {
                var key = values.Key;
                object val;
                if (values.Value is Dictionary<string, object> objects)
                    val = ParseDictionary(objects, globalTypes, t2, null);
                else
                    val = ChangeType(values.Value, t2);
                col.Add(key, val);
            }

            return col;
        }

        object CreateDictionary(IEnumerable reader, Type pt, IList<Type> types, IDictionary<string, object> globalTypes)
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

        static Type GetChangeType(Type conversionType)
        {
            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return conversionType.GetGenericArguments()[0];

            return conversionType;
        }

        DataSet CreateDataset(IDictionary<string, object> reader, IDictionary<string, object> globalTypes)
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

        void ReadSchema(IDictionary<string, object> reader, DataSet ds, IDictionary<string, object> globalTypes)
        {
            var schema = reader["$schema"];

            if (schema is string s)
            {
                TextReader tr = new StringReader(s);
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

        void ReadDataTable(IEnumerable rows, DataTable dt)
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

        DataTable CreateDataTable(IDictionary<string, object> reader, IDictionary<string, object> globalTypes)
        {
            var dt = new DataTable();

            // read dataset schema here
            var schema = reader["$schema"];

            if (schema is string s)
            {
                TextReader tr = new StringReader(s);
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