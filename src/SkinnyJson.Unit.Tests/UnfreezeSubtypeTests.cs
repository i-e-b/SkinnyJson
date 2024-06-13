using System.Linq;
using NUnit.Framework;
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class UnfreezeSubtypeTests
    {
        [Test]
        public void an_empty_path_results_in_a_single_item_defrosted_from_the_root_json_object ()
        {
            var input = RepositoryType.RawList();

            var result1 = Json.DefrostFromPath<ISubtype>("", input);
            var result2 = Json.DefrostFromPath<ISubtype>(null!, input);

            Assert.That(result1.Select(t=>t.Value), Is.EquivalentTo(new []{ "1st" }));
            Assert.That(result2.Select(t=>t.Value), Is.EquivalentTo(new []{ "1st" }));
        }

        [Test]
        public void should_be_able_to_provide_a_path_an_get_back_an_enumeration_of_matches()
        {
            var input = RepositoryType.SimpleList();

            var result = Json.DefrostFromPath<ISubtype>("Path.To.Thing", input).ToList();

            Assert.That(result.Select(t=>t.Value), Is.EquivalentTo(new []{ "1st", "2nd" }));
            Assert.That(result.Select(t=>t.Name), Is.EquivalentTo(new []{ "First", "Second" }));
        }

        [Test]
        public void should_get_an_empty_enumeration_if_no_paths_match ()
        {
            var input = RepositoryType.SimpleList();

            var result = Json.DefrostFromPath<ISubtype>("Path.To.Thing.This.Is.Too.Far", input);

            Assert.That(result, Is.Empty);
        }
        
        [Test]
        public void should_get_empty_enumeration_if_path_does_not_match_target_type ()
        {
            var input = RepositoryType.SimpleList();

            var result = Json.DefrostFromPath<ISubtype>("Path.To", input); // points to an array of this type

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void should_get_an_enumeration_with_a_single_item_if_only_one_object_matches ()
        {
            var input = RepositoryType.SimpleSingleItem();

            var result = Json.DefrostFromPath<ISubtype>("Path.To.Thing", input).ToList();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Select(t=>t.Name), Is.EquivalentTo(new []{ "Only" }));
        }

        [Test]
        public void should_get_a_single_enumeration_with_all_results_if_multiple_paths_match()
        {
            var input = RepositoryType.ForkedPath();

            var result = Json.DefrostFromPath<ISubtype>("Path.To.Thing", input).ToList();

            Assert.That(result.Select(t=>t.Value), Is.EquivalentTo(new []{ "1st", "2nd", "3rd", "4th" }));
            Assert.That(result.Select(t=>t.Name), Is.EquivalentTo(new []{ "First", "Second", "Third", "Fourth" }));
        }

        [Test]
        public void complex_sub_path_tests()
        {
            var input = RepositoryType.ComplexSubObjects();

            // here we ask for the second item of each "values"
            var result = Json.DefrostFromPath<string>("metrics.time_series.cpu.values[*].[1]", input).ToList();
            Assert.That(result, Is.EquivalentTo(new []{ "249","467","209" })); // 4th item has no 2nd child
            
            
            // here we ask for all paths under the option at index 0
            result = Json.DefrostFromPath<string>("metrics.options[0].name", input).ToList();
            Assert.That(result, Is.EquivalentTo(new []{ "first option" }));
        }

        [Test(Description = "DefrostFromPath is affected by case sensitivity in both the path walking, and the serialisation output")]
        public void defrostFromPath_should_respect_case_sensitivity_settings()
        {
            var input = RepositoryType.CaseComplex();
            
            var result = Json.DefrostFromPath<ISubtype>("Root.Options", input).ToList();
            Assert.That(result.Count, Is.EqualTo(2), "found item");
            
            Assert.That(result[0]!.Name, Is.EqualTo("First Option"), "Item 1 name");
            Assert.That(result[0]!.Value, Is.EqualTo("1"), "Item 1 value");
            
            Assert.That(result[1]!.Name, Is.EqualTo("Second Option"), "Item 2 name");
            Assert.That(result[1]!.Value, Is.EqualTo("2"), "Item 2 value");
            
            
            result = Json.DefrostFromPath<ISubtype>("Root.Options", input, JsonParameters.Compatible).ToList();
            Assert.That(result.Count, Is.Zero, "Should not find item");
        }
    }

    public interface ISubtype {
        string? Name { get; set; }
        string? Value { get; set; }
    }

    public class SampleSubtype : ISubtype
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
    }

    public class RepositoryType
    {
        public static string RawList(){
            return Json.Freeze(
                new SampleSubtype { Name = "First", Value = "1st" }
            );
        }

        public static string SimpleList()
        {
            return Json.Freeze(
                new
                {
                    Path = new
                    {
                        To = new
                        {
                            Thing = new ISubtype[]{
                                new SampleSubtype { Name = "First", Value = "1st" },
                                new SampleSubtype { Name = "Second", Value = "2nd" }
                            }
                        }
                    }
                }
            );
        }
        
        public static string SimpleSingleItem()
        {
            return Json.Freeze(
                new
                {
                    Path = new
                    {
                        To = new
                        {
                            Thing = new SampleSubtype { Name = "Only", Value = "Ewe" }
                        }
                    }
                }
            );
        }

        public static string CaseComplex()
        {
            return @"
{
  'ROOT': {
    'options': [
        {
            'Name':'First Option',
            'value':'1'
        },
        {
            'name':'Second Option',
            'Value':'2'
        }
    ],
  }
}".Replace('\'', '"');
        }

        public static string ForkedPath()
        {
            return Json.Freeze(
                new
                {
                    Path = new
                    {
                        To = new []
                        {
                            new {
                                Thing = new ISubtype[]{
                                    new SampleSubtype { Name = "First", Value = "1st" },
                                    new SampleSubtype { Name = "Second", Value = "2nd" }
                                }
                            },
                            new {
                                Thing = new ISubtype[]{
                                    new SampleSubtype { Name = "Third", Value = "3rd" },
                                    new SampleSubtype { Name = "Fourth", Value = "4th" }
                                }
                            }
                        }
                    }
                }
            );
        }

        public static string ComplexSubObjects()
        {
            return @"{
  'metrics': {
    'start': '2022-03-03T00:00:00+00:00',
    'end': '2022-03-04T00:00:00+00:00',
    'step': 3600,
    'options': [
        {'name':'first option', 'value':1},
        {'name':'second option', 'value':2}
    ],
    'time_series': {
      'cpu': {
        'values': [
          [
            1646265600,
            '249'
          ],
          [
            1646269200,
            '467'
          ],
          [
            1646272800,
            '209'
          ],
          [
            1646276400,
          ]
        ]
      }
    }
  }
}".Replace('\'', '"');
        }
    }
}