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
            StaticClassExample.Span = TimeSpan.FromSeconds(255);
            StaticClassExample.SubClass.Name = "frozen name";
            StaticClassExample.SubClass.DateTime = new DateTime(1999, 12, 31);
            
            var json = Json.Freeze(typeof(StaticClassExample));
            
            Console.WriteLine(json);
            
            StaticClassExample.StringProperty = "property modified";
            StaticClassExample.StringFieldValue = "field modified";
            StaticClassExample.Span = TimeSpan.Zero;
            StaticClassExample.SubClass.Name = "wrong name";
            StaticClassExample.SubClass.DateTime = new DateTime(2099, 1, 1);
            
            Json.DefrostInto(typeof(StaticClassExample), json);
            
            Assert.That(StaticClassExample.StringProperty, Is.EqualTo("property set"));
            Assert.That(StaticClassExample.StringFieldValue, Is.EqualTo("I am field"));
            Assert.That(StaticClassExample.Span.Ticks, Is.EqualTo(TimeSpan.FromSeconds(255).Ticks));
            Assert.That(StaticClassExample.SubClass.Name, Is.EqualTo("frozen name"));
            Assert.That(StaticClassExample.SubClass.DateTime, Is.EqualTo(new DateTime(1999, 12, 31)));
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
            var setOff = JsonParameters.Default.WithCaseSensitivity();
            
            StaticClassExample.StringProperty = "property modified";
            StaticClassExample.StringFieldValue = "field modified";
            Json.DefrostInto(typeof(StaticClassExample), json, setOff);
            
            Assert.That(StaticClassExample.StringProperty, Is.EqualTo("property modified"));
            Assert.That(StaticClassExample.StringFieldValue, Is.EqualTo("field modified"));
            
            
            // Should match lower cased if IgnoreCaseOnDeserialize set
            StaticClassExample.StringProperty = "property modified";
            StaticClassExample.StringFieldValue = "field modified";
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
        }
        
        
        [Test]
        public void Should_be_able_to_freeze_and_restore_the_properties_of_a_static_class_ignoring_extra_whitespace_in_json_string()
        {
            StaticClassExample.StringProperty = "property set";
            StaticClassExample.StringFieldValue = "I am field";
            
            var json = Json.Freeze(typeof(StaticClassExample));
            json = json.Replace(",","\n,\r\n"); // any kind of new line, tabs, spaces
            
            Console.WriteLine(json);
            
            StaticClassExample.StringProperty = "property modified";
            StaticClassExample.StringFieldValue = "field modified";
            
            Json.DefrostInto(typeof(StaticClassExample), json);
            
            Assert.That(StaticClassExample.StringProperty, Is.EqualTo("property set"));
            Assert.That(StaticClassExample.StringFieldValue, Is.EqualTo("I am field"));
        }

        public static class StaticClassExample
        {
            public static string StringProperty { get; set; } = "I am property";
			
            public static string StringFieldValue = "I am field";

            public static TimeSpan Span { get; set; } = TimeSpan.FromDays(0.5);

            public static SubClass SubClass { get; set; } = new();
        }
    }

    public class SubClass
    {
        public string Name { get; set; } = "Test";
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
    }
}