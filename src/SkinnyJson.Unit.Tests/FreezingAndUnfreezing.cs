using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute
#pragma warning disable CS8602

// ReSharper disable InconsistentNaming
namespace SkinnyJson.Unit.Tests {
	[TestFixture]
	public class FreezingAndUnfreezing {

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
		public void Should_be_able_to_freeze_and_unfreeze_objects_as_byte_arrays()
		{
			var original = ObjectWithoutAnInterface.Make();
			var frozen = Json.FreezeToBytes(original);
			
			Console.WriteLine(frozen);
			var defrosted = Json.Defrost<ObjectWithoutAnInterface>(frozen);

			Assert.That(defrosted.A, Is.EqualTo(original.A));
			Assert.That(defrosted.B, Is.EqualTo(original.B));
		}
        
        [Test]
        public void Should_ignore_extra_whitespace_in_json_string()
        {
	        var original = ObjectWithoutAnInterface.Make();
	        var frozen = Json.Freeze(original);
	        
	        frozen = frozen.Replace(":"," \r\n :   \t\t \n "); // any kind of new line, tabs, spaces
	        
	        Console.WriteLine(frozen);
	        var defrosted = Json.Defrost<ObjectWithoutAnInterface>(frozen);

	        Assert.That(defrosted.A, Is.EqualTo(original.A));
	        Assert.That(defrosted.B, Is.EqualTo(original.B));
        }
        
        [Test]
        public void Should_encode_newlines_in_json_output()
        {
	        var original = ObjectWithoutAnInterface.Make();
	        original.A = "Line one\r\nLine two\rLine three\nLine four.";
	        var frozen = Json.Freeze(original);

	        Assert.That(frozen, Contains.Substring("Line one\\r\\nLine two\\rLine three\\nLine four."));
	        
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
			var frozen = Json.Freeze(SimpleObjectUnderInterface.Make(), JsonSettings.TypeConstrained);
			Console.WriteLine(frozen);
			object defrosted = Json.Defrost(frozen, JsonSettings.TypeConstrained);

			Assert.That(defrosted is ISimpleObject, Is.True);
		}

