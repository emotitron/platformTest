//Copyright 2019, Davin Carten, All rights reserved

using UnityEngine;
using emotitron.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{

	public enum DisplayToggle { GameObject, Component, Renderer }

	public class OnStateChangeToggle : NetComponent
	, IOnStateChange
	{

		[HideInInspector]
		[Tooltip("How this object should be toggled. GameObject toggles gameObject.SetActive(), Renderer toggles renderer.enabled, and Component toggles component.enabled.")]
		public DisplayToggle toggle = DisplayToggle.GameObject;

		[Tooltip("User specified component to toggle enabled.")]
		[HideInInspector]
		public Component component;

		[HideInInspector]
		public GameObject _gameObject;

		[HideInInspector]
		public Renderer _renderer;

		[HideInInspector]
		public ObjStateLogic stateLogic = new ObjStateLogic();

		// Cached
		bool reactToAttached;
		MonoBehaviour monob;

#if UNITY_EDITOR

		[HideInInspector]
		[Utilities.GUIUtilities.VersaMask(typeof(ObjState), true)]
		public ObjState currentState;

		
		protected override void Reset()
		{
			_gameObject = gameObject;
			_renderer = GetComponent<Renderer>();
		}

#endif
		public override void OnAwake()
		{
			base.OnAwake();
		
			if (toggle == DisplayToggle.Renderer)
			{
				if (_renderer == null)
					_renderer = GetComponent<Renderer>();
			}
			else if (toggle == DisplayToggle.Component)
			{
				monob = component as MonoBehaviour;
			}
			else
			{
				if (_gameObject == null)
					_gameObject = gameObject;
			}

			stateLogic.RecalculateMasks();

			reactToAttached = (((stateLogic.notMask & (int)ObjState.Attached) == 0) && (stateLogic.stateMask & (int)ObjState.Attached) != 0);

		}
		
		public void OnStateChange(ObjState state, Transform pickup, Mount attachedTo = null, bool isReady = true)
		{

#if UNITY_EDITOR
			currentState = state;
#endif
			bool show;

			if (!isReady)
			{
				//Debug.Log(name + " not ready!");
				show = false;
			}
			else
			{
				bool match = stateLogic.Evaluate((int)state);

				if (match)
				{
					show = true;

					/// If there is no object to attach to yet (due to keyframes) we need to keep this invisible.
					if (reactToAttached)
						if (attachedTo == null)
							show = false;
				}
				else
					show = false;
			}

			//Debug.Log(name + " <b>toggle!</b>" + toggle + " " + show);

			switch (toggle)
			{

				case DisplayToggle.GameObject:
					{
						_gameObject.SetActive(show);
						break;
					}

				case DisplayToggle.Component:
					{
						if (monob)
							monob.enabled = show;
						break;
					}

				case DisplayToggle.Renderer:
					{
						if (_renderer)
							_renderer.enabled = show;
						break;
					}
			}
		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(OnStateChangeToggle))]
	[CanEditMultipleObjects]
	public class OnStateChangeToggleEditor : ReactorHeaderEditor
	{

		protected override string Instructions
		{
			get { return "Ties object toggles to OnStateChange callbacks."; }
		}

		protected int[] stateValues = (int[])System.Enum.GetValues(typeof(ObjState));
		protected string[] stateNames = System.Enum.GetNames(typeof(ObjState));

		protected SerializedProperty stateMask, notMask;
		protected SerializedProperty toggle, operation, currentState;

		public override void OnEnable()
		{
			base.OnEnable();
			stateMask = serializedObject.FindProperty("stateMask");
			notMask = serializedObject.FindProperty("notMask");
			toggle = serializedObject.FindProperty("toggle");
			operation = serializedObject.FindProperty("operation");
			currentState = serializedObject.FindProperty("currentState");
			currentState.isExpanded = true;
		}

		public override void OnInspectorGUI()
		{
			
			base.OnInspectorGUI();
			var _target = target as OnStateChangeToggle;

			

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(toggle);
			
			if (toggle.enumValueIndex == (int)DisplayToggle.Component)
				EditorGUILayout.PropertyField(serializedObject.FindProperty("component"));
			else if (toggle.enumValueIndex == (int)DisplayToggle.GameObject)
				EditorGUILayout.PropertyField(serializedObject.FindProperty("_gameObject"));
			else if (toggle.enumValueIndex == (int)DisplayToggle.Renderer)
				EditorGUILayout.PropertyField(serializedObject.FindProperty("_renderer"));

			_target.stateLogic.DrawGUI(serializedObject.FindProperty("stateLogic"));


			if (_target.GetComponent<NetObject>())
				EditorGUILayout.HelpBox("<b>NetObject detected on this GameObject!</b>\n\nThis component OnPickup will disable the entire net object (including the respawn timer), which is likely unintentional." +
					" Make the NetObject root an empty object and put the mesh and this component on a child instead, so that networked object remains active.", MessageType.Warning);

			if (Application.isPlaying)
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.PropertyField(currentState);
				EditorGUI.EndDisabledGroup();
			}

		}
	}
#endif
}
