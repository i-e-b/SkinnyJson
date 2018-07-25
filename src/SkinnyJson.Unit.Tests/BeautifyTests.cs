using System.IO;
using System.Text;
using NUnit.Framework;

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class BeautifyTests {
        [Test]
        public void beautify_results_are_stable (){
            
            var input = "{\"key\":\"value\"}";

            var result_1 = Json.Beautify(input);
            var result_2 = Json.Beautify(result_1);
            
            Assert.That(result_1, Is.EqualTo(result_2));
        }

        [Test]
        public void quote_types_dont_get_confused () {
            var input = "{\"k\"  :  \"v',\\\"\",\"another\":4}"; //      {"k":"v',\"","another":4}

            var expected = "{\r\n    \"k\" : \"v',\\\"\",\r\n    \"another\" : 4\r\n}";
            /* Should end up like:
{
    "k": "v',\"",
    "another": 4
}
            */


            var result = Json.Beautify(input);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void can_beautify_with_input_and_output_streams ()
        {
            var ins = new MemoryStream();
            var outs = new MemoryStream();
            
            var input = "{\"key\":\"value\"}";
            var buf = Encoding.UTF32.GetBytes(input);

            ins.Write(buf, 0, buf.Length);
            ins.Seek(0, SeekOrigin.Begin);

            Json.BeautifyStream(ins, Encoding.UTF32, outs, Encoding.UTF7);

            outs.Seek(0, SeekOrigin.Begin);
            var result = Encoding.UTF7.GetString(outs.ToArray());

            Assert.That(result, Is.EqualTo("{\r\n    \"key\" : \"value\"\r\n}"));
        }
    }
}