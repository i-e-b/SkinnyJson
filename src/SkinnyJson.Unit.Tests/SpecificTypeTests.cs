using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SkinnyJson.Unit.Tests.ExampleData;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8602

namespace SkinnyJson.Unit.Tests
{
    /// <summary>
    /// Tests for real-world applications and bugs
    /// </summary>
    [TestFixture]
    public class SpecificTypeTests
    {
        [Test]
        public void Timespan_from_all_properties()
        {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>(
                "{\"timeSpan\":" +
                "{\"Ticks\":100000000,\"Days\":0,\"Hours\":0,\"Milliseconds\":0,\"Minutes\":0,\"Seconds\":10," +
                "\"TotalDays\":0.00011574074074074075,\"TotalHours\":0.002777777777777778,\"TotalMilliseconds\":10000,\"TotalMinutes\":0.16666666666666666,\"TotalSeconds\":10}}");

            Assert.That(defrosted.timeSpan.Ticks, Is.EqualTo(100000000), "TimeSpan was not interpreted correctly");
        }

        [Test]
        public void Timespan_from_basic_properties()
        {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>(
                "{\"timeSpan\":" +
                "{\"Ticks\":100000000,\"Days\":0,\"Hours\":0,\"Milliseconds\":0,\"Minutes\":0,\"Seconds\":10}}");

            Assert.That(defrosted.timeSpan.Ticks, Is.EqualTo(100000000), "TimeSpan was not interpreted correctly");
        }

        [Test]
        public void Timespan_from_just_ticks()
        {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"timeSpan\":{\"Ticks\":100000000}}");

            Assert.That(defrosted.timeSpan.Ticks, Is.EqualTo(100000000), "TimeSpan was not interpreted correctly");
        }

        [Test]
        public void Timespan_serialises_to_compact_string()
        {
            Json.DefaultParameters.EnableAnonymousTypes = true;
            var frozen = Json.Freeze(new TimespanContainer { Timespan = TimeSpan.FromMinutes(101.101) });

            Assert.That(frozen, Is.EqualTo("{\"Timespan\":\"01:41:06.0600000\"}"));
        }

        [Test]
        public void Timespan_deserialised_from_compact_string()
        {
            var defrosted = Json.Defrost<TimespanContainer>("{\"Timespan\":\"01:41:06.0600000\"}");
            Assert.That(defrosted.Timespan.Ticks, Is.EqualTo(60660600000), "TimeSpan was not interpreted correctly");

            defrosted = Json.Defrost<TimespanContainer>("{\"Timespan\":\"01:41:05\"}");
            Assert.That(defrosted.Timespan.Ticks, Is.EqualTo(60650000000), "TimeSpan was not interpreted correctly");
        }

        [Test]
        public void Can_map_bool_strings_to_bool_fields_and_properties()
        {
            var defrosted = Json.Defrost<ILikeBools>("{\"val1\":\"true\",\"val2\":\"false\",\"val3\":true,\"val4\":false}");

            Assert.That(defrosted.val1, Is.True, "'true' string was not correctly interpreted");
            Assert.That(defrosted.val2, Is.False, "'false' string was not correctly interpreted");
            Assert.That(defrosted.val3, Is.True, "'true' bool was not correctly interpreted");
            Assert.That(defrosted.val4, Is.False, "'false' bool was not correctly interpreted");
        }

        [Test]
        public void Can_chain_well_known_containers_and_unknown_interface_types()
        {
            // See Json.cs::TryMakeStandardContainer() if your container type isn't supported
            var d = Json.Defrost<ContainerOfEnumerators>(Quote("{'Prop1':[{'val1':'true','val2':'false','val3':true,'val4':false}], 'Prop2':['Hello', 'world']}"));

            Assert.That(d.Prop1, Is.Not.Null, "First enumerable was null");
            var l1 = d.Prop1.ToList();
            Assert.That(l1.Count, Is.EqualTo(1), "First enumerable was wrong length");

            Assert.That(d.Prop2, Is.Not.Null, "Second enumerable was null");
            var l2 = d.Prop2.ToList();
            Assert.That(l2.Count, Is.EqualTo(2), "Second enumerable was wrong length");
        }

