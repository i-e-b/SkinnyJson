﻿using System.Text;
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException

namespace SkinnyJson.Unit.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class ChangingSettings
    {
        [Test]
        public void can_change_defaults()
        {
			var original = ObjectWithoutAnInterface.Make();

            Json.DefaultParameters.EnableAnonymousTypes = true;
            var on = Json.Freeze(original);
            Json.DefaultParameters.EnableAnonymousTypes = false;
            var off = Json.Freeze(original);
            
            Json.DefaultStreamEncoding = Encoding.UTF7;

            Assert.That(on, Does.Not.Contain("$type"));
            Assert.That(off, Contains.Substring("$type"));
            Json.DefaultParameters.EnableAnonymousTypes = true;
        }


        [Test]
        public void can_exclude_null_types () 
        {
            var input1 = new OptionalSample { One = "set" };
            var input2 = new OptionalSample { Two = "set" };
            
            Json.DefaultParameters.EnableAnonymousTypes = true;

            Json.DefaultParameters.SerializeNullValues = true;
            var on = Json.Freeze(input1);
            Json.DefaultParameters.SerializeNullValues = false;
            var off1 = Json.Freeze(input1);
            var off2 = Json.Freeze(input2);

            Assert.That(on, Is.EqualTo("{\"One\":\"set\",\"Two\":null,\"Three\":null}"));
            Assert.That(off1, Is.EqualTo("{\"One\":\"set\"}"));
            Assert.That(off2, Is.EqualTo("{\"Two\":\"set\"}"));
        }

        [Test]
        public void nulls_in_the_middle_of_structures_get_correct_delimiting ()
        {
            var input = new OptionalSample { One = "set", Three = "set" };
            
            Json.DefaultParameters.EnableAnonymousTypes = true;
            Json.DefaultParameters.SerializeNullValues = false;
            
            var off = Json.Freeze(input);
            
            Assert.That(off, Is.EqualTo("{\"One\":\"set\",\"Three\":\"set\"}"));
        }

        public struct OptionalSample {
            public string One;
            public string Two;
            public string Three;
        }
    }
}