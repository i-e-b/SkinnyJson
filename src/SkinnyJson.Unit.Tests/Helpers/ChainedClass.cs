namespace SkinnyJson.Unit.Tests {
	public class ChainedClass: TopClass
	{
		public string? Z;

		public static ChainedClass Make()
		{
			return new ChainedClass{FromBase = "Ronny", X="x", Y="y", Z="z"};
		}
	}
	public class TopClass: MiddleClass
	{
		public string? Y;
	}

	public class MiddleClass: IKnowMyPlace
	{
		public string? FromBase { get; set; }
		public string? X;
	}

	public interface IKnowMyPlace
	{
		string? FromBase { get; set; }
	}

}