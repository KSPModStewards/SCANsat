#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANPaletteGroup - Class to hold info on a group of color palettes
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SCANsat.SCAN_Platform;

namespace SCANsat.SCAN_Palettes
{
	public class SCANPaletteGroup : SCAN_ConfigNodeStorage
	{
		[Persistent]
		private string name;
		[Persistent]
		private List<SCANPalette> Palettes = new List<SCANPalette>();

		private DictionaryValueList<int, SCANPalette> MasterPaletteList = new DictionaryValueList<int, SCANPalette>();

		private SCANPaletteKind _kind;

		public SCANPaletteGroup() { }

		public SCANPaletteGroup(string _name, SCANPaletteKind kind)
		{
			name = _name;
			_kind = kind;
		}

		public SCANPaletteGroup(string _name, SCANPaletteKind kind, SCANPalette palette)
		{
			name = _name;
			_kind = kind;

			Palettes.Add(palette);

			if (!MasterPaletteList.Contains(palette.Count))
			{
				MasterPaletteList.Add(palette.Count, palette);
			}
		}

		public SCANPaletteKind Kind
		{
			get { return _kind; }
			set { _kind = value; }
		}

		public string PaletteName
		{
			get { return name; }
		}

		public SCANPalette GetPalette(int length)
		{
			if (_kind == SCANPaletteKind.Fixed && MasterPaletteList.Count > 0)
			{
				return MasterPaletteList.At(0);
			}

			if (MasterPaletteList.Contains(length))
			{
				return MasterPaletteList[length];
			}

			if (MasterPaletteList.Count > 0)
			{
				int max = MasterPaletteList.Values.Max(p => p.Count);

				if (MasterPaletteList.Contains(max))
				{
					return MasterPaletteList[max];
				}

				return MasterPaletteList.At(0);
			}

			return null;
		}

		public void SaveDefaultPalettes()
		{
			Palettes = new List<SCANPalette>();

			int count = 1;

			try
			{
				switch (_kind)
				{
					case SCANPaletteKind.Fixed:
						if (name != "Default")
						{
							var fixedPalette = typeof(FixedColorPalettes);
							var fixedPaletteMethod = fixedPalette.GetMethod(name);
							var fixedColorPalette = fixedPaletteMethod.Invoke(null, null);
							Palettes.Add((SCANPalette)fixedColorPalette);
							OnDecodeFromConfigNode();
						}
						return;
					case SCANPaletteKind.Diverging:
						count = 11;
						break;
					case SCANPaletteKind.Qualitative:
						count = 12;
						break;
					case SCANPaletteKind.Sequential:
						count = 9;
						break;
					default:
						break;
				}

				var brewerPalette = typeof(BrewerPalettes);
				var brewerPaletteMethod = brewerPalette.GetMethod(name);

				for (int i = 3; i <= count; i++)
				{
					var brewerColorPalette = brewerPaletteMethod.Invoke(null, new object[] { i });
					Palettes.Add((SCANPalette)brewerColorPalette);
				}
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error Generating Color Palettes: {0}", e);
			}

			OnDecodeFromConfigNode();
		}

		public void setPaletteKind(SCANPaletteKind kind)
		{
			for (int i = MasterPaletteList.Count - 1; i >= 0; i--)
			{
				SCANPalette p = MasterPaletteList.At(i);

				p.Kind = kind;
			}
		}

		public override void OnDecodeFromConfigNode()
		{
			try
			{
				for (int i = Palettes.Count - 1; i >= 0; i--)
				{
					SCANPalette p = Palettes[i];

					if (p == null)
					{
						continue;
					}

					if (!MasterPaletteList.Contains(p.Count))
					{
						MasterPaletteList.Add(p.Count, p);
					}

					p.Name = name;
				}
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error while loading SCANsat palette config settings: {0}", e);
			}
		}

		public override void OnEncodeToConfigNode()
		{
			try
			{
				Palettes = MasterPaletteList.Values.ToList();
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error while saving SCANsat palette config data: {0}", e);
			}
		}
	}
}
