using System.Diagnostics.CodeAnalysis;
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException
// ReSharper disable MemberCanBePrivate.Global

namespace SkinnyJson.Unit.Tests
{
    using NUnit.Framework;

    [TestFixture]
    [SuppressMessage("ReSharper", "NotAccessedField.Global")]
    public class ChangingSettings
    {
        [Test]
        public void can_change_defaults()
        {
			var original = ObjectWithoutAnInterface.Make();

            var setOn = new JsonSettings { EnableAnonymousTypes = true};
            var setOff = new JsonSettings { EnableAnonymousTypes = false, UseTypeExtensions = true};
            
            var on = Json.Freeze(original, setOn);
            var off = Json.Freeze(original, setOff);
            
            Assert.That(on, Does.Not.Contain("$type"));
            Assert.That(off, Contains.Substring("$type"));
        }


        [Test]
        public void can_exclude_null_types () 
        {
            var input1 = new OptionalSample { One = "set" };
            var input2 = new OptionalSample { Two = "set" };
            
            var set1 = new JsonSettings{
                SerializeNullValues = true
            };
            var set2 = new JsonSettings{
                SerializeNullValues = false
            };

            
            var on = Json.Freeze(input1, set1);
            
            
            var off1 = Json.Freeze(input1, set2);
            var off2 = Json.Freeze(input2, set2);

            Assert.That(on, Is.EqualTo("{\"One\":\"set\",\"Two\":null,\"Three\":null}"));
            Assert.That(off1, Is.EqualTo("{\"One\":\"set\"}"));
            Assert.That(off2, Is.EqualTo("{\"Two\":\"set\"}"));
        }

        [Test]
        public void nulls_in_the_middle_of_structures_get_correct_delimiting ()
        {
            var input = new OptionalSample { One = "set", Three = "set" };
            
            var settings = new JsonSettings{
                EnableAnonymousTypes = true,
                SerializeNullValues = false
            };
            
            var off = Json.Freeze(input, settings);
            
            Assert.That(off, Is.EqualTo("{\"One\":\"set\",\"Three\":\"set\"}"));
        }

        public struct OptionalSample {
            public string One;
            public string Two;
            public string Three;
        }
    }
}