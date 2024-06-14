using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace SkinnyJson
{
    internal static class TypeManager
    {
        private static readonly object[] _emptyObjectArray = new object[0];

        private class CacheSet
        {
            /// <summary> Used to map incoming <c>"$type"</c> keys to dotnet types </summary>
            internal readonly SafeDictionary<string, Type> NamedTypeCache = new();
            internal readonly SafeDictionary<Type, string> TypeNameMap = new();
            internal readonly SafeDictionary<Type, CreateObject> ConstructorCache = new();
            internal readonly List<Type> AssemblyCache = new();
            internal readonly SafeDictionary<Type, List<Getters>> GetterCache = new();
            internal readonly SafeDictionary<Type, SafeDictionary<string, TypePropertyInfo>> PropertyCache = new();
        }
        
        /// <summary> Per-settings caches of type info</summary>
        private static readonly SafeDictionary<int, CacheSet> _caches = new ();

        /// <summary>
        /// Get caches for setting spec. Creates a new set if needed.
        /// </summary>
        private static CacheSet Cache(JsonSettings settings)
        {
            var key = settings.ParameterKey();
            _caches.TryAdd(key, new CacheSet());
            if (!_caches.TryGetValue(key, out var result)) throw new Exception("Internal error: Failed to build type cache");
            return result;
        }

        /// <summary>
        /// Delegate for creating a new type instance
        /// </summary>
        private delegate object CreateObject();
        
        /// <summary>
        /// Function definition to get the value of a field or properly on an object
        /// </summary>
        /// <param name="obj)">object instance to provide the value</param>
        internal delegate object GenericGetter(object obj);

        /// <summary>
        /// Function definition to set the value of a field or properly on an object
        /// </summary>
        /// <param name="target">object instance to accept the value</param>
        /// <param name="value">value of property to set</param>
        /// <param name="key">optional key for dictionaries</param>
        internal delegate void GenericSetter(object? target, object value, object? key);
        
        
        /// <summary>
        /// Get a shortened string name for a type's containing assembly
        /// </summary>
        internal static string GetTypeAssemblyName(Type t, JsonSettings settings)
        {
            var cache = Cache(settings);
            if (cache.TypeNameMap.TryGetValue(t, out string val)) return val;

            string name;
            if (t.BaseType == typeof(object)) {
                // on Windows, this can be just "t.GetInterfaces()" but Mono doesn't return in order.
                var interfaceType = t.GetInterfaces().FirstOrDefault(i => !t.GetInterfaces().Any(i2 => i2.GetInterfaces().Contains(i)));
                name = ShortenName((interfaceType ?? t).AssemblyQualifiedName ?? t.ToString());
            } else {
                name = ShortenName(t.AssemblyQualifiedName ?? t.ToString());
            }

            cache.TypeNameMap.Add(t, name);

            return cache.TypeNameMap[t];
        }

        /// <summary>
        /// Shorten an assembly qualified name
        /// </summary>
        private static string ShortenName(string assemblyQualifiedName) {
            var one = assemblyQualifiedName.IndexOf(',');
            var two = assemblyQualifiedName.IndexOf(',', one+1);
            return assemblyQualifiedName.Substring(0, two);
        }


        /// <summary>
        /// Try to get or build a type for a given type-name
        /// </summary>
        internal static Type? GetTypeFromCache(string typename, JsonSettings settings) {
            var cache = Cache(settings);
            if (cache.NamedTypeCache.TryGetValue(typename, out Type val)) return val;

            var typeParts = typename.Split(',');

            Type? t;
            if (typeParts.Length > 1) {
                var assemblyName = typeParts[1];
                var fullName = typeParts[0];
                var available = Assembly.Load(assemblyName)?.GetTypes();
                t = available?.SingleOrDefault(type => type?.FullName?.Equals(fullName, StringComparison.OrdinalIgnoreCase) == true);
            } else if (typeParts.Length == 1) {
                // slow but robust way of finding a type fragment.
                if (cache.AssemblyCache.Count < 1) { cache.AssemblyCache.AddRange(AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes())); }
                t = cache.AssemblyCache.SingleOrDefault(type => type.FullName?.StartsWith(typeParts[0], StringComparison.OrdinalIgnoreCase) ?? false);
            } else throw new Exception("Invalid type description: "+typename);
            
            if (t != null) {
                cache.NamedTypeCache.Add(typename, t);
            }
            return t;
        }


        /// <summary>
        /// Try to make a new instance of a type.
        /// Will drop down to 'SlowCreateInstance' in special cases
        /// </summary>
        internal static object? FastCreateInstance(Type? objType, JsonSettings settings)
        {
            var cache = Cache(settings);
            if (objType == null) return null;
            if (cache.ConstructorCache.TryGetValue(objType, out var cc)) return cc();

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
                    return SlowCreateInstance(objType, settings);
                }

                var dynMethod = new DynamicMethod("_", objType, null);
                var ilGen = dynMethod.GetILGenerator();

                ilGen.Emit(OpCodes.Newobj, constructorInfo);
                ilGen.Emit(OpCodes.Ret);
                var c = (CreateObject)dynMethod.CreateDelegate(typeof(CreateObject));
                cache.ConstructorCache.Add(objType, c);
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

        private static object SlowCreateInstance(Type objType, JsonSettings settings)
        {
            if (objType == typeof(string)) {
                throw new Exception("Invalid parser state");
            }
            var allCtor = objType.GetConstructors();
            if (allCtor.Length < 1) {
                throw new Exception($"Failed to create instance for type '{objType.FullName}' from assembly '{objType.AssemblyQualifiedName}'. No constructors found.");
            }
            
            var types = allCtor[0].GetParameters().Select(p=>p.ParameterType).ToArray();
            var instances = types.Select(t=>FastCreateInstance(t, settings)).ToArray();
            var constructorInfo = objType.GetConstructor(types);
            return constructorInfo?.Invoke(instances)!;
        }
        
        
        /// <summary>
        /// Return a list of property/field access proxies for a type.
        /// This is cached after first access for each type.
        /// </summary>
        internal static List<Getters> GetGetters(Type type, JsonSettings settings)
        {
            var cache = Cache(settings);
            if (cache.GetterCache.TryGetValue(type, out var val)) return val;

            var publicProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            var getters = new List<Getters>();
            foreach (var property in publicProperties)
            {
                if (!property.CanWrite && settings is { ShowReadOnlyProperties: false, EnableAnonymousTypes: false }) continue;
                if (property.GetCustomAttributes(typeof(System.Xml.Serialization.XmlIgnoreAttribute), false).Any()) continue;
                
                var getMethod = CreateGetMethod(property);
                if (getMethod == null) continue;
                
                getters.Add(MakePropertyGetterWithPreferredName(property, getMethod));
            }

            var publicFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var fieldInfo in publicFields)
            {
                if (fieldInfo.GetCustomAttributes(typeof(System.Xml.Serialization.XmlIgnoreAttribute), false).Any()) continue;
                var getMethod = CreateGetField(type, fieldInfo);
                getters.Add(MakeFieldGetterWithPreferredName(fieldInfo, getMethod));
            }

            cache.GetterCache.Add(type, getters);
            return getters;
        }

        /// <summary>
        /// Build a 'getter', used for serialisation.
        /// This will use the first name from any name-overriding attribute,
        /// or the name of the member directly if not overridden.
        /// </summary>
        private static Getters MakeFieldGetterWithPreferredName(FieldInfo field, GenericGetter getMethod)
        {
            var name = GetAlternativeNames(field).FirstOrDefault() ?? field.Name;
            return new Getters { Name = name, Getter = getMethod, PropertyType = field.FieldType, FieldInfo = field};
        }
        
        /// <summary>
        /// Build a 'getter', used for serialisation.
        /// This will use the first name from any name-overriding attribute,
        /// or the name of the member directly if not overridden.
        /// </summary>
        private static Getters MakePropertyGetterWithPreferredName(PropertyInfo property, GenericGetter getMethod)
        {
            var name = GetAlternativeNames(property).FirstOrDefault() ?? property.Name;
            return new Getters { Name = name, Getter = getMethod, PropertyType = property.PropertyType };
        }

        /// <summary>
        /// Read reflection data for a type
        /// </summary>
        internal static TypePropertyInfo CreateMyProp(Type t, string name)
        {
            var d = new TypePropertyInfo { 
                filled = true,
                Name = name,
                CanWrite = true,
                parameterType = t,
                isDictionary = t.Name.Contains("Dictionary")
            };
            
            if (d.isDictionary) d.GenericTypes = t.GetGenericArguments();
            if (d is { isDictionary: true, GenericTypes: { Length: > 0 } } && d.GenericTypes[0] == typeof(string))
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
        /// <c>true</c> if the type is <c>Nullable&lt;T&gt;</c>
        /// </summary>
        private static bool IsNullableWrapper(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
        
        /// <summary>
        /// Get the true target type, excluding known wrappers
        /// </summary>
        private static Type GetChangeType(Type conversionType)
        {
            if (IsNullableWrapper(conversionType))
                return conversionType.GetGenericArguments()[0] ?? throw new Exception("Invalid generic arguments");

            return conversionType;
        }
        
        /// <summary>
        /// Try to create a value-setting proxy for an object property
        /// </summary>
        internal static GenericSetter? CreateSetMethod(PropertyInfo propertyInfo)
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
        internal static GenericGetter CreateGetField(Type type, FieldInfo fieldInfo)
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
        internal static GenericSetter CreateSetField(Type type, FieldInfo fieldInfo)
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
        internal static GenericGetter? CreateGetMethod(PropertyInfo propertyInfo)
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
            return propertyInfo.GetGetMethod()?.Invoke(src, _emptyObjectArray) ?? throw new Exception($"Could not get value for {propertyInfo.Name}");
        }

        /// <summary>
        /// Returns alternative names for a field, based on various other libraries' code attributes
        /// </summary>
        internal static IEnumerable<string> GetAlternativeNames(MemberInfo info)
        {
            foreach (var attr in info.GetCustomAttributes())
            {
                var type = attr.GetType();
                switch (type.Name)
                {
                    case "JsonPropertyNameAttribute":
                    {
                        if (type.Namespace != "System.Text.Json.Serialization") continue;
                        var name = type.GetProperty("Name")?.GetValue(attr).ToString();
                        if (!string.IsNullOrWhiteSpace(name)) yield return name!;
                        break;
                    }
                    case "DataMemberAttribute":
                    {
                        if (type.Namespace != "System.Runtime.Serialization") continue;
                        var name = type.GetProperty("Name")?.GetValue(attr).ToString();
                        if (!string.IsNullOrWhiteSpace(name)) yield return name!;
                        break;
                    }
                    case "JsonPropertyAttribute":
                    {
                        if (type.Namespace != "Newtonsoft.Json") continue;
                        var name = type.GetProperty("PropertyName")?.GetValue(attr).ToString();
                        if (!string.IsNullOrWhiteSpace(name)) yield return name!;
                        break;
                    }
                    default: continue;
                }
            }
        }

        public static bool GetPropertySet(Type type, JsonSettings settings, out SafeDictionary<string, TypePropertyInfo> sd)
        {
            var cache = Cache(settings);
            return cache.PropertyCache.TryGetValue(type, out sd);
        }

        public static void AddToPropertyCache(Type type, SafeDictionary<string,TypePropertyInfo> sd, JsonSettings settings)
        {
            var cache = Cache(settings);
            cache.PropertyCache.Add(type, sd);
        }
    }
}