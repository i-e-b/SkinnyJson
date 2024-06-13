using System; // ReSharper disable InconsistentNaming
using NUnit.Framework;

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class ClarityTests
    {
        [Test]
        public void a_dictionary_of_dictionaries_should_freeze_to_a_tree_of_object_literals()
        {
            var settings = new JsonParameters { 
                EnableAnonymousTypes = true,
                IgnoreCaseOnDeserialize = false
            };

            var original = ComplexTypes.DictionaryOfDictionary();
            var frozen = Json.Freeze(original, settings);
            Console.WriteLine(frozen);
            var defrosted = Json.Defrost(frozen, settings);

            Assert.That(defrosted, Is.EqualTo(original).AsCollection);
        }

        [Test]
        public void complex_type_dictionaries_have_good_json_representations()
        {
            var original = ComplexTypes.DictionaryOfDictionaryOfTupleWithList();
            var frozen = Json.Freeze(original);
            Console.WriteLine(frozen);

            Assert.That(frozen, Is.EqualTo("{\"Hello\":{\"Bob\":{\"Item1\":1,\"Item2\":2,\"Item3\":[1,2,3]}},\"World\":{\"Sam\":{\"Item1\":3,\"Item2\":4,\"Item3\":[10,20,30]}}}"));
        }

         
    }
}