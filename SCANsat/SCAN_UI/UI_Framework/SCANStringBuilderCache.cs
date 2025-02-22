using System;
using System.Text;

namespace SCANsat.SCAN_UI.UI_Framework
{
	public static class SCANStringBuilderCache
	{
		[ThreadStatic]
		private static StringBuilder CachedBuilder;

		public static StringBuilder Acquire(int capacity = 64)
		{
			if (capacity <= 360)
			{
				StringBuilder cached = CachedBuilder;

				if (cached != null && capacity <= cached.Capacity)
				{
					CachedBuilder = null;
					cached.Length = 0;
					return cached;
				}
			}

			return new StringBuilder(capacity);
		}

		public static void SCANRelease(this StringBuilder sb)
		{
			if (sb.Capacity <= 360)
			{
				CachedBuilder = sb;
			}
		}

		public static string SCANToStringAndRelease(this StringBuilder sb)
		{
			string s = sb.ToString();
			sb.Release();
			return s;
		}
	}
}
