using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SkinnyJson.Unit.Tests.ExampleData;

// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8602

namespace SkinnyJson.Unit.Tests
{
    /// <summary>
    /// Tests for real-world applications and bugs
    /// </summary>
    [TestFixture]
    public class SpecificTypeTests
    {
        [Test]
        public void Timespan_from_all_properties()
        {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>(
                "{\"timeSpan\":" +
                "{\"Ticks\":100000000,\"Days\":0,\"Hours\":0,\"Milliseconds\":0,\"Minutes\":0,\"Seconds\":10," +
                "\"TotalDays\":0.00011574074074074075,\"TotalHours\":0.002777777777777778,\"TotalMilliseconds\":10000,\"TotalMinutes\":0.16666666666666666,\"TotalSeconds\":10}}");

            Assert.That(defrosted.timeSpan.Ticks, Is.EqualTo(100000000), "TimeSpan was not interpreted correctly");
        }

        [Test]
        public void Timespan_from_basic_properties()
        {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>(
                "{\"timeSpan\":" +
                "{\"Ticks\":100000000,\"Days\":0,\"Hours\":0,\"Milliseconds\":0,\"Minutes\":0,\"Seconds\":10}}");

            Assert.That(defrosted.timeSpan.Ticks, Is.EqualTo(100000000), "TimeSpan was not interpreted correctly");
        }

        [Test]
        public void Timespan_from_just_ticks()
        {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"timeSpan\":{\"Ticks\":100000000}}");

            Assert.That(defrosted.timeSpan.Ticks, Is.EqualTo(100000000), "TimeSpan was not interpreted correctly");
        }

        [Test]
        public void Timespan_serialises_to_compact_string()
        {
            Json.DefaultParameters.EnableAnonymousTypes = true;
            var frozen = Json.Freeze(new TimespanContainer { Timespan = TimeSpan.FromMinutes(101.101) });

            Assert.That(frozen, Is.EqualTo("{\"Timespan\":\"01:41:06.0600000\"}"));
        }

        [Test]
        public void Timespan_deserialised_from_compact_string()
        {
            var defrosted = Json.Defrost<TimespanContainer>("{\"Timespan\":\"01:41:06.0600000\"}");
            Assert.That(defrosted.Timespan.Ticks, Is.EqualTo(60660600000), "TimeSpan was not interpreted correctly");

            defrosted = Json.Defrost<TimespanContainer>("{\"Timespan\":\"01:41:05\"}");
            Assert.That(defrosted.Timespan.Ticks, Is.EqualTo(60650000000), "TimeSpan was not interpreted correctly");
        }

        [Test]
        public void Can_map_bool_strings_to_bool_fields_and_properties()
        {
            var defrosted = Json.Defrost<ILikeBools>("{\"val1\":\"true\",\"val2\":\"false\",\"val3\":true,\"val4\":false}");

            Assert.That(defrosted.val1, Is.True, "'true' string was not correctly interpreted");
            Assert.That(defrosted.val2, Is.False, "'false' string was not correctly interpreted");
            Assert.That(defrosted.val3, Is.True, "'true' bool was not correctly interpreted");
            Assert.That(defrosted.val4, Is.False, "'false' bool was not correctly interpreted");
        }

        [Test]
        public void Can_chain_well_known_containers_and_unknown_interface_types()
        {
            // See Json.cs::TryMakeStandardContainer() if your container type isn't supported
            var d = Json.Defrost<ContainerOfEnumerators>(Quote("{'Prop1':[{'val1':'true','val2':'false','val3':true,'val4':false}], 'Prop2':['Hello', 'world']}"));

            Assert.That(d.Prop1, Is.Not.Null, "First enumerable was null");
            var l1 = d.Prop1.ToList();
            Assert.That(l1.Count, Is.EqualTo(1), "First enumerable was wrong length");

            Assert.That(d.Prop2, Is.Not.Null, "Second enumerable was null");
            var l2 = d.Prop2.ToList();
            Assert.That(l2.Count, Is.EqualTo(2), "Second enumerable was wrong length");
        }

        [Test]
        public void warnings_if_property_case_doesnt_match()
        {
            var input = @"{
  ""success"": false,
  ""errorMessage"": ""Failed to publish command message to queue""
}";
            
            Json.DefaultParameters.IgnoreCaseOnDeserialize = true;
            var output = Json.Defrost<BaseResponse>(input);
            
            Assert.That(output.Success, Is.False);
            Assert.That(output.ErrorMessage, Is.EqualTo("Failed to publish command message to queue"));
            
            Json.DefaultParameters.IgnoreCaseOnDeserialize = false;
            var exception = Assert.Throws<Exception>(()=>Json.Defrost<BaseResponse>(input));
            
            Assert.That(exception.Message, Contains.Substring("Properties would match if IgnoreCaseOnDeserialize was set to true"));
        }

        [Test]
        public void complex_rabbitmq_api_example()
        {
            var text = File.ReadAllText("ExampleData/RabbitMq.txt");
            
            Json.DefaultParameters.EnableAnonymousTypes = true;
            Json.DefaultParameters.IgnoreCaseOnDeserialize = true;
            
            Json.DefaultParameters.StrictMatching = true;
            Assert.Throws<Exception>(()=>Json.Defrost<List<RabbitMqStatistic>>(text));
            
            Json.DefaultParameters.StrictMatching = false;
            var objects = Json.Defrost<List<RabbitMqStatistic>>(text);
            
            Json.DefaultParameters.StrictMatching = true;
            
            Assert.That(objects, Is.Not.Null);
            Assert.That(objects[0].Name, Is.Not.Null);
        }

        [Test]
        public void byte_array_should_fall_back_to_hex_string_if_input_is_not_base64()
        {
            var resultBase64 = Json.Defrost<ByteArrayContainer>(Quote("{'Bytes':'Y29udmVydA=='}"));
            var resultHexStr = Json.Defrost<ByteArrayContainer>(Quote("{'Bytes':'636F6E76657274'}"));

            Assert.That(Encoding.UTF8.GetString(resultBase64.Bytes), Is.EqualTo("convert"), "base64 should be converted");
            Assert.That(Encoding.UTF8.GetString(resultHexStr.Bytes), Is.EqualTo("convert"), "hex string should be converted");
        }

        private string Quote(string src) => src.Replace('\'', '"');
    }

    public class ByteArrayContainer
    {
        public byte[] Bytes { get; set; }
    }

    public class ContainerOfEnumerators
    {
#pragma warning disable 8618
        public IEnumerable<ILikeBools> Prop1 { get; set; }
        public IEnumerable<string> Prop2 { get; set; }
#pragma warning restore 8618
    }

    // ReSharper disable once IdentifierTypo
    public interface ILikeBools {
        bool val1 { get; set; }
        bool val2 { get; set; }
        bool val3 { get; set; }
        bool val4 { get; set; }
    }
    public interface IHaveLotsOfTypes {
        DateTime date_time { get; set; }
        TimeSpan timeSpan { get; set; }
    }
    public class TimespanContainer
    {
        public TimeSpan Timespan { get; set; }
    }
    
    public class BaseResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}