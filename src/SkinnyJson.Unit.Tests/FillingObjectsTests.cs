using NUnit.Framework;

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class FillingObjectsTests{
        [Test]
        public void can_set_the_properties_of_an_existing_object_using_a_json_string ()
        {
            var target = new ObjectWithoutAnInterface();
            var source = "{\"A\":\"master\",\r\n \"B\": \"blaster\"}";

            var same = Json.FillObject(target, source);

            Assert.That(target.A, Is.EqualTo("master"));
            Assert.That(target.B, Is.EqualTo("blaster"));
            Assert.That(same, Is.SameAs(target), "Did not return original");
        }
        
        [Test]
        public void can_set_the_properties_of_an_existing_object_with_escaped_strings ()
        {
            var target = new ObjectWithoutAnInterface();
            var source = "{\"A\":\"C:\\\\temp\\\\test\",\r\n \"B\": \"/slash\\t\\r\\n\"}";

            var same = Json.FillObject(target, source);

            Assert.That(target.A, Is.EqualTo(@"C:\temp\test"));
            Assert.That(target.B, Is.EqualTo("/slash\t\r\n"));
            Assert.That(same, Is.SameAs(target), "Did not return original");
        }

        [Test]
        public void can_set_the_values_of_an_implicitly_typed_object_from_a_json_string ()
        {
            Json.DefaultParameters.IgnoreCaseOnDeserialize = false;
            var target = new { A = "old", B = "old" };
            var source = "{\"A\":\"master\",\r\n \"B\": \"blaster\"}";

            Json.FillObject(target, source);

            Assert.That(target.A, Is.EqualTo("master"));
            Assert.That(target.B, Is.EqualTo("blaster"));
        }
    }
}