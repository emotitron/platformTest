﻿
using UnityEngine;


namespace emotitron.Networking
{
	public struct StateChangeInfo
	{
		public ObjState objState;
		public Mount mount;
		public Vector3? offsetPos;
		public Vector3? offsetRot;
		public Vector3? velocity;
		public bool force;

		public StateChangeInfo(StateChangeInfo src)
		{
			this.objState = src.objState;
			this.mount = src.mount;
			this.offsetPos = src.offsetPos;
			this.offsetRot = src.offsetRot;
			this.velocity = src.velocity;
			this.force = src.force;
		}

		public StateChangeInfo(ObjState itemState, Mount mount, Vector3? offsetPos, Vector3? offsetRot, Vector3? velocity, bool force)
		{
			this.objState = itemState;
			this.mount = mount;
			this.offsetPos = offsetPos;
			this.offsetRot = offsetRot;
			this.velocity = velocity;
			this.force = force;
		}

		public StateChangeInfo(ObjState itemState, Mount mount, Vector3? offsetPos, Vector3? velocity, bool force)
		{
			this.objState = itemState;
			this.mount = mount;
			this.offsetPos = offsetPos;
			this.offsetRot = null;
			this.velocity = velocity;
			this.force = force;
		}

		public StateChangeInfo(ObjState itemState, Mount mount, bool force)
		{
			this.objState = itemState;
			this.mount = mount;
			this.offsetPos = null;
			this.offsetRot = null;
			this.velocity = null;
			this.force = force;
		}

#if UNITY_EDITOR
		public override string ToString()
		{
			return objState + " " + mount + " pos: " + offsetPos + " rot: " + offsetRot + " vel: " + velocity + " frce: " + force;
		}
#endif
	}
}