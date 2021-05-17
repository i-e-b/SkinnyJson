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
        
        public class AnnotatedCoreType
        {
#pragma warning disable 8618 // warning that non-nullable is not valid
            public string NonNullString { get; set; }
            public string? NullableString { get; set; }
#pragma warning restore 8618
        
        }
    }
}