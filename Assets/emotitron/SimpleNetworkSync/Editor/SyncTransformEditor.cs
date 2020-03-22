// Copyright 2019, Davin Carten, All rights reserved
// This code may only be used in game development, but may not be used in any tools or assets that are sold or made publicly available to other developers.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	[CustomEditor(typeof(SyncTransform))]
	[CanEditMultipleObjects]
	public class SyncTransformEditor : SyncObjectTFrameEditor
	{
		protected override string Instructions
		{
			get
			{
				return "Attach this component to any root or child GameObject to sync its transform over the network. The root GameObject must be networked (has a NetworkIdentity or PhotonView).";
			}
		}

		protected override string HelpURL
		{
			get
			{
				return "https://docs.google.com/document/d/1ySmkOBsL0qJnIk7iN9lbXPlfmYTGkN7JFgKDBdqj9e8/edit#bookmark=id.jckx89d02ueb";
			}
		}

		protected override string TextTexturePath
		{
			get
			{
				return "Header/SyncTransformText";
			}
		}

		//public override void OnInspectorGUI()
		//{
		//	base.OnInspectorGUI();
		//}

		protected override void OnInspectorGUIInjectMiddle()
		{
			base.OnInspectorGUIInjectMiddle();

			/// Warn that a component may be playing with settings
			var iautosync = (target as Component).GetComponent<ITransformController>();
			if (!ReferenceEquals(iautosync, null))
			{
				if (iautosync.AutoSync)
				{
					EditorGUILayout.HelpBox((iautosync as Component).GetType().Name + " has AutoSync enabled, and is managing some crusher settings.", MessageType.Info);
				}
			}
		}
	}
}

