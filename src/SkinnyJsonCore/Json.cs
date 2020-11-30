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
using JetBrains.Annotations;

namespace SkinnyJson
{
    /// <summary>
    /// SkinnyJson entry point. Use the static methods of this class to interact with JSON data
    /// </summary>
    public partial class Json
    {
        /// <summary>
        /// String encoding to use for streams, when no specific encoding is provided.
        /// Initial value is UTF8.
        /// </summary>
        public static Encoding DefaultStreamEncoding { get; set; } = new UTF8Encoding(false);

        /// <summary> Turn an object into a JSON string </summary>
		public static string Freeze(object obj)
		{
            if (obj is DynamicWrapper dyn) {
                return Freeze(dyn.Parsed);
            }
            if (!IsAnonymousTypedObject(obj)) return Instance.ToJson(obj, DefaultParameters);
            
            // If we are passed an anon type, turn off type information -- it will all be junk.
            var jsonParameters = DefaultParameters.Clone();
            jsonParameters.UseExtensions = false; 
            jsonParameters.UsingGlobalTypes = false;
            jsonParameters.EnableAnonymousTypes = true;

            return Instance.ToJson(obj, jsonParameters);
        }

        /// <summary> Write an object to a stream as a JSON string </summary>
        public static void Freeze(object obj, Stream target, Encoding? encoding = null)
        {
            if (encoding == null) encoding = DefaultStreamEncoding;

            if (obj is DynamicWrapper dyn) {
                Freeze(dyn.Parsed, target);
            }
            if (IsAnonymousTypedObject(obj))
            { // If we are passed an anon type, turn off type information -- it will all be junk.
                var jsonParameters = DefaultParameters.Clone();
                jsonParameters.UseExtensions = false; 
                jsonParameters.UsingGlobalTypes = false;
                jsonParameters.EnableAnonymousTypes = true;

                Instance.ToJsonStream(obj, target, jsonParameters, encoding);
            }
            Instance.ToJsonStream(obj, target, DefaultParameters, encoding);
        }

        /// <summary> Turn a JSON string into a detected object </summary>
		public static object Defrost(string json)
		{
			return Instance.ToObject(json, null, null);
		}

        /// <summary> Turn a JSON byte array into a detected object </summary>
        public static object Defrost(byte[] json)
        {
            return Instance.ToObject(json, null, null);
        }
        
        /// <summary> Turn a JSON data stream into a detected object </summary>
        public static object Defrost(Stream json, Encoding? encoding = null)
        {
            return Instance.ToObject(json, null, encoding ?? DefaultStreamEncoding);
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
			return (T)Instance.ToObject(json, typeof(T), null);
		}
        
        /// <summary> Turn a JSON data stream into a specific object </summary>
        public static T Defrost<T>(Stream json, Encoding? encoding = null)
        {
            return (T)Instance.ToObject(json, typeof(T), encoding ?? DefaultStreamEncoding);
        }
        
        /// <summary> Turn a JSON string into a runtime type </summary>
        public static object Defrost(string json, Type runtimeType)
        {
            return Instance.ToObject(json, runtimeType, null);
        }

        /// <summary> Turn a JSON byte array into a runtime type </summary>
        public static object Defrost(byte[] json, Type runtimeType, Encoding? encoding = null)
        {
            return Instance.ToObject(json, runtimeType, encoding);
        }

        /// <summary> Turn a JSON data stream into a runtime type </summary>
        public static object Defrost(Stream json, Type runtimeType, Encoding? encoding = null)
        {
            return Instance.ToObject(json, runtimeType, encoding);
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
            
            return Instance.SelectObjects<T>(json, path, null);
        }

        /// <summary> Create a copy of an object through serialisation </summary>
        public static T Clone<T>(T obj)
        {
            if (obj == null) return obj;
            return Defrost<T>(Freeze(obj));
        }

