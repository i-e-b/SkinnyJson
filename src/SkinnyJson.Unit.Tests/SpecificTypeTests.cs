using System;
using NUnit.Framework;

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class SpecificTypeTests {
        [Test]
        public void DateTime_No_leading_zero() {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10 9:48:27\"}");
            
            Assert.That(SimilarDate(defrosted?.date_time, new DateTime(2019,1,10,9,48,27)), "Date was not interpreted correctly");

            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10 09:48:27"), "Date was stored in an unexpected format");
        }
        
        [Test]
        public void DateTime_With_leading_zero() {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10 09:48:27\"}");
            
            Assert.That(SimilarDate(defrosted?.date_time, new DateTime(2019,1,10,9,48,27)), "Date was not interpreted correctly");
            
            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10 09:48:27"), "Date was stored in an unexpected format");
        }
        
        [Test]
        public void DateTime_With_leading_zero_and_T() {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10T09:48:27\"}");
            
            Assert.That(SimilarDate(defrosted?.date_time, new DateTime(2019,1,10,9,48,27)), "Date was not interpreted correctly");
            
            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10 09:48:27"), "Date was stored in an unexpected format");
        }

        private bool SimilarDate(DateTime? defrostedDateTime, DateTime dateTime)
        {
            if (defrostedDateTime == null) return false;
            var diff = dateTime - defrostedDateTime.Value;
            return (diff.TotalSeconds * diff.TotalSeconds) < 2;
        }
    }

    public interface IHaveLotsOfTypes {
        DateTime date_time { get; set; }
    }
}