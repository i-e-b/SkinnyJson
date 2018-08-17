using System; // ReSharper disable InconsistentNaming
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SkinnyJson.Unit.Tests {

    [TestFixture]
    public class CloningTests {

        
        [Test]
        public void clone_has_same_property_values_as_original (){
            var original = ComplexTypes.DictionaryOfDictionary();

            var clone = Json.Clone(original);

            Assert.That(clone.Count, Is.EqualTo(original.Count), "Item count incorrect");
            Assert.That(clone["A"]["X"], Is.EqualTo(original["A"]["X"]));
            Assert.That(clone["B"]["1"], Is.EqualTo(original["B"]["1"]));
        }

        [Test]
        public void clone_has_same_property_values_as_original_hard (){
            var original = ComplexTypes.DictionaryOfDictionaryOfTupleWithList();

            var str = Json.Freeze(original);
            Console.WriteLine(str);

            // TODO: fix tuple deserialising
            var clone = Json.Clone(original);

            Assert.That(clone["Hello"]["Bob"], Is.EqualTo(original["Hello"]["Bob"]));
            Assert.That(clone["World"]["Sam"], Is.EqualTo(original["World"]["Sam"]));
        }

        [Test]
        public void modifying_the_original_does_not_affect_the_clone (){
            Assert.Fail();
        }

        [Test]
        public void modifying_the_clone_does_not_affect_the_original() {
            Assert.Fail();
        }
    }

    [TestFixture]
	public class FreezingAndUnfreezing {
        [TestFixtureSetUp]
        public void setup() {
            Json.DefaultParameters.EnableAnonymousTypes = false;
        }

		[Test]
        public void Should_be_able_to_freeze_and_unfreeze_objects()
        {
			var original = ObjectWithoutAnInterface.Make();
			var frozen = Json.Freeze(original);
			Console.WriteLine(frozen);
			var defrosted = Json.Defrost<ObjectWithoutAnInterface>(frozen);

			Assert.That(defrosted.A, Is.EqualTo(original.A));
			Assert.That(defrosted.B, Is.EqualTo(original.B));
		}

		[Test]
		public void Should_be_able_to_freeze_an_interface () {
			ISimpleObject original = SimpleObjectUnderInterface.Make();
			var frozen = Json.Freeze(original);
			Console.WriteLine(frozen);
			var defrosted = Json.Defrost<ISimpleObject>(frozen);

			Assert.That(defrosted.B, Is.EqualTo(original.B));
		}

		[Test]
		public void Should_be_able_to_assert_type_on_boxed_defrost () {
			var frozen = Json.Freeze(SimpleObjectUnderInterface.Make());
			Console.WriteLine(frozen);
			object defrosted = Json.Defrost(frozen);

			Assert.That(defrosted is ISimpleObject, Is.True);
		}

		[Test]
		public void Should_be_able_to_filter_boxed_objects_on_type () {
			var frozen = new List<string>{
				Json.Freeze(SimpleObjectUnderInterface.Make()),
				Json.Freeze(SimpleObjectUnderInterface.Make()),
				Json.Freeze(ObjectWithoutAnInterface.Make()),
				Json.Freeze(ObjectWithoutAnInterface.Make()),
				Json.Freeze(SimpleObjectUnderInterface.Make()),
			};

			var defrosted = frozen
				.Select(Json.Defrost)
				.Where(o => o is ISimpleObject);

			Assert.That(defrosted.Count(), Is.EqualTo(3));
		}

		[Test]
		public void Should_be_able_to_defrost_to_an_interface () {
			var original = SimpleObjectUnderInterface.Make();
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
			var original = SimpleObjectUnderInterface.Make();
			var frozen = Json.Freeze(original)
				.Replace("SkinnyJson.Unit.Tests", "A.Different.Assembly")
				.Replace("Version=1.0.0.0", "Version=2.3.4.5");

			Console.WriteLine(frozen);
			var defrosted = Json.Defrost<ISimpleObject>(frozen);

			Assert.That(defrosted.B, Is.EqualTo(original.B));
		}

		[Test]
		public void Should_be_able_to_defrost_to_type_name_by_namespace_alone () {
			var original = SimpleObjectUnderInterface.Make();
			var frozen = Json.Freeze(original)
				.Replace(", SkinnyJson.Unit.Tests", "");

			Console.WriteLine(frozen);
			var defrosted = Json.Defrost(frozen);

			Assert.That(defrosted, Is.InstanceOf<ISimpleObject>());
		}

		[Test]
		public void Should_be_able_to_freeze_to_an_interface_where_available () {
			var original = SimpleObjectUnderInterface.Make();
			var frozen = Json.Freeze(original);

			Assert.That(frozen, Contains.Substring("SkinnyJson.Unit.Tests.ISimpleObject"));
		}

		[Test]
		public void Should_be_able_to_handle_chain_of_interfaces () {
			var original = ChainedInterface.Make();
			var frozen = Json.Freeze(original);
			var defrosted = Json.Defrost<ITopLevel>(frozen);

			Assert.That(frozen, Contains.Substring("SkinnyJson.Unit.Tests.ITopLevel"));

			Assert.That(defrosted.A, Is.EqualTo(original.A));
			Assert.That(defrosted.B, Is.EqualTo(original.B));
			Assert.That(defrosted.C, Is.EqualTo(original.C));
		}

		[Test]
		public void Should_be_able_to_handle_chain_of_classes () {
			var original = ChainedClass.Make();
			var frozen = Json.Freeze(original);
			var defrosted = Json.Defrost<ChainedClass>(frozen);

			Assert.That(defrosted.X, Is.EqualTo(original.X));
			Assert.That(defrosted.Y, Is.EqualTo(original.Y));
			Assert.That(defrosted.Z, Is.EqualTo(original.Z));
			Assert.That(defrosted.IKMP, Is.EqualTo(original.IKMP));
		}

		[Test]
		public void Should_be_able_to_handler_marker_interfaces () {
			var original = new UsesMarker { AnId = Guid.NewGuid(), AnotherThing = "hello" };
			var frozen = Json.Freeze(original);
			var defrosted_marker = Json.Defrost<IMarkerInterface>(frozen);
			Console.WriteLine(frozen);
			var defrosted_anon = Json.Defrost(frozen);

			Assert.That(defrosted_marker.AnId, Is.EqualTo(original.AnId));
			Assert.That(defrosted_anon, Is.InstanceOf<IMarkerInterface>());
		}

		[Test]
		public void Should_be_able_to_handle_extended_marker_interfaces () {
			var original = new UsesMarker { AnId = Guid.NewGuid(), AnotherThing = "hello" };
			var frozen = Json.Freeze(original);
			var defrosted = Json.Defrost<IExtendedMarker>(frozen);

			Assert.That(defrosted.AnId, Is.EqualTo(original.AnId));
			Assert.That(defrosted.AnotherThing, Is.EqualTo(original.AnotherThing));
		}

		[Test]
		public void Should_not_strip_interface_levels ()
		{
			ITopLevel original = ChainedInterface.Make();
			var frozen = Json.Freeze(original);
			var defrosted = Json.Defrost(frozen);
			var refrozen = Json.Freeze(defrosted);
			var double_defrosted = Json.Defrost(refrozen);
			Console.WriteLine(refrozen);

			Assert.That(double_defrosted, Is.InstanceOf<ITopLevel>());
		}

	}
}