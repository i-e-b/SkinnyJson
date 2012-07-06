namespace SkinnyJson.Unit.Tests {
	public class ChainedClass: TopClass
	{
		public string Z;

		public static ChainedClass Make()
		{
			return new ChainedClass{IKMP = "Ronny", X="x", Y="y", Z="z"};
		}
	}
	public class TopClass: MiddleClass
	{
		public string Y;
	}

	public class MiddleClass: IKnowMyPlace
	{
		public string IKMP { get; set; }
		public string X;
	}

	public interface IKnowMyPlace
	{
		string IKMP { get; set; }
	}

}