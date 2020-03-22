//Copyright 2020 Davin Carten, All rights reserved

using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities.GenericHitscan;
using emotitron.Utilities.GhostWorlds;
using emotitron.Utilities.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	public class SyncHitscan : SyncNetHitBase
	{

		public override int ApplyOrder { get { return ApplyOrderConstants.HITSCAN; } }

		#region Inspector

		public HitscanDefinition hitscanDefinition;

		[Tooltip("Render widgets that represent the shape of the hitscan when triggered.")]
		public bool visualizeHitscan = true;

		#endregion

		#region Initialization

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			triggerKey = KeyCode.R;
			
		}
#endif

		protected override void PopulateFrames()
		{
			frames = new WeaponFrame[frameCount + 1];
			for (int i = 0; i <= frameCount; ++i)
				frames[i] = new WeaponFrame(this, hitscanDefinition.nearestOnly, i);
		}

		#endregion

		#region Trigger

		/// <summary>
		/// Calls the actual generic hitscan and gets the results. Results are stored to the frame.
		/// </summary>
		/// <param name="frame"></param>
		protected override void Trigger(WeaponFrame frame, int subFrameId)
		{
			//Debug.Log(name + " Triggered");
			NetworkHits results = frame.netHits[subFrameId];
			List<NetworkHit> hits = results.hits;

			hitscanDefinition.GenericHitscanNonAlloc(origin, ref hits, ref results.nearestIndex, Realm.Primary, visualizeHitscan);

			HitsCallbacks(results);
			//TriggerCosmetic();
		}
		
		/// <summary>
		/// Override this method and insert your own hitscan cosmentics/audio triggers here.
		/// </summary>
		protected virtual void TriggerCosmetic()
		{

		}

#endregion
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(SyncHitscan))]
	[CanEditMultipleObjects]
	public class SyncHitscanEditor : SyncNetHitBaseEditor
	{
		protected override string Instructions
		{
			get
			{
				return "Attach this component to any root or child GameObject to define a networkable hitscan. " +
					"A NetObject is required on this object or a parent.\n\n" +
					"Initiate a hitscan by calling:\n" +
					"this" + typeof(SyncHitscan).Name + ".QueueTrigger()";
			}
		}

		protected override string HelpURL
		{
			get
			{
				return "";
			}
		}

		protected override string TextTexturePath
		{
			get
			{
				return "Header/SyncHitscanText";
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if ((target as SyncHitscan).visualizeHitscan && !EditorUserBuildSettings.development)
				EditorGUILayout.HelpBox("Hitscan visualizations will not appear in release builds. 'Development Build' in 'Build Settings' is currently unchecked.", MessageType.Error);
		}
	}
#endif
}
