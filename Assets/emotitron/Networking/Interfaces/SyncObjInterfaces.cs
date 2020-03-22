
using emotitron.Utilities.GhostWorlds;
using emotitron.Utilities.Networking;

/// <summary>
/// A collection of interfaces SyncObjects can implement.
/// </summary>
namespace emotitron.Networking
{
	public interface ISyncAnimator { }

	
	//public delegate int ApplyOrderAction();
	
	public interface IApplyOrder
	{
		int ApplyOrder { get; }
	}
	/// <summary>
	/// Flags a ApplyOrder as adjustable in the inspector. 
	/// </summary>
	public interface IAdjustableApplyOrder : IApplyOrder
	{

	}


	/// <summary>
	/// Flags a SyncObject as needing to be recieve a complete frame or manually call netObj.SyncObjSetReady() before the netObj is flagged as ready.
	/// </summary>
	public interface IReadyable
	{ 
		bool AlwaysReady { get; }
	}

	public interface IUseKeyframes
	{
		bool IsKeyframe(int frameId);
	}

	
	public interface IDeltaFrameChangeDetect : IUseKeyframes
	{
		bool UseDeltas { get; set; }
	}

	public interface ISerializationOptional
	{
		bool IncludeInSerialization { get; }
	}


	//public delegate SerializationFlags OnNetSerializeDelegate(int frameId, byte[] buffer, ref int bitposition);
	public interface IOnNetSerialize
	{
		SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags);
		bool SkipWhenEmpty { get; }
	}

	//public delegate void OnNetDeserializeDelegate(int sourceFrameId, int originFrameId, int localFrameId, byte[] buffer, ref int bitposition);
	public interface IOnNetDeserialize
	{
		SerializationFlags OnNetDeserialize(int originFrameId, int localFrameId, byte[] buffer, ref int bitposition, FrameArrival frameArrival);
	}


	//public delegate void OnSnapshotDelegate(int newTargetFrameId, bool initialize);
	public interface IOnSnapshot {  bool OnSnapshot(int newTargetFrameId); }

	//public delegate void OnInterpolateDelegate(float t);
	public interface IOnInterpolate { bool OnInterpolate(int snapFrameId, int targFrameId, float t); }

	//public delegate void OnQuantizeDelegate(int frameId, Realm realm);
	public interface IOnQuantize { void OnQuantize(int frameId, Realm realm); }

	//public delegate void OnCaptureCurrentValuesDelegate(int frameId, bool amActingAuthority, Realm realm);
	public interface IOnCaptureState { void OnCaptureCurrentState(int frameId, Realm realm); }
	public interface IOnAuthorityChanged { void OnAuthorityChanged(bool isMine, bool asServer); }

}
