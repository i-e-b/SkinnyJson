namespace SkinnyJson.Unit.Tests {

	public interface IHaveComplexProperties {
		string AProperty { get; set; }
		IHaveProperties BProperty { get; set; }
	}

	public interface IHaveProperties {
		string AProperty { get; set; }
	}

	public interface IHaveMethods {
		object AMethod ();
	}
}
