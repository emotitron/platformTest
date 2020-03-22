using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace emotitron.Utilities.GUIUtilities
{
	public class VersaMaskAttribute : PropertyAttribute
	{
		public bool definesZero;
		public Type enumType;

		public VersaMaskAttribute(Type enumType, bool definesZero = false)
		{
			this.definesZero = definesZero;
			this.enumType = enumType;
		}

	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(VersaMaskAttribute))]
	public class VersaMaskAttributeDrawer : VersaMaskDrawer
	{
		protected override bool FirstIsZero
		{
			get
			{
				var attr = attribute as VersaMaskAttribute;
				return attr.definesZero;
			}
		}
		protected override string[] StringNames
		{
			get
			{
				var attr = attribute as VersaMaskAttribute;
				var names = System.Enum.GetNames(attr.enumType);
				return names;
			}
		}
	}
#endif
}

