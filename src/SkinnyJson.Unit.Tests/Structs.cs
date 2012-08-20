using System; // ReSharper disable InconsistentNaming
using NUnit.Framework;

namespace SkinnyJson.Unit.Tests {
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

	}

	public struct ABasicStruct {
		public string aField;
		public string aProp { get; set; }
	}
}
