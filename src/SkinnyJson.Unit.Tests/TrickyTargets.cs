using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverUpdated.Global

#pragma warning disable CS8602

namespace SkinnyJson.Unit.Tests;

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

    [Test, Description("One-way recovery of named fields to a static value")]
    public void static_readonly_fields_as_name_conversions()
    {
        const string source =
            """
            {"id":5,"purpose":"HouseholdMeter"}
            """;
        var result = Json.Defrost<StaticReadonlyContainer>(source);

        Assert.That(result.Id, Is.EqualTo(5));
        Assert.That(result.Purpose.DisplayName, Is.EqualTo("Household Meter"));
    }

    [Test, Description("Creating types that have no empty constructor")]
    public void private_non_empty_constructors()
    {
        const string source =
            """
            {"id":5,"purpose":{"displayName":"Household Meter","isDispenser":true,"name":"HouseholdMeter","value":"HouseholdMeter"}}
            """;
        var result = Json.Defrost<StaticReadonlyContainer>(source);

        Assert.That(result.Id, Is.EqualTo(5));
        Assert.That(result.Purpose.DisplayName, Is.EqualTo("Household Meter"));
    }

    [Test]
    public void string_to_long_conversion()
    {
        const string dataString = """
                                  {"queryDate":"2026-07-09T09:40:49.2471877Z","logDate":"2026-07-09T09:40:49.168277","processorDate":"2026-07-09T09:40:43.418961","data":{"settings":[{"name":"MiFare block address","description":"MiFare block address 1..255 that will be used","access":"Protected","autoControl":true,"value":{"desiredValue":"10","desiredValueDate":"2025-03-06T10:13:00.080917","lastKnownValue":null,"lastKnownDate":null},"settingPosition":2,"byteLength":1,"settingKey":"MiFareBlockAddress","commandCorrelationId":"279f5e02-501d-40da-9576-fa0fe83c9e1a"}],"assetId":2077,"firmwareVersion":"SECMFD_LP_EWC2_5_TAP_MT_PRELOAD V2.22 19/08/24","firmwareType":"TapOnly"},"success":true,"errorMessage":null}
                                  """;
        var result = Json.Defrost<ResponseWithAge<GetAssetSettingsResponse>>(dataString);

        Console.WriteLine(Json.Freeze(result));
        Assert.That(result, Is.Not.Null);
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
    public const string IMEI       = "IMEI";
    public const string AssetId    = "Asset";
    public const string LedgerId   = "Ledger";
    public const string eWaterApp  = "eWApp";
    public const string PulseApp   = "PulseApp";
    public const string EntityId   = "EntityId";
    public const string EntityType = "EntityType";
}

public class StaticReadonlyContainer
{
    public int Id { get; set; }
    public AssetPurpose? Purpose { get; set; }
}

public class AssetPurpose
{
    /// <summary>
    /// A dispenser installed in a public location for communal use
    /// </summary>
    public static readonly AssetPurpose CommunityTap = new(nameof(CommunityTap), "CommunityTap", isDispenser: true, displayName: "Community Tap");

    /// <summary>
    /// A water meter installed in an individual household
    /// </summary>
    public static readonly AssetPurpose HouseholdMeter = new(nameof(HouseholdMeter), "HouseholdMeter", isDispenser: true, displayName: "Household Meter");

    /// <summary>
    /// A dispenser installed at an institution such as a school or hospital
    /// </summary>
    public static readonly AssetPurpose InstitutionTap = new(nameof(InstitutionTap), "InstitutionTap", isDispenser: true, displayName: "Institution Tap");

    /// <summary>
    /// A dispenser installed as part of a kiosk installation
    /// </summary>
    public static readonly AssetPurpose KioskTap = new(nameof(KioskTap), "KioskTap", isDispenser: true, displayName: "Kiosk Tap");

