using System;
using System.Runtime.Serialization;
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


        [Test]
        public void reading_properties_by_data_member_name()
        {
            var defrosted = Json.Defrost<LogLineSources>("{\"Asset\": \"2138\"}");

            Assert.That(defrosted.AssetId, Is.EqualTo("2138"));
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


    public class LogLineSources
    {
        [DataMember(Name=KnownSources.AssetId)]
        public string? AssetId { get; set; }

        [DataMember(Name=KnownSources.EntityId)]
        public string? EntityId { get; set; }

        [DataMember(Name=KnownSources.EntityType)]
        public string? EntityType { get; set; }

        [DataMember(Name=KnownSources.IMEI)]
        public string? Imei { get; set; }

        [DataMember(Name=KnownSources.LedgerId)]
        public string? LedgerId { get; set; }

        [DataMember(Name=KnownSources.eWaterApp)]
        public string? WaterAppInstanceId { get; set; }

        [DataMember(Name=KnownSources.PulseApp)]
        public string? PulseAppInstanceId { get; set; }

        public bool AnySet()
        {
            // This ignores AssetId deliberately
            return Imei is not null
                || LedgerId is not null
                || WaterAppInstanceId is not null
                || PulseAppInstanceId is not null;
        }
    }

public static class KnownSources
{
    public const string IMEI = "IMEI";
    public const string AssetId = "Asset";
    public const string LedgerId = "Ledger";
    public const string eWaterApp = "eWApp";
    public const string PulseApp = "PulseApp";
    public const string EntityId = "EntityId";
    public const string EntityType = "EntityType";
}
}