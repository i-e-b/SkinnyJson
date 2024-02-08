using System;
using System.Text; // ReSharper disable InconsistentNaming
using System.Collections.Generic;
using NUnit.Framework;
// ReSharper disable AssignNullToNotNullAttribute

namespace SkinnyJson.Unit.Tests {
	[TestFixture]
	public class FreezeAndDefrostStructs {
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

		[Test]
		public void Should_be_able_to_fill_an_array_field_inside_a_struct()
		{
			var original = new ContainerStruct {
				dummy = "A.N. Otherfield",
				array = new[] {
					new ABasicStruct { aField = "value1", aProp = "prop1" },
					new ABasicStruct { aField = "value2", aProp = "prop2" }
				}
			};
			var frozen = Json.Freeze(original);

			Console.WriteLine(frozen);

			var defrosted = Json.Defrost<ContainerStruct>(frozen);

			Assert.That(defrosted.dummy, Is.EqualTo(original.dummy));
			Assert.That(defrosted.array[0], Is.EqualTo(original.array[0]));
			Assert.That(defrosted.array[1], Is.EqualTo(original.array[1]));
		}
		
		[Test]
		public void Can_defrost_from_byte_array_with_encoding()
		{
			var original = new ABasicStruct { aField = "value", aProp = "different!" };
			var frozen = Json.Freeze(original);
			var frozenBytes = Encoding.UTF8.GetBytes(frozen);

			Console.WriteLine(frozen);

			var defrosted = Json.Defrost<ABasicStruct>(frozenBytes, new UTF8Encoding());

			Assert.That(original, Is.EqualTo(defrosted));
		}
	}

	public struct ContainerStruct
	{
		public string dummy;
		public ABasicStruct[] array;
	}

	public struct ABasicStruct {
		public string aField;
		public string aProp { get; set; }
	}
}
