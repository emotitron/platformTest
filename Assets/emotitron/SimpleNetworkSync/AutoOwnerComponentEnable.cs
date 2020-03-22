using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if PUN_2_OR_NEWER
using Photon.Pun;
#endif

#if UNITY_EDITOR
using UnityEditor;
using emotitron.Utilities;
#endif

namespace emotitron.Networking
{
	public class AutoOwnerComponentEnable : NetComponent
		, IOnAuthorityChanged
	{
		public enum EnableIf { Ignore, Owner, Other }

		[System.Serializable]
		public class ComponentToggle
		{
			public MonoBehaviour component;
			public EnableIf enableIfOwned = EnableIf.Owner;
		}

		[HideInInspector] [SerializeField] private List<ComponentToggle> componentToggles = new List<ComponentToggle>();

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			componentToggles.Clear();
			FindUnrecognizedComponents();
		}
#endif

		public override void OnStart()
		{
			base.OnStart();

			SwitchAuth(IsMine);
		}

		public override void OnJoinedRoom()
		{
			base.OnJoinedRoom();
			SwitchAuth(IsMine);
		}

		public override void OnAuthorityChanged(bool isMine, bool asServer)
		{
			base.OnAuthorityChanged(isMine, asServer);
			SwitchAuth(isMine);
		}

		private void SwitchAuth(bool isMine)
		{
			for (int i = 0; i < componentToggles.Count; ++i)
			{
				var item = componentToggles[i];

				if (item != null && item.enableIfOwned != EnableIf.Ignore && item.component != null)
					item.component.enabled = (item.enableIfOwned == EnableIf.Owner) ? isMine : !isMine;
			}

		}

#if UNITY_EDITOR

		private static List<MonoBehaviour> components = new List<MonoBehaviour>();
		private static HashSet<MonoBehaviour> temp = new HashSet<MonoBehaviour>();
		public void FindUnrecognizedComponents()
		{
			temp.Clear();
			/// Cull any null components in the list
			int cnt = componentToggles.Count;
			for (int i = cnt - 1; i >= 0; --i)
			{
				var comp = componentToggles[i].component;
				if (comp == null || temp.Contains(comp))
					componentToggles.RemoveAt(i);
				else
					temp.Add(comp);
			}


			GetComponents(components);
			foreach (var comp in components)
			{
				if (comp == null)
					continue;

				var nspace = comp.GetType().Namespace;

				if (nspace == null || nspace == "" | nspace != typeof(MonoBehaviour).Namespace && !nspace.Contains("emotitron") && !nspace.Contains("Photon"))
				{
					if (!temp.Contains(comp))
						componentToggles.Add(new ComponentToggle() { component = comp, enableIfOwned = EnableIf.Owner });
				}
			}
		}
#endif
	}


#if UNITY_EDITOR
	[CustomEditor(typeof(AutoOwnerComponentEnable))]
	internal class AutoOwnerComponentEnableEditor : NetUtilityHeaderEditor
	{
		SerializedProperty componentToggles;

		protected override string Instructions
		{
			get
			{
				return "Automatically enables and disables components on a Net Object based on ownership. " +
					"Use this to disable controller code on non-authority instances " +
					"(which is a requirement for networking).";
			}
		}

		protected bool isExpanded = true;

		public override void OnEnable()
		{
			base.OnEnable();
			componentToggles = serializedObject.FindProperty("componentToggles");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			(target as AutoOwnerComponentEnable).FindUnrecognizedComponents();
			serializedObject.Update();

			RenderList(componentToggles);
		}

		private void RenderList(SerializedProperty list)
		{
			int cnt = list.arraySize;
			GUIContent foldoutlabel = new GUIContent("[" + cnt + "] Component" + ((cnt == 1) ? ":" : "s:"));

			var headerrect = EditorGUILayout.GetControlRect(true, 18);
			//headerrect.xMin += 12;

			isExpanded = EditorGUI.Foldout(headerrect, isExpanded, foldoutlabel,(GUIStyle)"Foldout");
			//EditorGUI.LabelField(headerrect, "[" + cnt + "] Component" + ((cnt == 1) ? ":" : "s:"), new GUIStyle() { padding = new RectOffset(6, 6, 2, 6) });
			EditorGUI.LabelField(headerrect, "Enable If:", new GUIStyle() { padding = new RectOffset(6, 6, 2, 6), alignment = TextAnchor.UpperRight });

			if (isExpanded)
			{

				int deleteIndex = -1;


				for (int i = 0; i < cnt; ++i)
				{
					var listitem = list.GetArrayElementAtIndex(i);
					var comp = listitem.FindPropertyRelative("component");

					EditorGUILayout.BeginVertical(new GUIStyle("HelpBox")/*{ padding = new RectOffset(6, 6, 6, 6) }*/);

					/// Row One
					{
						EditorGUILayout.BeginHorizontal();

						var obj = comp.objectReferenceValue;
						if (obj)
							EditorGUILayout.LabelField(comp.objectReferenceValue.GetType().Name, (GUIStyle)"BoldLabel");
						else
							EditorGUILayout.LabelField("none");

						EditorGUILayout.EndHorizontal();
					}

					/// Row Two
					{
						EditorGUILayout.BeginHorizontal();

						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField(comp, GUIContent.none, GUILayout.MinWidth(84));
						EditorGUILayout.PropertyField(listitem.FindPropertyRelative("enableIfOwned"), GUIContent.none, GUILayout.MaxWidth(64), GUILayout.MinWidth(48));

						if (EditorGUI.EndChangeCheck())
						{
							serializedObject.ApplyModifiedProperties();
						}
						EditorGUILayout.EndHorizontal();
					}
					EditorGUILayout.EndVertical();
				}

				/// Find / Add Buttons
				{
					EditorGUILayout.BeginHorizontal();
					if (GUI.Button(EditorGUILayout.GetControlRect(), "Find Components"))
					{
						Undo.RecordObject(target, "Find Components");

						(target as AutoOwnerComponentEnable).FindUnrecognizedComponents();
						serializedObject.Update();
					}

					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space();

				if (deleteIndex != -1)
				{
					Undo.RecordObject(target, "Delete List Item " + deleteIndex);
					list.DeleteArrayElementAtIndex(deleteIndex);
					serializedObject.ApplyModifiedProperties();
				}
			}
		}
	}
#endif
}
