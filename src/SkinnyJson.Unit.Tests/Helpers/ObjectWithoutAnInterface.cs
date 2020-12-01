// ReSharper disable InconsistentNaming
namespace SkinnyJson.Unit.Tests {
	public class ObjectWithoutAnInterface {
		public static ObjectWithoutAnInterface Make () {
			var x = new ObjectWithoutAnInterface {A = "this is a", B = "this is B"};
			return x;
		}

		public string? A;
		public string? B {get;set;}
	}
}
