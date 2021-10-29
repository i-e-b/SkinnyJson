using NUnit.Framework;

namespace SkinnyJson.NetCore.UnitTests
{
    [TestFixture]
    public class BasicTests
    {

        [Test]
        public void incompatible_nullable_types_are_handled()
        {
            Json.DefaultParameters.EnableAnonymousTypes = true;
            var result = Json.Defrost<AnnotatedCoreType>("{}");
            
            Assert.That(result, Is.Not.Null, "Outer type");
            Assert.That(result.NullableString, Is.Null, "Nullable type");
            Assert.That(result.NonNullString, Is.Null, "Not null type");
        }
        
        [Test]
        public void null_value_nullable_types_are_handled()
        {
            Json.DefaultParameters.EnableAnonymousTypes = true;
            var result = Json.Defrost<AnnotatedCoreType>("{\"NullableString\":null, \"NonNullString\":\"\"}");
            
            Assert.That(result, Is.Not.Null, "Outer type");
            Assert.That(result.NullableString, Is.Null, "Nullable type");
            Assert.That(result.NonNullString, Is.Not.Null, "Not null type");
        }

        [Test]
        public void handling_missing_constructor()
        {
            Json.DefaultParameters.EnableAnonymousTypes = true;
            var result = Json.Defrost<BadConstructor>("{\"StringValue\":\"Hello, World\"}");
            
            Assert.That(result, Is.Not.Null, "Outer type");
            Assert.That(result.StringValue, Is.EqualTo("Hello, World"), "Nullable type");
        }

        public class AnnotatedCoreType
        {
#pragma warning disable 8618 // warning that non-nullable is not valid
            public string NonNullString { get; set; }
            public string? NullableString { get; set; }
#pragma warning restore 8618
        }

        public class BadConstructor
        {
            public BadConstructor(int requirement) { }
            public string? StringValue { get; set; }
        }
    }
}