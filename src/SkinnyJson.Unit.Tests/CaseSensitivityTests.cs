using NUnit.Framework;

namespace SkinnyJson.Unit.Tests
{
#pragma warning disable CS8602
    [TestFixture]
    public class CaseSensitivityTests
    {
        [Test]
        public void with_ignore_case_off_properties_must_exactly_match()
        {
            // Only one of these matches
            Json.DefaultParameters.IgnoreCaseOnDeserialize = false;
            var defrosted = Json.Defrost<CasedTypes>("{\"word gap\":\"OK\", \"LOWER_CASE\":\"OK\", \"uppercase\":\"OK\", \"pascal_case\":\"OK\", \"camelCase\":\"OK\", \"SnakeCase\":\"OK\"}");
            
            Assert.That(defrosted.PascalCase, Is.Null, "snake to pascal copied, but should not have");
            Assert.That(defrosted.camelCase, Is.EqualTo("OK"), "camel to camel should have copied, but did not");
            Assert.That(defrosted.snake_case, Is.Null, "pascal to snake copied, but should not have");
            Assert.That(defrosted.UPPER_CASE, Is.Null, "lower to upper copied, but should not have");
            Assert.That(defrosted.lowercase, Is.Null, "upper to lower copied, but should not have");
            Assert.That(defrosted.WordGap, Is.Null, "word gap copied, but should not have");
            
            defrosted = Json.Defrost<CasedTypes>("{\"Word Gap\":\"OK\", \"UPPER_CASE\":\"OK\", \"PASCALCASE\":\"OK\", \"camelcase\":\"OK\", \"lowerCase\":\"OK\", \"SNAKE_case\":\"OK\"}");
            
            Assert.That(defrosted.PascalCase, Is.Null, "pascal copied, but should not have");
            Assert.That(defrosted.camelCase, Is.Null, "camel copied, but should not have");
            Assert.That(defrosted.snake_case, Is.Null, "snake copied, but should not have");
            Assert.That(defrosted.UPPER_CASE, Is.EqualTo("OK"), "upper should have copied, but did not");
            Assert.That(defrosted.lowercase, Is.Null, "lower copied, but should not have");
            Assert.That(defrosted.WordGap, Is.Null, "word gap copied, but should not have");
        }

        [Test]
        public void with_ignore_case_on_property_need_just_the_same_letters_and_numbers()
        {
            // everything should have a match
            Json.DefaultParameters.IgnoreCaseOnDeserialize = true;
            var defrosted = Json.Defrost<CasedTypes>("{\"LOWER_CASE\":\"1\", \"uppercase\":\"2\", \"pascal_case\":\"3\", \"camelCase\":\"4\", \"SnakeCase\":\"5\"}");
            
            Assert.That(defrosted.lowercase, Is.EqualTo("1"));
            Assert.That(defrosted.UPPER_CASE, Is.EqualTo("2"));
            Assert.That(defrosted.PascalCase, Is.EqualTo("3"));
            Assert.That(defrosted.camelCase, Is.EqualTo("4"));
            Assert.That(defrosted.snake_case, Is.EqualTo("5"));
            
            defrosted = Json.Defrost<CasedTypes>("{\"UPPER-CASE\":\"1\", \"1 is a number\":\"2\", \"camel-case\":\"3\", \"lowerCase\":\"4\", \"SNAKE_case\":\"5\"}");
            
            Assert.That(defrosted.UPPER_CASE, Is.EqualTo("1"));
            Assert.That(defrosted._1_IsANumber, Is.EqualTo("2"));
            Assert.That(defrosted.camelCase, Is.EqualTo("3"));
            Assert.That(defrosted.lowercase, Is.EqualTo("4"));
            Assert.That(defrosted.snake_case, Is.EqualTo("5"));
        }

        public class CasedTypes
        {
            public string? _1_IsANumber { get; set; }
            public string? UPPER_CASE { get; set; }
            public string? PascalCase { get; set; }
            public string? camelCase { get; set; }
            public string? lowercase { get; set; }
            public string? snake_case { get; set; }
            public string? WordGap { get; set; }
        }
    }
}