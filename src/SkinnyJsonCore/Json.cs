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
        public static string Freeze(object? obj)
        {
            if (obj == null) return "";
            if (obj is DynamicWrapper dyn) {
                return Freeze(dyn.Parsed);
            }

            if (obj is Type typeKind) // caller has `Json.Freeze(typeof(SomeClass));`
            {
                return Instance.ToJsonStatics(typeKind, DefaultParameters);
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
        public static T Defrost<[MeansImplicitUse]T>(string json)
        {
            return (T)Instance.ToObject(json, typeof(T), null);
        }
        
        /// <summary> Turn a JSON data stream into a specific object </summary>
        public static T Defrost<[MeansImplicitUse]T>(Stream json, Encoding? encoding = null)
        {
            return (T)Instance.ToObject(json, typeof(T), encoding ?? DefaultStreamEncoding);
        }
        
        /// <summary> Turn a JSON byte array into a specific object </summary>
        public static T Defrost<[MeansImplicitUse]T>(byte[] json, Encoding? encoding = null)
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
        
        /// <summary> Turn a JSON string into an object containing properties found </summary>
        public static dynamic DefrostDynamic(Stream json, Encoding? encoding = null)
        {
            return new DynamicWrapper(Instance.ToObject(json, null, encoding));
        }
        
        /// <summary>
        /// Turn a sub-path of a JSON document into an enumeration of values, by specific type
        /// </summary>
        /// <remarks>This is intended to extract useful fragments from repository-style files</remarks>
        /// <typeparam name="T">Type of the fragments to be returned</typeparam>
        /// <param name="path">Dotted path through document. If the path can't be found, an empty enumeration will be returned.
        /// An empty path is equivalent to `Defrost&lt;T&gt;`</param>
        /// <param name="json">The JSON document string to read</param>
        public static IEnumerable<T> DefrostFromPath<[MeansImplicitUse]T>(string path, string json)
        {
            if (string.IsNullOrWhiteSpace(path)) {
                return new[] { Defrost<T>(json) };
            }
            
            return Instance.SelectObjects<T>(json, path, null, null);
        }

        /// <summary> Create a copy of an object through serialisation </summary>
        public static T Clone<[MeansImplicitUse]T>(T obj)
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

        /// <summary>
        /// Pretty print a JSON string. This is done without value parsing.
        /// <p/>
        /// Note that any JS comments in the input are removed in the output.
        /// </summary>
        public static string Beautify(string input)
        {
            return Formatter.PrettyPrint(input);
        }
        
        /// <summary>
        /// Pretty print a JSON data stream to another stream.
        /// This is done without value parsing or buffering, so very large streams can be processed.
        /// The input and output encodings can be the same or different.
        /// <p/>
        /// Note that any JS comments in the input are removed in the output.
        /// </summary>
        public static void BeautifyStream(Stream input, Encoding inputEncoding, Stream output, Encoding outputEncoding)
        {
            Formatter.PrettyStream(input, inputEncoding, output, outputEncoding);
        }
        
        /// <summary>Fill the members of an .Net object from a JSON object string</summary>
        /// <remarks>Alias for <see cref="FillObject"/></remarks>
        public static object? DefrostInto(object input, string json) { return FillObject(input, json); }

        /// <summary>Fill the members of an .Net object from a JSON object string</summary>
        public static object? FillObject(object input, string json)
        {
            var ht = new JsonParser(json, DefaultParameters.IgnoreCaseOnDeserialize).Decode() as Dictionary<string, object>;

            if (ht == null) return null;
            
            if (input is Type type)
            {
                Instance.ParseDictionary(ht, null, type, null, null);
                return null;
            }

            return Instance.ParseDictionary(ht, null, input.GetType(), input, null);
        }

        /// <summary>
        /// You can set these parameters globally for all calls
        /// </summary>
        [NotNull] public static readonly JsonParameters DefaultParameters = new JsonParameters();

        [NotNull]
        internal static Json Instance = new Json();
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
        
        /// <summary>
        /// Read public static properties and fields from a type, output as JSON
        /// </summary>
        internal string ToJsonStatics(Type type, JsonParameters param)
        {
            _jsonParameters = param.Clone();
            if (_jsonParameters.EnableAnonymousTypes) { _jsonParameters.UseExtensions = false; _jsonParameters.UsingGlobalTypes = false; }
            return new JsonSerializer(_jsonParameters).ConvertStaticsToJson(type);
        }

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
        private IEnumerable<T> SelectObjects<T>(object json, string path, Encoding? encoding, WarningSet? warnings)
        {
            var parser = ParserFromStreamOrStringOrBytes(json, encoding);
            var globalTypes = new Dictionary<string, object>();

            var ignoreCase = _jsonParameters?.IgnoreCaseOnDeserialize ?? DefaultParameters.IgnoreCaseOnDeserialize;
            
            var rawObject = parser.Decode();
            var pathParts = (ignoreCase ? NormaliseCase(path) : path).Split('.');

            return PathWalk<T>(rawObject, globalTypes, pathParts, 0, false, warnings);
        }

        /// <summary>
        /// Recursive helper for SelectObjects˂T˃
        /// </summary>
        private IEnumerable<T> PathWalk<T>(object? rawObject, Dictionary<string, object> globalTypes, string[] pathParts, int pathIndex, bool parentIsArray, WarningSet? warnings)
        {
            if (rawObject == null) yield break;
            
            var type = typeof(T);
            if (pathIndex >= pathParts.Length) {
                var container = StrengthenType(type, rawObject, globalTypes, warnings); // Really, convert the raw object to the target and output it
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
            var isIndexed = false;
            var index = 0;
            
            // check for indexing in the path element
            if (step.EndsWith("]"))
            {
                var bits = step.Split('[');
                
                var indexStr = bits[1].Trim(']');
                if (indexStr == "*") // special "[*]" syntax to cope with arrays-inside-arrays
                {
                    index = -1;
                }
                else
                {
                    if ((bits.Length != 2) || (!int.TryParse(bits[1].Trim(']'), out index)))
                        throw new Exception($"Invalid path element: '{step}'");
                    if (index < 0) throw new Exception($"Invalid array index: '{step}'");
                }

                step = bits[0]; // trim name
                isIndexed = true; // set index flag
                
                // now, if we have an element name, we expect to step into a dictionary and find an array
                if (step != "")
                {
                    var container = rawObject as Dictionary<string, object>;
                    if (container is null || !container.ContainsKey(step)) yield break; // doesn't exist at this path
                    rawObject = container[step]; // step down into container
                }
                else if (pathIndex >= pathParts.Length-1) // indexing the last step on a path
                {
                    if (parentIsArray) // we should enumerate the parent array on pick n-th elements from child
                    {
                        var elems = PathWalk<T>(rawObject, globalTypes, pathParts, pathIndex, false, warnings);
                        foreach (var elem in elems) { yield return elem; }
                        yield break;
                    }
                }
            }

            switch (rawObject)
            {
                case Dictionary<string, object> objects:
                {
                    if (!objects.ContainsKey(step)) yield break;
                    var next = objects[step];

                    if (isIndexed) // try to step into an array
                    {
                        var arrayList = next as ArrayList;
                        if (arrayList is null) yield break; // this path point isn't an array
                        if (index < 0) // we used the '*' syntax
                        {
                            foreach (var item in arrayList)
                            {
                                var indexElems = PathWalk<T>(item, globalTypes, pathParts, pathIndex+1, true, warnings);
                                foreach (var elem in indexElems) { yield return elem; }
                            }
                        }
                        else // we want a single item here
                        {
                            if (index >= arrayList.Count) yield break; // not available on this path
                            var item = arrayList[index];
                            var indexElems = PathWalk<T>(item, globalTypes, pathParts, pathIndex+1, true, warnings);
                            foreach (var elem in indexElems) { yield return elem; }
                        }
                    }

                    var elems = PathWalk<T>(objects[step], globalTypes, pathParts, pathIndex + 1, false, warnings);
                    foreach (var elem in elems) { yield return elem; }
                    yield break;
                }

                case ArrayList arrayList:
                {
                    if (isIndexed) // try to return a single item out of this path
                    {
                        if (index >= arrayList.Count) yield break; // not available on this path

                        if (index < 0) // '*' option
                        {
                            foreach (var item in arrayList)
                            {
                                var indexElems = PathWalk<T>(item, globalTypes, pathParts, pathIndex+1, true, warnings);
                                foreach (var elem in indexElems) { yield return elem; }
                            }
                        }
                        else // specific index
                        {
                            var item = arrayList[index];
                            var elems = PathWalk<T>(item, globalTypes, pathParts, pathIndex + 1, true, warnings);
                            foreach (var elem in elems)
                            {
                                yield return elem;
                            }
                        }

                        yield break;
                    }

                    foreach (var item in arrayList) // return every child of this path
                    {
                        var elems = PathWalk<T>(item, globalTypes, pathParts, pathIndex, true, warnings); // ignore arrays while walking path name elements
                        foreach (var elem in elems)
                        {
                            yield return elem;
                        }
                    }
                    yield break;
                }

                default:
                {
                    if (step != "") throw new Exception($"Don't understand this JSON at {step}");

                    var elems = PathWalk<T>(rawObject, globalTypes, pathParts, pathIndex + 1, false, warnings);
                    foreach (var elem in elems)
                    {
                        yield return elem;
                    }

                    yield break;
                }
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
            parser.UseWideNumbers = type is not null;

            var decodedObject = parser.Decode();
            if (decodedObject == null) return new {};

            var warnings = new WarningSet();
            try
            {
                var result = StrengthenType(type, decodedObject, globalTypes, warnings);
                if (result is null) throw new Exception("Can't interpret json as type " + type + warnings);

                return result;
            }
            catch (Exception ex)
            {
                if (warnings.Any) throw new Exception(ex.Message + warnings, ex);
                throw;
            }
        }

        /// <summary>
        /// Try to decode a parsed json object into a new type instance
        /// </summary>
        /// <param name="type">Target output type</param>
        /// <param name="decodedObject">raw memory map of json</param>
        /// <param name="globalTypes">cache of type matches</param>
        /// <param name="warnings">Additional information will be added to this</param>
        private object? StrengthenType(Type? type, object decodedObject, Dictionary<string, object> globalTypes, WarningSet? warnings)
        {
            switch (decodedObject)
            {
                // TODO: See if this can be integrated with `TryMakeStandardContainer()`
                case Dictionary<string, object> objects:
                    return ParseDictionary(objects, globalTypes, type, null, warnings);

                case ArrayList arrayList:
                    if (type?.IsArray == true)
                    {
                        var elementType = type.GetElementType() ?? typeof(object);
                        var list = ConvertToList(elementType, globalTypes, arrayList, warnings);
                        return ListToArray(list, elementType);
                    }
                    else
                    {
                        var containedType = (type?.GetGenericArguments().SingleOrDefault() ?? type) ?? typeof(object);
                        
                        var setType = GenericSetInterfaceType(containedType);
                        if (type == setType) return ConvertToSet(containedType, globalTypes, arrayList, warnings);
                        return ConvertToList(containedType, globalTypes, arrayList, warnings);
                    }

                default:
                    return type is null ? decodedObject : ConvertItem(type, globalTypes, decodedObject, warnings);
            }
        }

        private Array ListToArray(IList list, Type elementType)
        {
            var x = new ArrayList(list);
            return x.ToArray(elementType);
        }

        private IList ConvertToList(Type elementType, Dictionary<string, object> globalTypes, ArrayList arrayList, WarningSet? warnings)
        {
            var list = (IList) Activator.CreateInstance(GenericListType(elementType))!;
            foreach (var obj in arrayList)
            {
                list.Add(ConvertItem(elementType, globalTypes, obj, warnings));
            }

            return list;
        }

        private object? ConvertItem(Type elementType, Dictionary<string, object> globalTypes, object? obj, WarningSet? warnings)
        {
            if (obj == null) return obj;
            if (obj.GetType().IsAssignableFrom(elementType)) return obj;
            if (obj is WideNumber wide) return wide.CastTo(elementType, out _);
            
            // a complex type?
            var dict = obj as Dictionary<string, object>;
            if (dict == null) throw new Exception($"Element {obj.GetType().Name} not assignable to the array type {elementType.Name}");
            var parsed = ParseDictionary(dict, globalTypes, elementType, null, warnings);
            return parsed;
        }

        private object ConvertToSet(Type elementType, Dictionary<string, object> globalTypes, ArrayList arrayList, WarningSet? warnings)
        {
            var set = Activator.CreateInstance(GenericHashSetType(elementType));
            var adder = set?.GetType().GetMethod("Add", BindingFlags.Public|BindingFlags.Instance) ?? throw new Exception("Failed to find add method on set");
            foreach (var obj in arrayList)
            {
                if (obj == null) continue;
                
                var toAdd = ConvertItem(elementType, globalTypes, obj, warnings);
                adder.Invoke(set, new []{toAdd});
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
        readonly List<Type> _assemblyCache = new List<Type>();

        /// <summary>
        /// Try to get or build a type for a given type-name
        /// </summary>
        private Type? GetTypeFromCache(string typename) {
            if (_typeCache.TryGetValue(typename, out Type val)) return val;

            var typeParts = typename.Split(',');

            Type? t;
            if (typeParts.Length > 1) {
                var assemblyName = typeParts[1];
                var fullName = typeParts[0]!;
                var available = Assembly.Load(assemblyName!)?.GetTypes();
                t = available?.SingleOrDefault(type => type?.FullName?.Equals(fullName, StringComparison.OrdinalIgnoreCase) == true);
            } else if (typeParts.Length == 1) {
                // slow but robust way of finding a type fragment.
                if (_assemblyCache.Count < 1) { _assemblyCache.AddRange(AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes())); }
                t = _assemblyCache.SingleOrDefault(type => type.FullName?.StartsWith(typeParts[0]!, StringComparison.OrdinalIgnoreCase) ?? false);
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
            catch (Exception)
            {
                return null;
            }
        }

        private static bool TryMakeStandardContainer(Type objType, out object? inst)
        {
            inst = null;
            switch (objType.Name)
            {
                case "IDictionary`2":
                {
                    inst = CreateGenericInstance(objType, typeof(Dictionary<,>));
                    return true;
                }
                case "IEnumerable`1":
                case "IList`1":
                case "IContainer`1":
                {
                    inst = CreateGenericInstance(objType, typeof(List<>));
                    return true;
                }
                case "ISet`1":
                {
                    inst = CreateGenericInstance(objType, typeof(HashSet<>));
                    return true;
                }
            }

            return false;
        }

        private static object CreateGenericInstance(Type interfaceType, Type concreteType)
        {
            var typeParameters = interfaceType.GetGenericArguments();
            var constructed = concreteType.MakeGenericType(typeParameters);
            return Activator.CreateInstance(constructed)!;
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

        readonly SafeDictionary<Type, List<Getters>> _getterCache = new();
        readonly SafeDictionary<string, SafeDictionary<string, TypePropertyInfo>> _propertyCache = new();
        
        internal delegate object GenericGetter(object obj);

        /// <param name="target">object instance to accept the value</param>
        /// <param name="value">value of property to set</param>
        /// <param name="key">optional key for dictionaries</param>
        private delegate void GenericSetter(object? target, object value, object? key);

        /// <summary>
        /// Read a weakly-typed dictionary tree into a strong type. If the keys do not match exactly,
        /// all matching field/properties will be filled.
        /// If *no* keys match the target type, this will return `null`
        /// <p></p>
        /// If the input object is null, but the input type is not,
        /// this will attempt to fill static members of the given type.
        /// </summary>
        private object? ParseDictionary(IDictionary<string, object> jsonValues, IDictionary<string, object>? globalTypes, Type? type, object? input, WarningSet? warnings)
        {
            _jsonParameters ??= DefaultParameters;

            TryFillGlobalTypes(jsonValues, globalTypes);

            var found = jsonValues.TryGetValue("$type", out var tn);
            if (found == false && type == typeof(object))
            {
                var ds = CreateDataset(jsonValues, globalTypes);
                return ds != null ? ds : jsonValues;
            }
            if (found)
            {
                if (_usingGlobals && globalTypes != null)
                {
                    if (globalTypes.TryGetValue((string)tn!, out object tName)) tn = tName;
                }
                if (type is null || !type.IsInterface) type = GetTypeFromCache((string)tn!);
            }

            var targetObject = input ?? FastCreateInstance(type);

            if (targetObject is null && type is null) return jsonValues; // can't work out what object to fill, send back the raw values

            var targetType = targetObject?.GetType() ?? type;
            if (targetType is null) throw new Exception("Unable to determine type of object");

            var props = GetProperties(targetType, targetType.Name);
            if (!IsDictionary(targetType) && NoPropertiesMatch(props, jsonValues.Keys))
            {
                if (!_jsonParameters.IgnoreCaseOnDeserialize &&
                    warnings is not null &&
                    PropertiesWouldMatchCaseInsensitive(props, jsonValues.Keys))
                {
                    warnings.Append($"; Properties would match if {nameof(DefaultParameters.IgnoreCaseOnDeserialize)} was set to true");
                }

                if (jsonValues.Count > 0)
                {
                    // unless we were passed an empty object, this type doesn't match
                    if (_jsonParameters.StrictMatching)
                    {
                        // if we are doing strict matching (the default), then reject the mapping
                        // (otherwise we will continue and return whatever container we made)
                        warnings?.Append($"; Expected to match at least one of [{string.Join(", ", jsonValues.Keys)}]");
                        return null;
                    }
                    
                }
            }

            foreach (var key in jsonValues.Keys)
            {
                MapJsonValueToObject(key, targetType, targetObject, jsonValues, globalTypes, props, warnings);
            }

            return targetObject;
        }

        private bool PropertiesWouldMatchCaseInsensitive(SafeDictionary<string,TypePropertyInfo> props, ICollection<string> jsonValuesKeys)
        {
            var normalKeys = props.Keys.Select(NormaliseCase);
            return jsonValuesKeys.Any(jsonKey => normalKeys.Contains(NormaliseCase(jsonKey)));
        }

        private void TryFillGlobalTypes(IDictionary<string, object> jsonValues, IDictionary<string, object>? globalTypes)
        {
            if (jsonValues.TryGetValue("$types", out var tn))
            {
                if (globalTypes != null)
                {
                    var dic = ((Dictionary<string, object>)tn!);
                    foreach (var kvp in dic)
                    {
                        if (kvp.Value != null) globalTypes.Add((string)kvp.Value, kvp.Key);
                    }

                    _usingGlobals = true;
                }
            }
        }

        private static bool IsDictionary(Type? targetType)
        {
            return targetType?.GetInterface("IDictionary") != null;
        }

        private static bool NoPropertiesMatch(SafeDictionary<string, TypePropertyInfo> props, ICollection<string> jsonValuesKeys)
        {
            return jsonValuesKeys.All(jsonKey => !props.HasKey(jsonKey));
        }

        /// <summary>
        /// Map json value dictionary to the properties and fields of a target object instance.
        /// </summary>
        void MapJsonValueToObject(string objectKey, Type? targetType, object? targetObject, IDictionary<string, object> jsonValues, IDictionary<string, object>? globalTypes,
            SafeDictionary<string, TypePropertyInfo> props, WarningSet? warnings)
        {
            var name = objectKey;
            if (_jsonParameters?.IgnoreCaseOnDeserialize == true) name = NormaliseCase(name);
            if (name == "$map" && targetObject is not null)
            {
                ProcessMap(targetObject, props, jsonValues[name] as Dictionary<string, object>);
                return;
            }

            if (props.TryGetValue(name, out var propertyInfo) == false) {
                if (targetObject is IDictionary) {
                    var ok = props.TryGetValue("Item", out propertyInfo);
                    if (!ok) return;
                }
                else return;
            }
            if (!propertyInfo.filled) return;
            var v = jsonValues[name];

            if (v == null) return;

            object setObj;
            try
            {
                setObj = MakeSettableObject(globalTypes, propertyInfo, v, warnings);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to convert json {v.GetType()} to target object {propertyInfo.changeType?.ToString() ?? "<unknown>"} on property {propertyInfo.Name}", ex);
            }

            try
            {
                if (IsDictionary(targetType))
                {
                    // We want to add a key/value rather than setting a single value
                    AddToDictionary(name, setObj, targetObject, propertyInfo);
                }
                else
                {
                    if (propertyInfo.CanWrite) WriteValueToTypeInstance(name, targetType, targetObject, propertyInfo, setObj);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to write value from json {v.GetType()} to target object {propertyInfo.changeType?.ToString() ?? "<unknown>"} on property {propertyInfo.Name}", ex);
            }
        }

        private void AddToDictionary(string key, object value, object? target, TypePropertyInfo typePropertyInfo)
        {
            if (target is null) throw new Exception("Target dictionary was null");

            if (target is Dictionary<string, object> simple)
            {
                simple.Add(key, value);
            }
            else if (target is Dictionary<string, string> shallow)
            {
                if (value is string str)
                {
                    shallow.Add(key, str);
                }
                else
                {
                    shallow.Add(key, ToJson(value, _jsonParameters ?? DefaultParameters));
                }
            }
            else if (target is IDictionary other)
            {
                other.Add(key, value);
            }
            else
            {
                throw new Exception("Invalid dictionary target");
            }
        }

        /// <summary>
        /// Try to create an object instance that can be directly assigned to the property or field
        /// defined by 'propertyInfo'.
        /// The value will be the best interpretation of 'inputValue' that is available.
        /// </summary>
        private object MakeSettableObject(IDictionary<string, object>? globalTypes, TypePropertyInfo propertyInfo, object inputValue, WarningSet? warnings)
        {
            object setObj;
            var precisionLoss = false;

            if (propertyInfo.isNumeric)
                setObj = CastNumericType(inputValue, propertyInfo, out precisionLoss)
                         ?? throw new Exception($"Failed to map input type '{inputValue.GetType().Name}' to target '{propertyInfo.parameterType?.Name ?? "<null>"}'");

            else if (propertyInfo.isString) setObj = inputValue;
            else if (propertyInfo.isBool) setObj = InterpretBool(inputValue);
            else if (propertyInfo.isGenericType && propertyInfo.isValueType == false && propertyInfo.isDictionary == false 
                     && propertyInfo.isEnumerable && inputValue is IEnumerable)
                setObj = CreateGenericList((ArrayList)inputValue, propertyInfo.parameterType, propertyInfo.bt, globalTypes);
            
            else if (propertyInfo.isByteArray)
                setObj = ConvertBytes(inputValue);

            else if (propertyInfo.isArray && propertyInfo.isValueType == false)
                setObj = CreateArray((ArrayList)inputValue, propertyInfo.bt, globalTypes);
            else if (propertyInfo.isGuid)
                setObj = CreateGuid((string)inputValue);
            else if (propertyInfo.isDataSet)
                setObj = CreateDataset((Dictionary<string, object>)inputValue, globalTypes) ?? throw new Exception("Failed to create dataset");

            else if (propertyInfo.isDataTable)
                setObj = CreateDataTable((Dictionary<string, object>)inputValue, globalTypes);

            else if (propertyInfo.isStringDictionary)
                setObj = CreateStringKeyDictionary((Dictionary<string, object>)inputValue, propertyInfo.parameterType, propertyInfo.GenericTypes, globalTypes);

            else if (propertyInfo.isDictionary || propertyInfo.isHashtable)
                setObj = CreateDictionary((ArrayList)inputValue, propertyInfo.parameterType, propertyInfo.GenericTypes, globalTypes);

            else if (propertyInfo.isEnum)
            {
                if (inputValue is string valStr) setObj = CreateEnum(propertyInfo.parameterType, valStr);
                else if (inputValue is WideNumber wn)
                    setObj = NumberToEnum(propertyInfo.parameterType, wn) ?? throw new Exception($"Failed to convert number ({wn}) to enum {propertyInfo.parameterType?.Name ?? "<null>"}");
                else throw new Exception($"Failed to convert value of type {inputValue.GetType().Name} to enum {propertyInfo.parameterType?.Name ?? "<null>"}");
            }

            else if (propertyInfo.isDateTime)
                setObj = CreateDateTime(inputValue);

            else if (propertyInfo.isTimeSpan)
                setObj = CreateTimeSpan(inputValue);

            else if (propertyInfo.isClass && inputValue is Dictionary<string, object> objects)
            {
                setObj = ParseDictionary(objects, globalTypes, propertyInfo.parameterType, null, warnings)
                         ?? throw new Exception($"Failed to map to class '{propertyInfo.parameterType?.Name ?? "<null>"}'");
            }
            else if (propertyInfo.isInterface && inputValue is Dictionary<string, object> proxyObjects)
                setObj = ParseDictionary(proxyObjects, globalTypes, propertyInfo.parameterType, null, warnings)
                         ?? throw new Exception("Failed to map to proxy class of interface");

            else if (propertyInfo.isValueType)
                setObj = ChangeType(inputValue, propertyInfo.changeType) ?? throw new Exception("Failed to create value type");
            else if (inputValue is ArrayList list)
                setObj = CreateArray(list, typeof(object), globalTypes);
            else
                setObj = inputValue;


            if (precisionLoss)
            {
                warnings?.Append($"{propertyInfo.Name} may have lost precision");
            }


            return setObj;
        }

        private static object? CastNumericType(object inputValue, TypePropertyInfo p, out bool precisionLost)
        {
            if (inputValue is WideNumber w)
            {
                var result = w.CastTo(p.parameterType, out var loss);
                precisionLost = loss;
                return result;
            }

            if (inputValue is double)
            {
                precisionLost = false; // could be...?
                return ChangeType(inputValue, p.parameterType);
            }

            precisionLost = false;
            return null;
        }

        /// <summary>
        /// Our default is Base64, but we will fall back on hex if it's input
        /// </summary>
        private static byte[] ConvertBytes(object inputValue)
        {
            try
            {
                return Convert.FromBase64String((string)inputValue);
            }
            catch
            {
                return HexToByteArray((string)inputValue) ?? throw new Exception("Input to byte array was not valid Base64 or hex string");
            }
        }
        
        /// <summary>
        /// Convert a hex string to a byte array.
        /// <p/>
        /// Use <c>Convert.FromHexString</c> where available
        /// </summary>
        public static byte[]? HexToByteArray(string? hex)
        {
            if (hex is null || string.IsNullOrWhiteSpace(hex)) return null;
            
            var temp = new List<byte>(hex.Length / 2);
            var shift = 4; // byte position
            var v = 0;
            
            for (var i = 0; i < hex.Length; i++)
            {
                var c = hex[i];
                switch (c)
                {
                    case >= '0' and <= '9': v |= (c - '0') << shift; break;
                    case >= 'A' and <= 'F': v |= (c - 'A' + 10) << shift; break;
                    case >= 'a' and <= 'f': v |= (c - 'a' + 10) << shift; break;
                    case '-': case ' ': continue; // accept spaces and hyphens as separators
                    default: return null; // invalid character
                }
                
                shift = 4 - shift;
                if (shift != 4) continue;
                temp.Add((byte)v);
                v = 0;
            }
            if (shift != 4) return null; // did not complete final byte
            return temp.ToArray();
        }

        private bool InterpretBool(object o)
        {
            if (o is bool b) return b;
            if (o is string s)
            {
                if (_jsonParameters?.IgnoreCaseOnDeserialize == true) s = NormaliseCase(s);
                switch (s)
                {
                    case "true": return true;
                    case "false": return false;
                }
            }
            throw new Exception("Can't interpret '"+o+"' as a boolean");
        }

        /// <summary>
        /// Inject a value into an object's property.
        /// If object is null, we will attempt to write to a static member.
        /// </summary>
        static void WriteValueToTypeInstance(string name, Type? type, object? targetObject, TypePropertyInfo pi, object objSet) {
            try
            {
                var typ = targetObject?.GetType() ?? type;
                if (typ is null) throw new Exception("No type information survived into WriteValueToTypeInstance");

                if (typ.IsValueType)
                {
                    var fi = typ.GetField(pi.Name);
                    if (fi != null)
                    {
                        fi.SetValue(targetObject!, objSet);
                        return;
                    }
                    var pr = typ.GetProperty(pi.Name, BindingFlags.Instance | BindingFlags.Public);
                    if (pr != null)
                    {
                        pr.SetValue(targetObject!, objSet, null!);
                        return;
                    }
                }

                if (pi.isDictionary || pi.isHashtable) // use display name
                {
                    pi.setter?.Invoke(targetObject!, objSet, name);
                }
                else // use reflection name
                {
                    pi.setter?.Invoke(targetObject!, objSet, pi.Name);
                }

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
            var usePrivateFields = typename.StartsWith("Tuple`", StringComparison.Ordinal) || IsAnonymousType(type);
            var ignoreCase = _jsonParameters?.IgnoreCaseOnDeserialize ?? DefaultParameters.IgnoreCaseOnDeserialize;

            if (_propertyCache.TryGetValue(typename, out SafeDictionary<string, TypePropertyInfo> sd)) return sd;
            sd = new SafeDictionary<string, TypePropertyInfo>();

            var pr = new List<PropertyInfo>();

            // Instance fields and static fields
            var fi = new List<FieldInfo>();
            fi.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
            fi.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Static));

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
                var d = CreateMyProp(f.FieldType, f.Name);
                d.setter = CreateSetField(type, f);
                d.getter = CreateGetField(type, f);

                sd.Add(f.Name, d);
                if (ignoreCase) sd.TryAdd(NormaliseCase(f.Name), d);
                if (usePrivateFields){
                    var privateName = f.Name.Replace("m_", "");
                    sd.Add(AnonFieldFilter(privateName), d);
                    if (ignoreCase) sd.TryAdd(NormaliseCase(privateName), d);
                }
            }

            // Instance properties
            pr.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
            foreach (var prop in type.GetInterfaces()
                         .SelectMany(i => i.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(prop => pr.All(p => p.Name != prop.Name)))) {
                pr.Add(prop);
            }

            foreach (var p in pr)
            {
                var d = CreateMyProp(p.PropertyType, p.Name);
                d.CanWrite = p.CanWrite;
                d.setter = CreateSetMethod(p);
                if (d.setter == null) continue;
                d.getter = CreateGetMethod(p);
                sd.Add(p.Name, d);
                if (ignoreCase) sd.TryAdd(NormaliseCase(p.Name), d);
            }
            
            // Static properties are special
            var staticProps = type.GetProperties(BindingFlags.Public | BindingFlags.Static);
            foreach (var sp in staticProps)
            {
                var d = CreateMyProp(sp.PropertyType, sp.Name);
                d.CanWrite = sp.CanWrite;
                d.setter = (_, value, _) => sp.SetValue(null!, value);
                d.getter = _ => sp.GetValue(null!)!;
                sd.Add(sp.Name, d);
                if (ignoreCase) sd.TryAdd(NormaliseCase(sp.Name), d);
            }

            if (type.GetGenericArguments().Length < 1) {
                _propertyCache.Add(typename, sd);
            }
            return sd;
        }

        /// <summary>
        /// Convert a string to lower case, removing a set of joining and non-printing characters
        /// </summary>
        public static string NormaliseCase(string? src)
        {
            if (src is null || src.Length < 1) return "";
            var sb = new StringBuilder();

            foreach (var c in src)
            {
                if (c <= ' ') continue;
                if (c == '_') continue;
                if (c == '-') continue;
                if (char.IsControl(c)) continue;
                if (char.IsWhiteSpace(c)) continue;
                if (char.IsSeparator(c)) continue;
                sb.Append(char.ToLowerInvariant(c));
            }

            return sb.ToString();
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
        /// Return a list of property/field access proxies for a type
        /// </summary>
        internal List<Getters> GetGetters(Type type)
        {
            if (_getterCache.TryGetValue(type, out List<Getters> val)) return val;
            _jsonParameters ??= DefaultParameters;

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var getters = (from p in props where (p.CanWrite || _jsonParameters.ShowReadOnlyProperties || _jsonParameters.EnableAnonymousTypes)
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
                var gg = new Getters {Name = f.Name, Getter = g, PropertyType = f.FieldType, FieldInfo = f};
                getters.Add(gg);
            }

            _getterCache.Add(type, getters);
            return getters;
        }

        /// <summary>
        /// Read reflection data for a type
        /// </summary>
        private static TypePropertyInfo CreateMyProp(Type t, string name)
        {
            var d = new TypePropertyInfo { 
                filled = true,
                Name = name,
                CanWrite = true,
                parameterType = t,
                isDictionary = t.Name.Contains("Dictionary")
            };
            
            if (d.isDictionary) d.GenericTypes = t.GetGenericArguments();
            if (d.isDictionary && d.GenericTypes?.Length > 0 && d.GenericTypes[0] == typeof(string))
                d.isStringDictionary = true;

            d.isInterface = t.IsInterface;
            d.isValueType = t.IsValueType;
            d.isEnumerable = t.GetInterfaces().Contains(typeof(IEnumerable));
            
            d.isGenericType = t.IsGenericType;
            if (d.isGenericType) d.bt = t.GetGenericArguments().FirstOrDefault();
            
            d.isArray = t.IsArray;
            if (d.isArray) d.bt = t.GetElementType();
            
            d.isByteArray = t == typeof(byte[]);
            d.isHashtable = t == typeof(Hashtable);
            d.isDataSet = t == typeof(DataSet);
            d.isDataTable = t == typeof(DataTable);
            d.changeType = GetChangeType(t);
            
            d.isEnum = t.IsEnum;
            d.isClass = t.IsClass;
            
            if (IsNullableWrapper(t)) t = t.GetGenericArguments().FirstOrDefault() ?? t;
            
            d.isGuid = t == typeof(Guid);
            d.isDateTime = t == typeof(DateTime);
            d.isTimeSpan = t == typeof(TimeSpan);
            d.isString = t == typeof(string);
            d.isBool = t == typeof(bool);
            
            // Check for number types
            d.isNumeric = t == typeof(sbyte)   || t == typeof(short)  || t == typeof(int)  || t == typeof(long)
                       || t == typeof(byte)    || t == typeof(ushort) || t == typeof(uint) || t == typeof(ulong)
                       || t == typeof(decimal) || t == typeof(float)  || t == typeof(double);

            return d;
        }

        /// <summary>
        /// Try to create a value-setting proxy for an object property
        /// </summary>
        static GenericSetter? CreateSetMethod(PropertyInfo propertyInfo)
        {
            if (propertyInfo.GetSetMethod(true) == null) return null;

            var indexes = propertyInfo.GetIndexParameters();

            if (indexes.Length < 1) { return (a, b, _) => propertyInfo.SetValue(a, b, null!); }
            if (indexes.Length < 2) { return (a, b, k) => propertyInfo.SetValue(a, b, new []{ k }); }

            return (_, _, _) => throw new Exception("Multiple index data types are not supported");
        }

        /// <summary>
        /// Create a value-reading proxy for an object field
        /// </summary>
        static GenericGetter CreateGetField(Type type, FieldInfo fieldInfo)
        {
            var dynamicGet = new DynamicMethod("_"+fieldInfo.Name, typeof(object), new[] { typeof(object) }, type, true);
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
        static GenericGetter? CreateGetMethod(PropertyInfo propertyInfo)
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
            il.EmitCall(OpCodes.Callvirt, getMethod, null!);

            if (!propertyInfo.PropertyType.IsClass)
                il.Emit(OpCodes.Box, propertyInfo.PropertyType);

            il.Emit(OpCodes.Ret);

            try
            {
                return (GenericGetter)getter.CreateDelegate(typeof(GenericGetter));
            }
            catch (InvalidProgramException)
            {
                // This seems to happen with dotnet6 in some conditions.
                // We fall back to some reflection work.
                return obj => GetProperty(obj, propertyInfo);
            }
        }

        private static object GetProperty(object src, PropertyInfo propertyInfo)
        {
            return propertyInfo.GetGetMethod()?.Invoke(src, new object[0]) ?? throw new System.Exception($"Could not get value for {propertyInfo.Name}");
        }

        /// <summary>
        /// Convert between runtime types
        /// </summary>
        static object? ChangeType(object? value, Type? conversionType)
        {
            if (value == null || conversionType == null) return null;
            if (conversionType == typeof(int)) return (int)CreateLong(value);
            if (conversionType == typeof(long)) return CreateLong(value);
            if (conversionType == typeof(string)) return value;
            if (conversionType == typeof(Guid)) return CreateGuid((string)value);
            if (conversionType.IsEnum)
            {
                if (value is string str) return CreateEnum(conversionType, str);
                if (value is WideNumber wn) return NumberToEnum(conversionType, wn);
            }

            return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
        }

        static void ProcessMap(object obj, SafeDictionary<string, TypePropertyInfo> props, Dictionary<string, object>? dic)
        {
            if (dic == null) return;
            foreach (var kv in dic)
            {
                if (kv.Value is null || kv.Key is null) continue;
                var p = props[kv.Key];
                var o = p.getter?.Invoke(obj);
                var t = Type.GetType((string)kv.Value);
                
                var s = o as string;
                if (s == null) continue;
                if (t == typeof(Guid)) p.setter?.Invoke(obj, CreateGuid(s), null);
            }
        }

        static long CreateLong(object? obj){
            if (obj is null) return 0;
            if (obj is string s) return ParseLong(s);
            if (obj is double d) return (long)d;
            if (obj is WideNumber w) return w.ToLong();
            throw new Exception("Unsupported int type: "+obj.GetType());
        }
        static double CreateDouble(object? obj){
            if (obj is null) return 0;
            if (obj is string s) return double.Parse(s);
            if (obj is double d) return d;
            if (obj is WideNumber w) return w.ToDouble();
            throw new Exception("Unsupported numeric type: "+obj.GetType());
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

        static object CreateEnum(Type? pt, string v)
        {
            if (pt == null) throw new Exception("Invalid property type");
            return Enum.Parse(pt, v);
        }
        

        private static object? NumberToEnum(Type? pt, WideNumber wn)
        {
            if (pt == null) throw new Exception("Invalid property type");
            
            return Enum.ToObject(pt, wn.ToLong());
        }

        static Guid CreateGuid(string s)
        {
            return s.Length > 30 ? new Guid(s) : new Guid(Convert.FromBase64String(s));
        }

        /// <summary>
        /// Date formats we expect from JSON strings
        /// </summary>
        protected static readonly string[] DateFormatsInPreferenceOrder = {
            // ReSharper disable StringLiteralTypo
            "yyyy-MM-ddTHH:mm:ss",  // correct ISO 8601 'extended'
            "yyyy-MM-dd HH:mm:ss",  // our old output format
            "yyyy-MM-dd H:mm:ss",   // Erlang style
            "yyyy-MM-ddTH:mm:ss",   // Erlang style with a T
            "yyyy-MM-ddTHH:mm:ssK", // with zone specifier
            "yyyy-MM-dd HH:mm:ssK", // with zone specifier, but no T
            "yyyy-MM-ddTHHmmss",    // ISO 8601 'basic'
            "yyyy-MM-dd",           // ISO 8601, just the date
            // ReSharper restore StringLiteralTypo
        };

        private DateTime CreateDateTime(object? value)
        {
            _jsonParameters ??= DefaultParameters;
            if (value is null) return DateTime.MinValue;

            if (value is WideNumber wide) return InterpretNumberAsDate(wide.ToLong());
            if (value is long ticksLong) return InterpretNumberAsDate(ticksLong);
            if (value is double ticksDouble) return InterpretNumberAsDate((long)ticksDouble);

            var str = value.ToString();
            if (str is null) return DateTime.MinValue;
            
            var style = _jsonParameters.UseUtcDateTime ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.AssumeLocal;
            if (str.EndsWith("Z")) style = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

            foreach (var format in DateFormatsInPreferenceOrder)
            {
                if (DateTime.TryParseExact(str, format, null!, style, out var dateVal))
                {
                    return dateVal;
                }
            }

            // None of our preferred formats, so let .Net guess
            return DateTime.Parse(str, null!, style);
        }

        private static DateTime InterpretNumberAsDate(long value)
        {
            var asTicks = new DateTime(value);
            if (asTicks.Year > 1900 && asTicks.Year < 3000) return asTicks;
            
            var asUnixMs = new DateTime(1970,1,1,0,0,0, DateTimeKind.Utc).AddMilliseconds(value);
            if (asUnixMs.Year > 1980 && asUnixMs.Year < 3000) return asUnixMs;
            
            var asUnixSeconds = new DateTime(1970,1,1,0,0,0, DateTimeKind.Utc).AddSeconds(value);
            return asUnixSeconds;
        }

        private TimeSpan CreateTimeSpan(object? v)
        {
            if (v is null) return TimeSpan.Zero;
            if (v is string str) return TimeSpan.Parse(str);

            if (v is Dictionary<string, object> objects)
            {
                // TimeSpan is tricky, and can vary based on Framework version
                // So we pick it apart manually.
                if (objects.ContainsKey("Ticks")) return new TimeSpan(ticks: CreateLong(objects["Ticks"]));
                if (objects.ContainsKey("ticks")) return new TimeSpan(ticks: CreateLong(objects["ticks"]));
                
                if (objects.ContainsKey("TotalSeconds")) return TimeSpan.FromSeconds(CreateDouble(objects["TotalSeconds"]));
                if (objects.ContainsKey("total_seconds")) return TimeSpan.FromSeconds(CreateDouble(objects["total_seconds"]));
                
                var days = (int)TryGetDouble(objects, "Days");
                var hours = (int)TryGetDouble(objects, "Hours");
                var minutes = (int)TryGetDouble(objects, "Minutes");
                var seconds = (int)TryGetDouble(objects, "Seconds");
                var milliseconds = (int)TryGetDouble(objects, "Milliseconds");
                return new TimeSpan(days, hours, minutes, seconds, milliseconds);
            }
            
            throw new Exception("Failed to map to TimeSpan");
        }

        /// <summary>
        /// Try to get a keyed value as a double, or return zero. 
        /// </summary>
        private double TryGetDouble(Dictionary<string,object> objects, string key)
        {
            if (objects.ContainsKey(key))
            {
                var obj = objects[key];
                if (obj is double d) return d;
                if (obj is string s && double.TryParse(s, out var dVal)) return dVal;
                return 0.0;
            }

            if (_jsonParameters?.IgnoreCaseOnDeserialize != true) return 0.0;
            
            // do a case insensitive search
            foreach (var objKey in objects.Keys)
            {
                if (!EqualsInNormaliseCase(objKey,key)) continue;
                
                var obj = objects[objKey];
                if (obj is double d) return d;
                if (obj is string s && double.TryParse(s, out var dVal)) return dVal;
            }
            return 0.0;
        }

        private static bool EqualsInNormaliseCase(string a, string b)
        {
            if (a == b) return true;
            if (a.Equals(b, StringComparison.InvariantCultureIgnoreCase)) return true;
            return NormaliseCase(a) == NormaliseCase(b);
        }

        object CreateArray(IEnumerable data, Type? elementType, IDictionary<string, object>? globalTypes)
        {
            if (elementType == null) throw new Exception("Invalid element type");
            var col = new ArrayList();
            foreach (var ob in data)
            {
                col.Add(
                    ob is IDictionary
                        ? ParseDictionary((Dictionary<string, object>) ob, globalTypes, elementType, null, null)
                        : ChangeType(ob, elementType)
                );
            }
            return col.ToArray(elementType);
        }

        object CreateGenericList(IEnumerable data, Type? pt, Type? bt, IDictionary<string, object>? globalTypes)
        {
            if (pt == null) throw new Exception("Invalid container type");
            if (bt == null) throw new Exception("Invalid element type");
            var col = FastCreateInstance(pt) as IList;
            if (col == null) throw new Exception("Failed to create instance of " + pt);
            foreach (var ob in data)
            {
                if (ob is IDictionary)
                    col.Add(ParseDictionary((Dictionary<string, object>)ob, globalTypes, bt, null, null));
                else if (ob is ArrayList list)
                    col.Add(list.ToArray());
                else
                    col.Add(ChangeType(ob, bt));
            }
            return col;
        }

        object CreateStringKeyDictionary(Dictionary<string, object> reader, Type? pt, IList<Type>? types, IDictionary<string, object>? globalTypes)
        {
            if (pt == null) throw new Exception("Target type was null");
            var col = FastCreateInstance(pt) as IDictionary;
            if (col == null) throw new Exception("Failed to create instance of " + pt);
            Type? t2 = null;
            if (types != null) t2 = types[1];

            foreach (var values in reader)
            {
                var key = values.Key;
                if (key == null) continue;
                object? val;
                if (values.Value is Dictionary<string, object> objects)
                {
                    val = ParseDictionary(objects, globalTypes, t2, null, null);
                }
                else
                {
                    val = ChangeType(values.Value, t2) ?? throw new Exception("Failed to change type");
                }
                if (val == null) continue;
                col.Add(key, val);
            }

            return col;
        }

        object CreateDictionary(IEnumerable reader, Type? pt, IList<Type>? types, IDictionary<string, object>? globalTypes)
        {
            if (pt == null) throw new Exception("Invalid container type");
            var col = FastCreateInstance(pt) as IDictionary;
            if (col == null) throw new Exception("Failed to create instance of " + pt);
            Type? t1 = null;
            Type? t2 = null;
            if (types != null)
            {
                t1 = types[0];
                t2 = types[1];
            }

            foreach (Dictionary<string, object> values in reader)
            {
                var key = values["k"];
                var val = values["v"];

                if (key is Dictionary<string, object> objects)
                    key = ParseDictionary(objects, globalTypes, t1, null, null);
                else
                    key = ChangeType(key, t1);

                if (val is Dictionary<string, object> dictionary)
                    val = ParseDictionary(dictionary, globalTypes, t2, null, null);
                else
                    val = ChangeType(val, t2);

                if (key != null && val != null) col.Add(key, val);
            }

            return col;
        }

        private static bool IsNullableWrapper(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        static Type GetChangeType(Type conversionType)
        {
            if (IsNullableWrapper(conversionType))
                return conversionType.GetGenericArguments()[0] ?? throw new Exception("Invalid generic arguments");

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
                if (pair.Key == null || pair.Key == "$type" || pair.Key == "$schema") continue;

                var rows = pair.Value as ArrayList;
                if (rows == null) continue;

                var dt = ds.Tables[pair.Key];
                ReadDataTable(rows, dt);
            }

            ds.EndInit();

            return ds;
        }

        bool ReadSchema(IDictionary<string, object>? reader, DataSet ds, IDictionary<string, object>? globalTypes)
        {
            if (reader?.ContainsKey("$schema") != true) return false;

            var schema = reader["$schema"];
            if (schema == null) return false;
            
            if (schema is string s)
            {
                TextReader tr = new StringReader(s);
                ds.ReadXmlSchema(tr);
            }
            else
            {
                var ms = ParseDictionary((Dictionary<string, object>)schema, globalTypes, typeof(DatasetSchema), null, null) as DatasetSchema;
                if (ms?.Info == null) return false;
                ds.DataSetName = ms.Name;
                for (int i = 0; i < ms.Info.Count; i += 3)
                {
                    var info = ms.Info[i];
                    if (info == null) continue;
                    
                    if (ds.Tables.Contains(info) == false) ds.Tables.Add(info);
                    
                    var info1 = ms.Info[i + 1];
                    var info2 = ms.Info[i + 2];
                    if (info1 == null || info2 == null) continue;
                    var type = Type.GetType(info2);
                    if (type == null) continue;
                    
                    ds.Tables[info]?.Columns.Add(info1, type);
                }
            }
            return true;
        }

        void ReadDataTable(IEnumerable rows, DataTable? dt)
        {
            if (dt == null) return;
            _jsonParameters ??= DefaultParameters;
            
            dt.BeginInit();
            dt.BeginLoadData();
            var guidCols = new List<int>();
            var dateCol = new List<int>();

            foreach (DataColumn c in dt.Columns)
            {
                if (c.DataType == typeof(Guid) || c.DataType == typeof(Guid?))
                    guidCols.Add(c.Ordinal);
                if (_jsonParameters.UseUtcDateTime && (c.DataType == typeof(DateTime) || c.DataType == typeof(DateTime?)))
                    dateCol.Add(c.Ordinal);
            }

            foreach (ArrayList row in rows)
            {
                var v = new object[row.Count];
                row.CopyTo(v, 0);
                foreach (int i in guidCols)
                {
                    if (v[i] is string s && s.Length < 36)
                        v[i] = new Guid(Convert.FromBase64String(s));
                }
                if (_jsonParameters.UseUtcDateTime)
                {
                    foreach (int i in dateCol)
                    {
                        if (v[i] is string s)
                            v[i] = CreateDateTime(s);
                    }
                }
                dt.Rows.Add(v);
            }

            dt.EndLoadData();
            dt.EndInit();
        }

        DataTable CreateDataTable(IDictionary<string, object> reader, IDictionary<string, object>? globalTypes)
        {
            var dt = new DataTable();

            // read dataset schema here
            var schema = reader.ContainsKey("$schema") != true ? null : reader["$schema"];

            switch (schema)
            {
                case string s:
                {
                    TextReader tr = new StringReader(s);
                    dt.ReadXmlSchema(tr);
                    break;
                }
                case Dictionary<string, object> dictSchema:
                {
                    if (ParseDictionary(dictSchema, globalTypes, typeof(DatasetSchema), null, null) is DatasetSchema ms && ms.Info != null)
                    {
                        dt.TableName = ms.Info[0];
                        for (int i = 0; i < ms.Info.Count; i += 3)
                        {
                            var info1 = ms.Info[i + 1];
                            var info2 = ms.Info[i + 2];

                            if (info1 == null || info2 == null) continue;

                            var type = Type.GetType(info2);
                            if (type == null) continue;
                            dt.Columns.Add(info1, type);
                        }
                    }

                    break;
                }
            }

            foreach (var pair in reader)
            {
                if (pair.Key == null || pair.Key == "$type" || pair.Key == "$schema") continue;

                var rows = pair.Value as ArrayList;
                
                if (rows == null) continue;
                if (dt.TableName?.Equals(pair.Key, StringComparison.InvariantCultureIgnoreCase) != true) continue;

                ReadDataTable(rows, dt);
            }

            return dt;
        }

        /// <summary>
        /// Reset to defaults and clear caches.
        /// </summary>
        public static void Reset()
        {
            DefaultParameters.Reset();
            ClearCaches();
        }

        /// <summary>
        /// Clear all caches.
        /// </summary>
        public static void ClearCaches()
        {
            Instance = new Json();
        }
    }

    internal class WarningSet
    {
        private readonly HashSet<string> _messages = new();
        public bool Any => _messages.Count > 0;

        public void Append(string msg)
        {
            _messages.Add(msg);
        }

        public override string ToString()
        {
            if (_messages.Count < 1) return "";
            return string.Join("; ", _messages);
        }
    }
}