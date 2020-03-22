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

	public class AutoOwnerGameObjEnable : MonoBehaviour
		, IOnAuthorityChanged
	{
		public enum EnableIf { Owner, Other }

		public EnableIf enableIf = EnableIf.Owner;

		public void Start()
		{
#if PUN_2_OR_NEWER
			var pv = GetComponentInParent<PhotonView>();

			if (pv)
				SwitchAuth(pv.IsMine);
#endif
		}

		public void OnAuthorityChanged(bool isMine, bool asServer)
		{
			Debug.Log("AuthChanged");
			SwitchAuth(isMine);
		}

		private void SwitchAuth(bool isMine)
		{
			gameObject.SetActive(enableIf == EnableIf.Owner ? isMine : !isMine);
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(AutoOwnerGameObjEnable))]
	internal class AutoOwnerGameObjEnableEditor : HeaderEditorBase
	{
		SerializedProperty componentToggles;

		protected override string Instructions
		{
			get
			{
				return "Automatically enables and disables this GameObject based on NetObject ownership.";
			}
		}

		protected override string TextTexturePath
		{
			get
			{
				return "Header/UtilityText";
			}
		}

		protected override string BackTexturePath
		{
			get
			{
				return "Header/BlueGridBack";
			}
		}
		public override void OnEnable()
		{
			base.OnEnable();
		}

		
	}
#endif
}
