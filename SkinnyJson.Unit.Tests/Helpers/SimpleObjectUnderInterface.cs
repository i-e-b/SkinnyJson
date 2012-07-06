// ReSharper disable InconsistentNaming
namespace SkinnyJson.Unit.Tests {
	public class SimpleObjectUnderInterface : ISimpleObject {
		public static SimpleObjectUnderInterface Make () {
			var x = new SimpleObjectUnderInterface {A = "this is a", B = "this is B"};
			return x;
		}

		public string A;
		public string B {get;set;}
	}
}
