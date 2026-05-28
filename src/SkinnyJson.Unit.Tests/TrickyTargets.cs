using System;
using System.Collections.Generic;
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

        [Test]
        public void wide_number_to_string()
        {
            var defrosted = Json.Defrost<WideNumberConfusionType>("{\"BigNumber\": 2305843009213693952}");

            Assert.That(defrosted.BigNumber, Is.EqualTo("2305843009213693952"));
        }

        [Test]
        public void strings_to_numbers()
        {
            var defrosted = Json.Defrost<WideNumberConfusionType>("{\"DoubleType\": \"1000.01\", \"DecimalType\": \"1000.01\", \"IntegerType\": \"1000.01\"}");

            Assert.That(defrosted.DoubleType, Is.EqualTo(1000.01), "double parsed incorrectly");
            Assert.That(defrosted.DecimalType, Is.EqualTo(1000.01m), "decimal parsed incorrectly");
            Assert.That(defrosted.IntegerType, Is.EqualTo(1000), "integer parsed incorrectly");
        }

        [Test]
        public void handle_obvious_infinite_loops()
        {
            var a = new BidirectionalType { Name = "A" };
            var b = new BidirectionalType { Name = "B" };
            var c = new BidirectionalType { Name = "C" };
            var d = new BidirectionalType { Name = "D" };

            a.Child = b;
            b.Child = c;
            c.Child = d;

            d.Parent = c;
            c.Parent = b;
            b.Parent = a;

            var result = Json.Freeze(a);
            Console.WriteLine(result);
        }

        [Test]
        public void string_to_int_keys()
        {
            const string input =
                """
                {
                    "settingMap": {
                      "2": 10,
                      "3": 5,
                      "4": 255,
                      "5": 1,
                      "6": 3,
                      "7": 9,
                      "8": 7,
                      "9": 239,
                      "10": 242,
                      "11": 109,
                      "12": 83
                    }
                }
                """;


            var result = Json.Defrost<IntKeyMapType>(input);

            Assert.That(result.SettingMap.Count, Is.EqualTo(11), "Should have correct entry count");

            Assert.That(result.SettingMap[2], Is.EqualTo(10));
            Assert.That(result.SettingMap[3], Is.EqualTo(5));
            Assert.That(result.SettingMap[4], Is.EqualTo(255));
            Assert.That(result.SettingMap[5], Is.EqualTo(1));
            Assert.That(result.SettingMap[6], Is.EqualTo(3));
            Assert.That(result.SettingMap[7], Is.EqualTo(9));
            Assert.That(result.SettingMap[8], Is.EqualTo(7));
            Assert.That(result.SettingMap[9], Is.EqualTo(239));
            Assert.That(result.SettingMap[10], Is.EqualTo(242));
            Assert.That(result.SettingMap[11], Is.EqualTo(109));
            Assert.That(result.SettingMap[12], Is.EqualTo(83));
        }

        [Test]
        public void string_to_int_keys_genericised()
        {
            const string input =
                """
                {
                    "settingMap": [
                      {"k":2, "v":10},
                      {"k":3, "v":5},
                      {"k":4, "v":255},
                      {"k":5, "v":1},
                      {"k":6, "v":3},
                      {"k":7, "v":9},
                      {"k":8, "v":7},
                      {"k":9, "v":239},
                      {"k":10, "v":242},
                      {"k":11, "v":109},
                      {"k":12, "v":83}
                    ]
                }
                """;


            var result = Json.Defrost<IntKeyMapType>(input);

            Assert.That(result.SettingMap.Count, Is.EqualTo(11), "Should have correct entry count");

            Assert.That(result.SettingMap[2], Is.EqualTo(10));
            Assert.That(result.SettingMap[3], Is.EqualTo(5));
            Assert.That(result.SettingMap[4], Is.EqualTo(255));
            Assert.That(result.SettingMap[5], Is.EqualTo(1));
            Assert.That(result.SettingMap[6], Is.EqualTo(3));
            Assert.That(result.SettingMap[7], Is.EqualTo(9));
            Assert.That(result.SettingMap[8], Is.EqualTo(7));
            Assert.That(result.SettingMap[9], Is.EqualTo(239));
            Assert.That(result.SettingMap[10], Is.EqualTo(242));
            Assert.That(result.SettingMap[11], Is.EqualTo(109));
            Assert.That(result.SettingMap[12], Is.EqualTo(83));
        }

        [Test]
        public void int_key_dictionary_serialising()
        {
            var input = new IntKeyMapType();
            input.SettingMap.Add(1, 2);
            input.SettingMap.Add(3, 4);
            input.SettingMap.Add(5, 6);
            input.SettingMap.Add(7, 8);

            var result = Json.Freeze(input);

            Console.WriteLine(result);

            Assert.That(result, Is.EqualTo("{\"SettingMap\":{\"1\":2,\"3\":4,\"5\":6,\"7\":8}}"));
        }

        [Test]
        public void json_string_values_cascade_into_subtypes()
        {
            const string src =
                """
                {
                  "ResultId": 2,
                  "Time": "2026-04-30T10:19:14Z",
                  "DeviceType": "Beam",
                  "TesterName": "Iain Ballard",
                  "BatchId": "002",
                  "DeviceSerial": "BEAM_V-1-5-1__5qelm_0026",
                  "OverallResult": "pass",
                  "Data": "{\"beam-ble\":\"pass\",\"beam-rs232\":\"pass\",\"beam-nvs\":\"pass\",\"imei\":\"869595067002518\",\"beam-modem\":\"pass\",\"beam-sim\":\"pass\",\"beam-phone-home\":\"pass\",\"beam-sdcard\":\"pass\"}"
                }
                """;

            var result = Json.Defrost<TestResult>(src);

            var frozen = Json.Freeze(result);
            Console.WriteLine(frozen);

            Assert.That(result.ResultId, Is.EqualTo(2));
            Assert.That(result.Time.ToString("yyyy-MM-ddTHH:mm:ss"), Is.EqualTo("2026-04-30T10:19:14"));
            Assert.That(result.DeviceType, Is.EqualTo("Beam"));
            Assert.That(result.TesterName, Is.EqualTo("Iain Ballard"));
            Assert.That(result.BatchId, Is.EqualTo("002"));
            Assert.That(result.DeviceSerial, Is.EqualTo("BEAM_V-1-5-1__5qelm_0026"));
            Assert.That(result.OverallResult, Is.EqualTo("pass"));

            Assert.That(result.Data["beam-ble"], Is.EqualTo("pass"));
            Assert.That(result.Data["beam-rs232"], Is.EqualTo("pass"));
            Assert.That(result.Data["beam-nvs"], Is.EqualTo("pass"));
            Assert.That(result.Data["imei"], Is.EqualTo("869595067002518"));
        }
    }

    public class TestResult
    {
        public int ResultId { get; set; }
        public DateTime Time { get; set; }
        public string TesterName { get; set; } = "";
        public string BatchId { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public string DeviceSerial { get; set; } = "";
        public string OverallResult { get; set; } = "unknown";

        public Dictionary<string, string> Data { get; set; } = [];
    }

    public class IntKeyMapType
    {
        // ReSharper disable once CollectionNeverUpdated.Global
        public Dictionary<int, byte> SettingMap { get; set; } = [];
    }

    public class BidirectionalType
    {
        public BidirectionalType? Parent { get; set; }
        public BidirectionalType? Child { get; set; }

        public string Name { get; set; } = "";
    }

    public class WideNumberConfusionType
    {
        public string BigNumber { get; set; } = "";
        public double DoubleType { get; set; }
        public decimal DecimalType { get; set; }
        public long IntegerType { get; set; }
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