    /// <summary>
    /// An eSENSE installation, controlling one or more sensors (tank height, flow meters)
    /// </summary>
    /// // ReSharper disable once InconsistentNaming
    public static readonly AssetPurpose eSense = new(nameof(eSense), "eSense", isDispenser: false, displayName: "eSENSE");

    /// <summary>
    /// An asset used for registration only: initialses tags and provides a collection point for initial top-ups.
    /// These devices have no plumbing attached and cannot dispense water. They are essentially a brain in a box.
    /// </summary>
    public static readonly AssetPurpose RegistrationUnit = new(nameof(RegistrationUnit), "RegistrationUnit", isDispenser: false, displayName: "Registration Unit");

    public string Name { get; }
    public string Value { get; }

    /// <summary>
    /// Display name for the purpose
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Defines whether assets of this purpose are intended to dispense water to end users
    /// </summary>
    public bool IsDispenser { get; }


    private AssetPurpose(string name, string value, string displayName, bool isDispenser)
    {
        Name = name;
        Value = value;
        DisplayName = displayName;
        IsDispenser = isDispenser;
    }
}


/// <summary>
/// Response object containing all the settings for this EWC.
/// Includes last known, and last desired/commanded values.
/// <p/>
/// Note that if this EWC is not under DataWaterfall control,
/// the last desired values will be empty.
/// </summary>
public class GetAssetSettingsResponse
{
    /// <summary>
    /// All known settings that apply to this EWC, with values where known.
    /// </summary>
    public IEnumerable<EwcSettingStateResponse> Settings { get; set; } = Array.Empty<EwcSettingStateResponse>();

    /// <summary>
    /// Asset ID matching the request
    /// </summary>
    public int AssetId { get; set; }

    /// <summary>
    /// Last known firmware version from the EWC.
    /// If this is null, the firmware is not known, and <see cref="FirmwareType"/> will be a guess.
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Type of firmware running on the EWC.
    /// This affects the memory map.
    /// </summary>
    public string FirmwareType { get; set; } = "";
}

/// <summary>
/// Information about an EWC setting, with values where known
/// </summary>
public class EwcSettingStateResponse
{
    /// <summary>
    /// Name of the setting
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Description of setting
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Protection and access for the setting
    /// </summary>
    public string Access { get; set; } = "";

    /// <summary>
    /// If <c>true</c>, this EWC setting is controlled automatically,
    /// and the user should not be given controls to change it manually.
    /// If <c>false</c>, manual changes may be allowed.
    /// </summary>
    public bool AutoControl { get; set; }

    /// <summary>
    /// Represents the last known and desired state of the setting
    /// </summary>
    public EwcMemorySetting<long?> Value { get; set; } = new ();

    /// <summary>
    /// EEPROM Memory offset of this setting.
    /// </summary>
    public int SettingPosition { get; set; }

    /// <summary>
    /// EEPROM Memory size of this setting
    /// </summary>
    public int ByteLength { get; set; }

    /// <summary>
    /// Key for this setting, used to request value changes
    /// </summary>
    public string SettingKey { get; set; }="";

    /// <summary>
    /// Correlation ID for the most recent command trying to send a
    /// value for this setting to the EWC.
    /// <c>null</c> if no such command.
    /// </summary>
    public Guid? CommandCorrelationId { get; set; }
}

/// <summary>
/// Represents the last known and desired state of a setting as stored in an EWC's EEPROM memory.
/// This setting may bridge multiple bytes of memory.
/// </summary>
public class EwcMemorySetting<T>
{
    /// <summary>
    /// Last value that was commanded. Null if no commands issued
    /// </summary>
    public T? DesiredValue { get; set; }

    /// <summary>
    /// Date and time the last command was issued that affected this byte
    /// </summary>
    public DateTime? DesiredValueDate { get; set; }

    /// <summary>
    /// Most recent value returned by the EWC. Null if not known
    /// </summary>
    public T? LastKnownValue { get; set; }

    /// <summary>
    /// Last date and time the value was given by the EWC. Null if not known
    /// </summary>
    public DateTime? LastKnownDate { get; set; }
}