#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANPalette - Class to hold color palette info
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using UnityEngine;
using SCANsat.SCAN_Platform;

namespace SCANsat.SCAN_Palettes
{
	public class SCANPalette : SCAN_ConfigNodeStorage
	{
		[Persistent]
		private string Colors;

		private int _count;
		private int _hash;
		private string _name;
		private SCANPaletteKind _kind;
		private Color32[] _colors;
		private Color32[] _colorsReverse;

		public int Count
		{
			get { return _count; }
		}

		public int Hash
		{
			get { return _hash; }
		}

		public string Name
		{
			get { return _name; }
			set
			{
				_name = value;

				_hash = (_name + _count).GetHashCode();
			}
		}

		public SCANPaletteKind Kind
		{
			get { return _kind; }
			set { _kind = value; }
		}

		public Color32[] ColorsArray
		{
			get { return _colors; }
		}

		public Color32[] ColorsReverse
		{
			get { return _colorsReverse; }
		}

		public SCANPalette() { }

		public SCANPalette(Color32[] colors, string name, SCANPaletteKind kind, int size)
		{
			_name = name;
			_count = size;
			_kind = kind;
			_hash = (name + size).GetHashCode();

			_colors = colors;
			_colorsReverse = new Color32[_count];
			_colors.CopyTo(_colorsReverse, 0);
			Array.Reverse(_colorsReverse);
		}

		public override void OnDecodeFromConfigNode()
		{
			string[] split = Colors.Split('|');

			_count = split.Length;
			_colors = new Color32[_count];

			for (int i = 0; i < _count; i++)
			{
				string color = split[i];
				Color32 c = Color.clear;

				try
				{
					c = ConfigNode.ParseColor32(color);
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error while parsing SCANsat palette colors - {0}\n{1}", color, e);
				}

				_colors[i] = c;
			}

			_colorsReverse = new Color32[_count];
			_colors.CopyTo(_colorsReverse, 0);
			Array.Reverse(_colorsReverse);
		}

		public override void OnEncodeToConfigNode()
		{
			string[] colors = new string[_colors.Length];

			for (int i = 0; i < _colors.Length; i++)
			{
				Color32 c = _colors[i];

				string s = string.Format("{0},{1},{2},{3}", c.r, c.g, c.b, c.a);

				colors[i] = s;
			}

			Colors = string.Join("|", colors);
		}
	}
}
