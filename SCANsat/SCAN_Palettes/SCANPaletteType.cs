#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANPaletteType - Class to hold info on related color palette types
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
	public class SCANPaletteType : SCAN_ConfigNodeStorage
	{
		[Persistent]
		private string name;
		[Persistent]
		private List<SCANPaletteGroup> PaletteGroups = new List<SCANPaletteGroup>();

		private DictionaryValueList<string, SCANPaletteGroup> MasterPaletteGroupList = new DictionaryValueList<string, SCANPaletteGroup>();

		private SCANPaletteKind _kind;
		private Texture2D[] _paletteSwatch;
		private int _swatchLength = -1;

		public SCANPaletteType() { }

		public SCANPaletteType(string type)
		{
			name = type;

			try
			{
				_kind = (SCANPaletteKind)Enum.Parse(typeof(SCANPaletteKind), type);
			}
			catch (Exception e)
			{
				_kind = SCANPaletteKind.Unknown;
				SCANUtil.SCANlog("Error assigning SCANsat palette type - Type: {0}\n{1}", type, e);
			}
		}

		public SCANPaletteKind Kind
		{
			get { return _kind; }
		}

		public Texture2D[] PaletteSwatch
		{
			get { return _paletteSwatch; }
		}

		public int Count
		{
			get { return MasterPaletteGroupList.Count; }
		}

		public void SaveDefaultPalettes()
		{
			PaletteGroups = new List<SCANPaletteGroup>();

			switch (_kind)
			{
				case SCANPaletteKind.Diverging:
					PaletteGroups.Add(new SCANPaletteGroup("Spectral", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("RdYlGn", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("RdBu", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("PiYG", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("PRGn", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("RdYlBu", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("BrBG", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("RdGy", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("PuOr", _kind));
					break;
				case SCANPaletteKind.Qualitative:
					PaletteGroups.Add(new SCANPaletteGroup("Set2", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("Accent", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("Set1", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("Set3", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("Dark2", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("Paired", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("Pastel2", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("Pastel1", _kind));
					break;
				case SCANPaletteKind.Sequential:
					PaletteGroups.Add(new SCANPaletteGroup("OrRd", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("BuPu", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("BuGn", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("YlOrBr", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("YlGn", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("Reds", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("RdPu", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("YlGnBu", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("YlOrRd", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("PuRd", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("Blues", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("PuBuGn", _kind));
					break;
				case SCANPaletteKind.Fixed:
					PaletteGroups.Add(new SCANPaletteGroup("blackForest", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("mars", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("departure", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("northRhine", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("wiki2", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("plumbago", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("cw1_013", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("arctic", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("mercury", _kind));
					PaletteGroups.Add(new SCANPaletteGroup("venus", _kind));
					PaletteGroups.Add(SCAN_Palette_Config.DefaultPalette);
					break;
				default:
					break;
			}

			for (int i = 0; i < PaletteGroups.Count; i++)
			{
				PaletteGroups[i].SaveDefaultPalettes();
			}

			OnDecodeFromConfigNode();
		}

		public void AddPaletteGroup(SCANPaletteGroup group)
		{

		}

		public SCANPaletteGroup GetPaletteGroup(string name)
		{
			if (name == "Default" || name == "default")
			{
				return SCAN_Palette_Config.DefaultPalette;
			}

			for (int i = MasterPaletteGroupList.Count - 1; i >= 0; i--)
			{
				SCANPaletteGroup group = MasterPaletteGroupList.At(i);

				if (group.PaletteName != name)
				{
					continue;
				}

				return group;
			}

			return null;
		}

		public string[] GetGroupNames()
		{
			string[] names = new string[Count];

			for (int i = 0; i < MasterPaletteGroupList.Count; i++)
			{
				names[i] = MasterPaletteGroupList.At(i).PaletteName;
			}

			return names;
		}

		public SCANPaletteGroup GetFirstGroup()
		{
			if (MasterPaletteGroupList.Count > 0)
			{
				return MasterPaletteGroupList.At(0);
			}

			return null;
		}

		public List<SCANPalette> GetPaletteList(int length)
		{
			List<SCANPalette> palettes = new List<SCANPalette>();

			for (int i = 0; i < MasterPaletteGroupList.Count; i++)
			{
				SCANPaletteGroup group = MasterPaletteGroupList.At(i);

				SCANPalette palette = group.GetPalette(length);

				if (palette != null)
				{
					palettes.Add(palette);
				}
			}

			return palettes;
		}

		public Texture2D[] GenerateSwatches(int length)
		{
			if (_paletteSwatch == null || length != _swatchLength)
			{
				_paletteSwatch = new Texture2D[MasterPaletteGroupList.Count];

				for (int i = 0; i < MasterPaletteGroupList.Count; i++)
				{
					SCANPalette palette = MasterPaletteGroupList.At(i).GetPalette(length);

					int k = 0;
					int m = 120;
					int paletteSize = palette.Count;

					_swatchLength = length;

					if (paletteSize == 11)
					{
						m = 121;
					}
					else if (paletteSize == 18)
					{
						m = 126;
					}
					else if (paletteSize == 9)
					{
						m = 117;
					}
					else if (paletteSize == 7)
					{
						m = 119;
					}
					else if (paletteSize == 34)
					{
						m = 136;
					}
					else if (m % paletteSize != 0)
					{
						int s = 115;

						while (s % paletteSize != 0)
						{
							s++;
						}

						m = s;
					}

					Texture2D t = new Texture2D(m, 1);

					Color32[] pix = new Color32[m];
					int sW = m / paletteSize;

					for (int j = 0; j < m; j++)
					{
						if (j % sW == 0)
						{
							k++;
						}

						pix[j] = palette.ColorsArray[k - 1];
					}

					t.SetPixels32(pix);
					t.Apply();
					_paletteSwatch[i] = t;
				}
				return _paletteSwatch;
			}
			else
			{
				return _paletteSwatch;
			}
		}

		public override void OnDecodeFromConfigNode()
		{
			try
			{
				_kind = (SCANPaletteKind)Enum.Parse(typeof(SCANPaletteKind), name);
			}
			catch (Exception e)
			{
				_kind = SCANPaletteKind.Unknown;
				SCANUtil.SCANlog("Error assigning SCANsat palette type - Type: {0}\n{1}", name, e);
			}

			try
			{
				for (int i = PaletteGroups.Count - 1; i >= 0; i--)
				{
					SCANPaletteGroup p = PaletteGroups[i];

					if (p == null)
					{
						continue;
					}

					if (!MasterPaletteGroupList.Contains(p.PaletteName))
					{
						MasterPaletteGroupList.Add(p.PaletteName, p);
					}

					p.Kind = _kind;

					p.setPaletteKind(_kind);
				}

				if (_kind == SCANPaletteKind.Fixed)
				{
					if (!MasterPaletteGroupList.Contains("Default"))
					{
						MasterPaletteGroupList.Add("Default", SCAN_Palette_Config.DefaultPalette);
					}
				}
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error while loading SCANsat palette config settings: {0}", e);
			}
		}

		public override void OnEncodeToConfigNode()
		{
			base.OnEncodeToConfigNode();
		}
	}
}
