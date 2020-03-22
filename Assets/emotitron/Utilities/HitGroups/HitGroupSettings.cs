//Copyright 2020, Davin Carten, All rights reserved

using System.Collections.Generic;
using emotitron.Utilities.GUIUtilities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Utilities.HitGroups
{

#if UNITY_EDITOR
	[HelpURL(HELP_URL)]
#endif

	public class HitGroupSettings : SettingsScriptableObject<HitGroupSettings>
	{
		public static bool initialized;
		public const string DEF_NAME = "Default";

		[HideInInspector]
		public List<string> hitGroupTags = new List<string>(2) { DEF_NAME, "Critical" };
		public Dictionary<string, int> rewindLayerTagToId = new Dictionary<string, int>();

		[System.NonSerialized]
		public static int bitsForMask;

#if UNITY_EDITOR
		public override string SettingsName { get { return "Hit Group Settings"; } }
#endif

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void Bootstrap()
		{
			var single = Single;
		}

		public override void Initialize()
		{
			single = this;
			base.Initialize();

			if (initialized)
				return;

			initialized = true;

			bitsForMask = hitGroupTags.Count - 1;

			// populate the lookup dictionary
			for (int i = 0; i < hitGroupTags.Count; i++)
				if (rewindLayerTagToId.ContainsKey(hitGroupTags[i]))
				{
					Debug.LogError("The tag '" + hitGroupTags[i] + "' is used more than once in '" + GetType().Name + "'. Repeats will be discarded, which will likely break some parts of rewind until they are removed.");
				}
				else
				{
					rewindLayerTagToId.Add(hitGroupTags[i], i);
				}

			//XDebug.Log(!XDebug.logInfo ? null : ("Initialized HitGroupMasterSettings - Total Layer Tags Count: " + hitGroupTags.Count));
		}

		/// <summary>
		/// Supplied a previous index and hitgroup name, and will return the index of the best guess in the current list of layer tags. First checks for name,
		/// then if the previous int still exists, if none of the above returns 0;
		/// </summary>
		/// <returns></returns>
		[System.Obsolete("Left over from NST, likely not useful any more.")]
		public static int FindClosestMatch(string n, int id)
		{
			var hgs = Single;

			if (hgs.hitGroupTags.Contains(n))
				return hgs.hitGroupTags.IndexOf(n);
			if (id < hgs.hitGroupTags.Count)
				return id;

			return 0;
		}
#if UNITY_EDITOR
		//private bool expanded = true;

		public const string HELP_URL = "";
		public override string HelpURL { get { return HELP_URL; } }

		public static string instructions = "These tags are used by " + typeof(HitGroupAssign).Name + " to assign colliders to hitbox groups, for things like headshots and critical hits.";


		int prevHitGroupCount = -1;
		string summaryString;

		public override bool DrawGui(Object target, bool asFoldout, bool includeScriptField, bool initializeAsOpen = true, bool asWindow = false)
		{
			

			bool isExpanded = base.DrawGui(target, asFoldout, includeScriptField, initializeAsOpen, asWindow);
			//bool isHierarchyMode = EditorGUIUtility.hierarchyMode;

			if (!asFoldout || isExpanded)
			{
				EditorGUI.BeginChangeCheck();

				SerializedObject soTarget = new SerializedObject(Single);
				
				SerializedProperty tags = soTarget.FindProperty("hitGroupTags");

				EditorGUILayout.PrefixLabel("Defined Groups:");

				EditorUtils.DrawEditableList(tags, true, "Group");

				EditorGUILayout.HelpBox(instructions, MessageType.None);

				int hitgroupCount = hitGroupTags.Count;

				if (prevHitGroupCount != hitgroupCount)
					summaryString = (hitGroupTags.Count - 1) + " bits per hit used for hitmasks.";

				EditorGUILayout.HelpBox(summaryString, MessageType.None);

				prevHitGroupCount = hitgroupCount;

				/// Save changes
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(Single, "Modify HitGroupSettings ");
					soTarget.ApplyModifiedProperties();

					EditorUtility.SetDirty(this);
					AssetDatabase.SaveAssets();
				}
				
			}
			return isExpanded;
		}

		public static void DrawLinkToSettings()
		{
			Rect r = EditorGUILayout.GetControlRect(false, 48f);

			GUI.Box(r, GUIContent.none, (GUIStyle)"HelpBox");

			float padding = 4;
			float line = r.yMin + padding;
			float width = r.width - padding * 2;

			GUI.Label(new Rect(r.xMin + padding, line, width, 16), "Add/Remove Hit Box Groups here:", (GUIStyle)"MiniLabel");
			line += 18;

			if (GUI.Button(new Rect(r.xMin + padding, line, width, 23), new GUIContent("Find Hit Group Settings")))
			{
				EditorGUIUtility.PingObject(HitGroupSettings.Single);
			}
		}
#endif
	}


#if UNITY_EDITOR

	[CustomEditor(typeof(HitGroupSettings))]
	[CanEditMultipleObjects]
	public class HitGroupSettingsEditor : Editor
	{

		public override void OnInspectorGUI()
		{
			HitGroupSettings.Single.DrawGui(target, false, false, true);
		}
	}
#endif
}

