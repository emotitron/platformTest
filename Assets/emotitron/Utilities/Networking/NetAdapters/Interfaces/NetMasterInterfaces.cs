using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Networking
{
	//public delegate void OnPreUpdateDelegate();
	public interface IOnPreUpdate { void OnPreUpdate(); }
	public interface IOnPostUpdate { void OnPostUpdate(); }

	public interface IOnPreLateUpdate { void OnPreLateUpdate(); }
	public interface IOnPostLateUpdate { void OnPostLateUpdate(); }

	//public delegate void OnCaptureInputsDelegate(int frameId, int subFrameId);
	public interface IOnCaptureInputs { void OnCaptureInputs(int frameId, int subFrameId); }

	//public delegate void OnPreSimulateDelegate(int frameId, int subFrameId);
	public interface IOnPreSimulate { void OnPreSimulate(int frameId, int subFrameId); }

	//public delegate void OnPostSimulateDelegate(int frameId, int subFrameId);
	public interface IOnPostSimulate { void OnPostSimulate(int frameId, int subFrameId, bool isNetTick); }

	//public delegate void OnIncrementFrameDelegate(int newFrameId, int newSubFrameId, int previousFrameId, int prevSubFrameId);
	public interface IOnIncrementFrame { void OnIncrementFrame(int newFrameId, int newSubFrameId, int previousFrameId, int prevSubFrameId); }

	//public delegate void OnSnapshotAction(int frameId, bool initialization);
	//public interface IOnSnapshot { void OnSnapshot(int frameId); }

	//public delegate void OnQuitDelegate();
	public interface IOnPreQuit { void OnPreQuit(); }

}