		/// <summary>Read a JSON object into an anonymous .Net object</summary>
        public static object Parse(string json)
        {
            return (new JsonParser(json, DefaultParameters.IgnoreCaseOnDeserialize).Decode()) ?? new {};
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
        public static object? FillObject(object input, string json)
        {
            var ht = new JsonParser(json, DefaultParameters.IgnoreCaseOnDeserialize).Decode() as Dictionary<string, object>;
            return ht == null ? null : Instance.ParseDictionary(ht, null, input.GetType(), input);
        }

        /// <summary>
        /// You can set these parameters globally for all calls
        /// </summary>
        [NotNull] public static readonly JsonParameters DefaultParameters = new JsonParameters();

        [NotNull]
    	internal static readonly Json Instance = new Json();
        private Json(){
            _jsonParameters = DefaultParameters;
        }

        
        static bool IsAnonymousTypedObject(object? obj)
        {
            return IsAnonymousType(obj?.GetType());
        }

        static bool IsAnonymousType(Type? type)
        {
            if (type == null) return false;
            return (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$")) && (type.Attributes.HasFlag(TypeAttributes.NotPublic));
        }

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private JsonParameters? _jsonParameters;

        internal string ToJson(object obj, JsonParameters param)
        {
            _jsonParameters = param.Clone();
            if (_jsonParameters.EnableAnonymousTypes) { _jsonParameters.UseExtensions = false; _jsonParameters.UsingGlobalTypes = false; }
            return new JsonSerializer(_jsonParameters).ConvertToJson(obj);
        }

        internal void ToJsonStream(object obj, Stream target, JsonParameters param, Encoding encoding)
        {
            _jsonParameters = param.Clone();
            if (_jsonParameters.EnableAnonymousTypes) { _jsonParameters.UseExtensions = false; _jsonParameters.UsingGlobalTypes = false; }
            new JsonSerializer(_jsonParameters).ConvertToJson(obj, target, encoding);
        }
        
        /// <summary>
        /// Pick items out of a parsed object using dotted string path
        /// </summary>
        private IEnumerable<T> SelectObjects<T>(object json, string path, Encoding? encoding)
        {
            var parser = ParserFromStreamOrStringOrBytes(json, encoding);
            var globalTypes = new Dictionary<string, object>();

            var rawObject = parser.Decode();
            if (rawObject == null) return Array.Empty<T>();
            
            var pathParts = path.Split('.');

            return PathWalk<T>(rawObject, globalTypes, pathParts, 0);
        }

        /// <summary>
        /// Recursive helper for SelectObjects˂T˃
        /// </summary>
        private IEnumerable<T> PathWalk<T>(object? rawObject, Dictionary<string, object> globalTypes, string[] pathParts, int pathIndex)
        {
            if (rawObject == null) yield break;
            
            var type = typeof(T);
            if (pathIndex >= pathParts.Length) {
                var container = StrengthenType(type, rawObject, globalTypes); // Really, convert the raw object to the target and output it
                if (container is IList results) {
                    foreach (var result in results) { yield return (T)result; }
                } else if (container is T result){
                    yield return result;
                } else {
                    yield break;
                }
                    
                yield break;
            }
            var step = pathParts[pathIndex];
            
            switch (rawObject)
            {
                case Dictionary<string, object> objects:
                    {
                        if (objects.ContainsKey(step)) {
                            var elems = PathWalk<T>(objects[step], globalTypes, pathParts, pathIndex + 1);
                            foreach (var elem in elems) { yield return elem; }
                        }
                        yield break;
                    }

                case ArrayList arrayList:
                    {
                        foreach (var item in arrayList)
                        {
                            var elems = PathWalk<T>(item, globalTypes, pathParts, pathIndex); // ignore arrays while walking path name elements
                            foreach (var elem in elems) { yield return elem; }
                        }
                        yield break;
                    }

                default:
                    throw new Exception("Don't understand this JSON");
            }
        }

        /// <summary>
        /// Create a new object by type, using input json data
        /// </summary>
        /// <param name="json">Either a stream of utf-8 data or an in-memory `string`</param>
        /// <param name="type">Target return type</param>
        /// <param name="encoding">String encoding to use, if reading from a stream</param>
        internal object ToObject(object json, Type? type, Encoding? encoding)
        {
			_jsonParameters ??= DefaultParameters;
			var globalTypes = new Dictionary<string, object>();
			
            var parser = ParserFromStreamOrStringOrBytes(json, encoding);

            var decodedObject = parser.Decode();
            if (decodedObject == null) return new {};
            
			return StrengthenType(type, decodedObject, globalTypes);
        }

        /// <summary>
        /// Try to decode a parsed json object into a new type instance
        /// </summary>
        /// <param name="type">Target output type</param>
        /// <param name="decodedObject">raw memory map of json</param>
        /// <param name="globalTypes">cache of type matches</param>
        private object StrengthenType(Type? type, object decodedObject, Dictionary<string, object> globalTypes)
        {
            switch (decodedObject)
            {
                case Dictionary<string, object> objects:
                    return ParseDictionary(objects, globalTypes, type, null);

                case ArrayList arrayList:
                    if (type?.IsArray == true)
                    {
                        var elementType = type.GetElementType() ?? typeof(object);
                        var list = ConvertToList(elementType, globalTypes, arrayList);
                        return ListToArray(list, elementType);
                    }
                    else
                    {
                        var containedType = (type?.GetGenericArguments().SingleOrDefault() ?? type) ?? typeof(object);
                        
                        var setType = GenericSetInterfaceType(containedType);
                        if (type == setType) return ConvertToSet(containedType, globalTypes, arrayList);
                        return ConvertToList(containedType, globalTypes, arrayList);
                    }

                default:
                    throw new Exception("Don't understand this JSON");
            }
        }

        private Array ListToArray(IList list, Type elementType)
        {
            var x = new ArrayList(list);
            return x.ToArray(elementType);
        }

        private IList ConvertToList(Type elementType, Dictionary<string, object> globalTypes, ArrayList arrayList)
        {
            var list = (IList) Activator.CreateInstance(GenericListType(elementType))!;
            foreach (var obj in arrayList)
            {
                if (obj == null) list.Add(obj);
                else if (obj.GetType().IsAssignableFrom(elementType)) list.Add(obj);
                else
                { // a complex type?
                    var dict = obj as Dictionary<string, object>;
                    if (dict == null) throw new Exception("Element of array not assignable to the array type");
                    var parsed = ParseDictionary(dict, globalTypes, elementType, null);
                    list.Add(parsed);
                }
            }

            return list;
        }
        
        private object ConvertToSet(Type elementType, Dictionary<string, object> globalTypes, [NotNull] ArrayList arrayList)
        {
            var set = Activator.CreateInstance(GenericHashSetType(elementType));
            var adder = set?.GetType().GetMethod("Add", BindingFlags.Public|BindingFlags.Instance) ?? throw new Exception("Failed to find add method on set");
            foreach (var obj in arrayList)
            {
                if (obj == null) continue;
                
                if (obj.GetType().IsAssignableFrom(elementType)) adder.Invoke(set, new []{obj});
                else
                { // a complex type?
                    var dict = obj as Dictionary<string, object>;
                    if (dict == null) throw new Exception("Element of array not assignable to the array type");
                    //var child = Activator.CreateInstance(elementType);
                    var parsed = ParseDictionary(dict, globalTypes, elementType, null);//child);
                    adder.Invoke(set, new[] {parsed});
                }
            }

            return set;
        }

        /// <summary>
        /// Pass in either a string or a stream and get back a parser instance
        /// </summary>
        [NotNull]
        private static JsonParser ParserFromStreamOrStringOrBytes(object json, Encoding? encoding)
        {
            JsonParser parser;
            switch (json)
            {
                case Stream jsonStream:
                    parser = new JsonParser(jsonStream, DefaultParameters.IgnoreCaseOnDeserialize, encoding);
                    break;
                case string jsonString:
                    parser = new JsonParser(jsonString, DefaultParameters.IgnoreCaseOnDeserialize);
                    break;
                case byte[] jsonBytes:
                    parser = new JsonParser(jsonBytes, DefaultParameters.IgnoreCaseOnDeserialize, encoding);
                    break;
                default:
                    throw new Exception("supplied object is not json data");
            }

            return parser;
        }

        /// <summary>
        /// Make an IList˂T˃() instance for a runtime type
        /// </summary>
        [NotNull]
        private Type GenericListType(Type containedType)
	    {
			var d1 = typeof(List<>);
			Type[] typeArgs = { containedType };
			return d1.MakeGenericType(typeArgs);
	    }
        
        /// <summary>
        /// Make an ISet˂T˃() instance for a runtime type
        /// </summary>
        [NotNull]
        private Type GenericSetInterfaceType(Type containedType)
        {
            var d1 = typeof(ISet<>);
            Type[] typeArgs = { containedType };
            return d1.MakeGenericType(typeArgs);
        }
        
        /// <summary>
        /// Make an HashSet˂T˃() instance for a runtime type
        /// </summary>
        [NotNull]
        private Type GenericHashSetType(Type containedType)
        {
            var d1 = typeof(HashSet<>);
            Type[] typeArgs = { containedType };
            return d1.MakeGenericType(typeArgs);
        }

	    readonly SafeDictionary<Type, string> _typeNameMap = new SafeDictionary<Type, string>();

        /// <summary>
        /// Get a shortened string name for a type's containing assembly
        /// </summary>
        internal string GetTypeAssemblyName(Type t)
        {
            if (_typeNameMap.TryGetValue(t, out string val)) return val;

        	string name;
			if (t.BaseType == typeof(object)) {
				// on Windows, this can be just "t.GetInterfaces()" but Mono doesn't return in order.
				var interfaceType = t.GetInterfaces().FirstOrDefault(i => !t.GetInterfaces().Any(i2 => i2.GetInterfaces().Contains(i)));
				name = ShortenName((interfaceType ?? t).AssemblyQualifiedName);
			} else {
				name = ShortenName(t.AssemblyQualifiedName ?? t.ToString()!);
			}

        	_typeNameMap.Add(t, name);

        	return _typeNameMap[t];
        }

        /// <summary>
        /// Shorten an assembly qualified name
        /// </summary>
    	static string ShortenName(string assemblyQualifiedName) {
			var one = assemblyQualifiedName.IndexOf(',');
			var two = assemblyQualifiedName.IndexOf(',', one+1);
			return assemblyQualifiedName.Substring(0, two);
    	}

    	readonly SafeDictionary<string, Type> _typeCache = new SafeDictionary<string, Type>();

        /// <summary>
        /// Try to get or build a type for a given type-name
        /// </summary>
        private Type? GetTypeFromCache(string typename) {
            if (_typeCache.TryGetValue(typename, out Type val)) return val;

			var typeParts = typename.Split(',');

			Type? t;
			if (typeParts.Length > 1) {
				var assemblyName = typeParts[1];
				var fullName = typeParts[0];
				var available = Assembly.Load(assemblyName!)?.GetTypes();
				t = available?.SingleOrDefault(type => type?.FullName?.ToLower() == fullName!.ToLower());
			} else if (typeParts.Length == 1) {
				// slow but robust way of finding a type fragment.
				t = AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(asm => asm.GetTypes())
					.SingleOrDefault(type => type.FullName?.StartsWith(typeParts[0]!) ?? false);
			} else throw new Exception("Invalid type description: "+typename);
            
        	if (t != null) {
				_typeCache.Add(typename, t);
			}
			return t;
		}

    	readonly SafeDictionary<Type, CreateObject> _constructorCache = new SafeDictionary<Type, CreateObject>();
        private delegate object CreateObject();

        /// <summary>
        /// Try to make a new instance of a type.
        /// Will drop down to 'SlowCreateInstance' in special cases
        /// </summary>
		private object? FastCreateInstance(Type? objType)
        {
            if (objType == null) return null;
            if (_constructorCache.TryGetValue(objType, out var cc)) return cc();

            if (objType.IsInterface)
            {
                if (TryMakeStandardContainer(objType, out var inst)) return inst;
                return DynamicProxy.GetInstanceFor(objType);
            }

            if (objType.IsValueType) return FormatterServices.GetUninitializedObject(objType);
            
            try
            {
            	var constructorInfo = objType.GetConstructor(Type.EmptyTypes);
				if (constructorInfo == null)
                {
                    return SlowCreateInstance(objType);
                }

                var dynMethod = new DynamicMethod("_", objType, null);
            	var ilGen = dynMethod.GetILGenerator();

            	ilGen.Emit(OpCodes.Newobj, constructorInfo);
            	ilGen.Emit(OpCodes.Ret);
            	var c = (CreateObject)dynMethod.CreateDelegate(typeof(CreateObject));
            	_constructorCache.Add(objType, c);
            	return c();
            }
            catch (Exception exc)
            {
                throw new Exception($"Failed to fast create instance for type '{objType.FullName}' from assembly '{objType.AssemblyQualifiedName}'", exc);
            }
        }

        private static bool TryMakeStandardContainer(Type objType, out object? inst)
        {
            inst = null;
            if (objType.Name == "IDictionary`2")
            {
                var d1 = typeof(Dictionary<,>);
                var typeParameters = objType.GetGenericArguments();
                var constructed = d1.MakeGenericType(typeParameters);
                inst = Activator.CreateInstance(constructed);
                return true;
            }

            return false;
        }

        private object SlowCreateInstance(Type objType)
        {
            if (objType == typeof(string)) {
                throw new Exception("Invalid parser state");
            }
            var allCtor = objType.GetConstructors();
            if (allCtor.Length < 1) {
                throw new Exception($"Failed to create instance for type '{objType.FullName}' from assembly '{objType.AssemblyQualifiedName}'. No constructors found.");
            }
            
            var types = allCtor[0]!.GetParameters().Select(p=>p.ParameterType).ToArray();
            var instances = types.Select(FastCreateInstance).ToArray();
            var constructorInfo = objType.GetConstructor(types);
            return constructorInfo?.Invoke(instances)!;
        }

        bool _usingGlobals;

        readonly SafeDictionary<Type, List<Getters>> _getterCache = new SafeDictionary<Type, List<Getters>>();
    	readonly SafeDictionary<string, SafeDictionary<string, TypePropertyInfo>> _propertyCache = new SafeDictionary<string, SafeDictionary<string, TypePropertyInfo>>();
        
        internal delegate object GenericGetter(object obj);

        /// <param name="target">object instance to accept the value</param>
        /// <param name="value">value of property to set</param>
        /// <param name="key">optional key for dictionaries</param>
        private delegate void GenericSetter(object target, object value, object? key);

        /// <summary>
        /// Read a weakly-typed dictionary tree into a strong type. If the keys do not match exactly,
        /// all matching field/properties will be filled.
        /// If *no* keys match the target type, this will return `null`
        /// </summary>
        private object ParseDictionary(IDictionary<string, object> jsonValues, IDictionary<string, object>? globalTypes, Type? type, object? input)
        {
            _jsonParameters ??= DefaultParameters;

            if (jsonValues.TryGetValue("$types", out var tn))
            {
                if (globalTypes != null)
                {
                    var dic = ((Dictionary<string, object>) tn!);
                    foreach (var kvp in dic) { if (kvp.Value != null) globalTypes.Add((string) kvp.Value, kvp.Key); }
                    _usingGlobals = true;
                }
            }

            var found = jsonValues.TryGetValue("$type", out tn);
            if (found == false && type == typeof(object))
            {
                var ds = CreateDataset(jsonValues, globalTypes);
                return ds != null ? (object) ds : jsonValues;
            }
            if (found)
            {
                if (_usingGlobals && globalTypes != null)
                {
                    if (globalTypes.TryGetValue((string)tn!, out object tName)) tn = tName;
                }
                if (type == null || !type.IsInterface) type = GetTypeFromCache((string)tn!);
            }

            var targetObject = input ?? FastCreateInstance(type);

            if (targetObject == null) return jsonValues; // can't work out what object to fill, send back the raw values

            var targetType = targetObject.GetType();

			var props = GetProperties(targetType, targetType.Name);
            if (!IsDictionary(targetType) && NoPropertiesMatch(props.Keys, jsonValues.Keys)) return null;
            foreach (var key in jsonValues.Keys)
            {
                MapJsonValueToObject(key, targetObject, jsonValues, globalTypes, props);
            }

            return targetObject;
        }

        private bool IsDictionary(Type targetType)
        {
            return targetType.GetInterface("IDictionary") != null;
        }

        private bool NoPropertiesMatch(string[] props, ICollection<string> jsonValuesKeys)
        {
            foreach (var jsonKey in jsonValuesKeys)
            {
                if (props.Contains(jsonKey)) return false;
            }
            return true;
        }

        /// <summary>
        /// Map json value dictionary to the properties and fields of a target object instance
        /// </summary>
        void MapJsonValueToObject(string objectKey, object targetObject, IDictionary<string, object> jsonValues, IDictionary<string, object>? globalTypes, SafeDictionary<string, TypePropertyInfo> props)
    	{
    		var name = objectKey;
    		if (_jsonParameters?.IgnoreCaseOnDeserialize == true) name = name.ToLower();
    		if (name == "$map")
    		{
    			ProcessMap(targetObject, props, (Dictionary<string, object>) jsonValues[name]);
    			return;
    		}
    		TypePropertyInfo pi;
    		if (props.TryGetValue(name, out pi) == false) {
                if (targetObject is IDictionary) {
                    var ok = props.TryGetValue("Item", out pi);
                    if (!ok) return;
                }
                else return;
            }
    		if (!pi.filled) return;
    		var v = jsonValues[name];

    		if (v == null) return;
    		object oset;

    		if (pi.isInt) oset = (int) CreateLong(v);
    		else if (pi.isLong) oset = CreateLong(v);
    		else if (pi.isString) oset = v;
    		else if (pi.isBool) oset = InterpretBool(v);
    		else if (pi.isGenericType && pi.isValueType == false && pi.isDictionary == false)
    			oset = CreateGenericList((ArrayList) v, pi.pt, pi.bt, globalTypes);
    		else if (pi.isByteArray)
    			oset = Convert.FromBase64String((string) v);

    		else if (pi.isArray && pi.isValueType == false)
    			oset = CreateArray((ArrayList) v, pi.bt, globalTypes);
    		else if (pi.isGuid)
    			oset = CreateGuid((string) v);
    		else if (pi.isDataSet)
    			oset = CreateDataset((Dictionary<string, object>) v, globalTypes);

    		else if (pi.isDataTable)
    			oset = CreateDataTable((Dictionary<string, object>) v, globalTypes);

    		else if (pi.isStringDictionary)
    			oset = CreateStringKeyDictionary((Dictionary<string, object>) v, pi.pt, pi.GenericTypes, globalTypes);

    		else if (pi.isDictionary || pi.isHashtable)
    			oset = CreateDictionary((ArrayList) v, pi.pt, pi.GenericTypes, globalTypes);

    		else if (pi.isEnum)
    			oset = CreateEnum(pi.pt, (string) v);

    		else if (pi.isDateTime)
    			oset = CreateDateTime((string) v);

    		else if (pi.isClass && v is Dictionary<string, object> objects)
    			oset = ParseDictionary(objects, globalTypes, pi.pt, null);

    		else if (pi.isValueType)
    			oset = ChangeType(v, pi.changeType);
    		else if (v is ArrayList list)
    			oset = CreateArray(list, typeof (object), globalTypes);
    		else
    			oset = v;

    		if (pi.CanWrite) WriteValueToTypeInstance(name, targetObject, pi, oset);
    	}

        private bool InterpretBool(object o)
        {
            if (o is bool b) return b;
            if (o is string s)
            {
                if (_jsonParameters.IgnoreCaseOnDeserialize) s = s.ToLowerInvariant();
                switch (s)
                {
                    case "true": return true;
                    case "false": return false;
                }
            }
            throw new Exception("Can't interpret '"+o+"' as a boolean");
        }

        /// <summary>
        /// Inject a value into an object's property
        /// </summary>
        static void WriteValueToTypeInstance(string name, object targetObject, TypePropertyInfo pi, object oset) {
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

        /// <summary>
        /// Read the properties and public fields of a type.
        /// In special cases, this will also read private fields
        /// </summary>
        SafeDictionary<string, TypePropertyInfo> GetProperties(Type type, string typename)
        {
            bool usePrivateFields = 
                typename.StartsWith("Tuple`")
                || IsAnonymousType(type);

            SafeDictionary<string, TypePropertyInfo> sd;
            if (_propertyCache.TryGetValue(typename, out sd)) return sd;
        	sd = new SafeDictionary<string, TypePropertyInfo>();

			var pr = new List<PropertyInfo>();

			var fi = new List<FieldInfo>();
        	fi.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Instance));

            if (usePrivateFields)
            {
                fi.AddRange(type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance));
            }

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
	            if (usePrivateFields){
                    sd.Add(AnonFieldFilter(f.Name.Replace("m_", "")), d);
                }
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
                if (d.setter == null) continue;
        		d.getter = CreateGetMethod(p);
        		sd.Add(p.Name, d);
        	}

