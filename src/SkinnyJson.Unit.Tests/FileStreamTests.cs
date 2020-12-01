using System.IO;
using System.Text;
using NUnit.Framework;
// ReSharper disable InconsistentNaming
// ReSharper disable AssignNullToNotNullAttribute

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class FileStreamTests {
        const string path = @"C:\Temp\SkinnyJsonOutput.json";

        [TearDown]
        public void Cleanup(){
            if (File.Exists(path)) File.Delete(path);
        }

        [Test]
        public void Can_read_and_write_file_streams()
        {
            Json.DefaultStreamEncoding = Encoding.UTF8;
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
                Json.Freeze(original, fs_out, Encoding.UTF32);
            }

            var result_1 = File.ReadAllText(path, Encoding.UTF32);
            Assert.That(result_1, Is.EqualTo("{\"B\":\"this is B\",\"A\":\"this is a\"}"));

            ISimpleObject result;
            // Read:
            using (var fs_in = File.Open(path, FileMode.Open, FileAccess.Read)){
                result = Json.Defrost<ISimpleObject>(fs_in, Encoding.UTF32);
            }

            Assert.That(result.B, Is.EqualTo(original.B));
        }
    }
}