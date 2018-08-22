// ReSharper disable InconsistentNaming

using System.IO;
using System.Text;

namespace SkinnyJson.Unit.Tests {
    using System;
    using System.Collections.Generic;

    public class StreamData {
        public static Stream StreamOfJson() {
            var stringForm = Json.Freeze(ComplexTypes.DictionaryOfDictionaryOfTupleWithList());

            var output = new MemoryStream();
            var bytes = Encoding.UTF8.GetBytes(stringForm);
            output.Write(bytes, 0, bytes.Length);
            output.Seek(0, SeekOrigin.Begin);

            return output;
        }
    }

    public class ComplexTypes {
		public static IDictionary<string, Dictionary<string, string>> DictionaryOfDictionary() {
			var x = new Dictionary<string, Dictionary<string, string>>();
            x.Add("A", new Dictionary<string, string>());
            x.Add("B", new Dictionary<string, string>());
            x["A"].Add("X", "Y");
            x["A"].Add("W", "Z");
            x["B"].Add("1", "2");
            x["B"].Add("3", "4");
			return x;
		}

        public static Dictionary<string, Dictionary<string, Tuple<int, int, List<int>>>> DictionaryOfDictionaryOfTupleWithList()
        {
            var x = new Dictionary<string, Dictionary<string, Tuple<int, int, List<int>>>>();
            x.Add("Hello", new Dictionary<string, Tuple<int, int, List<int>>>());
            x.Add("World", new Dictionary<string, Tuple<int, int, List<int>>>());

            x["Hello"].Add("Bob", new Tuple<int, int, List<int>>(1, 2, new List<int>()));
            x["Hello"]["Bob"].Item3.Add(1);
            x["Hello"]["Bob"].Item3.Add(2);
            x["Hello"]["Bob"].Item3.Add(3);

            x["World"].Add("Sam", new Tuple<int, int, List<int>>(3, 4, new List<int>()));
            x["World"]["Sam"].Item3.Add(10);
            x["World"]["Sam"].Item3.Add(20);
            x["World"]["Sam"].Item3.Add(30);

            return x;
        }
    }
}
