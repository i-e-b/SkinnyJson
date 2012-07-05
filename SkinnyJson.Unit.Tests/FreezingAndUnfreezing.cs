using System;
using NUnit.Framework;

namespace SkinnyJson.Unit.Tests {
	[TestFixture]
	public class FreezingAndUnfreezing {
		[Test]
		public void Should_be_able_to_freeze_and_unfreeze_objects () {
			var original = new SimpleObject();
			var frozen = Json.Freeze(original);
			Console.WriteLine(frozen);
			var defrosted = Json.Defrost<SimpleObject>(frozen);

			Assert.That(defrosted.A, Is.EqualTo(original.A));
			Assert.That(defrosted.B, Is.EqualTo(original.B));
		}
	}
}