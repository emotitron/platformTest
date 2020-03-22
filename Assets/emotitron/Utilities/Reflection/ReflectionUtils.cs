using System;
using System.Collections.Generic;

namespace emotitron.Utilities.Reflection
{
	public static class ReflectionUtils
	{

		public static bool IsList(this Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
		}
	}

}
