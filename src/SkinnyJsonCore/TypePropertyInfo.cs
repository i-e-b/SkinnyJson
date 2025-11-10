using System;

namespace SkinnyJson
{
    /// <summary>
    /// Record of reflected type info for fields and properties on objects
    /// </summary>
    internal class TypePropertyInfo
    {
        public override string ToString()
        {
            return $"{ContainerName}.{Name}";
        }

        // ReSharper disable InconsistentNaming
        public bool                       filled;

        /// <summary>
        /// Type of the property or field
        /// </summary>
        public Type?                      PropertyType;

        /// <summary>
        /// Array element type, or enumerable generic item type
        /// </summary>
        public Type?                      elementType;

        /// <summary>
        /// Type of the property or field after removing known wrapper types (such as <c>Nullable&lt;T&gt;</c>)
        /// </summary>
        public Type?                      changeType;
        public bool                       isDictionary;
        public bool                       isInterface;
        public bool                       isValueType;
        public bool                       isGenericType;
        public bool                       isArray;
        public bool                       isByteArray;
        public bool                       isGuid;
        public bool                       isDataSet;
        public bool                       isDataTable;
        public bool                       isHashtable;
        public bool                       isEnum;
        public bool                       isDateTime;
        public bool                       isTimeSpan;
        public Type[]?                    GenericTypes;
        public bool                       isNumeric;
        public bool                       isString;
        public bool                       isBool;
        public bool                       isClass;

        public TypeManager.GenericSetter? setter;
        public TypeManager.GenericGetter? getter;

        public bool                       isStringDictionary;
        public bool                       CanWrite;
        public string?                    Name;
        public bool                       isEnumerable;

        public Type? customSerialiser;
        public object[]? customSerialiserParams;

        public string? ContainerName;
        // ReSharper restore InconsistentNaming
    }
}