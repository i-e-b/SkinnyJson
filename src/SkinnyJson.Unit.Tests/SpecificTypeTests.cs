using System;
using NUnit.Framework;
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable InconsistentNaming

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class SpecificTypeTests {
        [Test]
        public void DateTime_No_leading_zero_without_T() {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10 9:48:27\"}");
            
            Assert.That(SimilarDate(defrosted.date_time, new DateTime(2019,1,10,9,48,27)), "Date was not interpreted correctly");

            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10T09:48:27"), "Date was stored in an unexpected format");
        }

        [Test]
        public void DateTime_No_leading_zero_with_T() {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10T9:48:27\"}");
            
            Assert.That(SimilarDate(defrosted.date_time, new DateTime(2019,1,10,9,48,27)), "Date was not interpreted correctly");

            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10T09:48:27"), "Date was stored in an unexpected format");
        }
        
        [Test]
        public void DateTime_With_leading_zero_without_T() {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10 09:48:27\"}");
            
            Assert.That(SimilarDate(defrosted.date_time, new DateTime(2019,1,10,9,48,27)), "Date was not interpreted correctly");
            
            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10T09:48:27"), "Date was stored in an unexpected format");
        }
        
        [Test]
        public void DateTime_With_leading_zero_and_T() {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10T09:48:27\"}");
            
            Assert.That(SimilarDate(defrosted.date_time, new DateTime(2019,1,10,9,48,27)), "Date was not interpreted correctly");
            
            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10T09:48:27"), "Date was stored in an unexpected format");
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

        private bool SimilarDate(DateTime? defrostedDateTime, DateTime dateTime)
        {
            if (defrostedDateTime == null) return false;
            var diff = dateTime - defrostedDateTime.Value;
            return (diff.TotalSeconds * diff.TotalSeconds) < 2;
        }
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
    }
}