using System; // ReSharper disable InconsistentNaming
using System.Collections.Generic;
using NUnit.Framework;

namespace SkinnyJson.Unit.Tests {
    [TestFixture]
    public class FillingObjectsTests{
        [Test]
        public void can_set_the_properties_of_an_existing_object_using_a_json_string ()
        {
            var target = new ObjectWithoutAnInterface();
            var source = "{\"A\":\"master\",\r\n \"B\": \"blaster\"}";

            Json.FillObject(target, source);

            Assert.That(target.A, Is.EqualTo("master"));
            Assert.That(target.B, Is.EqualTo("blaster"));
        }

        [Test]
        public void can_set_the_values_of_an_implicitly_typed_object_from_a_json_string ()
        {
            var target = new { A = "old", B = "old" };
            var source = "{\"A\":\"master\",\r\n \"B\": \"blaster\"}";

            Json.FillObject(target, source);

            Assert.That(target.A, Is.EqualTo("master"));
            Assert.That(target.B, Is.EqualTo("blaster"));
        }
    }

	[TestFixture]
	public class Structs {
		[Test]
		public void Should_be_able_to_freeze_and_unfreeze_a_struct () {
			var original = new ABasicStruct { aField = "value", aProp = "different!" };
			var frozen = Json.Freeze(original);

			Console.WriteLine(frozen);

			var defrosted = Json.Defrost<ABasicStruct>(frozen);

			Assert.That(original, Is.EqualTo(defrosted));
		}

		[Test]
		public void Should_be_able_to_freeze_and_unfreeze_a_list_of_structs ()
		{
			var original = new List<ABasicStruct> {
				new ABasicStruct { aField = "value1", aProp = "prop1" },
				new ABasicStruct { aField = "value2", aProp = "prop2" },
			};
			var frozen = Json.Freeze(original);

			Console.WriteLine(frozen);

			var defrosted = Json.Defrost<IEnumerable<ABasicStruct>>(frozen);

			Assert.That(defrosted, Is.EquivalentTo(original));
		}
	}

	public struct ABasicStruct {
		public string aField;
		public string aProp { get; set; }
	}
}
