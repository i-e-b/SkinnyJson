using System;
using NUnit.Framework;

namespace SkinnyJson.Unit.Tests
{
#pragma warning disable CS8602
    [TestFixture]
    public class ErrorMessages
    {
        
        [Test]
        public void an_empty_json_string_gives_a_useful_error()
        {
            var exception = Assert.Throws<Exception>(()=>Json.Defrost<ObjectWithoutAnInterface>(""));

            Console.WriteLine(exception.Message);
            Assert.That(exception.Message, Is.EqualTo("Reached end of input unexpectedly; Zero length input"));
        }
        
        [Test]
        public void a_truncated_json_string_gives_a_useful_error()
        {
            var exception = Assert.Throws<Exception>(()=>Json.Defrost<ObjectWithoutAnInterface>(Quote("{'A':'Hello', 'B'")));

            Console.WriteLine(exception.Message);
            Assert.That(exception.Message, Is.EqualTo("Reached end of input unexpectedly; {\"A\":\"Hello\", \"B\u035f\""));
        }
        
        [Test]
        public void long_truncated_json_string_gives_a_useful_error()
        {
            var exception = Assert.Throws<Exception>(()=>Json.Defrost<ObjectWithoutAnInterface>(Quote("{'A':'Hello, world. Call me Ishmael.', 'B'")));

            Console.WriteLine(exception.Message);
            Assert.That(exception.Message, Is.EqualTo("Reached end of input unexpectedly; {\"A\":\"Hello, wor…e Ishmael.\", \"B\u035f\""));
        }
        
        
        [Test]
        [TestCase(1)]
        [TestCase(12)]
        [TestCase(31)]
        [TestCase(32)]
        [TestCase(33)]
        [TestCase(41)]
        [TestCase(42)]
        [TestCase(51)]
        [TestCase(100)]
        public void long_truncated_json_string_gives_a_useful_error(int length)
        {
            var src = Quote("{'A':'Hello, world. Call me Ishmael.', 'B':'But I must explain to you how all this mistaken idea of denouncing pleasure and praising pain was born and I will give you a complete account of the system, and expound the actual teachings of the great explorer of the truth, the master-builder of human happiness. No one rejects, dislikes, or avoids pleasure itself, because it is pleasure, but because those who do not know how to pursue pleasure rationally encounter consequences that are extremely painful. Nor again is there anyone who loves or pursues or desires to obtain pain of itself, because it is pain, but because occasionally circumstances occur in which toil and pain can procure him some great pleasure.'}");
            var chunk = src.Substring(0, length);
            var exception = Assert.Throws<Exception>(()=>Json.Defrost<ObjectWithoutAnInterface>(chunk));

            Console.WriteLine(exception.Message);
            Assert.That(exception.Message, Contains.Substring("Reached end of input unexpectedly;").Or.Contains("Unexpectedly reached end of string value;"));
            Assert.Pass(exception.Message);
        }

        [Test]
        public void a_string_that_is_not_valid_json_gives_a_useful_error()
        {
            var exception = Assert.Throws<Exception>(()=>Json.Defrost<ObjectWithoutAnInterface>("Failed: This is an error message, not a \"JSON\" string!"));
			
            Console.WriteLine(exception.Message);
            Assert.That(exception.Message, Is.EqualTo("Could not find token at index 0; Got 'F' (0x46); \u035fFailed: This is an error message"));
        }
        
        [Test]
        public void a_short_string_that_is_not_valid_json_gives_a_useful_error()
        {
            var exception = Assert.Throws<Exception>(()=>Json.Defrost<ObjectWithoutAnInterface>("Failed"));
			
            Console.WriteLine(exception.Message);
            Assert.That(exception.Message, Is.EqualTo("Could not find token at index 0; Got 'F' (0x46); \u035fFailed"));
        }
		
        private static string Quote(string src) => src.Replace('\'', '"');
    }
}