using System;
using System.Collections.Generic;

namespace SevenDigital.Jester.EventStore.Serialisation
{
    internal class Getters
    {
        public string Name;
        public Json.GenericGetter Getter;
        public Type PropertyType;
    }

    public class DatasetSchema
    {
        public List<string> Info { get; set; }
        public string Name { get; set; }
    }
}
