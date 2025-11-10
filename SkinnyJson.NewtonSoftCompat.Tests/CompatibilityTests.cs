using NUnit.Framework;
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace SkinnyJson.NewtonSoftCompat.Tests;

/// <summary>
/// Tests that we can support a few Newtonsoft.Json decorators
/// (without requiring the packages from SkinnyJson)
/// </summary>
[TestFixture]
public class CompatibilityTests
{
    [Test]
    public void custom_names_can_be_pulled_from_Newtonsoft_annotations()
    {
        var defrosted = Json.Defrost<NewtonsoftJsonNamed>(
            "{\"CUSTOM_name\":\"Value\"}");

        Assert.That(defrosted.OriginalName, Is.EqualTo("Value"));

        var frozen = Json.Freeze(defrosted);
        Console.WriteLine(frozen);

        Assert.That(frozen, Is.EqualTo("{\"CUSTOM_name\":\"Value\"}"));
    }


    [Test]
    public void custom_serialisers_can_be_pulled_from_untyped_Newtonsoft_annotations()
    {
        var defrosted = Json.Defrost<NewtonsoftCustomSerialised>(
            "{\"CustomProp\":\"Value\"}");

        Assert.That(defrosted.CustomProp.OriginalName, Is.EqualTo("Value"));

        var frozen = Json.Freeze(defrosted);
        Console.WriteLine(frozen);

        Assert.That(frozen, Is.EqualTo("{\"CustomProp\":\"Value\"}"));
    }


    [Test]
    public void custom_serialisers_can_be_pulled_from_typed_Newtonsoft_annotations()
    {
        var defrosted = Json.Defrost<NewtonsoftTypedCustomSerialised>(
            "{\"CustomProp\":\"Value\"}");

        Assert.That(defrosted.CustomProp.OriginalName, Is.EqualTo("Value"));

        var frozen = Json.Freeze(defrosted);
        Console.WriteLine(frozen);

        Assert.That(frozen, Is.EqualTo("{\"CustomProp\":\"Value\"}"));
    }
}

public class NewtonsoftConverter : Newtonsoft.Json.JsonConverter
{
    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
    {
        var obj = value as NewtonsoftJsonNamed;
        if (obj?.OriginalName is null) return;

        var t = Newtonsoft.Json.Linq.JToken.FromObject(obj.OriginalName);
        t.WriteTo(writer);
    }

    public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        var str = reader.Value as string;
        if (str is null) return null;

        return new NewtonsoftJsonNamed { OriginalName = str };
    }

    public override bool CanConvert(Type objectType) => true;
}


public class TypedNewtonsoftConverter : Newtonsoft.Json.JsonConverter<NewtonsoftJsonNamed>
{
    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, NewtonsoftJsonNamed? value, Newtonsoft.Json.JsonSerializer serializer)
    {
        writer.WriteValue(value?.OriginalName);
    }

    public override NewtonsoftJsonNamed ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, NewtonsoftJsonNamed? existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        var got = reader.Value;
        return new NewtonsoftJsonNamed{
            OriginalName = got as string ?? "fail!"
        };
    }
}

public class NewtonsoftCustomSerialised
{
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftConverter))]
    public NewtonsoftJsonNamed? CustomProp { get; set; }
}

public class NewtonsoftTypedCustomSerialised
{
    [Newtonsoft.Json.JsonConverter(typeof(TypedNewtonsoftConverter))]
    public NewtonsoftJsonNamed? CustomProp { get; set; }
}

public class NewtonsoftJsonNamed
{
    [Newtonsoft.Json.JsonProperty("CUSTOM_name")]
    public string? OriginalName { get; set; }
}