            if (type.GetGenericArguments().Length < 1) {
        	    _propertyCache.Add(typename, sd);
            }
        	return sd;
        }

        // Anonymous fields like "A" will be named like "<A>i__Field" in the type def.
        // so we filter them here:
        private string AnonFieldFilter(string name)
        {
            if (name[0] != '<') return name;
            var idx = name.IndexOf('>', 2);
            if (idx < 2) return name;
            return name.Substring(1, idx - 1);
        }

        /// <summary>
        /// Return a list of propery/field access proxies for a type
        /// </summary>
        internal List<Getters> GetGetters(Type type)
        {
            List<Getters> val;
            if (_getterCache.TryGetValue(type, out val)) return val;

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var getters = (from p in props where p.CanWrite || _jsonParameters.ShowReadOnlyProperties || _jsonParameters.EnableAnonymousTypes
						   let att = p.GetCustomAttributes(typeof (System.Xml.Serialization.XmlIgnoreAttribute), false)
						   where att.Length <= 0 let g = CreateGetMethod(p) where g != null 
						   
						   select new Getters {Name = p.Name, Getter = g, PropertyType = p.PropertyType}).ToList();

        	var fi = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
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

            _getterCache.Add(type, getters);
            return getters;
        }

        /// <summary>
        /// Read reflection data for a type
        /// </summary>
        static TypePropertyInfo CreateMyProp(Type t)
        {
        	var d = new TypePropertyInfo {filled = true, CanWrite = true, pt = t, isDictionary = t.Name.Contains("Dictionary")};
        	if (d.isDictionary) d.GenericTypes = t.GetGenericArguments();

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

        /// <summary>
        /// Try to create a value-setting proxy for an object property
        /// </summary>
        static GenericSetter CreateSetMethod(PropertyInfo propertyInfo)
        {
            if (propertyInfo.GetSetMethod(true) == null) return null;

            var idxs = propertyInfo.GetIndexParameters();

            if (idxs.Length < 1) { return (a, b, k) => propertyInfo.SetValue(a, b, null); }
            if (idxs.Length < 2) { return (a, b, k) => propertyInfo.SetValue(a, b, new []{ k }); }

            return (a, b, k) => throw new Exception("Multiple index data types are not supported");
        }

        /// <summary>
        /// Create a value-reading proxy for an object field
        /// </summary>
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

        /// <summary>
        /// Create a value-setting proxy for an object field
        /// </summary>
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

        
        /// <summary>
        /// Try to create a value-reading proxy for an object property
        /// </summary>
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

        /// <summary>
        /// Convert between runtime types
        /// </summary>
        static object? ChangeType(object value, Type conversionType)
        {
            if (conversionType == typeof(int)) return (int)CreateLong(value);
        	if (conversionType == typeof(long)) return CreateLong(value);
        	if (conversionType == typeof(string)) return value;
        	if (conversionType == typeof(Guid)) return CreateGuid((string)value);
        	if (conversionType.IsEnum) return CreateEnum(conversionType, (string)value);
        	return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
        }

    	static void ProcessMap(object obj, SafeDictionary<string, TypePropertyInfo> props, Dictionary<string, object>? dic)
        {
            if (dic == null) return;
            foreach (var kv in dic)
            {
                if (kv.Value == null) continue;
                var p = props[kv.Key];
                var o = p.getter(obj);
                var t = Type.GetType((string)kv.Value);
                if (t == typeof(Guid)) p.setter(obj, CreateGuid((string)o), null);
            }
        }

        static long CreateLong(object obj){
            if (obj is string s) return ParseLong(s);
            if (obj is double d) return (long)d;
            throw new Exception("Unsupported int type: "+obj.GetType());
        }

        static long ParseLong(IEnumerable<char> s)
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

        private static readonly string[] DateFormatsInPreferenceOrder = {
            "yyyy-MM-ddTHH:mm:ss",  // correct ISO 8601 'extended'
            "yyyy-MM-dd HH:mm:ss",  // our old output format
            "yyyy-MM-dd H:mm:ss",   // Erlang style
            "yyyy-MM-ddTH:mm:ss",   // Erlang style with a T
            "yyyy-MM-ddTHH:mm:ssZ", // with zone specifier
            "yyyy-MM-dd HH:mm:ssZ", // with zone specifier, but no T
            "yyyy-MM-ddTHHmmss",    // ISO 8601 'basic'
        };

        static DateTime CreateDateTime(string value)
        {
            if (DateFormatsInPreferenceOrder == null) return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:ss", null);
            foreach (var format in DateFormatsInPreferenceOrder)
            {
                if (DateTime.TryParseExact(value, format, null, DateTimeStyles.AssumeLocal, out var dateVal)) {
                    return dateVal;
                }
            }
            // None of our prefered formats, so let .Net guess
            return DateTime.Parse(value);
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
                {
                    val = ParseDictionary(objects, globalTypes, t2, null);
                }
                else
                {
                    val = ChangeType(values.Value, t2);
                }
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

        DataSet? CreateDataset(IDictionary<string, object> reader, IDictionary<string, object>? globalTypes)
        {
        	var ds = new DataSet {EnforceConstraints = false};
        	ds.BeginInit();

            // read dataset schema here
            if (!ReadSchema(reader, ds, globalTypes)) return null;

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

        bool ReadSchema(IDictionary<string, object> reader, DataSet ds, IDictionary<string, object> globalTypes)
        {
            if (reader?.ContainsKey("$schema") != true) return false;

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
            return true;
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
                if (_jsonParameters.UseUtcDateTime && (c.DataType == typeof(DateTime) || c.DataType == typeof(DateTime?)))
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
                if (_jsonParameters.UseUtcDateTime)
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

            object schema;
            // read dataset schema here
            if (reader?.ContainsKey("$schema") != true)
            {
                schema = null;
            }
            else
            {
                schema = reader["$schema"];
            }

            if (schema is string s)
            {
                TextReader tr = new StringReader(s);
                dt.ReadXmlSchema(tr);
            }
            else if (schema is Dictionary<string, object> dictSchema)
            {
                var ms = (DatasetSchema)ParseDictionary(dictSchema, globalTypes, typeof(DatasetSchema), null);
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