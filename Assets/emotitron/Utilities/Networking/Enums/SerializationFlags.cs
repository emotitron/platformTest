
namespace emotitron.Utilities.Networking
{
	public enum SerializationFlags { None = 0, HasChanged = 1, Force = 2, ForceReliable = 4, SendToSelf = 8, NewConnection = 16, IsComplete = 32 }
	public enum FrameArrival { IsFuture, IsTarget, IsSnap, IsLate }

}

