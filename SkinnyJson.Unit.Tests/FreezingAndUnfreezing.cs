using System; // ReSharper disable InconsistentNaming
using NUnit.Framework;

namespace SkinnyJson.Unit.Tests {
	[TestFixture]
	public class FreezingAndUnfreezing {
		[Test]
		public void Should_be_able_to_freeze_and_unfreeze_objects () {
			var original = SimpleObject.Make();
			var frozen = Json.Freeze(original);
			Console.WriteLine(frozen);
			var defrosted = Json.Defrost<SimpleObject>(frozen);

			Assert.That(defrosted.A, Is.EqualTo(original.A));
			Assert.That(defrosted.B, Is.EqualTo(original.B));
		}

		[Test]
		public void Should_be_able_to_freeze_an_interface () {
			ISimpleObject original = SimpleObject.Make();
			var frozen = Json.Freeze(original);
			Console.WriteLine(frozen);
			var defrosted = Json.Defrost<SimpleObject>(frozen);

			Assert.That(defrosted.A, Is.EqualTo("this is a"));
			Assert.That(defrosted.B, Is.EqualTo(original.B));
		}

		[Test]
		public void Should_be_able_to_defrost_to_an_interface () {
			var original = SimpleObject.Make();
			var frozen = Json.Freeze(original);
			Console.WriteLine(frozen);
			var defrosted = Json.Defrost<ISimpleObject>(frozen);

			Assert.That(defrosted.B, Is.EqualTo(original.B));
		}

		[Test]
		public void Can_proxy_a_basic_interface () {
			var px = DynamicProxy.GetInstanceFor<IHaveMethods>();

			Assert.That(px.AMethod(), Is.Null);
		}

		[Test]
		public void Can_proxy_an_interface_with_properties () {
			var px = DynamicProxy.GetInstanceFor<IHaveProperties>();

			Assert.That(px.AProperty, Is.Null);
		}

		[Test]
		public void Can_persist_to_proxy_properties () {
			var px = DynamicProxy.GetInstanceFor<IHaveProperties>();
			px.AProperty = "hello";

			Assert.That(px.AProperty, Is.EqualTo("hello"));
		}

		[Test]
		public void Can_proxy_an_interface_with_interface_properties () {
			var px = DynamicProxy.GetInstanceFor<IHaveComplexProperties>();

			px.BProperty = DynamicProxy.GetInstanceFor<IHaveProperties>();
			px.BProperty.AProperty = "hello";

			Assert.That(px.BProperty.AProperty, Is.EqualTo("hello"));
		}

		[Test]
		public void Should_be_able_to_defrost_to_an_interface_when_original_is_not_available () {
			var original = SimpleObject.Make();
			var frozen = Json.Freeze(original).Replace("SkinnyJson.Unit.Tests", "A.Different.Assembly");
			Console.WriteLine(frozen);
			var defrosted = Json.Defrost<ISimpleObject>(frozen);

			Assert.That(defrosted.B, Is.EqualTo(original.B));
		}

		[Test]
		public void Should_be_freeze_to_an_interface_where_available () {
			var original = SimpleObject.Make();
			var frozen = Json.Freeze(original);

			Assert.That(frozen, Contains.Substring("SkinnyJson.Unit.Tests.ISimpleObject"));
		}
	}

	public interface IHaveComplexProperties {
		string AProperty { get; set; }
		IHaveProperties BProperty { get; set; }
	}

	public interface IHaveProperties {
		string AProperty { get; set; }
	}

	public interface IHaveMethods {
		object AMethod ();
	}
}