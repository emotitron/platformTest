using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Networking
{
	[RequireComponent(typeof(Mount))]

	public class MountThrow : NetComponent
		, IOnPreUpdate
		, IOnPreSimulate
	{

		public KeyCode throwKey = KeyCode.Alpha5;
		public Mount mount;

		public Vector3 localOffset = new Vector3();
		public Vector3 velocity = new Vector3(0, .5f, 2f);

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			if (mount == null)
				mount = GetComponent<Mount>();
		}
#endif
		public override void OnAwake()
		{
			base.OnAwake();
			if (mount == null)
				mount = GetComponent<Mount>();
		}


		public void OnPreUpdate()
		{
			if (!IsMine)
				return;

			if (Input.GetKeyDown(throwKey))
				throwQueued = true;
		}

		bool throwQueued;

		public void OnPreSimulate(int frameId, int subFrameId)
		{

			if (!throwQueued)
				return;

			throwQueued = false;


			var mountedObjs = mount.mountedObjs;

			for (int i = 0; i < mountedObjs.Count; ++i)
			{
				var obj = mountedObjs[i];
				var rb = obj.Rb;
				if (rb && obj.IsThrowable)
				{
					var syncState = obj as SyncState;

					if (!syncState)
						return;

					//Debug.Log("Throw success " + syncState.name + " being thrown from mount " + mount.name + " : " + name);

					syncState.Throw(localOffset, velocity);
					
					return;
				}
			}

		}

	}
}

