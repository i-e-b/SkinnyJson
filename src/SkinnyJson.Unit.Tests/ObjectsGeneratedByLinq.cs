namespace SkinnyJson.Unit.Tests
{
    using System;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class ObjectsGeneratedByLinq
    {

        [Test]
        public void can_freeze_anonymous_type()
        {
            var obj = new { One = "one", Two = "two" };
            var result = Json.Freeze(obj);

            Console.WriteLine(result);
            Assert.That(result, Is.EqualTo(fix("{'One':'one','Two':'two'}")));
        }

        [Test]
        public void can_generate_an_object_using_linq_functions_and_serialise_it()
        {
            var gen = new []{"one","two","three"};
            var obj = gen.Select(n =>
                new
                {
                    Name = n,
                    Kind = "a thing"
                }
                ).ToArray();

            var result = Json.Freeze(obj);
            Assert.That(result, Is.EqualTo(fix("[{'Name':'one','Kind':'a thing'},{'Name':'two','Kind':'a thing'},{'Name':'three','Kind':'a thing'}]")));
        }

        [Test]
        public void can_stack_anonymous_types_inside_each_other()
        {
            var gen = new[] { "one", "two", "three" };
            var obj = new
            {
                Static = "no",
                Dynamic = gen.Select(n => new { Name = n, Kind = gen } ).ToArray()
            };

            var result = Json.Freeze(obj);
            Console.WriteLine(result);
            Assert.That(result, Is.EqualTo(fix("{'Static':'no','Dynamic':[{'Name':'one','Kind':['one','two','three']},{'Name':'two','Kind':['one','two','three']},{'Name':'three','Kind':['one','two','three']}]}")));
        }

        [Test]
        public void can_serialise_generator_functions()
        {
            var gen = new[] { "one", "two", "three" };
            var obj = new
            {
                Dynamic = gen.Select(n => new { Name = n, Kind = gen })
            };
            var result = Json.Freeze(obj);
            Console.WriteLine(result);
            Assert.That(result, Is.EqualTo(fix("{'Dynamic':[{'Name':'one','Kind':['one','two','three']},{'Name':'two','Kind':['one','two','three']},{'Name':'three','Kind':['one','two','three']}]}")));
        }

        static string fix(string s) { return s.Replace("'", "\""); }
    }
}