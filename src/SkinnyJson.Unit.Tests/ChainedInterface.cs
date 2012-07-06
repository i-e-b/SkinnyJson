namespace SkinnyJson.Unit.Tests
{
	public class ChainedInterface : ITopLevel
	{
		public string A{ get; set; }

		public string B{ get; set; }

		public string C{ get; set; }

		public static ChainedInterface Make()
		{
			return new ChainedInterface{A = "a", B = "b", C = "c"};
		}
	}
	
	public interface ITopLevel:IMiddle
	{
		string C { get; set; }
	}

	public interface IMiddle: IBottom
	{
		string B { get; set; }
	}

	public interface IBottom
	{
		string A { get; set; }
	}
}