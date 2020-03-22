

using System.Collections.Generic;

namespace emotitron.Networking
{
	public static class ApplyOrderConstants
	{
		public const int MAX_ORDER_VAL = 15;
#if UNITY_EDITOR
		public const string TOOLTIP =
			"Manually set the order in which callbacks occur. When components share a order value, " +
			"they will execute in the order in which they exist in the GameObjects hierarchy." +
			"It is recommended you leave this setting at the default, as strange behavior can result with some component orders.";

		public static Dictionary<System.Type, int> applyOrderForType = new Dictionary<System.Type, int>();
#endif
		public const int COLLISIONS = 2;
		public const int STATES = 3;
		public const int TRANSFORM = 4;
		public const int ANIMATOR = 5;
		public const int DEFAULT = 7;
		public const int VITALS = 9;
		public const int HITSCAN = 11;
		public const int WEAPONS = 13;

	}
}
