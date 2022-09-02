using System;
using NUnit.Framework;

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class StaticClassTests
    {
        [Test]
        public void Should_be_able_to_freeze_and_restore_the_properties_of_a_static_class()
        {
            StaticClassExample.StringProperty = "property set";
            StaticClassExample.StringFieldValue = "I am field";
            
            var json = Json.Freeze(typeof(StaticClassExample));
            
            Console.WriteLine(json);
            
            StaticClassExample.StringProperty = "property modified";
            StaticClassExample.StringFieldValue = "field modified";
            
            Json.DefrostInto(typeof(StaticClassExample), json);
            
            Assert.That(StaticClassExample.StringProperty, Is.EqualTo("property set"));
            Assert.That(StaticClassExample.StringFieldValue, Is.EqualTo("I am field"));
        }
        
        [Test]
        public void Static_class_defrost_should_work_if_extra_properties_are_present()
        {
            StaticClassExample.StringProperty = "property set";
            StaticClassExample.StringFieldValue = "I am field";
            
            var json = Json.Freeze(typeof(StaticClassExample));
            
            json = json.Substring(0, json.Length - 1) + ",\"ExtraProperty\":123456}";
            
            Console.WriteLine(json);
            
            StaticClassExample.StringProperty = "property modified";
            StaticClassExample.StringFieldValue = "field modified";
            
            Json.DefrostInto(typeof(StaticClassExample), json);
            
            Assert.That(StaticClassExample.StringProperty, Is.EqualTo("property set"));
            Assert.That(StaticClassExample.StringFieldValue, Is.EqualTo("I am field"));
        }
        
        [Test]
        public void Static_class_defrost_should_obey_case_sensitivity_setting()
        {
            StaticClassExample.StringProperty = "property set";
            StaticClassExample.StringFieldValue = "I am field";
            
            var json = Json.Freeze(typeof(StaticClassExample));
            
            Console.WriteLine(json);
            
            json = json.ToLowerInvariant();
            
            // Should not match if IgnoreCaseOnDeserialize not set
            Json.DefaultParameters.IgnoreCaseOnDeserialize = false;
            StaticClassExample.StringProperty = "property modified";
            StaticClassExample.StringFieldValue = "field modified";
            Json.DefrostInto(typeof(StaticClassExample), json);
            
            Assert.That(StaticClassExample.StringProperty, Is.EqualTo("property modified"));
            Assert.That(StaticClassExample.StringFieldValue, Is.EqualTo("field modified"));
            
            
            // Should match lower cased if IgnoreCaseOnDeserialize set
            StaticClassExample.StringProperty = "property modified";
            StaticClassExample.StringFieldValue = "field modified";
            Json.DefaultParameters.IgnoreCaseOnDeserialize = true;
            Json.DefrostInto(typeof(StaticClassExample), json);
            
            Assert.That(StaticClassExample.StringProperty, Is.EqualTo("property set"));
            Assert.That(StaticClassExample.StringFieldValue, Is.EqualTo("i am field"));
            
            // Should match upper cased if IgnoreCaseOnDeserialize set
            json = json.ToUpperInvariant();
            StaticClassExample.StringProperty = "property modified";
            StaticClassExample.StringFieldValue = "field modified";
            Json.DefrostInto(typeof(StaticClassExample), json);
            
            Assert.That(StaticClassExample.StringProperty, Is.EqualTo("PROPERTY SET"));
            Assert.That(StaticClassExample.StringFieldValue, Is.EqualTo("I AM FIELD"));
            
            // restore default
            Json.DefaultParameters.IgnoreCaseOnDeserialize = false;
        }

        public static class StaticClassExample
        {
            public static string StringProperty { get; set; } = "I am property";
			
            public static string StringFieldValue = "I am field";
        }
    }
}