using System;

namespace SkinnyJson.Unit.Tests {
	public interface IMarkerInterface
	{
		Guid AnId { get; }
	}
	public interface IExtendedMarker:IMarkerInterface {
		string AnotherThing { get; set; }
	}

	public class UsesMarker : IMarkerInterface
	{
		public Guid AnId{get; set;}
		public string AnotherThing { get; set; }
	}
}