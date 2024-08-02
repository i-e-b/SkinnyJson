using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;

namespace SkinnyJson
{
    /// <summary>
    /// SkinnyJson entry point. Use the static methods of this class to interact with JSON data
    /// </summary>
    [SuppressMessage("ReSharper", "RedundantNullnessAttributeWithNullableReferenceTypes")]
    public abstract class Json
    {
        /*
         * IMPORTANT: If you change, remove, or add to any of the methods here,
         *            make sure to update the `SJson` alias to match.
         * 
         */
        
        
        /// <summary> Turn an object into a JSON string </summary>
        public static string Freeze(object? obj, JsonSettings? settings = null)
        {
            if (obj == null) return "";
            if (obj is DynamicWrapper dyn) {
                return Freeze(dyn.Parsed, settings);
            }

            settings ??= JsonSettings.Default;

            if (obj is Type typeKind) // caller has `Json.Freeze(typeof(SomeClass));`
            {
                return ToJsonStatics(typeKind, settings);
            }

            if (!IsAnonymousTypedObject(obj)) return ToJson(obj, settings);
            
            // If we are passed an anon type, turn off type information -- it will all be junk.
            if (settings.UseTypeExtensions || !settings.EnableAnonymousTypes) settings = settings.WithAnonymousTypes();

            return ToJson(obj, settings);
        }
        
        /// <summary> Turn an object into a JSON string encoded to a byte array </summary>
        public static byte[] FreezeToBytes(object? obj, JsonSettings? settings = null)
        {
            settings ??= JsonSettings.Default;
            return settings.StreamEncoding.GetBytes(Freeze(obj, settings));
        }

        /// <summary> Write an object to a stream as a JSON string </summary>
        public static void Freeze(object? obj, Stream target, JsonSettings? settings = null)
        {
            if (obj == null) return;
            settings ??= JsonSettings.Default;

            if (obj is DynamicWrapper dyn) {
                Freeze(dyn.Parsed, target);
            }
            
            // If we are passed an anon type, turn off type information -- it will all be junk.
            if (IsAnonymousTypedObject(obj)) settings = settings.WithAnonymousTypes();
            
            ToJsonStream(obj, target, settings);
        }

        /// <summary> Turn a JSON string into a detected object </summary>
        public static object Defrost(string json, JsonSettings? settings = null)
        {
            return ToObject(json, null, settings);
        }

        /// <summary> Turn a JSON byte array into a detected object </summary>
        public static object Defrost(byte[] json, JsonSettings? settings = null)
        {
            return ToObject(json, null, settings);
        }
        
        /// <summary> Turn a JSON data stream into a detected object </summary>
        public static object Defrost(Stream json, JsonSettings? settings = null)
        {
            settings ??= JsonSettings.Default;
            
            return ToObject(json, null, settings);
        }

        /// <summary> Return the type name that SkinnyJson will use for the serialising the object </summary>
        public static string WrapperType(object obj, JsonSettings? settings = null)
        {
            settings ??= JsonSettings.Default;
            if (obj is Type type) return TypeManager.GetTypeAssemblyName(type, settings);
            return TypeManager.GetTypeAssemblyName(obj.GetType(), settings);
        }

        /// <summary> Turn a JSON string into a specific object </summary>
        public static T Defrost
            <[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]T>
            (string json, JsonSettings? settings = null)
        {
            settings ??= JsonSettings.Default;
            return (T)ToObject(json, typeof(T), settings);
        }
        
        /// <summary> Turn a JSON data stream into a specific object </summary>
        public static T Defrost
            <[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]T>
            (Stream json, JsonSettings? settings = null)
        {
            settings ??= JsonSettings.Default;
            
            return (T)ToObject(json, typeof(T), settings);
        }
        
        /// <summary> Turn a JSON byte array into a specific object </summary>
        public static T Defrost
            <[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]T>
            (byte[] json, JsonSettings? settings = null)
        {
            settings ??= JsonSettings.Default;
            
            return (T)ToObject(json, typeof(T), settings);
        }
        
        /// <summary> Turn a JSON string into a runtime type </summary>
        public static object Defrost(string json, Type runtimeType, JsonSettings? settings = null)
        {
            return ToObject(json, runtimeType, settings);
        }

        /// <summary> Turn a JSON byte array into a runtime type </summary>
        public static object Defrost(byte[] json, Type runtimeType, JsonSettings? settings = null)
        {
            return ToObject(json, runtimeType, settings);
        }

        /// <summary> Turn a JSON data stream into a runtime type </summary>
        public static object Defrost(Stream json, Type runtimeType, JsonSettings? settings = null)
        {
            return ToObject(json, runtimeType, settings);
        }

        /// <summary> Turn a JSON string into an object containing properties found </summary>
        public static dynamic DefrostDynamic(string json, JsonSettings? settings = null)
        {
            return new DynamicWrapper(Parse(json, settings));
        }
        
        /// <summary> Turn a JSON string into an object containing properties found </summary>
        public static dynamic DefrostDynamic(Stream json, JsonSettings? settings = null)
        {
            return new DynamicWrapper(ToObject(json, null, settings));
        }

