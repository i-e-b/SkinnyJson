﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

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

        private string Quote(string src) => src.Replace('\'', '"');

        private bool SimilarDate(DateTime? defrostedDateTime, DateTime dateTime)
        {
            if (defrostedDateTime == null) return false;
            var diff = dateTime - defrostedDateTime.Value;
            return (diff.TotalSeconds * diff.TotalSeconds) < 2;
        }
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
    }
}