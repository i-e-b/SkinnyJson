using System;
using System.Collections.Generic;
using System.Reflection;

namespace SkinnyJson
{
    internal class Getters
    {
        public string Name;
        public Json.GenericGetter Getter;
        public Type PropertyType;
		public FieldInfo FieldInfo;
    }

    public class DatasetSchema
    {
        public List<string> Info { get; set; }
        public string Name { get; set; }
    }
}
