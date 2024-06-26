﻿using System;

namespace SkinnyJson
{
    /// <summary>
    /// Record of reflected type info for fields and properties on objects
    /// </summary>
    internal struct TypePropertyInfo
    {
// ReSharper disable InconsistentNaming
        public bool filled;
        public Type? parameterType;
        public Type? bt;
        public Type? changeType;
        public bool isDictionary;
        public bool isInterface;
        public bool isValueType;
        public bool isGenericType;
        public bool isArray;
        public bool isByteArray;
        public bool isGuid;
        public bool isDataSet;
        public bool isDataTable;
        public bool isHashtable;
        public TypeManager.GenericSetter? setter;
        public bool isEnum;
        public bool isDateTime;
        public bool isTimeSpan;
        public Type[] GenericTypes;
        public bool isNumeric;
        public bool isString;
        public bool isBool;
        public bool isClass;
        public TypeManager.GenericGetter? getter;
        public bool isStringDictionary;
        public bool CanWrite;
        public string Name;
        public bool isEnumerable;
// ReSharper restore InconsistentNaming
    }
}