
using UnityEngine;
using emotitron.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	public class EnableOnNetObjReady : MonoBehaviour
		, IOnNetObjReady
	{
		
		public GameObject visibilityObject;

		public void Reset()
		{
			visibilityObject = gameObject;
		}

		public void Awake()
		{
			if (visibilityObject == null)
				visibilityObject = gameObject;
		}

		private void Start()
		{
			NetObject no = GetComponentInParent<NetObject>();
			if (no)
				visibilityObject.SetActive(no.AllObjsAreReady);
		}

		public void OnNetObjReadyChange(bool ready)
		{
			if (visibilityObject == null)
				visibilityObject = gameObject;

			visibilityObject.SetActive(ready);

			if (ready)
				Debug.Log(Time.time + " <b>APPEAR</b>");
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(EnableOnNetObjReady))]
	[CanEditMultipleObjects]
	public class EnableOnNetObjReadyEditor : ReactorHeaderEditor
	{
		protected override string Instructions
		{
			get
			{
				return "Automaticaly enables and disables GameObject based on the Ready state of NetObject.";
			}
		}

		//public override void OnInspectorGUI()
		//{
		//	base.OnInspectorGUI();

		//	EditorGUILayout.LabelField("<b>OnNetObjReady()</b> { Enable Object }", richLabel);
		//}
	}

#endif
}

