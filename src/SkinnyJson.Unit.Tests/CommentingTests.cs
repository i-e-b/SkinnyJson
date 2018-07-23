using NUnit.Framework;

namespace SkinnyJson.Unit.Tests
{
    [TestFixture]
    public class CommentingTests
    {
        [Test]
        public void inline_comments_are_ignored()
        {
            var input = @"
{
    // This file has JS / C++ style one-line comments.
    // These are filtered any time you are *not* in a
    // string until the next \r or \n
    ""value"": ""This is not a comment: //, but..."" // this is
}";
            var result = Json.DefrostDynamic(input);

            Assert.That(result.value(), Is.EqualTo("This is not a comment: //, but..."));
        }

        
        [Test]
        public void can_handle_multi_line_arrays()
        {
            var input = @"
[
    //one:
    1,
    // two:
    2, // finally,
    3  // <- three
]";
            var result = Json.Defrost<double[]>(input);

            Assert.That(result, Is.EqualTo(new[] { 1.0, 2.0, 3.0 }));
        }


        [Test]
        public void beautify_removes_comments ()
        {
            var input = @"
{
    // This file has JS / C++ style one-line comments.
    // These are filtered any time you are *not* in a
    // string until the next \r or \n
    ""value"": ""This is not a comment: //, but..."" // this is
}";

            var expected = "{\r\n    \"value\" : \"This is not a comment: //, but...\"\r\n}";

            var result = Json.Beautify(input);

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}