        [Test]
        public void warnings_if_property_case_doesnt_match()
        {
            var input = @"{
  ""success"": false,
  ""errorMessage"": ""Failed to publish command message to queue""
}";
            
            Json.DefaultParameters.IgnoreCaseOnDeserialize = true;
            var output = Json.Defrost<BaseResponse>(input);
            
            Assert.That(output.Success, Is.False);
            Assert.That(output.ErrorMessage, Is.EqualTo("Failed to publish command message to queue"));
            
            Json.DefaultParameters.IgnoreCaseOnDeserialize = false;
            var exception = Assert.Throws<Exception>(()=>Json.Defrost<BaseResponse>(input));
            
            Assert.That(exception.Message, Contains.Substring("Properties would match if IgnoreCaseOnDeserialize was set to true"));
        }

        [Test]
        public void complex_rabbitmq_api_example()
        {
            var text = File.ReadAllText("ExampleData/RabbitMq.txt");
            
            Json.DefaultParameters.EnableAnonymousTypes = true;
            Json.DefaultParameters.IgnoreCaseOnDeserialize = true;
            
            Json.DefaultParameters.StrictMatching = true;
            Assert.Throws<Exception>(()=>Json.Defrost<List<RabbitMqStatistic>>(text));
            
            Json.DefaultParameters.StrictMatching = false;
            var objects = Json.Defrost<List<RabbitMqStatistic>>(text);
            
            Json.DefaultParameters.StrictMatching = true;
            
            Assert.That(objects, Is.Not.Null);
            Assert.That(objects[0].Name, Is.Not.Null);
        }

        [Test]
        public void byte_array_should_fall_back_to_hex_string_if_input_is_not_base64()
        {
            var resultBase64 = Json.Defrost<ByteArrayContainer>(Quote("{'Bytes':'Y29udmVydA=='}"));
            var resultHexStr = Json.Defrost<ByteArrayContainer>(Quote("{'Bytes':'636F6E76657274'}"));

            Assert.That(Encoding.UTF8.GetString(resultBase64.Bytes), Is.EqualTo("convert"), "base64 should be converted");
            Assert.That(Encoding.UTF8.GetString(resultHexStr.Bytes), Is.EqualTo("convert"), "hex string should be converted");
        }

        [Test]
        public void decimal_type_can_carry_large_values()
        {
            var original = new LargeNumericValues
            {
                NullableMoney = null,
                DecMoney = 9414880748822855695599764289m, // too much precision for 64 bit types
                SignedLong = 941488074882285569L, // loses value when cast to double
                UnsignedLong = 0xFFFFFFFFFFFFFFFEUL // would go negative if cast to signed long (as -2)
            };
            
            var frozen = Json.Freeze(original);
            Console.WriteLine(frozen);
            
            var defrosted = Json.Defrost<LargeNumericValues>(frozen);

            Assert.That(defrosted.DecMoney, Is.EqualTo(original.DecMoney), "decimal type lost precision");
            Assert.That(defrosted.SignedLong, Is.EqualTo(original.SignedLong), "long type lost precision");
            Assert.That(defrosted.UnsignedLong, Is.EqualTo(original.UnsignedLong), "ulong type lost precision");
        }

        [Test]
        public void testing_api_deserialising()
        {
            var reply = Quote(@"{
  'queryDate': '2024-02-27T14:38:15.1288665Z',
  'logDate': '2024-02-27T14:38:14',
  'processorDate': '2024-02-27T14:38:11',
  'data': {
    'tamperSwitchState': {
      'value': 1,
      'date': '2024-02-27T14:34:31'
    },
    'batteryAdcReading': {
      'value': 226,
      'date': '2024-02-27T14:34:31'
    },
    'pressureSensorReading': {
      'value': 0,
      'date': '2024-02-27T14:34:31'
    },
    'sensor1': {
      'value': 126,
      'date': '2024-02-27T14:34:31'
    },
    'sensor2': {
      'value': 0,
      'date': '2024-02-27T14:34:31'
    },
    'sensor3': {
      'value': 0,
      'date': '2024-02-27T14:34:31'
    },
    'healthFlags': {
      'value': 137,
      'date': '2024-02-27T14:34:31'
    },
    'flowSampleTimeMs': {
      'value': 1020,
      'date': '2024-02-27T14:28:56'
    },
    'flowSampleTicks': {
      'value': 0,
      'date': '2024-02-27T14:28:56'
    }
  }
}");
            
            Json.DefaultParameters.IgnoreCaseOnDeserialize = true;
            var defrosted = Json.Defrost<ResponseWithAge<EwcStatusValues>>(reply);
            
            Assert.That(defrosted, Is.Not.Null);
            
            Console.WriteLine(Json.Beautify(Json.Freeze(defrosted)));
        }

        [Test]
        public void slot_deserialising()
        {
            var reply = Quote("[{'assetId':1020,'supertapSlot':1,'commandCorrelationId':null,'commandPhase':8,'desiredTagId':1800556754,'desiredCreditMits':0,'commandDate':null,'lastKnownTagId':null,'lastKnownCreditMits':null,'lastKnownDate':'2024-04-19T03:54:45','lastKnownValueComplete':false},{'assetId':1020,'supertapSlot':2,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':0,'desiredCreditMits':0,'commandDate':'2023-12-19T05:24:02','lastKnownTagId':4086724336,'lastKnownCreditMits':293333,'lastKnownDate':'2024-04-18T23:12:37','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':3,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':0,'desiredCreditMits':0,'commandDate':'2024-02-21T12:27:37','lastKnownTagId':1267026642,'lastKnownCreditMits':703999,'lastKnownDate':'2024-04-19T10:26:34','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':4,'commandCorrelationId':null,'commandPhase':8,'desiredTagId':730745554,'desiredCreditMits':0,'commandDate':null,'lastKnownTagId':null,'lastKnownCreditMits':null,'lastKnownDate':'2024-04-19T11:03:48','lastKnownValueComplete':false},{'assetId':1020,'supertapSlot':5,'commandCorrelationId':null,'commandPhase':8,'desiredTagId':3687535058,'desiredCreditMits':0,'commandDate':null,'lastKnownTagId':null,'lastKnownCreditMits':null,'lastKnownDate':'2024-04-19T04:09:45','lastKnownValueComplete':false},{'assetId':1020,'supertapSlot':6,'commandCorrelationId':null,'commandPhase':8,'desiredTagId':3005571824,'desiredCreditMits':0,'commandDate':null,'lastKnownTagId':null,'lastKnownCreditMits':null,'lastKnownDate':'2024-04-18T12:05:40','lastKnownValueComplete':false},{'assetId':1020,'supertapSlot':7,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':0,'desiredCreditMits':0,'commandDate':'2024-02-24T07:39:22','lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-18T14:33:01','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':8,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':0,'desiredCreditMits':0,'commandDate':'2024-02-05T09:34:29','lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T01:42:47','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':9,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':0,'desiredCreditMits':0,'commandDate':'2023-11-15T13:19:54','lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T07:10:02','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':10,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':0,'desiredCreditMits':0,'commandDate':'2023-11-15T09:21:21','lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T01:16:47','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':11,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':0,'desiredCreditMits':0,'commandDate':'2023-11-19T11:17:41','lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T07:14:03','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':12,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T07:14:21','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':13,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T07:14:40','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':14,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T07:10:20','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':15,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T07:10:38','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':16,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T07:14:58','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':17,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T07:13:03','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':18,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T01:45:50','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':19,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T07:15:16','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':20,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-18T19:19:22','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':21,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T01:43:09','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':22,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-18T21:47:30','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':23,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-18T19:19:45','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':24,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T01:19:46','lastKnownValueComplete':true},{'assetId':1020,'supertapSlot':25,'commandCorrelationId':null,'commandPhase':0,'desiredTagId':null,'desiredCreditMits':null,'commandDate':null,'lastKnownTagId':0,'lastKnownCreditMits':0,'lastKnownDate':'2024-04-19T03:47:50','lastKnownValueComplete':true}]");
            
            
            Json.DefaultParameters.IgnoreCaseOnDeserialize = true;
            var defrosted = Json.Defrost<IEnumerable<EwcSupertapSlot>>(reply);
            
            Assert.That(defrosted, Is.Not.Null);
            
            Console.WriteLine(Json.Beautify(Json.Freeze(defrosted)));
        }

        private static string Quote(string src) => src.Replace('\'', '"');
    }

    public class LargeNumericValues
    {
        public decimal? NullableMoney { get; set; }
        
        public decimal DecMoney { get; set; }

        public ulong UnsignedLong { get; set; }

        public long SignedLong { get; set; }
    }

    public class ByteArrayContainer
    {
        public byte[] Bytes { get; set; }
    }

    public class ContainerOfEnumerators
    {
#pragma warning disable 8618
        public IEnumerable<ILikeBools> Prop1 { get; set; }
        public IEnumerable<string> Prop2 { get; set; }
#pragma warning restore 8618
    }

    // ReSharper disable once IdentifierTypo
    public interface ILikeBools {
        bool val1 { get; set; }
        bool val2 { get; set; }
        bool val3 { get; set; }
        bool val4 { get; set; }
    }
    public interface IHaveLotsOfTypes {
        DateTime date_time { get; set; }
        TimeSpan timeSpan { get; set; }
    }
    public class TimespanContainer
    {
        public TimeSpan Timespan { get; set; }
    }
    
    public class BaseResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }





/// <summary>
/// A response wrapper that holds information about the age and freshness of the data contained
/// </summary>
public class ResponseWithAge<T>
{
    /// <summary>
    /// Create an age wrapper around some data
    /// </summary>
    public ResponseWithAge(T data, DateTime queryDate, DateTime? processorDate, DateTime? logDate)
    {
        Data = data;
        QueryDate = queryDate;
        ProcessorDate = processorDate;
        LogDate = logDate;
    }
    
    /// <summary>
    /// Time that the query was made
    /// </summary>
    public DateTime QueryDate { get; set; }

    /// <summary>
    /// The received date of the most recent data in the log,
    /// at the time the query was made.
    /// </summary>
    /// <remarks>
    /// Use this to determine how much lag is in the log.
    /// We generally assume that messages are coming in
    /// at least one per second at all times.
    /// </remarks>
    public DateTime? LogDate { get; set; }

    /// <summary>
    /// The received date from the log,
    /// of the most recent processed data,
    /// at the time the query was made.
    /// </summary>
    /// <remarks>
    /// Use this to determine how 'stale' or 'fresh'
    /// the processed data is in comparison to the log
    /// </remarks>
    public DateTime? ProcessorDate { get; set; }

    /// <summary>
    /// Data from the API call
    /// </summary>
    public T Data { get; set; }
}