		[Test]
		public void Should_be_able_to_filter_boxed_objects_on_type () {
			var frozen = new List<string>{
				Json.Freeze(SimpleObjectUnderInterface.Make(), JsonSettings.TypeConstrained),
				Json.Freeze(SimpleObjectUnderInterface.Make(), JsonSettings.TypeConstrained),
				Json.Freeze(ObjectWithoutAnInterface.Make(), JsonSettings.TypeConstrained),
				Json.Freeze(ObjectWithoutAnInterface.Make(), JsonSettings.TypeConstrained),
				Json.Freeze(SimpleObjectUnderInterface.Make(), JsonSettings.TypeConstrained),
			};

			var defrosted = frozen
				.Select(s => Json.Defrost(s, JsonSettings.TypeConstrained))
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
		public void Well_known_interfaces_are_treated_specially()
		{
			var input = Quote("[{'B':'x'},{'B':'y'},{'B':'z'}]");
			//var resultEnumerable = Json.Defrost<IEnumerable<ISimpleObject>>(input);
			//var resultCollection = Json.Defrost<ICollection<ISimpleObject>>(input);
			var resultSet = Json.Defrost<ISet<ISimpleObject>>(input);
			
			//Assert.That(resultEnumerable.ToList().Count, Is.EqualTo(3), "Enumerable length is wrong");
			//Assert.That(resultCollection.Count, Is.EqualTo(3), "Collection length is wrong");
			Assert.That(resultSet.Count, Is.EqualTo(3), "Set length is wrong");
		}

		[Test]
		public void Can_deserialise_string_to_a_runtime_type()
		{
			var result = Json.Defrost(Quote("['a','b','c']"), typeof(IEnumerable<string>));
			
			Assert.That(result, Is.Not.Null);
			
			var final = result as IEnumerable<string>;
			Assert.That(final?.ToList().Count, Is.EqualTo(3), "Did not get expected list");
		}
		
		[Test]
		public void Can_deserialise_bytes_to_a_runtime_type()
		{
			var settings = new JsonSettings { StreamEncoding = Encoding.Unicode };
			var bytes = Encoding.Unicode.GetBytes(Quote("['a','b','c']"));
			var result = Json.Defrost(bytes, typeof(IEnumerable<string>), settings);
			
			Assert.That(result, Is.Not.Null);
			
			var final = result as IEnumerable<string>;
			Assert.That(final?.ToList().Count, Is.EqualTo(3), "Did not get expected list");
		}
		
		[Test]
		public void Can_deserialise_a_stream_to_a_runtime_type()
		{
			var settings = new JsonSettings { StreamEncoding = Encoding.Unicode };
			var bytes = Encoding.Unicode.GetBytes(Quote("['a','b','c']"));
			var stream = new MemoryStream(bytes);
			stream.Seek(0, SeekOrigin.Begin);
			var result = Json.Defrost(stream, typeof(IEnumerable<string>), settings);
			
			Assert.That(result, Is.Not.Null);
			
			var final = result as IEnumerable<string>;
			Assert.That(final?.ToList().Count, Is.EqualTo(3), "Did not get expected list");
		}

		[Test]
		public void Runtime_type_root_array_and_child_arrays_use_same_container()
		{
			var result = Json.Defrost(Quote("[{'child':[{'top':1}]},{'child':[{'top':2}]}]"));
            
			Assert.That(result, Is.InstanceOf<IList>(), "root type");
            
			var outer = result as IList;
			Assert.That(outer, Is.Not.Null, "outer");
            
			var child = outer[0] as IDictionary<string,object>;
			Assert.That(child, Is.Not.Null, "child");
            
			var inner = child["child"];
			Assert.That(inner, Is.InstanceOf<IList>(), "child type");
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
			var settings = JsonSettings.TypeConstrained;
			var frozen = Json.Freeze(original, settings)
				.Replace(", SkinnyJson.Unit.Tests", "");

			Console.WriteLine(frozen);
			var defrosted = Json.Defrost(frozen, settings);

			Assert.That(defrosted, Is.InstanceOf<ISimpleObject>());
		}

		[Test]
		public void Should_be_able_to_freeze_to_an_interface_where_available () {
			var original = SimpleObjectUnderInterface.Make();
			var frozen = Json.Freeze(original, JsonSettings.TypeConstrained);

			Assert.That(frozen, Contains.Substring("SkinnyJson.Unit.Tests.ISimpleObject"));
		}

		[Test]
		public void Should_be_able_to_handle_chain_of_interfaces () {
			var original = ChainedInterface.Make();
			var frozen = Json.Freeze(original, JsonSettings.TypeConstrained);
			var defrosted = Json.Defrost<ITopLevel>(frozen, JsonSettings.TypeConstrained);

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
			Assert.That(defrosted.FromBase, Is.EqualTo(original.FromBase));
		}

		[Test]
		public void Should_be_able_to_handler_marker_interfaces () {
			var original = new UsesMarker { AnId = Guid.NewGuid(), AnotherThing = "hello" };
			var frozen = Json.Freeze(original, JsonSettings.TypeConstrained);
			var defrosted_marker = Json.Defrost<IMarkerInterface>(frozen, JsonSettings.TypeConstrained);
			Console.WriteLine(frozen);
			var defrosted_anon = Json.Defrost(frozen, JsonSettings.TypeConstrained);

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
			var frozen = Json.Freeze(original, JsonSettings.TypeConstrained);
			var defrosted = Json.Defrost(frozen, JsonSettings.TypeConstrained);
			var refrozen = Json.Freeze(defrosted, JsonSettings.TypeConstrained);
			var double_defrosted = Json.Defrost(refrozen, JsonSettings.TypeConstrained);
			Console.WriteLine(refrozen);

			Assert.That(double_defrosted, Is.InstanceOf<ITopLevel>());
		}

        [Test]
        public void Can_use_base_simple_objects_with_no_schema() {
            
            var str = "{ \"TEST\":\"test2\", \"signupId\": 4259648, \"postcode\": \"NP10 8UH\" }";

            var obj = Json.Defrost<object>(str);

            Assert.That(obj, Is.Not.Null);
        }

        [Test]
        public void Can_defrost_from_a_stream ()
        {
	        var settings = JsonSettings.Default.WithCaseSensitivity();
            var input = StreamData.StreamOfJson();
            var expected = "{\"Hello\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,2,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":4,\"Item3\":[10,20,30]}}}";

            var defrosted = Json.Defrost(input, settings) as Dictionary<string, object>;
            var interpreted = Json.Freeze(defrosted, settings);

            Assert.That(defrosted, Is.Not.Null);
            Assert.That(interpreted, Is.EqualTo(expected));
        }

        [Test]
        public void Can_serialise_to_a_stream ()
        {
            var input = ComplexTypes.DictionaryOfDictionaryOfTupleWithList();
            var expected = "{\"Hello\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,2,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":4,\"Item3\":[10,20,30]}}}";

            var ms = new MemoryStream();

            Json.Freeze(input, ms);


            ms.Seek(0, SeekOrigin.Begin);
            var actual = Encoding.UTF8.GetString(ms.ToArray());

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Can_defrost_from_a_byte_array()
        {
	        var settings = JsonSettings.Default.WithCaseSensitivity();
			
            var input = ByteData.ByteArrayOfJson();
            var expected = "{\"Hello\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,2,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":4,\"Item3\":[10,20,30]}}}";

            
            var defrosted = Json.Defrost(input, settings) as Dictionary<string, object>;
            var interpreted = Json.Freeze(defrosted, settings);

            Assert.That(defrosted, Is.Not.Null);
            Assert.That(interpreted, Is.EqualTo(expected));
        }
        
        [Test]
        public void Can_defrost_into_dictionary_object()
        {
	        var settings = JsonSettings.Default.WithCaseSensitivity();
	        
	        var expected = "{\"Hello\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,2,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":4,\"Item3\":[10,20,30]}}}";

	        var defrosted = Json.DefrostInto(new Dictionary<string,object>(), expected, settings);
	        var interpreted = Json.Freeze(defrosted, settings);

	        Assert.That(defrosted, Is.Not.Null);
	        Assert.That(interpreted, Is.EqualTo(expected));
        }
        
        [Test]
        public void Can_read_a_json_object_as_a_shallow_dictionary_of_strings()
        {
	        var settings = JsonSettings.Default.WithCaseSensitivity();
	        var input = "{'Key1':'Val1', 'Key2':'Val2', 'Complex':{'Key3':'Val3'}}".Replace('\'', '"');
	        var defrosted = Json.Defrost<Dictionary<string, string>>(input, settings);
	        
	        Assert.That(defrosted["Key1"], Is.EqualTo("Val1"), "1");
	        Assert.That(defrosted["Key2"], Is.EqualTo("Val2"), "2");
	        Assert.That(defrosted["Complex"], Is.EqualTo("{\"Key3\":\"Val3\"}"), "3");
        }
        
        [Test]
        public void Can_read_a_json_object_as_a_deep_dictionary_of_objects()
        {
	        var settings = JsonSettings.Default.WithCaseSensitivity();
	        var input = "{'Key1':'Val1', 'Key2':'Val2', 'Complex':{'Key3':'Val3'}}".Replace('\'', '"');
	        var defrosted = Json.Defrost<Dictionary<string, object>>(input, settings);
	        
	        Assert.That(defrosted["Key1"], Is.EqualTo("Val1"), "1");
	        Assert.That(defrosted["Key2"], Is.EqualTo("Val2"), "2");
	        Assert.That(defrosted["Complex"] is Dictionary<string, object>);
	        var complex = defrosted["Complex"] as Dictionary<string, object>;
	        Assert.That(complex["Key3"], Is.EqualTo("Val3"), "3");
	        
        }

        private static string Quote(string str) => str.Replace('\'', '"');
    }
}