#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN Palette - object to hold information about a color palette
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using UnityEngine;

namespace SCANsat.SCAN_Platform.Palettes
{
	//[Serializable]
	public struct Palette
	{
		public string name;
		public Kind kind;
		public int size;
		public int index;
		public string hash;

		public Color32[] colors;
		public Color32[] colorsReverse;

		public enum Kind {
			Diverging,
			Qualitative,
			Sequential,
			Fixed,
			Invertable,
			Unknown,
		}

		private static string[] getKindNames()
		{
			Kind[] v = (Kind[])Enum.GetValues(typeof(Kind));
			string[] r = new string[v.Length - 2];
			for (int i = 0; i < v.Length - 2; ++i)
				r[i] = v[i].ToString();
			return r;
		}

		public static string[] kindNames = getKindNames();

		public enum Is : ushort { Unsafe = 0, Safe = 1, Unsure = 2, Unknown = 3 }

		public Is blind;
		public Is print;
		public Is xerox;
		public Is panel;

		public Palette(Color32[] cs, string name, Kind k, Is blindSafe, Is printSafe, Is xeroxSafe, Is panelSafe) {
			this.colors = cs;
			this.colorsReverse = new Color32[cs.Length];
			this.colors.CopyTo(this.colorsReverse, 0);
			Array.Reverse(this.colorsReverse);
			this.name = name;
			this.kind = k;
			this.blind = blindSafe;
			this.print = printSafe;
			this.xerox = xeroxSafe;
			this.panel = panelSafe;
			this.size = cs.Length;
			this.index = 0;
			this.hash = name + size;
		}
	}

}

