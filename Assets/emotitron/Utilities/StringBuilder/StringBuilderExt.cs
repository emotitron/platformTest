
#if DEBUG || UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Text;
using emotitron.Utilities.SmartVars;

namespace emotitron.Utilities
{
	public static class StringBuilderExt
	{
		///public static StringBuilder reusableStringBuilder = new StringBuilder();

		public static StringBuilder _(this StringBuilder sb, object text)
		{
			return sb.Append(text.ToString());
		}
		public static StringBuilder _(this StringBuilder sb, SmartVar text)
		{
			return sb.Append(text.ToString());
		}

		public static StringBuilder _B(this StringBuilder sb, string text)
		{
			return sb.Append("<b>").Append(text.ToString()).Append("</b>");
		}
		public static StringBuilder _B(this StringBuilder sb, SmartVar text)
		{
			return sb.Append("<b>").Append(text.ToString()).Append("</b>");
		}

		public static StringBuilder _I(this StringBuilder sb, string text)
		{
			return sb.Append("<i>").Append(text.ToString()).Append("</i>");
		}
		public static StringBuilder _I(this StringBuilder sb, SmartVar text)
		{
			return sb.Append("<i>").Append(text.ToString()).Append("</i>");
		}

		public static StringBuilder _IB(this StringBuilder sb, string text)
		{
			return sb.Append("<i><b>").Append(text.ToString()).Append("</b></i>");
		}
		public static StringBuilder _IB(this StringBuilder sb, SmartVar text)
		{
			return sb.Append("<i><b>").Append(text.ToString()).Append("</b></i>");
		}

		public static StringBuilder _Mag(this StringBuilder sb, string text)
		{
			return sb.Append("<color=magenta>").Append(text.ToString()).Append("</color>");
		}
		public static StringBuilder _Mag(this StringBuilder sb, SmartVar text)
		{
			return sb.Append("<color=magenta>").Append(text.ToString()).Append("</color>");
		}

		public static StringBuilder _Cyn(this StringBuilder sb, string text)
		{
			return sb.Append("<color=cyan>").Append(text.ToString()).Append("</color>");
		}
		public static StringBuilder _Cyn(this StringBuilder sb, SmartVar text)
		{
			return sb.Append("<color=cyan>").Append(text.ToString()).Append("</color>");
		}

		public static StringBuilder _Red(this StringBuilder sb, string text)
		{
			return sb.Append("<color=red>").Append(text.ToString()).Append("</color>");
		}
		public static StringBuilder _Red(this StringBuilder sb, SmartVar text)
		{
			return sb.Append("<color=red>").Append(text.ToString()).Append("</color>");
		}

		public static StringBuilder _Grn(this StringBuilder sb, string text)
		{
			return sb.Append("<color=green>").Append(text.ToString()).Append("</color>");
		}
		public static StringBuilder _Grn(this StringBuilder sb, SmartVar text)
		{
			return sb.Append("<color=green>").Append(text.ToString()).Append("</color>");
		}

		public static StringBuilder _Blu(this StringBuilder sb, string text)
		{
			return sb.Append("<color=blue>").Append(text.ToString()).Append("</color>");
		}
		public static StringBuilder _Blu(this StringBuilder sb, SmartVar text)
		{
			return sb.Append("<color=blue>").Append(text.ToString()).Append("</color>");
		}

		public static StringBuilder _B_Mag(this StringBuilder sb, string text)
		{
			return sb.Append("<b><color=magenta>").Append(text.ToString()).Append("</color></b>");
		}
		public static StringBuilder _B_Mag(this StringBuilder sb, SmartVar text)
		{
			return sb.Append("<b><color=magenta>").Append(text.ToString()).Append("</color></b>");
		}

		public static StringBuilder _B_Cyn(this StringBuilder sb, string text)
		{
			return sb.Append("<b><color=cyan>").Append(text.ToString()).Append("</color></b>");
		}
		public static StringBuilder _B_Cyn(this StringBuilder sb, SmartVar text)
		{
			return sb.Append("<b><color=cyan>").Append(text.ToString()).Append("</color></b>");
		}

		public static StringBuilder _B_Red(this StringBuilder sb, string text)
		{
			return sb.Append("<b><color=red>").Append(text.ToString()).Append("</color></b>");
		}
		public static StringBuilder _RedB_(this StringBuilder sb, SmartVar text)
		{
			return sb.Append("<b><color=red>").Append(text.ToString()).Append("</color></b>");
		}

		public static StringBuilder _B_Grn(this StringBuilder sb, string text)
		{
			return sb.Append("<b><color=green>").Append(text.ToString()).Append("</color></b>");
		}
		public static StringBuilder _GrnB_(this StringBuilder sb, SmartVar text)
		{
			return sb.Append("<b><color=green>").Append(text.ToString()).Append("</color></b>");
		}

		public static StringBuilder _B_Blu(this StringBuilder sb, string text)
		{
			return sb.Append("<b><color=blue>").Append(text.ToString()).Append("</color></b>");
		}
		public static StringBuilder _B_Blu(this StringBuilder sb, SmartVar text)
		{
			return sb.Append("<b><color=blue>").Append(text.ToString()).Append("</color></b>");
		}

	}

}

#endif