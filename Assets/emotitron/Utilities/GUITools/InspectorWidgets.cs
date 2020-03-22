
#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace emotitron.Utilities.GUIUtilities
{
	public class InspectorWidgets : MonoBehaviour {

		/// <summary>
		/// Draw left mini-toggle.
		/// </summary>
		/// <returns>Returns if toggle has changed.</returns>
		public static bool MiniToggle(Object t, Rect r, GUIContent label, ref bool b, bool lockedOn = false)
		{
			EditorGUI.LabelField(new Rect(r.xMin + 16, r.yMin, r.width - 16, r.height), label, (GUIStyle)"MiniLabel");

			if (lockedOn)
				EditorGUI.BeginDisabledGroup(true);

			bool newb = EditorGUI.Toggle(new Rect(r.xMin, r.yMin, 32, r.height), "", b,  (GUIStyle)"OL Toggle");

			if (lockedOn)
				EditorGUI.EndDisabledGroup();

			bool haschanged = b != newb;
			if (haschanged)
			{
				Undo.RecordObject(t, "Toggle");
				b = newb;
			}

			return haschanged;
		}


		public static Rect GetIndentedControlRect(int indent)
		{
			Rect r = EditorGUILayout.GetControlRect();
			r.xMin = r.xMin + indent;
			r.width = r.width - indent;
			return r;
		}
	}
}

#endif