        /// <summary>
        /// Turn a sub-path of a JSON document into an enumeration of values, by specific type
        /// </summary>
        /// <remarks>This is intended to extract useful fragments from repository-style files</remarks>
        /// <typeparam name="T">Type of the fragments to be returned</typeparam>
        /// <param name="path">Dotted path through document. If the path can't be found, an empty enumeration will be returned.
        /// An empty path is equivalent to `Defrost&lt;T&gt;`</param>
        /// <param name="json">The JSON document string to read</param>
        /// <param name="settings">Json parsing settings</param>
        public static IEnumerable<T> DefrostFromPath
            <[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]T>
            (string path, string json, JsonSettings? settings = null)
        {
            if (string.IsNullOrWhiteSpace(path)) {
                return new[] { Defrost<T>(json) };
            }
            
            settings ??= JsonSettings.Default;
            return SelectObjects<T>(json, path, null, settings);
        }

        /// <summary> Create a copy of an object through serialisation </summary>
        public static T Clone
            <[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]T>
            (T obj)
        {
            if (obj == null) return obj;
            return Defrost<T>(Freeze(obj, JsonSettings.Compatible), JsonSettings.Compatible);
        }

        /// <summary>Read a JSON object into an anonymous .Net object</summary>
        public static object Parse(string json, JsonSettings? settings = null)
        {
            return new JsonParser(json, settings).Decode() ?? new {};
        }
        
