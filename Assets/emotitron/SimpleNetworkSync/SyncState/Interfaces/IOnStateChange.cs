using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Networking
{
	public interface IOnStateChange
	{
		void OnStateChange(ObjState state, Transform attachmentTransform, Mount attachTo = null, bool isReady = true);
	}
}

