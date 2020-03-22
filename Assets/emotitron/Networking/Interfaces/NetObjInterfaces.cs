using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Networking
{
	/// <summary>
	/// Callback Interface for when all SyncObjects on a NetObj have indicated Ready and are prepared for the NetObj to become visible.
	/// </summary>
	public interface IOnNetObjReady
	{
		
		void OnNetObjReadyChange(bool ready);

	}

	public interface IOnJoinedRoom
	{
		void OnJoinedRoom();
	}

	public interface IOnAwake
	{
		void OnAwake();
	}

	public interface IOnStart
	{
		void OnStart();
	}

	public interface IOnEnable
	{
		void OnPostEnable();
	}

	public interface IOnDisable
	{
		void OnPostDisable();
	}

	public interface IOnDestroy
	{
		void OnPostDestroy();
	}

}
