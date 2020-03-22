
using UnityEngine;

public enum RelayObjState
{
	Active = 1,         // Enabled/Disabled?
	Visible = 2,
	Owned = 4,			// A player is designated as the controller
	IsAttached = 8,		// Is attached to a net object, or a mounting point of a net object
	ISMoving = 16,		// Is generating TRS updates, either by a relay script or by controller inputs
	//HasTarget = 16,		// Has a target resting TRS
	//Unused1 = 32,
	//Unused2 = 64,
	//Unused3 = 128,
}

public enum RelayObjOptions
{
	None = 0,
	Is2D = 1,

}

/// <summary>
/// Options for how this object can be created/destroyed.
/// </summary>
public enum RelayObjCreationOptions
{
	// Relay can always create new objects
	MasterCanCreateRelayObjs = 1,   //
	ActorsCanCreateRelayObjs = 2,   //
}

/// <summary>
/// Options for what Players can and can't do to this object.
/// </summary>
public enum RelayObjControlOptions
{
	
	ActorsCanTakeControl = 2,       // Actors can demand control, rather than control being determined by relay logic
	ActorsCanStealControl = 4,      // Actors can demand control, even if other 
	ActorsCanAssignControl = 8      // Actors can designate other Actors as controller
}

public class RelayObj
{
	public short relayObjId;
	public RelayObjState relayObjState;

	public uint actorId;
	public uint attacheToObjId;
	public byte mountPointId;

	/// TRS - can FieldOffset Euler/Rotation to overlap
	public Vector3 position;
	public Vector3 eulerAngles;
	public Quaternion rotation;
	public Vector3 scale;

	public Vector3 velocity;
	public Vector3 angularVelocity;
}
