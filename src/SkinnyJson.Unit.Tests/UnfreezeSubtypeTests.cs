using System.Linq;
using NUnit.Framework;

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
            var result2 = Json.DefrostFromPath<ISubtype>(null, input);

            Assert.That(result1.Select(t=>t.Value), Is.EquivalentTo(new []{ "1st" }));
            Assert.That(result2.Select(t=>t.Value), Is.EquivalentTo(new []{ "1st" }));
        }

        [Test]
        public void should_be_able_to_provide_a_path_an_get_back_an_enumeration_of_matches()
        {
            var input = RepositoryType.SimpleList();

            var result = Json.DefrostFromPath<ISubtype>("Path.To.Thing", input);

            Assert.That(result.Select(t=>t.Value), Is.EquivalentTo(new []{ "1st", "2nd" }));
        }

        [Test]
        public void should_be_able_to_pass_a_dynamic_object_selection_and_get_back_an_enumeration_of_matches()
        {
            Assert.Fail();
        }

        [Test]
        public void should_get_an_empty_enumeration_if_no_paths_match ()
        {
            Assert.Fail();
        }

        [Test]
        public void should_get_an_enumeration_with_a_single_item_if_only_one_object_matches ()
        {
            Assert.Fail();
        }

        [Test]
        public void should_get_a_single_enumeration_with_all_results_if_multiple_paths_match()
        {
            Assert.Fail();
        }
    }

    public interface ISubtype {
        string Name { get; set; }
        string Value { get; set; }
    }

    public class SampleSubtype : ISubtype
    {
        public string Name { get; set; }
        public string Value { get; set; }
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
    }
}