using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
// ReSharper disable AssignNullToNotNullAttribute

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class DynamicTypeTests {
        [Test]
        public void Can_defrost_string_into_a_dynamic_and_read_from_the_result()
        {
            var original = "{\"Hello\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,2,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":\"What?\",\"Item3\":[10,20,30]}}}";
            dynamic subject = Json.DefrostDynamic(original);

            // direct cast access:
            Assert.That((int)subject.Hello.Bob.Item1, Is.EqualTo(1));
            Assert.That((int)subject.Hello.Bob.Item3[1], Is.EqualTo(2));
            Assert.That((string)subject.World.Sam.Item2, Is.EqualTo("What?"));

            // 'invoke' style access:
            Assert.That(subject.Hello.Bob.Item1(), Is.EqualTo(1));
            Assert.That(subject.Hello.Bob.Item3[1](), Is.EqualTo(2));
            Assert.That(subject.World.Sam.Item2(), Is.EqualTo("What?"));
            
            // using string indexes:
            Assert.That(subject["Hello"].Bob.Item1(), Is.EqualTo(1));
            Assert.That(subject.Hello["Bob"].Item3[1](), Is.EqualTo(2));
            Assert.That(subject.World.Sam["Item2"](), Is.EqualTo("What?"));
        }
        
        [Test]
        public void Can_defrost_stream_into_a_dynamic_and_read_from_the_result()
        {
            var original = "{\"Hello\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,2,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":\"What?\",\"Item3\":[10,20,30]}}}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(original));
            dynamic subject = Json.DefrostDynamic(stream);

            // direct cast access:
            Assert.That((int)subject.Hello.Bob.Item1, Is.EqualTo(1));
            Assert.That((int)subject.Hello.Bob.Item3[1], Is.EqualTo(2));
            Assert.That((string)subject.World.Sam.Item2, Is.EqualTo("What?"));

            // 'invoke' style access:
            Assert.That(subject.Hello.Bob.Item1(), Is.EqualTo(1));
            Assert.That(subject.World.Sam.Item2(), Is.EqualTo("What?"));
            Assert.That(subject.Hello.Bob.Item3[1](), Is.EqualTo(2));
            
            // using string indexes:
            Assert.That(subject["Hello"].Bob.Item1(), Is.EqualTo(1));
            Assert.That(subject.Hello["Bob"].Item3[1](), Is.EqualTo(2));
            Assert.That(subject.World.Sam["Item2"](), Is.EqualTo("What?"));
        }
        
        [Test]
        public void Can_defrost_stream_into_a_dynamic_and_read_from_the_result_with_custom_encoding()
        {
            var setBeu = JsonSettings.Default.WithEncoding(Encoding.BigEndianUnicode);
            var original = "{\"Hello\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,2,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":\"What?\",\"Item3\":[10,20,30]}}}";
            using var stream = new MemoryStream(Encoding.BigEndianUnicode.GetBytes(original));
            dynamic subject = Json.DefrostDynamic(stream, setBeu);

            // direct cast access:
            Assert.That((int)subject.Hello.Bob.Item1, Is.EqualTo(1));
            Assert.That((int)subject.Hello.Bob.Item3[1], Is.EqualTo(2));
            Assert.That((string)subject.World.Sam.Item2, Is.EqualTo("What?"));

            // 'invoke' style access:
            Assert.That(subject.Hello.Bob.Item1(), Is.EqualTo(1));
            Assert.That(subject.Hello.Bob.Item3[1](), Is.EqualTo(2));
            Assert.That(subject.World.Sam.Item2(), Is.EqualTo("What?"));
            
            // using string indexes:
            Assert.That(subject["Hello"].Bob.Item1(), Is.EqualTo(1));
            Assert.That(subject.Hello["Bob"].Item3[1](), Is.EqualTo(2));
            Assert.That(subject.World.Sam["Item2"](), Is.EqualTo("What?"));
        }

        [Test]
        public void Can_fork_a_dynamic_path_and_make_multiple_selections ()
        {
            var original = "{\"Hello\":{\"World\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,2,3]},\"Sam\":{\"Item1\":3,\"Item2\":\"What?\",\"Item3\":[10,20,30]}}}}}";
            dynamic subject = Json.DefrostDynamic(original);

            var parent = subject.Hello.World;
            var fork1 = parent.Bob.Item1();
            var fork2 = parent.Sam.Item1();

            Assert.That(fork1, Is.EqualTo(1));
            Assert.That(fork2, Is.EqualTo(3));
        }

        [Test]
        public void Can_defrost_and_refreeze_json_after_editing_as_dynamic (){
            
            var original = "{\"Hello\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,2,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":\"What?\",\"Item3\":[10,20,30]}}}";
            var expected = "{\"Hello\":{\"Bob\":{\"Item1\":\"Fish!\",\"Item2\":2,\"Item3\":[1,30,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":4,\"Item3\":[10,2,30]}}}";
            dynamic subject = Json.DefrostDynamic(original);

            subject.Hello.Bob.Item3[1] = 30;
            subject.Hello.Bob.Item1 = "Fish!";
            subject.World.Sam.Item3[1] = 2;
            subject.World.Sam.Item2 = 4;

            var actual = Json.Freeze(subject);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Can_use_direct_edit_on_a_string()
        {
            var original = "{\"Hello\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,2,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":4,\"Item3\":[10,20,30]}}}";
            var expected = "{\"Hello\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,30,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":4,\"Item3\":[10,2,30]}}}";

            var actual = Json.Edit(original, d => {
                d.Hello.Bob.Item3[1] = 30;
                d.World.Sam.Item3[1] = 2;
            });
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Can_inspect_type_names ()
        {
            var obj = new SimpleObjectUnderInterface();
            var result = Json.WrapperType(obj);
            Assert.That(result, Contains.Substring("ISimpleObject"));
        }

        [Test]
        public void Can_next_interfaces_inside_each_other_and_get_nested_proxies_as_output()
        {
            var content = Quote(@"
{
'servers': [
    {     
      'id': 42,     
      'labels': {},     
      'public_net': {
        'ipv4': {
          'blocked': false,
          'dns_ptr': 'server01.example.com',
          'id': 42,
          'ip': '1.2.3.4'
        }
      }   
    }
]
}
");
            
            var result = Json.DefrostFromPath<IServerInfo>("servers", content).ToArray().First();
            
            Assert.That(result.Id, Is.EqualTo(42));
            Assert.That(result.Labels.Count, Is.EqualTo(0));
            Assert.That(result.PublicNet.Ipv4.DnsPtr, Is.EqualTo("server01.example.com"));
        }


        private string Quote(string src) => src.Replace('\'', '"');
    }
}
    
public interface IServerInfo
{
    public long Id { get; }
    public Dictionary<string, object?> Labels { get; }
    public IPublicNetInfo PublicNet { get; }
}

public interface IPublicNetInfo
{
    public IIPv4Info Ipv4 { get; }
}

public interface IIPv4Info
{
    // ReSharper disable once InconsistentNaming
    public string DnsPtr { get; }
}