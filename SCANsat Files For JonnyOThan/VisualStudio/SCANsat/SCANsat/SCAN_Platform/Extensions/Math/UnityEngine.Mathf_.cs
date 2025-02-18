using System;
namespace UnityEngine
{
	public static class Mathf_
	{

		public static float Mathf_Round(this float f, int precision)
		{
			if (precision < -4 || precision > 4)
				throw new ArgumentOutOfRangeException("[SCANsat] Precision Must Be Between -4 And 4 For Rounding Operation");

			if (precision >= 0)
				return (float)Math.Round(f, precision);
			else
			{
				precision = (int)Math.Pow(10, Math.Abs(precision));
				if (f >= 0)
					f += (5 * precision / 10);
				else
					f -= (5 * precision / 10);
				return (float)Math.Round(f - (f % precision), 0);
			}
		}
	}
}

