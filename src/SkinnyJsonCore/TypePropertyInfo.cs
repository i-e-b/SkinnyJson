using System;

namespace SkinnyJson
{
    public partial class Json
    {
        /// <summary>
        /// Record of reflected type info for fields and properties on objects
        /// </summary>
        private struct TypePropertyInfo
        {
// ReSharper disable InconsistentNaming
            public bool filled;
            public Type? parameterType;
            public Type? bt;
            public Type? changeType;
            public bool isDictionary;
            public bool isValueType;
            public bool isGenericType;
            public bool isArray;
            public bool isByteArray;
            public bool isGuid;
            public bool isDataSet;
            public bool isDataTable;
            public bool isHashtable;
            public GenericSetter? setter;
            public bool isEnum;
            public bool isDateTime;
            public bool isTimeSpan;
            public Type[] GenericTypes;
            public bool isInt;
            public bool isLong;
            public bool isString;
            public bool isBool;
            public bool isClass;
            public GenericGetter? getter;
            public bool isStringDictionary;
// ReSharper restore InconsistentNaming
            public bool CanWrite;
            public string Name;
        }
    }
}