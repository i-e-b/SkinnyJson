using System;
using NUnit.Framework;

#pragma warning disable CS8602

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class TrickyTargets
    {
        [Test]
        public void round_trip_with_read_only_properties()
        {
            var original = new ReadOnlyProperties(123, "Hello world");
            var frozen   = Json.Freeze(original);
            Console.WriteLine(frozen);
            var defrosted = Json.Defrost<ReadOnlyProperties>(frozen);

            Assert.That(defrosted.Id, Is.EqualTo(original.Id));
            Assert.That(defrosted.Name, Is.EqualTo(original.Name));
        }
    }

    public class ReadOnlyProperties
    {
        public int Id { get; }

        public string Name { get; }

        public ReadOnlyProperties(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}