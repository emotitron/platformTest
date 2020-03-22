using emotitron.Utilities.GUIUtilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{

#if UNITY_EDITOR
	[HelpURL(HELP_URL)]
#endif

	public class MountSettings : SettingsScriptableObject<MountSettings>
	{

		#region Inspector


		[HideInInspector]
		public List<string> mountNames = new List<string>()
		{
			"Root",

			"1", "2", "3", "4"
		};


		#endregion

		public static int mountTypeCount;
		public static int bitsForMountId;

		public override void Initialize()
		{
			base.Initialize();
			mountTypeCount = Single.mountNames.Count;
			bitsForMountId = Compression.FloatCrusher.GetBitsForMaxValue((uint)(mountTypeCount - 1));
		}

#if UNITY_EDITOR

		public const string HELP_URL = "";
		public override string HelpURL { get { return HELP_URL; } }

		public override string SettingsName { get { return "Mount Settings"; } }

		//private static HashSet<int> temphash = new HashSet<int>();

		//protected override void OnValidate()
		//{
		//	base.OnValidate();


		//	if (Application.isPlaying)
		//		return;

		//	int cnt = mountNames.Count;

		//	/// Cap number of mounts at 32
		//	if (cnt > 32)
		//	{
		//		mountNames.RemoveRange(cnt, cnt - 32);
		//		mountNames.TrimExcess();
		//	}
		//	else if (cnt == 0)
		//	{
		//		mountNames.Add("1");
		//	}

		//	///Ensure unique names
		//	temphash.Clear();
		//	for (int i = 0; i < mountNames.Count; ++i)
		//	{
		//		var name = mountNames[i];
		//		var trimmed = name.Trim();
		//		if (name != trimmed)
		//		{
		//			name = trimmed;
		//		}

		//		/// Make sure if the name is blank or an index number, it matches the actual index
		//		if (name == null || name == "")
		//		{
		//			mountNames[i] = (i).ToString();
		//		}
		//		else
		//		{
		//			int nameasint;
		//			if (int.TryParse(name, out nameasint))
		//			{
		//				if (nameasint <= 32 && nameasint > 0)
		//					mountNames[i] = (i).ToString();
		//			}
		//		}

		//		while (temphash.Contains(mountNames[i].GetHashCode()))
		//		{
		//			mountNames[i] += "X";
		//		}

		//		temphash.Add(mountNames[i].GetHashCode());
		//	}
		//}

		public override bool DrawGui(Object target, bool asFoldout, bool includeScriptField, bool initializeAsOpen = true, bool asWindow = false)
		{

			bool isExpanded = base.DrawGui(target, asFoldout, includeScriptField, initializeAsOpen, asWindow);


			if (isExpanded)
			{
				EditorGUI.BeginChangeCheck();

				SerializedObject soTarget = new SerializedObject(Single);

				var mountNames = soTarget.FindProperty("mountNames");
				int cnt = mountNames.arraySize;


				/// Make sure we have the required root / 0 entry.
				if (cnt == 0)
				{
					mountNames.InsertArrayElementAtIndex(0);
				}
				var rootElement = mountNames.GetArrayElementAtIndex(0);

				if (rootElement.stringValue != "Root")
					rootElement.stringValue = "Root";

				/// Draw the Mount Define box
				EditorGUILayout.LabelField("Defined Mounts:");
				EditorUtils.DrawEditableList(mountNames, true, "Mount");

				/// Save changes
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(Single, "Modify Mount Settings " + mountNames.arraySize);
					soTarget.ApplyModifiedProperties();

					/// Modify our cached values for the new settings
					MountSettings.mountTypeCount = mountNames.arraySize;
					MountSettings.bitsForMountId = Compression.FloatCrusher.GetBitsForMaxValue((uint)(mountNames.arraySize - 1));

					EditorUtility.SetDirty(this);
					AssetDatabase.SaveAssets();
				}

				/// Count slider and bits report
				EditorGUILayout.HelpBox(bitsForMountId + " bits for MountIds", MessageType.None);

			}

			return isExpanded;
		}

#endif

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void Bootstrap()
		{
			var single = Single;
		}


		
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(MountSettings))]
	[CanEditMultipleObjects]
	public class MountSettingsEditor : Editor
	{
		public override void OnInspectorGUI()
		{

			MountSettings.Single.DrawGui(target, false, false, true);
		}
	}
#endif
}