/// <summary>
/// Holds last known values of various settings
/// </summary>
public class EwcStatusValues
{
    /// <summary>
    /// Last known tamper switch state (from tamper event or health check event)
    /// </summary>
    public LastKnownValue<TamperSwitchState?> TamperSwitchState { get; set; } = new();

    /// <summary>
    /// Last known battery ADC reading (not voltage: for that, use <c>(BatteryAdcReading / 256.0) * 15.0</c>)
    /// </summary>
    public LastKnownValue<byte?> BatteryAdcReading { get; set; } = new();

    /// <summary>
    /// Last reading from pressure sensor. This is resistive, so zero indicates good pressure.
    /// The majority of taps do not have a sensor fitted, and the probe lines are shorted (giving a zero reading)
    /// </summary>
    public LastKnownValue<byte?> PressureSensorReading { get; set; } = new();
    
    /// <summary>
    /// Last reading from eWater Sense ADC 1, if fitted.
    /// </summary>
    public LastKnownValue<byte?> Sensor1 { get; set; } = new();
    
    /// <summary>
    /// Last reading from eWater Sense ADC 2, if fitted.
    /// </summary>
    public LastKnownValue<byte?> Sensor2 { get; set; } = new();
    
    /// <summary>
    /// Last reading from eWater Sense ADC 3, if fitted.
    /// </summary>
    public LastKnownValue<byte?> Sensor3 { get; set; } = new();
    
