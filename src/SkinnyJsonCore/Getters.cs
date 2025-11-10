using System;
using System.Collections.Generic;
using System.Reflection;

namespace SkinnyJson
{
    internal class Getters
    {
        /// <summary>
        /// Preferred name from type of custom attributes
        /// </summary>
        public string? Name;

        /// <summary>
        /// Method to extract serialisable value
        /// </summary>
        public TypeManager.GenericGetter? Getter;

        /// <summary>
        /// Type of the field or property
        /// </summary>
        public Type? PropertyType;

        /// <summary>
        /// Field info from reflection, if this is a field (not a property)
        /// </summary>
        public FieldInfo? FieldInfo;

        /// <summary>
        /// The original name from type
        /// </summary>
        public string OriginalName = "";
    }

    internal class DatasetSchema
    {
        public List<string>? Info { get; set; }
        public string? Name { get; set; }
    }
}