        /// <summary>
        /// Deserialise a string, perform some edits then reform as a new string
        /// </summary>
        public static string Edit(string json, Action<dynamic> editAction, JsonSettings? settings = null)
        {
            DynamicWrapper dyn = DefrostDynamic(json, settings);
            editAction(dyn);
            return Freeze(dyn, settings);
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
        public static object? DefrostInto(object input, string json, JsonSettings? settings = null)
        {
            return FillObject(input, json, settings);
        }

        /// <summary>Fill the members of an .Net object from a JSON object string</summary>
        public static object? FillObject(object input, string json, JsonSettings? settings = null)
        {
            if (new JsonParser(json, settings).Decode() is not Dictionary<string, object> ht) return null;

            settings ??= JsonSettings.Default;
            if (input is Type type)
            {
                ParseDictionary(ht, null, type, null, null, settings);
                return null;
            }

            return ParseDictionary(ht, null, input.GetType(), input, null, settings);
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
            
            foreach (var c in hex)
            {
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

        #region Internal
        private static bool IsAnonymousTypedObject(object? obj)
        {
            return IsAnonymousType(obj?.GetType());
        }

        private static bool IsAnonymousType(Type? type)
        {
            if (type == null) return false;
            return (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$")) && (type.Attributes.HasFlag(TypeAttributes.NotPublic));
        }
        
        /// <summary>
        /// Read public static properties and fields from a type, output as JSON
        /// </summary>
        private static string ToJsonStatics(Type type, JsonSettings param)
        {
            return new JsonSerializer(param).ConvertStaticsToJson(type);
        }

        private static string ToJson(object obj, JsonSettings param)
        {
            return new JsonSerializer(param).ConvertToJson(obj);
        }

        private static void ToJsonStream(object obj, Stream target, JsonSettings param)
        {
            new JsonSerializer(param).ConvertToJson(obj, target, param.StreamEncoding);
        }
        
        /// <summary>
        /// Pick items out of a parsed object using dotted string path
        /// </summary>
        private static IEnumerable<T> SelectObjects<T>(object json, string path,WarningSet? warnings, JsonSettings settings)
        {
            var parser = ParserFromStreamOrStringOrBytes(json, settings);
            var globalTypes = new Dictionary<string, object>();

            var rawObject = parser.Decode();
            var pathParts = path.Split('.');

            return PathWalk<T>(rawObject, globalTypes, pathParts, 0, false, warnings, settings);
        }

        /// <summary>
        /// Recursive helper for SelectObjects˂T˃
        /// </summary>
        private static IEnumerable<T> PathWalk<T>(object? rawObject, Dictionary<string, object> globalTypes, string[] pathParts, int pathIndex, bool parentIsArray, WarningSet? warnings, JsonSettings settings)
        {
            if (rawObject == null) yield break;
            
            var type = typeof(T);
            if (pathIndex >= pathParts.Length) {
                var container = StrengthenType(type, rawObject, globalTypes, warnings, settings); // Really, convert the raw object to the target and output it
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
                    if (container is null || !container.TryGetValue(step, out var value)) yield break; // doesn't exist at this path
                    rawObject = value; // step down into container
                }
                else if (pathIndex >= pathParts.Length-1) // indexing the last step on a path
                {
                    if (parentIsArray) // we should enumerate the parent array on pick n-th elements from child
                    {
                        var elems = PathWalk<T>(rawObject, globalTypes, pathParts, pathIndex, false, warnings, settings);
                        foreach (var elem in elems) { yield return elem; }
                        yield break;
                    }
                }
            }

            switch (rawObject)
            {
                case Dictionary<string, object> objects:
                {
                    if (!FindNext(objects, step, settings, out var next)) yield break;

                    if (isIndexed) // try to step into an array
                    {
                        if (next is not ArrayList arrayList) yield break; // this path point isn't an array
                        if (index < 0) // we used the '*' syntax
                        {
                            foreach (var item in arrayList)
                            {
                                var indexElems = PathWalk<T>(item, globalTypes, pathParts, pathIndex+1, true, warnings, settings);
                                foreach (var elem in indexElems) { yield return elem; }
                            }
                        }
                        else // we want a single item here
                        {
                            if (index >= arrayList.Count) yield break; // not available on this path
                            var item = arrayList[index];
                            var indexElems = PathWalk<T>(item, globalTypes, pathParts, pathIndex+1, true, warnings, settings);
                            foreach (var elem in indexElems) { yield return elem; }
                        }
                    }

                    var elems = PathWalk<T>(next, globalTypes, pathParts, pathIndex + 1, false, warnings, settings);
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
                                var indexElems = PathWalk<T>(item, globalTypes, pathParts, pathIndex+1, true, warnings, settings);
                                foreach (var elem in indexElems) { yield return elem; }
                            }
                        }
                        else // specific index
                        {
                            var item = arrayList[index];
                            var elems = PathWalk<T>(item, globalTypes, pathParts, pathIndex + 1, true, warnings, settings);
                            foreach (var elem in elems)
                            {
                                yield return elem;
                            }
                        }

                        yield break;
                    }

                    foreach (var item in arrayList) // return every child of this path
                    {
                        var elems = PathWalk<T>(item, globalTypes, pathParts, pathIndex, true, warnings, settings); // ignore arrays while walking path name elements
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

                    var elems = PathWalk<T>(rawObject, globalTypes, pathParts, pathIndex + 1, false, warnings, settings);
                    foreach (var elem in elems)
                    {
                        yield return elem;
                    }

                    yield break;
                }
            }
        }

        private static bool FindNext(Dictionary<string,object> objects, string step, JsonSettings settings, out object? next)
        {
            if (objects.TryGetValue(step, out next)) return true;
            if (!settings.IgnoreCaseOnDeserialize) return false;

            // Try to find first match in normalised case
            var key = NormaliseCase(step);
            foreach (var kvp in objects)
            {
                if (NormaliseCase(kvp.Key) == key)
                {
                    next = kvp.Value;
                    return true;
                }
            }

            next = null;
            return false;
        }

        /// <summary>
        /// Create a new object by type, using input json data
        /// </summary>
        /// <param name="json">Either a stream of utf-8 data or an in-memory `string`</param>
        /// <param name="type">Target return type</param>
        /// <param name="settings">Json parsing options</param>
        private static object ToObject(object json, Type? type, JsonSettings? settings)
        {
            settings ??= JsonSettings.Default;
            var globalTypes = new Dictionary<string, object>();
			
            var parser = ParserFromStreamOrStringOrBytes(json, settings);

            var decodedObject = parser.Decode();
            if (decodedObject == null) return new {};

            var warnings = new WarningSet();
            try
            {
                var result = StrengthenType(type, decodedObject, globalTypes, warnings, settings);
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
        /// <param name="settings">Json parser settings</param>
        private static object? StrengthenType(Type? type, object decodedObject, Dictionary<string, object> globalTypes, WarningSet? warnings, JsonSettings settings)
        {
            switch (decodedObject)
            {
                case Dictionary<string, object> objects:
                    return ParseDictionary(objects, globalTypes, type, null, warnings, settings);

                case ArrayList arrayList:
                    if (type?.IsArray == true)
                    {
                        var elementType = type.GetElementType() ?? typeof(object);
                        var list = ConvertToList(elementType, globalTypes, arrayList, warnings, settings);
                        return ListToArray(list, elementType);
                    }
                    else
                    {
                        var containedType = (type?.GetGenericArguments().SingleOrDefault() ?? type) ?? typeof(object);
                        
                        var setType = GenericSetInterfaceType(containedType);
                        if (type == setType) return ConvertToSet(containedType, globalTypes, arrayList, warnings, settings);
                        return ConvertToList(containedType, globalTypes, arrayList, warnings, settings);
                    }

                default:
                    return type is null ? decodedObject : ConvertItem(type, globalTypes, decodedObject, warnings, settings);
            }
        }

        private static Array ListToArray(IList list, Type elementType)
        {
            var x = new ArrayList(list);
            return x.ToArray(elementType);
        }

        private static IList ConvertToList(Type elementType, Dictionary<string, object> globalTypes, ArrayList arrayList, WarningSet? warnings, JsonSettings settings)
        {
            var list = (IList) Activator.CreateInstance(GenericListType(elementType))!;
            foreach (var obj in arrayList)
            {
                list.Add(ConvertItem(elementType, globalTypes, obj, warnings, settings));
            }

            return list;
        }

        private static object? ConvertItem(Type elementType, Dictionary<string, object> globalTypes, object? obj, WarningSet? warnings, JsonSettings settings)
        {
            if (obj == null) return obj;
            if (obj.GetType().IsAssignableFrom(elementType)) return obj;
            if (obj is WideNumber wide) return wide.CastTo(elementType, out _);
            
            // a complex type?
            var dict = obj as Dictionary<string, object>;
            if (dict == null) throw new Exception($"Element {obj.GetType().Name} not assignable to the array type {elementType.Name}");
            var parsed = ParseDictionary(dict, globalTypes, elementType, null, warnings, settings);
            return parsed;
        }

        private static object ConvertToSet(Type elementType, Dictionary<string, object> globalTypes, ArrayList arrayList, WarningSet? warnings, JsonSettings settings)
        {
            var set = Activator.CreateInstance(GenericHashSetType(elementType));
            var adder = set?.GetType().GetMethod("Add", BindingFlags.Public|BindingFlags.Instance) ?? throw new Exception("Failed to find add method on set");
            foreach (var obj in arrayList)
            {
                if (obj == null) continue;
                
                var toAdd = ConvertItem(elementType, globalTypes, obj, warnings, settings);
                adder.Invoke(set, new []{toAdd});
            }

            return set;
        }

        /// <summary>
        /// Pass in either a string or a stream and get back a parser instance
        /// </summary>
        [NotNull]
        private static JsonParser ParserFromStreamOrStringOrBytes(object json, JsonSettings settings)
        {
            JsonParser parser;
            switch (json)
            {
                case Stream jsonStream:
                    parser = new JsonParser(jsonStream, settings);
                    break;
                case string jsonString:
                    parser = new JsonParser(jsonString, settings);
                    break;
                case byte[] jsonBytes:
                    parser = new JsonParser(jsonBytes, settings);
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
        private static Type GenericListType(Type containedType)
        {
            var d1 = typeof(List<>);
            Type[] typeArgs = { containedType };
            return d1.MakeGenericType(typeArgs);
        }
        
        /// <summary>
        /// Make an ISet˂T˃() instance for a runtime type
        /// </summary>
        [NotNull]
        private static Type GenericSetInterfaceType(Type containedType)
        {
            var d1 = typeof(ISet<>);
            Type[] typeArgs = { containedType };
            return d1.MakeGenericType(typeArgs);
        }
        
        /// <summary>
        /// Make an HashSet˂T˃() instance for a runtime type
        /// </summary>
        [NotNull]
        private static Type GenericHashSetType(Type containedType)
        {
            var d1 = typeof(HashSet<>);
            Type[] typeArgs = { containedType };
            return d1.MakeGenericType(typeArgs);
        }

        
        /// <summary>
        /// Read a weakly-typed dictionary tree into a strong type. If the keys do not match exactly,
        /// all matching field/properties will be filled.
        /// If *no* keys match the target type, this will return `null`
        /// <p></p>
        /// If the input object is null, but the input type is not,
        /// this will attempt to fill static members of the given type.
        /// </summary>
        private static object? ParseDictionary(IDictionary<string, object> jsonValues, IDictionary<string, object>? globalTypes, Type? type, object? input, WarningSet? warnings, JsonSettings settings)
        {
            TryFillGlobalTypes(jsonValues, globalTypes);

            var found = jsonValues.TryGetValue("$type", out var tn);
            if (found == false && type == typeof(object))
            {
                var ds = CreateDataset(jsonValues, globalTypes, settings);
                return ds != null ? ds : jsonValues;
            }
            if (found)
            {
                if (settings.UsingGlobalTypes && globalTypes != null)
                {
                    if (globalTypes.TryGetValue((string)tn!, out object tName)) tn = tName;
                }
                if (type is null || !type.IsInterface) type = TypeManager.GetTypeFromCache((string)tn!, settings);
            }

            var targetObject = input ?? TypeManager.FastCreateInstance(type, settings);

            if (targetObject is null && type is null) return jsonValues; // can't work out what object to fill, send back the raw values

            var targetType = targetObject?.GetType() ?? type;
            if (targetType is null) throw new Exception("Unable to determine type of object");

            var props = GetProperties(targetType, targetType.Name, settings);
            if (!IsDictionary(targetType) && NoPropertiesMatch(props, jsonValues.Keys, settings))
            {
                if (!settings.IgnoreCaseOnDeserialize &&
                    warnings is not null &&
                    PropertiesWouldMatchCaseInsensitive(props, jsonValues.Keys))
                {
                    warnings.Append($"; Properties would match if {nameof(settings.IgnoreCaseOnDeserialize)} was set to true");
                }

                if (jsonValues.Count > 0)
                {
                    // unless we were passed an empty object, this type doesn't match
                    if (settings.StrictMatching)
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
                MapJsonValueToObject(key, targetType, targetObject, jsonValues, globalTypes, props, warnings, settings);
            }

            return targetObject;
        }

        private static bool PropertiesWouldMatchCaseInsensitive(SafeDictionary<string,TypePropertyInfo> props, ICollection<string> jsonValuesKeys)
        {
            var normalKeys = props.Keys.Select(NormaliseCase);
            return jsonValuesKeys.Any(jsonKey => normalKeys.Contains(NormaliseCase(jsonKey)));
        }

        private static void TryFillGlobalTypes(IDictionary<string, object> jsonValues, IDictionary<string, object>? globalTypes)
        {
            if (!jsonValues.TryGetValue("$types", out var tn)) return;
            if (globalTypes == null) return;
            
            var dic = (Dictionary<string, object>)tn!;
            foreach (var kvp in dic)
            {
                if (kvp.Value != null) globalTypes.Add((string)kvp.Value, kvp.Key);
            }
        }

        private static bool IsDictionary(Type? targetType)
        {
            return targetType?.GetInterface("IDictionary") != null;
        }

        private static bool NoPropertiesMatch(SafeDictionary<string, TypePropertyInfo> props, ICollection<string> jsonValuesKeys, JsonSettings settings)
        {
            return settings.IgnoreCaseOnDeserialize
                ? jsonValuesKeys.All(jsonKey => !props.HasKey(NormaliseCase(jsonKey))) 
                : jsonValuesKeys.All(jsonKey => !props.HasKey(jsonKey));
        }

        /// <summary>
        /// Map json value dictionary to the properties and fields of a target object instance.
        /// </summary>
        private static void MapJsonValueToObject(string objectKey, Type? targetType, object? targetObject, IDictionary<string, object> jsonValues, IDictionary<string, object>? globalTypes,
            SafeDictionary<string, TypePropertyInfo> props, WarningSet? warnings, JsonSettings settings)
        {
            var name = objectKey;
            if (settings.IgnoreCaseOnDeserialize) name = NormaliseCase(name);
            if (name == "$map" && targetObject is not null)
            {
                ProcessMap(targetObject, props, jsonValues[name] as Dictionary<string, object>);
                return;
            }

            if (GetProp(props, name, objectKey, out var propertyInfo) == false) {
                if (targetObject is IDictionary) {
                    var ok = props.TryGetValue("Item", out propertyInfo);
                    if (!ok) return;
                }
                else return;
            }
            if (!propertyInfo.filled) return;
            var v = GetValue(jsonValues, objectKey, name);

            if (v == null) return;

            object setObj;
            try
            {
                setObj = MakeSettableObject(globalTypes, propertyInfo, v, warnings, settings);
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
                    AddToDictionary(name, setObj, targetObject, settings);
                }
                else
                {
                    if (propertyInfo.CanWrite) WriteValueToTypeInstance(name, targetType, targetObject, propertyInfo, setObj);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to write value from json {v.GetType()} via {setObj.GetType()} to target object {propertyInfo.changeType?.ToString() ?? "<unknown>"} on property {propertyInfo.Name}", ex);
            }
        }

        private static bool GetProp(SafeDictionary<string,TypePropertyInfo> props, string key1, string key2, out TypePropertyInfo prop)
        {
            if (props.TryGetValue(key1, out prop)) return true;
            if (props.TryGetValue(key2, out prop)) return true;
            return false;
        }

        private static object? GetValue(IDictionary<string, object> jsonValues, string exactKey, string altKey)
        {
            if (jsonValues.TryGetValue(exactKey, out var value1)) return value1;
            if (jsonValues.TryGetValue(altKey, out var value2)) return value2;
            return null;
        }

        private static void AddToDictionary(string key, object value, object? target, JsonSettings settings)
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
                    shallow.Add(key, ToJson(value, settings));
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
        private static object MakeSettableObject(IDictionary<string, object>? globalTypes, TypePropertyInfo propertyInfo, object inputValue, WarningSet? warnings, JsonSettings settings)
        {
            object setObj;
            var precisionLoss = false;

            if (propertyInfo.isNumeric)
                setObj = CastNumericType(inputValue, propertyInfo, out precisionLoss)
                         ?? throw new Exception($"Failed to map input type '{inputValue.GetType().Name}' to target '{propertyInfo.parameterType?.Name ?? "<null>"}'");

            else if (propertyInfo.isByteArray)
                setObj = ConvertBytes(inputValue);
            else if (propertyInfo.isEnumerable && propertyInfo.bt == typeof(byte))
                setObj = CreateGenericList(ConvertBytes(inputValue), propertyInfo.parameterType, propertyInfo.bt, globalTypes, settings);
            else if (propertyInfo.changeType == typeof(Stream))
                setObj = new MemoryStream(ConvertBytes(inputValue));
            
            else if (propertyInfo.isString || propertyInfo.parameterType == typeof(string)) setObj = inputValue;
            else if (propertyInfo.isBool) setObj = InterpretBool(inputValue, settings);
            else if (propertyInfo.isGenericType && propertyInfo is { isValueType: false, isDictionary: false, isEnumerable: true } && inputValue is IEnumerable)
                setObj = CreateGenericList((ArrayList)inputValue, propertyInfo.parameterType, propertyInfo.bt, globalTypes, settings);

            else if (propertyInfo is { isArray: true, isValueType: false })
                setObj = CreateArray((ArrayList)inputValue, propertyInfo.bt, globalTypes, settings);
            else if (propertyInfo.isGuid)
                setObj = CreateGuid((string)inputValue);
            else if (propertyInfo.isDataSet)
                setObj = CreateDataset((Dictionary<string, object>)inputValue, globalTypes, settings) ?? throw new Exception("Failed to create dataset");

            else if (propertyInfo.isDataTable)
                setObj = CreateDataTable((Dictionary<string, object>)inputValue, globalTypes, settings);

            else if (propertyInfo.isStringDictionary)
                setObj = CreateStringKeyDictionary((Dictionary<string, object>)inputValue, propertyInfo.parameterType, propertyInfo.GenericTypes, globalTypes, settings);

            else if (propertyInfo.isDictionary || propertyInfo.isHashtable)
                setObj = CreateDictionary((ArrayList)inputValue, propertyInfo.parameterType, propertyInfo.GenericTypes, globalTypes, settings);

            else if (propertyInfo.isEnum)
            {
                if (inputValue is string valStr) setObj = CreateEnum(propertyInfo.parameterType, valStr);
                else if (inputValue is WideNumber wn)
                    setObj = NumberToEnum(propertyInfo.parameterType, wn) ?? throw new Exception($"Failed to convert number ({wn}) to enum {propertyInfo.parameterType?.Name ?? "<null>"}");
                else throw new Exception($"Failed to convert value of type {inputValue.GetType().Name} to enum {propertyInfo.parameterType?.Name ?? "<null>"}");
            }

            else if (propertyInfo.isDateTime)
                setObj = CreateDateTime(inputValue, settings);

            else if (propertyInfo.isTimeSpan)
                setObj = CreateTimeSpan(inputValue, settings);

            else if (propertyInfo.isClass && inputValue is Dictionary<string, object> objects)
            {
                setObj = ParseDictionary(objects, globalTypes, propertyInfo.parameterType, null, warnings, settings)
                         ?? throw new Exception($"Failed to map to class '{propertyInfo.parameterType?.Name ?? "<null>"}'");
            }
            else if (propertyInfo.isInterface && inputValue is Dictionary<string, object> proxyObjects)
                setObj = ParseDictionary(proxyObjects, globalTypes, propertyInfo.parameterType, null, warnings, settings)
                         ?? throw new Exception("Failed to map to proxy class of interface");

            else if (propertyInfo.isValueType)
                setObj = ChangeType(inputValue, propertyInfo.changeType) ?? throw new Exception("Failed to create value type");
            else if (inputValue is ArrayList list)
                setObj = CreateArray(list, typeof(object), globalTypes, settings);
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
            // Incoming value might be a list of numeric values, which we will assume are bytes
            if (inputValue is ArrayList list)
            {
                var result = new List<byte>();
                foreach (var item in list)
                {
                    if (item is WideNumber wn) result.Add(wn.ToByte(null));
                    else throw new Exception("Input to byte array was not valid numeric list");
                }
                return result.ToArray();
            }

            // Otherwise, it should be a hex or base64 string.
            var inputStr = inputValue.ToString().Trim();
            if (string.IsNullOrWhiteSpace(inputStr)) return new byte[0];
            
            // If it has a hex marker, only accept hex
            if (inputStr[0] == '$') {
                return HexToByteArray(inputStr.Substring(1)) ?? throw new Exception("Input to byte array was not valid hex string");
            }

            // Otherwise, try reading as base64, and use hex as a back-up
            try
            {
                return Convert.FromBase64String(inputStr);
            }
            catch
            {
                return HexToByteArray(inputStr) ?? throw new Exception("Input to byte array was not valid Base64 or hex string");
            }
        }

        private static bool InterpretBool(object o, JsonSettings settings)
        {
            if (o is bool b) return b;
            if (o is string s)
            {
                if (settings.IgnoreCaseOnDeserialize) s = NormaliseCase(s);
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
        private static void WriteValueToTypeInstance(string name, Type? type, object? targetObject, TypePropertyInfo pi, object objSet) {
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
        private static SafeDictionary<string, TypePropertyInfo> GetProperties(Type type, string typename, JsonSettings settings)
        {
            var usePrivateFields = typename.StartsWith("Tuple`", StringComparison.Ordinal) || IsAnonymousType(type);

            //if (_propertyCache.TryGetValue(typename, out SafeDictionary<string, TypePropertyInfo> sd)) return sd;
            if (TypeManager.GetPropertySet(type, settings, out var sd)) return sd;
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

            foreach (var typeInterface in type.GetInterfaces())
            {
                fi.AddRange(typeInterface.GetFields(BindingFlags.Public | BindingFlags.Instance));
            }

            foreach (var f in fi)
            {
                var d = TypeManager.CreateMyProp(f.FieldType, f.Name);
                d.setter = TypeManager.CreateSetField(type, f);
                d.getter = TypeManager.CreateGetField(type, f);

                sd.Add(f.Name, d);
                if (settings.IgnoreCaseOnDeserialize) sd.TryAdd(NormaliseCase(f.Name), d);
                if (usePrivateFields){
                    var privateName = f.Name.Replace("m_", "");
                    sd.Add(AnonFieldFilter(privateName), d);
                    if (settings.IgnoreCaseOnDeserialize) sd.TryAdd(NormaliseCase(privateName), d);
                }
                
                foreach (var alt in TypeManager.GetAlternativeNames(f)) sd.TryAdd(alt, d);
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
                var d = TypeManager.CreateMyProp(p.PropertyType, p.Name);
                d.CanWrite = p.CanWrite;
                d.setter = TypeManager.CreateSetMethod(p);
                if (d.setter == null) continue;
                d.getter = TypeManager.CreateGetMethod(p);
                sd.Add(p.Name, d);
                if (settings.IgnoreCaseOnDeserialize) sd.TryAdd(NormaliseCase(p.Name), d);
                foreach (var alt in TypeManager.GetAlternativeNames(p)) sd.TryAdd(alt, d);
            }
            
            // Static properties are special
            var staticProps = type.GetProperties(BindingFlags.Public | BindingFlags.Static);
            foreach (var sp in staticProps)
            {
                var d = TypeManager.CreateMyProp(sp.PropertyType, sp.Name);
                d.CanWrite = sp.CanWrite;
                d.setter = (_, value, _) => sp.SetValue(null!, value);
                d.getter = _ => sp.GetValue(null!)!;
                
                sd.Add(sp.Name, d);
                if (settings.IgnoreCaseOnDeserialize) sd.TryAdd(NormaliseCase(sp.Name), d);
                foreach (var alt in TypeManager.GetAlternativeNames(sp)) sd.TryAdd(alt, d);
            }

            if (type.GetGenericArguments().Length < 1) {
                TypeManager.AddToPropertyCache(type, sd, settings);
            }
            return sd;
        }

        /// <summary>
        /// Convert a string to lower case, removing a set of joining and non-printing characters
        /// </summary>
        private static string NormaliseCase(string? src)
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
        private static string AnonFieldFilter(string name)
        {
            if (name[0] != '<') return name;
            var idx = name.IndexOf('>', 2);
            if (idx < 2) return name;
            return name.Substring(1, idx - 1);
        }
        
        /// <summary>
        /// Convert between runtime types
        /// </summary>
        private static object? ChangeType(object? value, Type? conversionType)
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

        private static void ProcessMap(object obj, SafeDictionary<string, TypePropertyInfo> props, Dictionary<string, object>? dic)
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

        private static long CreateLong(object? obj){
            if (obj is null) return 0;
            if (obj is string s) return ParseLong(s);
            if (obj is double d) return (long)d;
            if (obj is WideNumber w) return w.ToLong();
            throw new Exception("Unsupported int type: "+obj.GetType());
        }

        private static double CreateDouble(object? obj){
            if (obj is null) return 0;
            if (obj is string s) return double.Parse(s);
            if (obj is double d) return d;
            if (obj is WideNumber w) return w.ToDouble();
            throw new Exception("Unsupported numeric type: "+obj.GetType());
        }

        private static long ParseLong(IEnumerable<char> s)
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

        private static object CreateEnum(Type? pt, string v)
        {
            if (pt == null) throw new Exception("Invalid property type");
            return Enum.Parse(pt, v);
        }
        
        private static object NumberToEnum(Type? pt, WideNumber wn)
        {
            if (pt == null) throw new Exception("Invalid property type");
            
            return Enum.ToObject(pt, wn.ToLong());
        }

        private static Guid CreateGuid(string s)
        {
            return s.Length > 30 ? new Guid(s) : new Guid(Convert.FromBase64String(s));
        }

        private static DateTime CreateDateTime(object? value, JsonSettings settings)
        {
            if (value is null) return DateTime.MinValue;

            if (value is WideNumber wide) return InterpretNumberAsDate(wide.ToLong());
            if (value is long ticksLong) return InterpretNumberAsDate(ticksLong);
            if (value is double ticksDouble) return InterpretNumberAsDate((long)ticksDouble);

            var str = value.ToString();

            var style = settings.UseUtcDateTime ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None;
            if (str.EndsWith("Z")) style = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

            foreach (var format in settings.DateFormats)
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
            if (asTicks.Year is > 1900 and < 3000) return asTicks;
            
            var asUnixMs = new DateTime(1970,1,1,0,0,0, DateTimeKind.Utc).AddMilliseconds(value);
            if (asUnixMs.Year is > 1980 and < 3000) return asUnixMs;
            
            var asUnixSeconds = new DateTime(1970,1,1,0,0,0, DateTimeKind.Utc).AddSeconds(value);
            return asUnixSeconds;
        }

        private static TimeSpan CreateTimeSpan(object? v, JsonSettings settings)
        {
            if (v is null) return TimeSpan.Zero;
            if (v is string str) return TimeSpan.Parse(str);

            if (v is Dictionary<string, object> objects)
            {
                // TimeSpan is tricky, and can vary based on Framework version
                // So we pick it apart manually.
                if (objects.TryGetValue("Ticks", out var ticks1)) return new TimeSpan(ticks: CreateLong(ticks1));
                if (objects.TryGetValue("ticks", out var ticks2)) return new TimeSpan(ticks: CreateLong(ticks2));
                
                if (objects.TryGetValue("TotalSeconds", out var seconds1)) return TimeSpan.FromSeconds(CreateDouble(seconds1));
                if (objects.TryGetValue("total_seconds", out var seconds2)) return TimeSpan.FromSeconds(CreateDouble(seconds2));
                
                var days = (int)TryGetDouble(objects, "Days", settings);
                var hours = (int)TryGetDouble(objects, "Hours", settings);
                var minutes = (int)TryGetDouble(objects, "Minutes", settings);
                var seconds = (int)TryGetDouble(objects, "Seconds", settings);
                var milliseconds = (int)TryGetDouble(objects, "Milliseconds", settings);
                return new TimeSpan(days, hours, minutes, seconds, milliseconds);
            }
            
            throw new Exception("Failed to map to TimeSpan");
        }

        /// <summary>
        /// Try to get a keyed value as a double, or return zero. 
        /// </summary>
        private static double TryGetDouble(Dictionary<string,object> objects, string key, JsonSettings settings)
        {
            if (objects.TryGetValue(key, out var value))
            {
                if (value is double d) return d;
                if (value is string s && double.TryParse(s, out var dVal)) return dVal;
                return 0.0;
            }

            if (settings.IgnoreCaseOnDeserialize != true) return 0.0;
            
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

        private static object CreateArray(IEnumerable data, Type? elementType, IDictionary<string, object>? globalTypes, JsonSettings settings)
        {
            if (elementType == null) throw new Exception("Invalid element type");
            var col = new ArrayList();
            foreach (var ob in data)
            {
                col.Add(
                    ob is IDictionary
                        ? ParseDictionary((Dictionary<string, object>) ob, globalTypes, elementType, null, null, settings)
                        : ChangeType(ob, elementType)
                );
            }
            return col.ToArray(elementType);
        }

        private static object CreateGenericList(IEnumerable data, Type? pt, Type? bt, IDictionary<string, object>? globalTypes, JsonSettings settings)
        {
            if (pt == null) throw new Exception("Invalid container type");
            if (bt == null) throw new Exception("Invalid element type");
            if (TypeManager.FastCreateInstance(pt, settings) is not IList col) throw new Exception("Failed to create instance of " + pt);
            foreach (var ob in data)
            {
                if (ob is IDictionary)
                    col.Add(ParseDictionary((Dictionary<string, object>)ob, globalTypes, bt, null, null, settings));
                else if (ob is ArrayList list)
                    col.Add(list.ToArray());
                else
                    col.Add(ChangeType(ob, bt));
            }
            return col;
        }

        private static object CreateStringKeyDictionary(Dictionary<string, object> reader, Type? pt, IList<Type>? types, IDictionary<string, object>? globalTypes, JsonSettings settings)
        {
            if (pt == null) throw new Exception("Target type was null");
            if (TypeManager.FastCreateInstance(pt, settings) is not IDictionary col) throw new Exception("Failed to create instance of " + pt);
            Type? t2 = null;
            if (types != null) t2 = types[1];

            foreach (var values in reader)
            {
                var key = values.Key;
                if (key == null) continue;
                object? val;
                if (values.Value is Dictionary<string, object> objects)
                {
                    val = ParseDictionary(objects, globalTypes, t2, null, null, settings);
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

        private static object CreateDictionary(IEnumerable reader, Type? pt, IList<Type>? types, IDictionary<string, object>? globalTypes, JsonSettings settings)
        {
            if (pt == null) throw new Exception("Invalid container type");
            if (TypeManager.FastCreateInstance(pt, settings) is not IDictionary col) throw new Exception("Failed to create instance of " + pt);
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
                    key = ParseDictionary(objects, globalTypes, t1, null, null, settings);
                else
                    key = ChangeType(key, t1);

                if (val is Dictionary<string, object> dictionary)
                    val = ParseDictionary(dictionary, globalTypes, t2, null, null, settings);
                else
                    val = ChangeType(val, t2);

                if (key != null && val != null) col.Add(key, val);
            }

            return col;
        }

        private static DataSet? CreateDataset(IDictionary<string, object> reader, IDictionary<string, object>? globalTypes, JsonSettings settings)
        {
            var ds = new DataSet {EnforceConstraints = false};
            ds.BeginInit();

            // read dataset schema here
            if (!ReadSchema(reader, ds, globalTypes, settings)) return null;

            foreach (var pair in reader)
            {
                if (pair.Key == null || pair.Key == "$type" || pair.Key == "$schema") continue;

                var rows = pair.Value as ArrayList;
                if (rows == null) continue;

                var dt = ds.Tables[pair.Key];
                ReadDataTable(rows, dt, settings);
            }

            ds.EndInit();

            return ds;
        }

        private static bool ReadSchema(IDictionary<string, object>? reader, DataSet ds, IDictionary<string, object>? globalTypes, JsonSettings settings)
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
                var ms = ParseDictionary((Dictionary<string, object>)schema, globalTypes, typeof(DatasetSchema), null, null, settings) as DatasetSchema;
                if (ms?.Info == null) return false;
                ds.DataSetName = ms.Name ?? "Untitled";
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

        private static void ReadDataTable(IEnumerable rows, DataTable? dt, JsonSettings settings)
        {
            if (dt == null) return;
            
            dt.BeginInit();
            dt.BeginLoadData();
            var guidCols = new List<int>();
            var dateCol = new List<int>();

            foreach (DataColumn c in dt.Columns)
            {
                if (c.DataType == typeof(Guid) || c.DataType == typeof(Guid?))
                    guidCols.Add(c.Ordinal);
                if (settings.UseUtcDateTime && (c.DataType == typeof(DateTime) || c.DataType == typeof(DateTime?)))
                    dateCol.Add(c.Ordinal);
            }

            foreach (ArrayList row in rows)
            {
                var v = new object[row.Count];
                row.CopyTo(v, 0);
                foreach (int i in guidCols)
                {
                    if (v[i] is string { Length: < 36 } s)
                        v[i] = new Guid(Convert.FromBase64String(s));
                }
                if (settings.UseUtcDateTime)
                {
                    foreach (int i in dateCol)
                    {
                        if (v[i] is string s)
                            v[i] = CreateDateTime(s, settings);
                    }
                }
                dt.Rows.Add(v);
            }

            dt.EndLoadData();
            dt.EndInit();
        }

        private static DataTable CreateDataTable(IDictionary<string, object> reader, IDictionary<string, object>? globalTypes, JsonSettings settings)
        {
            var dt = new DataTable();

            // read dataset schema here
            var schema = reader.TryGetValue("$schema", out var value) != true ? null : value;

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
                    if (ParseDictionary(dictSchema, globalTypes, typeof(DatasetSchema), null, null, settings) is DatasetSchema { Info: not null } ms)
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

                ReadDataTable(rows, dt, settings);
            }

            return dt;
        }
        
        #endregion Internal
    }
}