    /// <summary>
    /// Last known flags from health check events
    /// </summary>
    public LastKnownValue<HealthCheckFlags?> HealthFlags { get; set; } = new();

    
    /// <summary>
    /// Time over which the last status-flow-sample was taken (in ms)
    /// </summary>
    public LastKnownValue<int?> FlowSampleTimeMs { get; set; } = new();
    
    /// <summary>
    /// Number of ticks read from last status-flow-sample
    /// </summary>
    public LastKnownValue<int?> FlowSampleTicks { get; set; } = new();
    
    
    /// <summary>
    /// Last known value and date that it was recorded (if any)
    /// </summary>
    public class LastKnownValue<T> {
        /// <summary>
        /// Most recent value returned by the EWC. Null if not known
        /// </summary>
        public T? Value { get; set; }

        /// <summary>
        /// Last date and time the value was given by the EWC. Null if not known
        /// </summary>
        public DateTime? Date { get; set; }
    }
}

[Flags]
public enum TamperSwitchState : byte
{
    /// <summary>All tamper switches are closed, state is normal</summary>
    [System.ComponentModel.Description("All switches are closed (normal state)")] NoTamper = 0,
    /// <summary>
    /// Tamper switch 1 has triggered. This is normally in the Solar4 board.
    /// </summary>
    [System.ComponentModel.Description("Tamper switch 1 has triggered. This is normally in the Solar4 board")] Tamper1 = 2,
    /// <summary>
    /// Tamper switch 2 has triggered. This is normally in the "bottom box"
    /// </summary>
    [System.ComponentModel.Description("Tamper switch 2 has triggered. This is normally in the 'bottom box'")] Tamper2 = 1,
}

[Flags]
public enum HealthCheckFlags : byte
{
    /// <summary>
    /// If set, RFID mode is disabled, we are on manual valve control
    /// </summary>
    [System.ComponentModel.Description("RFID mode is disabled, we are on manual valve control")] ManualValveControl = 128, // 0x80
    /// <summary>If set, low battery condition is detected</summary>
    [System.ComponentModel.Description("low battery condition is detected")] LowBattery = 64, // 0x40
    /// <summary>If set, magnet is detected but no tag found</summary>
    [System.ComponentModel.Description("magnet is detected but no tag found")] ProximitySensor = 32, // 0x20
    /// <summary>If set, No-flow lockout timeout is in progress</summary>
    [System.ComponentModel.Description("No-flow lockout timeout is in progress")] LowFlowLockout = 16, // 0x10
    /// <summary>
    /// If set, the valve is currently open. If not set, it is closed
    /// </summary>
    [System.ComponentModel.Description("the valve is currently open")] ValveOpen = 8,
    /// <summary>
    /// If set, the GSM is NOT locked (read from Gadwall state).
    /// This is only useful for Eseye support.
    /// </summary>
    [System.ComponentModel.Description("the GSM is NOT locked (read from Gadwall)")] GsmUnlocked = 4,
    /// <summary>Tamper switch 1 is open (Solar4)</summary>
    [System.ComponentModel.Description("Tamper switch 1 is open (Solar4)")] Tamper1 = 2,
    /// <summary>
    /// Tamper switch 2 is open (bottom box).
    /// This is not present on EWC2.3
    /// </summary>
    [System.ComponentModel.Description("Tamper switch 2 is open (bottom box)")] Tamper2 = 1,
}


/// <summary>
/// Holds last known and desired state of a single super-tap slot on an EWC.
/// This is an aggregation of <see cref="EwcModelMemoryEntry{T}"/>
/// </summary>
public class EwcSupertapSlot
{
    /// <summary>
    /// Asset that the related EWC is bound to
    /// </summary>
    public int AssetId { get; set; }

    /// <summary>
    /// Slot 1..25 that this represents
    /// </summary>
    public int SupertapSlot { get; set; }
    
