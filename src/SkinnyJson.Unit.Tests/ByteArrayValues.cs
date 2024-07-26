using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null value
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class ByteArrayValues
    {
        [Test]
        public void Should_be_able_to_freeze_byte_array_values_to_base64()
        {
            var input = new BytesSets
            {
                ByteArray = Encoding.UTF8.GetBytes("This is a byte array"),
                ByteList = new List<byte>(Encoding.UTF8.GetBytes("This is a byte array")),
                ByteStream = new MemoryStream()
            };
            input.ByteStream.Write(input.ByteArray, 0, input.ByteArray.Length);
            input.ByteStream.Seek(0, SeekOrigin.Begin);

            var frozen = Json.Freeze(input);
            Console.WriteLine(frozen);
            var result = Json.Defrost<BytesSets>(frozen);

            Assert.That(result.ByteArray, Is.EqualTo(input.ByteArray).AsCollection);
            Assert.That(result.ByteList, Is.EqualTo(input.ByteList).AsCollection);
            Assert.That(((MemoryStream)result.ByteStream).ToArray(), Is.EqualTo(input.ByteArray).AsCollection);
        }

        [Test]
        public void Can_defrost_byte_array_values_from_number_lists()
        {
            const string source = "{\"ByteArray\":[84,104,105,115,32,105,115,32,97,32,98,121,116,101,32,97,114,114,97,121]," +
                                  "\"ByteList\":[84,104,105,115,32,105,115,32,97,32,98,121,116,101,32,97,114,114,97,121]," +
                                  "\"ByteStream\":[84,104,105,115,32,105,115,32,97,32,98,121,116,101,32,97,114,114,97,121]}";
            
            var result = Json.Defrost<BytesSets>(source);
            var expected = Encoding.UTF8.GetBytes("This is a byte array");

            Assert.That(result.ByteArray, Is.EqualTo(expected).AsCollection);
            Assert.That(result.ByteList, Is.EqualTo(expected).AsCollection);
            Assert.That(((MemoryStream)result.ByteStream).ToArray(), Is.EqualTo(expected).AsCollection);
        }
        
        [Test]
        public void Can_defrost_byte_array_values_from_hex_values()
        {
            // Annoyingly, 0x... and 0h are both valid base64. So we only accept $... to force hex interpretation
            const string source = "{\"ByteArray\":\"$5468697320697320612062797465206172726179\"," +
                                  "\"ByteList\":\"$5468697320697320612062797465206172726179\"," +
                                  "\"ByteStream\":\"$5468697320697320612062797465206172726179\"}";

            var result = Json.Defrost<BytesSets>(source);
            var expected = Encoding.UTF8.GetBytes("This is a byte array");

            Assert.That(result.ByteArray, Is.EqualTo(expected).AsCollection);
            Assert.That(result.ByteList, Is.EqualTo(expected).AsCollection);
            Assert.That(((MemoryStream)result.ByteStream).ToArray(), Is.EqualTo(expected).AsCollection);
        }


        public class BytesSets
        {
            public byte[] ByteArray { get; set; } = Array.Empty<byte>();

            public List<byte>? ByteList { get; set; }

            public Stream? ByteStream { get; set; }
        }
    }
}