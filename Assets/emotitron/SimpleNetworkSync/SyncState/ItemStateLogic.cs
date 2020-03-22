//Copyright 2019, Davin Carten, All rights reserved

using emotitron.Utilities.GUIUtilities;

namespace emotitron.Networking
{
	[System.Serializable]
	public class ObjStateLogic : MaskLogic
	{
		protected static int[] stateValues = (int[])System.Enum.GetValues(typeof(ObjState));
		protected static string[] stateNames = System.Enum.GetNames(typeof(ObjState));

		protected override bool DefinesZero { get { return true; } }
		protected override string[] EnumNames { get { return stateNames; } }
		protected override int[] EnumValues { get { return stateValues; } }
		protected override int DefaultValue { get { return (int)ObjState.Visible; } }

#if UNITY_EDITOR
		protected override string EnumTypeName { get { return typeof(ObjState).Name; } }
#endif


	}
}