    /// <summary>
    /// Log CorrelationId for DW command that is trying to send a desired value to the EWC.
    /// Set to <c>null</c> when a new desired value is written.
    /// </summary>
    public Guid? CommandCorrelationId { get; set; }

    /// <summary>
    /// What phase of the read-write cycle is this slot in?
    /// <p/>
    /// <b>Note:</b> The desired value of a slot can only be changed when the phase is <see cref="EwcSlotCommandPhase.Idle"/>
    /// </summary>
    public EwcSlotCommandPhase CommandPhase { get; set; }

    /// <summary>
    /// TagID in last command sent. Null if no commands issued
    /// </summary>
    public uint? DesiredTagId { get; set; }

    /// <summary>
    /// Credit in last command sent, in MITS. Null if no commands issued
    /// </summary>
    public uint? DesiredCreditMits { get; set; }

    /// <summary>
    /// Date and time the last command was issued that affected this slot
    /// </summary>
    public DateTime? CommandDate { get; set; }

    /// <summary>
    /// Most recent value Tag Id returned by the EWC. Null if not known.
    /// Will be zero on clear or collection.
    /// </summary>
    public uint? LastKnownTagId { get; set; }

    /// <summary>
    /// Most recent credit returned by the EWC, in MITS. Null if not known.
    /// </summary>
    public uint? LastKnownCreditMits { get; set; }

    /// <summary>
    /// Last date and time the value was given by the EWC. Null if not known
    /// </summary>
    public DateTime? LastKnownDate { get; set; }

    /// <summary>
    /// True if the last known value has had all bytes written.
    /// This is used to detect when a sequence of patches has
    /// filled the entire value or not.
    /// </summary>
    public bool LastKnownValueComplete { get; set; }

    /// <summary>
    /// A super-tap slot representing an empty value.
    /// This has null values for desired and last-known.
    /// </summary>
    public static EwcSupertapSlot Empty => new() {SupertapSlot = -1, AssetId = -1, CommandPhase = EwcSlotCommandPhase.Idle};

    /// <summary>
    /// A super-tap slot representing an zeroed value.
    /// This has byte values for desired and last-known, equal to zero
    /// </summary>
    public static EwcSupertapSlot Zero => new() { SupertapSlot = -1, AssetId = -1, CommandPhase = EwcSlotCommandPhase.Idle,
        DesiredCreditMits = 0, DesiredTagId = 0, LastKnownCreditMits = 0, LastKnownTagId = 0};

}

/// <summary>
/// Phase of the read-write cycle that an EWC memory model's super tap slot is in.
/// </summary>
public enum EwcSlotCommandPhase
{
    /// <summary>
    /// The last read-write cycle is completed, or value has never been set.
    /// The EWC memory model slot can be changed
    /// to trigger another read-write cycle.
    /// </summary>
    Idle = 0,
    
    /// <summary>
    /// We have changed the desired value,
    /// but not yet send any commands.
    /// </summary>
    Waiting = 1,
    
    /// <summary>
    /// We have sent a command to read the current value on the tap.
    /// This happens immediately before a write.
    /// <p/>
    /// If reading times-out, we should set back to Waiting
    /// </summary>
    Reading = 2,
    
    /// <summary>
    /// We have sent a command to write a value to the tap, waiting for a reply.
    /// This should happen immediately after a read.
    /// After the reply, this should go back to <see cref="Idle"/>
    /// <p/>
    /// If writing times-out, we should set back to Waiting
    /// </summary>
    Writing = 4,
    
    /// <summary>
    /// The slot has received an event for collection of a top-up.
    /// The 'last known' values should be cleared. The desired values should
    /// have details of tag collection.
    /// </summary>
    Collected = 8,
    
    /// <summary>
    /// We think we detected that the EWC was swapped.
    /// All top-ups are in an unknown state. We will assume none are
    /// collected, and will reset any assigned top-ups back to awaiting slot.
    /// </summary>
    /// <remarks>See State.DataModels.EwcMemoryState.EwcSwapOccurred</remarks>
    Reset = 16
}
}