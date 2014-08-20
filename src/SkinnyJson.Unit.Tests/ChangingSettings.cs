namespace SkinnyJson.Unit.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class ChangingSettings
    {

        [Test]
        public void can_change_defaults()
        {
			var original = ObjectWithoutAnInterface.Make();

            Json.DefaultParameters.EnableAnonymousTypes = true;
            var on = Json.Freeze(original);
            Json.DefaultParameters.EnableAnonymousTypes = false;
            var off = Json.Freeze(original);

            Assert.That(on, Is.Not.StringContaining("$type"));
            Assert.That(off, Is.StringContaining("$type"));
        }

         
    }
}