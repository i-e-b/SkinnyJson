using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class StreamTests {
        const string path = @"C:\Temp\SkinnyJsonOutput.json";

        [TearDown]
        public void Cleanup(){
            if (File.Exists(path)) File.Delete(path);
        }

        [Test]
        public void Can_read_and_write_file_streams()
        {
            ISimpleObject original = SimpleObjectUnderInterface.Make();

            // Write:
            using (var fs_out = File.Open(path, FileMode.Create, FileAccess.Write)){
                Json.Freeze(original, fs_out);
            }

            var result_1 = File.ReadAllText(path);
            Assert.That(result_1, Is.EqualTo("{\"B\":\"this is B\",\"A\":\"this is a\"}"));

            ISimpleObject result;
            // Read:
            using (var fs_in = File.Open(path, FileMode.Open, FileAccess.Read)){
                result = Json.Defrost<ISimpleObject>(fs_in);
            }

            Assert.That(result.B, Is.EqualTo(original.B));
        }

        [Test]
        public void Can_read_and_write_file_streams_with_custom_encoding()
        {
            ISimpleObject original = SimpleObjectUnderInterface.Make();

            // Write:
            using (var fs_out = File.Open(path, FileMode.Create, FileAccess.Write)){
                Json.Freeze(original, fs_out, JsonSettings.Default.WithEncoding(Encoding.UTF32));
            }

            var result_1 = File.ReadAllText(path, Encoding.UTF32);
            Assert.That(result_1, Is.EqualTo("{\"B\":\"this is B\",\"A\":\"this is a\"}"));

            ISimpleObject result;
            // Read:
            using (var fs_in = File.Open(path, FileMode.Open, FileAccess.Read)){
                result = Json.Defrost<ISimpleObject>(fs_in, JsonSettings.Default.WithEncoding(Encoding.UTF32));
            }

            Assert.That(result.B, Is.EqualTo(original.B));
        }

        [Test]
        public void Can_defrost_from_streams_that_do_not_support_synchronous_methods()
        {
            // This is for Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestStream
            // which throws a `System.InvalidOperationException` if you try to use `Read(byte[], int, int)`
            // and requires `ReadAsync(byte[], int, int)`
            
            var settings = JsonSettings.Default.WithCaseSensitivity();
            var input = new NastyHttpStream(StreamData.StreamOfJson());
            var expected = "{\"Hello\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,2,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":4,\"Item3\":[10,20,30]}}}";

            var defrosted = Json.Defrost(input, settings) as Dictionary<string, object>;
            var result = Json.Freeze(defrosted, settings);

            Assert.That(defrosted, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Can_freeze_to_streams_that_do_not_support_synchronous_methods()
        {
            var settings = JsonSettings.Default.WithCaseSensitivity();
            var input = ByteData.ByteArrayOfJson();
            var expected = "{\"Hello\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,2,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":4,\"Item3\":[10,20,30]}}}";

            var defrosted = Json.Defrost(input, settings) as Dictionary<string, object>;

            using var ms = new MemoryStream();
            var target = new NastyHttpStream(ms);
            
            Json.Freeze(defrosted, target, settings);

            var result = Encoding.UTF8.GetString(ms.ToArray());

            Assert.That(defrosted, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}