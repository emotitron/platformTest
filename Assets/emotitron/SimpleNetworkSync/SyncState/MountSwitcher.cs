using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Networking
{
	public class MountSwitcher : NetComponent
		, IOnPreUpdate
		, IOnPreSimulate
	{
		public KeyCode keycode = KeyCode.M;
		public MountSelector mount = new MountSelector(1);

		protected bool triggered;

		public override void OnAwake()
		{
			base.OnAwake();

			if (!GetComponent<SyncState>())
			{
				Debug.LogWarning(GetType().Name + " on '"+ transform.parent.name + "/" + name + "' needs to be on the root of NetObject with component " + typeof(SyncState).Name +". Disabling.");
				netObj.RemoveInterfaces(this);
			}
		}

		public void OnPreUpdate()
		{
			if (Input.GetKeyDown(keycode))
				triggered = true;
		}

		public void OnPreSimulate(int frameId, int subFrameId)
		{
			//Debug.Log("<color=blue>" + pv.IsMine + "</color>");

			if (triggered)
			{

				triggered = false;

				var currMount = syncState.CurrentMount;

				if (ReferenceEquals(currMount, null))
					return;

				if (!currMount.IsMine)
					return;

				Debug.Log("Try change to mount : " + currMount + " : " + currMount.IsMine + " : " + mount.id);

				syncState.ChangeMount(mount.id);
			}
		}

	}
}

