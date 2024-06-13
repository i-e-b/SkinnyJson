using System.Collections.Generic;
using NUnit.Framework;

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class CloningTests {

        
        [Test]
        public void clone_has_same_property_values_as_original (){
            var original = ComplexTypes.DictionaryOfDictionary();

            var clone = Json.Clone(original);

            Assert.That(clone.Count, Is.EqualTo(original.Count), "Item count incorrect");
            Assert.That(clone["A"]["X"], Is.EqualTo(original["A"]["X"]));
            Assert.That(clone["B"]["1"], Is.EqualTo(original["B"]["1"]));
        }

        [Test]
        public void clone_has_same_property_values_as_original_hard (){
            var original = ComplexTypes.DictionaryOfDictionaryOfTupleWithList();

            var clone = Json.Clone(original);

            Assert.That(clone["Hello"]["Bob"].Item2, Is.EqualTo(original["Hello"]["Bob"].Item2));
            Assert.That(clone["World"]["Sam"].Item3[0], Is.EqualTo(original["World"]["Sam"].Item3[0]));
        }
        
        [Test]
        public void modifying_the_original_does_not_affect_the_clone (){
            var original = ComplexTypes.DictionaryOfDictionary();

            var clone = Json.Clone(original);

            original.Add("J", new Dictionary<string, string>());

            Assert.That(clone.Count, Is.Not.EqualTo(original.Count), "Item count incorrect");
            //Assert.That(clone.ContainsKey("J"), Is.False);
            //Assert.That(original.ContainsKey("J"), Is.True);
        }
        
        [Test]
        public void modifying_the_clone_does_not_affect_the_original() {
            var original = ComplexTypes.DictionaryOfDictionary();

            var clone = Json.Clone(original);

            clone.Remove("A");

            Assert.That(clone.Count, Is.Not.EqualTo(original.Count), "Item count incorrect");
            Assert.That(clone.ContainsKey("A"), Is.False);
            Assert.That(original.ContainsKey("A"), Is.True);
        }
    }
}