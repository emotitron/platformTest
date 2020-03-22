using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Utilities.HitGroups
{
	[System.Serializable]
	public struct HitGroupMaskSelector : IHitGroupMask
	{
		/// <summary>
		/// Default is an index of 0 and a layermask of 0
		/// </summary>
		[SerializeField] private int mask;
		public int Mask { get {return mask;} set { mask = value; } }

#if UNITY_EDITOR
		public bool expanded;
#endif

		public HitGroupMaskSelector(int mask)
		{
			this.mask = mask;
#if UNITY_EDITOR
			expanded = true;
#endif
	}

	public static implicit operator int(HitGroupMaskSelector selector)
		{
			return selector.mask;
		}

		public static implicit operator HitGroupMaskSelector(int mask)
		{
			return new HitGroupMaskSelector(mask);
		}

	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(HitGroupMaskSelector))]
	[CanEditMultipleObjects]
	public class HitGroupMaskSelectorDrawer : VersaMaskDrawer
	{
		protected override bool FirstIsZero
		{
			get
			{
				return true;
			}
		}
		protected override string[] StringNames
		{
			get
			{
				return HitGroupSettings.Single.hitGroupTags.ToArray();
			}
		}
	}

#endif
}

