// ReSharper disable InconsistentNaming
namespace SkinnyJson.Unit.Tests {
	public class SimpleObject {
		public static SimpleObject Make () {
			var x = new SimpleObject {A = "this is a", B = "this is B"};
			return x;
		}

		public string A;
		public string B {get;set;}
	}
}
