using System;
using NUnit.Framework;
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8602

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class SpecificNamingTests
    {
        [Test] // Note, we only use the name in [DataMember] or [JsonProperty], other settings are ignored
        public void should_recognise_and_use_common_property_name_overrides()
        {
            var defrosted = Json.Defrost<OverridePropertyType>(
                "{\"custom-name-one\":\"Value one\", \"custom name two\":\"Value two\", \"CUSTOM_name_THREE\":\"Value three\", \"OriginalNameFour\":\"Value four\"}");
            
            Assert.That(defrosted.OriginalNameOne, Is.EqualTo("Value one"));
            Assert.That(defrosted.OriginalNameTwo, Is.EqualTo("Value two"));
            Assert.That(defrosted.OriginalNameThree, Is.EqualTo("Value three"));
            Assert.That(defrosted.OriginalNameFour, Is.EqualTo("Value four"));

            var frozen = Json.Freeze(defrosted);
            Console.WriteLine(frozen);

            Assert.That(frozen, Is.EqualTo("{\"custom-name-one\":\"Value one\",\"custom name two\":\"Value two\",\"CUSTOM_name_THREE\":\"Value three\",\"OriginalNameFour\":\"Value four\"}"));
        }
        
        
        public class OverridePropertyType
        {
            [System.Text.Json.Serialization.JsonPropertyName("custom-name-one")]
            public string? OriginalNameOne { get; set; }
            
            [System.Runtime.Serialization.DataMemberAttribute(Name="custom name two")]
            public string? OriginalNameTwo { get; set; }
            
            [Newtonsoft.Json.JsonProperty("CUSTOM_name_THREE")]
            public string? OriginalNameThree { get; set; }
            
            public string? OriginalNameFour { get; set; }
        }
        
    }
}

namespace System.Text.Json.Serialization {
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonPropertyNameAttribute : Attribute
    {
        public JsonPropertyNameAttribute(string name) { Name = name; }
        public string Name { get; }
    }
}

namespace System.Runtime.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class DataMemberAttribute : Attribute
    {
        public string? Name { get; set; }
    }
}

namespace Newtonsoft.Json
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class JsonPropertyAttribute : Attribute
    {
        public JsonPropertyAttribute(string propertyName) { PropertyName = propertyName; }
        public string? PropertyName { get; set; }
    }
}