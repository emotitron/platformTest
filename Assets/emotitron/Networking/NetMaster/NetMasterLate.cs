// Copyright 2019, Davin Carten, All rights reserved
// This code may only be used in game development, but may not be used in any tools or assets that are sold or made publicly available to other developers.

using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using emotitron.Utilities;
#endif

namespace emotitron.Networking
{
	public class NetMasterLate : MonoBehaviour
	{
		/// <summary>
		/// Singleton instance of the NetMaster. "There can be only one."
		/// </summary>
		public static NetMasterLate single;

		private static List<IOnPostUpdate> onPostUpdateCallbacksCached;
		private static List<IOnPostLateUpdate> onPostLateUpdateCallbacksCached;

		private void Awake()
		{
			if (single && single != this)
			{
				/// If a singleton already exists, destroy the old one - TODO: Not sure about this behaviour yet. Allows for settings changes with scene changes.
				Destroy(single);
			}

			single = this;

			onPostUpdateCallbacksCached = NetMaster.onPostUpdates;
			onPostLateUpdateCallbacksCached = NetMaster.onPostLateUpdates;

			DontDestroyOnLoad(this);
		}


		private void FixedUpdate()
		{
			int cnt = NetMaster.onPreSimulates.Count;
			for (int i = 0; i < cnt; ++i)
				NetMaster.onPreSimulates[i].OnPreSimulate(NetMaster.CurrentFrameId, NetMaster.CurrentSubFrameId);
		}

		private void Update()
		{
			int cnt = onPostUpdateCallbacksCached.Count;
			for (int i = 0; i < cnt; ++i)
				onPostUpdateCallbacksCached[i].OnPostUpdate();
		}

		private void LateUpdate()
		{
			int cnt = onPostLateUpdateCallbacksCached.Count;
			for (int i = 0; i < cnt; ++i)
				onPostLateUpdateCallbacksCached[i].OnPostLateUpdate();

			NetMaster.ApplyQueuedRegistrations();
		}

	}

#if UNITY_EDITOR
	[CustomEditor(typeof(NetMasterLate))]
	public class NetMasterLateEditor : NetCoreHeaderEditor
	{
		protected override string BackTexturePath
		{
			get
			{
				return "Header/RedBack";
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("Late Timing singleton used by all Simple Network Sync components. " +
				"Effectively a tiny networking specific Update Manager.\n\n" +
				"This component will be added automatically at runtime if one does not exist in your scene.\n\n" +
				"This component should be operating on the latest possible Script Execution timing, " +
				"in order to produce Fixed/Late/Update callbacks after all other components have run. "
				, MessageType.None);
		}
	}

#endif
}
