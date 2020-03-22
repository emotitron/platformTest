using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Utilities.GhostWorlds
{
	/// <summary>
	/// Indicates to GhostWorld that the child gameobject this component is on requires replication.
	/// </summary>
	public interface INeedsGhostGameObject
	{

	}

	/// <summary>
	/// Indicates that this component needs to be copied exactly to the GhostWorld clone.
	/// </summary>
	public interface ICopyToGhost : INeedsGhostGameObject
	{

	}

	/// <summary>
	/// Indicates that a networked object needs to be recreated in the ghost world.
	/// </summary>
	public interface IHaunted : INeedsGhostGameObject
	{
		Ghost Ghost { get; }
		GameObject GameObject { get; }
	}

	/// <summary>
	/// Tags SyncObjects as needing/wanting to have connection to their ghostworld equivalent object.
	/// </summary>
	public interface IHauntedComponent : INeedsGhostGameObject
	{
		GhostComponent GhostComponent { set; }
	}
}

