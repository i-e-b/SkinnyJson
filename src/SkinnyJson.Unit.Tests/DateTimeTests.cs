using System;
using NUnit.Framework;

namespace SkinnyJson.Unit.Tests
{
#pragma warning disable CS8602
    [TestFixture]
    public class DateTimeTests
    {
        [Test]
        public void DateTime_from_tick_value()
        {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":638065330559726240}");

            Assert.That(SimilarDate(defrosted.date_time, new DateTime(2022, 12, 13, 12, 57, 35)), "Date was not interpreted correctly");

            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2022-12-13T12:57:35"), "Date was stored in an unexpected format");
        }

        [Test]
        public void DateTime_from_unix_timestamp()
        {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":1670858820000}");

            Assert.That(SimilarDate(defrosted.date_time, new DateTime(2022, 12, 12, 15, 27, 00)), "Date was not interpreted correctly");

            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2022-12-12T15:27:00"), "Date was stored in an unexpected format");
        }

        [Test]
        public void DateTime_No_leading_zero_without_T()
        {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10 9:48:27\"}");

            Assert.That(SimilarDate(defrosted.date_time, new DateTime(2019, 1, 10, 9, 48, 27)), "Date was not interpreted correctly");

            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10T09:48:27"), "Date was stored in an unexpected format");
        }

        [Test]
        public void DateTime_No_leading_zero_with_T()
        {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10T9:48:27\"}");

            Assert.That(SimilarDate(defrosted.date_time, new DateTime(2019, 1, 10, 9, 48, 27)), "Date was not interpreted correctly");

            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10T09:48:27"), "Date was stored in an unexpected format");
        }

        [Test]
        public void DateTime_With_leading_zero_without_T()
        {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10 09:48:27\"}");

            Assert.That(SimilarDate(defrosted.date_time, new DateTime(2019, 1, 10, 9, 48, 27)), "Date was not interpreted correctly");

            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10T09:48:27"), "Date was stored in an unexpected format");
        }

        [Test]
        public void DateTime_With_leading_zero_and_T()
        {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10T09:48:27\"}");

            Assert.That(SimilarDate(defrosted.date_time, new DateTime(2019, 1, 10, 9, 48, 27)), "Date was not interpreted correctly");

            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10T09:48:27"), "Date was stored in an unexpected format");
        }
        
        [Test]
        public void Universal_time_dates_have_Z_on_the_end_with_default_settings()
        {
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10T09:48:27Z\"}");

            Assert.That(defrosted.date_time.Kind, Is.EqualTo(DateTimeKind.Utc), "Date was not interpreted correctly");

            defrosted.date_time = new DateTime(2019, 1, 10, 9, 48, 27, DateTimeKind.Utc);
            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10T09:48:27Z"), "Date was stored in an unexpected format");
        }


        [Test]
        public void Universal_time_dates_have_Z_on_the_end_even_if_UseUtc_is_off()
        {
            Json.DefaultParameters.UseUtcDateTime = false;
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10T09:48:27Z\"}");

            Assert.That(defrosted.date_time.Kind, Is.EqualTo(DateTimeKind.Utc), "Date was not interpreted correctly");

            defrosted.date_time = new DateTime(2019, 1, 10, 9, 48, 27, DateTimeKind.Utc);
            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10T09:48:27Z"), "Date was stored in an unexpected format");
            Json.DefaultParameters.UseUtcDateTime = true;
        }
        

        [Test]
        public void Non_universal_times_are_left_unspecified_if_UseUtc_is_off()
        {
            Json.DefaultParameters.UseUtcDateTime = false;
            var defrosted = Json.Defrost<IHaveLotsOfTypes>("{\"date_time\":\"2019-01-10T09:48:27\"}");

            Assert.That(defrosted.date_time.Kind, Is.EqualTo(DateTimeKind.Unspecified), "Date was not interpreted correctly");

            defrosted.date_time = new DateTime(2019, 1, 10, 9, 48, 27, DateTimeKind.Local);
            var frozen = Json.Freeze(defrosted);
            Assert.That(frozen, Contains.Substring("2019-01-10T09:48:27\""), "Date was stored in an unexpected format");
            Json.DefaultParameters.UseUtcDateTime = true;
        }

        private bool SimilarDate(DateTime? defrostedDateTime, DateTime dateTime)
        {
            if (defrostedDateTime == null) return false;
            var diff = dateTime - defrostedDateTime.Value;
            return (diff.TotalSeconds * diff.TotalSeconds) < 2;
        }
    }
}