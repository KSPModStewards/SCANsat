#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN ColorBrewer - static class for generating Color Brewer color palettes
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using UnityEngine;


namespace SCANsat.SCAN_Platform.Palettes.ColorBrewer {
	public static class BrewerPalettes {
		static Func<byte,byte,byte,Color32> RGB = (r,g,b) => new Color32(r,g,b,255);

		/*** Diverging ***/
		public static Palette Spectral (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(252,141,089), RGB(255,255,191), RGB(153,213,148)}; break;
			case 04: c = new[] {RGB(215,025,028), RGB(253,174,097), RGB(171,221,164), RGB(043,131,186)}; break;
			case 05: c = new[] {RGB(215,025,028), RGB(253,174,097), RGB(255,255,191), RGB(171,221,164), RGB(043,131,186)}; break;
			case 06: c = new[] {RGB(213,062,079), RGB(252,141,089), RGB(254,224,139), RGB(230,245,152), RGB(153,213,148), RGB(050,136,189)}; break;
			case 07: c = new[] {RGB(213,062,079), RGB(252,141,089), RGB(254,224,139), RGB(255,255,191), RGB(230,245,152), RGB(153,213,148), RGB(050,136,189)}; break;
			case 08: c = new[] {RGB(213,062,079), RGB(244,109,067), RGB(253,174,097), RGB(254,224,139), RGB(230,245,152), RGB(171,221,164), RGB(102,194,165), RGB(050,136,189)}; break;
			case 09: c = new[] {RGB(213,062,079), RGB(244,109,067), RGB(253,174,097), RGB(254,224,139), RGB(255,255,191), RGB(230,245,152), RGB(171,221,164), RGB(102,194,165), RGB(050,136,189)}; break;
			case 10: c = new[] {RGB(158,001,066), RGB(213,062,079), RGB(244,109,067), RGB(253,174,097), RGB(254,224,139), RGB(230,245,152), RGB(171,221,164), RGB(102,194,165), RGB(050,136,189), RGB(094,079,162)}; break;
			default: c = new[] {RGB(158,001,066), RGB(213,062,079), RGB(244,109,067), RGB(253,174,097), RGB(254,224,139), RGB(255,255,191), RGB(230,245,152), RGB(171,221,164), RGB(102,194,165), RGB(050,136,189), RGB(094,079,162)}; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Diverging;
			var @blind = new[] {2, 2, 2, 0, 0, 0, 0, 0, 0};
			var @print = new[] {1, 1, 1, 0, 0, 0, 0, 0, 0};
			var @xerox = new[] {1, 1, 1, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 2, 0, 0, 0, 0, 0, 0};

			return new Palette(c, "Spectral", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette RdYlGn (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(252,141,089), RGB(255,255,191), RGB(145,207,096)}; break;
			case 04: c = new[] {RGB(215,025,028), RGB(253,174,097), RGB(166,217,106), RGB(026,150,065)}; break;
			case 05: c = new[] {RGB(215,025,028), RGB(253,174,097), RGB(255,255,191), RGB(166,217,106), RGB(026,150,065)}; break;
			case 06: c = new[] {RGB(215,048,039), RGB(252,141,089), RGB(254,224,139), RGB(217,239,139), RGB(145,207,096), RGB(026,152,080)}; break;
			case 07: c = new[] {RGB(215,048,039), RGB(252,141,089), RGB(254,224,139), RGB(255,255,191), RGB(217,239,139), RGB(145,207,096), RGB(026,152,080)}; break;
			case 08: c = new[] {RGB(215,048,039), RGB(244,109,067), RGB(253,174,097), RGB(254,224,139), RGB(217,239,139), RGB(166,217,106), RGB(102,189,099), RGB(026,152,080)}; break;
			case 09: c = new[] {RGB(215,048,039), RGB(244,109,067), RGB(253,174,097), RGB(254,224,139), RGB(255,255,191), RGB(217,239,139), RGB(166,217,106), RGB(102,189,099), RGB(026,152,080)}; break;
			case 10: c = new[] {RGB(165,000,038), RGB(215,048,039), RGB(244,109,067), RGB(253,174,097), RGB(254,224,139), RGB(217,239,139), RGB(166,217,106), RGB(102,189,099), RGB(026,152,080), RGB(000,104,055)}; break;
			default: c = new[] { RGB(165, 000, 038), RGB(215, 048, 039), RGB(244, 109, 067), RGB(253, 174, 097), RGB(254, 224, 139), RGB(255, 255, 191), RGB(217, 239, 139), RGB(166, 217, 106), RGB(102, 189, 099), RGB(026, 152, 080), RGB(000, 104, 055) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Diverging;
			var @blind = new[] {2, 2, 2, 0, 0, 0, 0, 0, 0};
			var @print = new[] {1, 1, 1, 2, 0, 0, 0, 0, 0};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 0, 0, 0, 0, 0, 0};

			return new Palette(c, "RdYlGn", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette RdBu (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(239,138,098), RGB(247,247,247), RGB(103,169,207)}; break;
			case 04: c = new[] {RGB(202,000,032), RGB(244,165,130), RGB(146,197,222), RGB(005,113,176)}; break;
			case 05: c = new[] {RGB(202,000,032), RGB(244,165,130), RGB(247,247,247), RGB(146,197,222), RGB(005,113,176)}; break;
			case 06: c = new[] {RGB(178,024,043), RGB(239,138,098), RGB(253,219,199), RGB(209,229,240), RGB(103,169,207), RGB(033,102,172)}; break;
			case 07: c = new[] {RGB(178,024,043), RGB(239,138,098), RGB(253,219,199), RGB(247,247,247), RGB(209,229,240), RGB(103,169,207), RGB(033,102,172)}; break;
			case 08: c = new[] {RGB(178,024,043), RGB(214,096,077), RGB(244,165,130), RGB(253,219,199), RGB(209,229,240), RGB(146,197,222), RGB(067,147,195), RGB(033,102,172)}; break;
			case 09: c = new[] {RGB(178,024,043), RGB(214,096,077), RGB(244,165,130), RGB(253,219,199), RGB(247,247,247), RGB(209,229,240), RGB(146,197,222), RGB(067,147,195), RGB(033,102,172)}; break;
			case 10: c = new[] {RGB(103,000,031), RGB(178,024,043), RGB(214,096,077), RGB(244,165,130), RGB(253,219,199), RGB(209,229,240), RGB(146,197,222), RGB(067,147,195), RGB(033,102,172), RGB(005,048,097)}; break;
			default: c = new[] { RGB(103, 000, 031), RGB(178, 024, 043), RGB(214, 096, 077), RGB(244, 165, 130), RGB(253, 219, 199), RGB(247, 247, 247), RGB(209, 229, 240), RGB(146, 197, 222), RGB(067, 147, 195), RGB(033, 102, 172), RGB(005, 048, 097) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Diverging;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 1, 1, 0, 0, 0, 0, 0};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 0, 0, 0, 0, 0, 0};

			return new Palette(c, "RdBu", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette PiYG (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(233,163,201), RGB(247,247,247), RGB(161,215,106)}; break;
			case 04: c = new[] {RGB(208,028,139), RGB(241,182,218), RGB(184,225,134), RGB(077,172,038)}; break;
			case 05: c = new[] {RGB(208,028,139), RGB(241,182,218), RGB(247,247,247), RGB(184,225,134), RGB(077,172,038)}; break;
			case 06: c = new[] {RGB(197,027,125), RGB(233,163,201), RGB(253,224,239), RGB(230,245,208), RGB(161,215,106), RGB(077,146,033)}; break;
			case 07: c = new[] {RGB(197,027,125), RGB(233,163,201), RGB(253,224,239), RGB(247,247,247), RGB(230,245,208), RGB(161,215,106), RGB(077,146,033)}; break;
			case 08: c = new[] {RGB(197,027,125), RGB(222,119,174), RGB(241,182,218), RGB(253,224,239), RGB(230,245,208), RGB(184,225,134), RGB(127,188,065), RGB(077,146,033)}; break;
			case 09: c = new[] {RGB(197,027,125), RGB(222,119,174), RGB(241,182,218), RGB(253,224,239), RGB(247,247,247), RGB(230,245,208), RGB(184,225,134), RGB(127,188,065), RGB(077,146,033)}; break;
			case 10: c = new[] {RGB(142,001,082), RGB(197,027,125), RGB(222,119,174), RGB(241,182,218), RGB(253,224,239), RGB(230,245,208), RGB(184,225,134), RGB(127,188,065), RGB(077,146,033), RGB(039,100,025)}; break;
			default: c = new[] { RGB(142, 001, 082), RGB(197, 027, 125), RGB(222, 119, 174), RGB(241, 182, 218), RGB(253, 224, 239), RGB(247, 247, 247), RGB(230, 245, 208), RGB(184, 225, 134), RGB(127, 188, 065), RGB(077, 146, 033), RGB(039, 100, 025) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Diverging;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 2, 0, 0, 0, 0, 0, 0};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 2, 0, 0, 0, 0, 0, 0};

			return new Palette(c, "PiYG", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette PRGn (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(175,141,195), RGB(247,247,247), RGB(127,191,123)}; break;
			case 04: c = new[] {RGB(123,050,148), RGB(194,165,207), RGB(166,219,160), RGB(000,136,055)}; break;
			case 05: c = new[] {RGB(123,050,148), RGB(194,165,207), RGB(247,247,247), RGB(166,219,160), RGB(000,136,055)}; break;
			case 06: c = new[] {RGB(118,042,131), RGB(175,141,195), RGB(231,212,232), RGB(217,240,211), RGB(127,191,123), RGB(027,120,055)}; break;
			case 07: c = new[] {RGB(118,042,131), RGB(175,141,195), RGB(231,212,232), RGB(247,247,247), RGB(217,240,211), RGB(127,191,123), RGB(027,120,055)}; break;
			case 08: c = new[] {RGB(118,042,131), RGB(153,112,171), RGB(194,165,207), RGB(231,212,232), RGB(217,240,211), RGB(166,219,160), RGB(090,174,097), RGB(027,120,055)}; break;
			case 09: c = new[] {RGB(118,042,131), RGB(153,112,171), RGB(194,165,207), RGB(231,212,232), RGB(247,247,247), RGB(217,240,211), RGB(166,219,160), RGB(090,174,097), RGB(027,120,055)}; break;
			case 10: c = new[] {RGB(064,000,075), RGB(118,042,131), RGB(153,112,171), RGB(194,165,207), RGB(231,212,232), RGB(217,240,211), RGB(166,219,160), RGB(090,174,097), RGB(027,120,055), RGB(000,068,027)}; break;
			default: c = new[] { RGB(064, 000, 075), RGB(118, 042, 131), RGB(153, 112, 171), RGB(194, 165, 207), RGB(231, 212, 232), RGB(247, 247, 247), RGB(217, 240, 211), RGB(166, 219, 160), RGB(090, 174, 097), RGB(027, 120, 055), RGB(000, 068, 027) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Diverging;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 1, 1, 0, 0, 0, 0, 0};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 2, 2, 0, 0, 0, 0, 0};

			return new Palette(c, "PRGn", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette RdYlBu (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(252,141,089), RGB(255,255,191), RGB(145,191,219)}; break;
			case 04: c = new[] {RGB(215,025,028), RGB(253,174,097), RGB(171,217,233), RGB(044,123,182)}; break;
			case 05: c = new[] {RGB(215,025,028), RGB(253,174,097), RGB(255,255,191), RGB(171,217,233), RGB(044,123,182)}; break;
			case 06: c = new[] {RGB(215,048,039), RGB(252,141,089), RGB(254,224,144), RGB(224,243,248), RGB(145,191,219), RGB(069,117,180)}; break;
			case 07: c = new[] {RGB(215,048,039), RGB(252,141,089), RGB(254,224,144), RGB(255,255,191), RGB(224,243,248), RGB(145,191,219), RGB(069,117,180)}; break;
			case 08: c = new[] {RGB(215,048,039), RGB(244,109,067), RGB(253,174,097), RGB(254,224,144), RGB(224,243,248), RGB(171,217,233), RGB(116,173,209), RGB(069,117,180)}; break;
			case 09: c = new[] {RGB(215,048,039), RGB(244,109,067), RGB(253,174,097), RGB(254,224,144), RGB(255,255,191), RGB(224,243,248), RGB(171,217,233), RGB(116,173,209), RGB(069,117,180)}; break;
			case 10: c = new[] {RGB(165,000,038), RGB(215,048,039), RGB(244,109,067), RGB(253,174,097), RGB(254,224,144), RGB(224,243,248), RGB(171,217,233), RGB(116,173,209), RGB(069,117,180), RGB(049,054,149)}; break;
			default: c = new[] { RGB(165, 000, 038), RGB(215, 048, 039), RGB(244, 109, 067), RGB(253, 174, 097), RGB(254, 224, 144), RGB(255, 255, 191), RGB(224, 243, 248), RGB(171, 217, 233), RGB(116, 173, 209), RGB(069, 117, 180), RGB(049, 054, 149) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Diverging;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 1, 1, 2, 0, 0, 0, 0};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 2, 0, 0, 0, 0, 0};

			return new Palette(c, "RdYlBu", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette BrBG (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(216,179,101), RGB(245,245,245), RGB(090,180,172)}; break;
			case 04: c = new[] {RGB(166,097,026), RGB(223,194,125), RGB(128,205,193), RGB(001,133,113)}; break;
			case 05: c = new[] {RGB(166,097,026), RGB(223,194,125), RGB(245,245,245), RGB(128,205,193), RGB(001,133,113)}; break;
			case 06: c = new[] {RGB(140,081,010), RGB(216,179,101), RGB(246,232,195), RGB(199,234,229), RGB(090,180,172), RGB(001,102,094)}; break;
			case 07: c = new[] {RGB(140,081,010), RGB(216,179,101), RGB(246,232,195), RGB(245,245,245), RGB(199,234,229), RGB(090,180,172), RGB(001,102,094)}; break;
			case 08: c = new[] {RGB(140,081,010), RGB(191,129,045), RGB(223,194,125), RGB(246,232,195), RGB(199,234,229), RGB(128,205,193), RGB(053,151,143), RGB(001,102,094)}; break;
			case 09: c = new[] {RGB(140,081,010), RGB(191,129,045), RGB(223,194,125), RGB(246,232,195), RGB(245,245,245), RGB(199,234,229), RGB(128,205,193), RGB(053,151,143), RGB(001,102,094)}; break;
			case 10: c = new[] {RGB(084,048,005), RGB(140,081,010), RGB(191,129,045), RGB(223,194,125), RGB(246,232,195), RGB(199,234,229), RGB(128,205,193), RGB(053,151,143), RGB(001,102,094), RGB(000,060,048)}; break;
			default: c = new[] { RGB(084, 048, 005), RGB(140, 081, 010), RGB(191, 129, 045), RGB(223, 194, 125), RGB(246, 232, 195), RGB(245, 245, 245), RGB(199, 234, 229), RGB(128, 205, 193), RGB(053, 151, 143), RGB(001, 102, 094), RGB(000, 060, 048) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Diverging;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 1, 1, 0, 0, 0, 0, 0};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 1, 0, 0, 0, 0, 0};

			return new Palette(c, "BrBG", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette RdGy (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(239,138,098), RGB(255,255,255), RGB(153,153,153)}; break;
			case 04: c = new[] {RGB(202,000,032), RGB(244,165,130), RGB(186,186,186), RGB(064,064,064)}; break;
			case 05: c = new[] {RGB(202,000,032), RGB(244,165,130), RGB(255,255,255), RGB(186,186,186), RGB(064,064,064)}; break;
			case 06: c = new[] {RGB(178,024,043), RGB(239,138,098), RGB(253,219,199), RGB(224,224,224), RGB(153,153,153), RGB(077,077,077)}; break;
			case 07: c = new[] {RGB(178,024,043), RGB(239,138,098), RGB(253,219,199), RGB(255,255,255), RGB(224,224,224), RGB(153,153,153), RGB(077,077,077)}; break;
			case 08: c = new[] {RGB(178,024,043), RGB(214,096,077), RGB(244,165,130), RGB(253,219,199), RGB(224,224,224), RGB(186,186,186), RGB(135,135,135), RGB(077,077,077)}; break;
			case 09: c = new[] {RGB(178,024,043), RGB(214,096,077), RGB(244,165,130), RGB(253,219,199), RGB(255,255,255), RGB(224,224,224), RGB(186,186,186), RGB(135,135,135), RGB(077,077,077)}; break;
			case 10: c = new[] {RGB(103,000,031), RGB(178,024,043), RGB(214,096,077), RGB(244,165,130), RGB(253,219,199), RGB(224,224,224), RGB(186,186,186), RGB(135,135,135), RGB(077,077,077), RGB(026,026,026)}; break;
			default: c = new[] { RGB(103, 000, 031), RGB(178, 024, 043), RGB(214, 096, 077), RGB(244, 165, 130), RGB(253, 219, 199), RGB(255, 255, 255), RGB(224, 224, 224), RGB(186, 186, 186), RGB(135, 135, 135), RGB(077, 077, 077), RGB(026, 026, 026) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Diverging;
			var @blind = new[] {2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2};
			var @print = new[] {1, 1, 1, 2, 0, 0, 0, 0, 0};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 2, 0, 0, 0, 0, 0, 0};

			return new Palette(c, "RdGy", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette PuOr (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(241,163,064), RGB(247,247,247), RGB(153,142,195)}; break;
			case 04: c = new[] {RGB(230,097,001), RGB(253,184,099), RGB(178,171,210), RGB(094,060,153)}; break;
			case 05: c = new[] {RGB(230,097,001), RGB(253,184,099), RGB(247,247,247), RGB(178,171,210), RGB(094,060,153)}; break;
			case 06: c = new[] {RGB(179,088,006), RGB(241,163,064), RGB(254,224,182), RGB(216,218,235), RGB(153,142,195), RGB(084,039,136)}; break;
			case 07: c = new[] {RGB(179,088,006), RGB(241,163,064), RGB(254,224,182), RGB(247,247,247), RGB(216,218,235), RGB(153,142,195), RGB(084,039,136)}; break;
			case 08: c = new[] {RGB(179,088,006), RGB(224,130,020), RGB(253,184,099), RGB(254,224,182), RGB(216,218,235), RGB(178,171,210), RGB(128,115,172), RGB(084,039,136)}; break;
			case 09: c = new[] {RGB(179,088,006), RGB(224,130,020), RGB(253,184,099), RGB(254,224,182), RGB(247,247,247), RGB(216,218,235), RGB(178,171,210), RGB(128,115,172), RGB(084,039,136)}; break;
			case 10: c = new[] {RGB(127,059,008), RGB(179,088,006), RGB(224,130,020), RGB(253,184,099), RGB(254,224,182), RGB(216,218,235), RGB(178,171,210), RGB(128,115,172), RGB(084,039,136), RGB(045,000,075)}; break;
			default: c = new[] { RGB(127, 059, 008), RGB(179, 088, 006), RGB(224, 130, 020), RGB(253, 184, 099), RGB(254, 224, 182), RGB(247, 247, 247), RGB(216, 218, 235), RGB(178, 171, 210), RGB(128, 115, 172), RGB(084, 039, 136), RGB(045, 000, 075) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Diverging;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 2, 2, 0, 0, 0, 0, 0};
			var @xerox = new[] {1, 1, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 1, 0, 0, 0, 0, 0};

			return new Palette(c, "PuOr", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		/*** Qualitative ***/
		public static Palette Set2 (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(102,194,165), RGB(252,141,098), RGB(141,160,203)}; break;
			case 04: c = new[] {RGB(102,194,165), RGB(252,141,098), RGB(141,160,203), RGB(231,138,195)}; break;
			case 05: c = new[] {RGB(102,194,165), RGB(252,141,098), RGB(141,160,203), RGB(231,138,195), RGB(166,216,084)}; break;
			case 06: c = new[] {RGB(102,194,165), RGB(252,141,098), RGB(141,160,203), RGB(231,138,195), RGB(166,216,084), RGB(255,217,047)}; break;
			case 07: c = new[] {RGB(102,194,165), RGB(252,141,098), RGB(141,160,203), RGB(231,138,195), RGB(166,216,084), RGB(255,217,047), RGB(229,196,148)}; break;
			default: c = new[] { RGB(102, 194, 165), RGB(252, 141, 098), RGB(141, 160, 203), RGB(231, 138, 195), RGB(166, 216, 084), RGB(255, 217, 047), RGB(229, 196, 148), RGB(179, 179, 179) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Qualitative;
			var @blind = new[] {1, 2, 2, 2, 0, 0};
			var @print = new[] {1, 1, 1, 2, 2, 2};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 2, 2, 2, 2};

			return new Palette(c, "Set2", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette Accent (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(127,201,127), RGB(190,174,212), RGB(253,192,134)}; break;
			case 04: c = new[] {RGB(127,201,127), RGB(190,174,212), RGB(253,192,134), RGB(255,255,153)}; break;
			case 05: c = new[] {RGB(127,201,127), RGB(190,174,212), RGB(253,192,134), RGB(255,255,153), RGB(056,108,176)}; break;
			case 06: c = new[] {RGB(127,201,127), RGB(190,174,212), RGB(253,192,134), RGB(255,255,153), RGB(056,108,176), RGB(240,002,127)}; break;
			case 07: c = new[] {RGB(127,201,127), RGB(190,174,212), RGB(253,192,134), RGB(255,255,153), RGB(056,108,176), RGB(240,002,127), RGB(191,091,023)}; break;
			default: c = new[] { RGB(127, 201, 127), RGB(190, 174, 212), RGB(253, 192, 134), RGB(255, 255, 153), RGB(056, 108, 176), RGB(240, 002, 127), RGB(191, 091, 023), RGB(102, 102, 102) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Qualitative;
			var @blind = new[] {2, 0, 0, 0, 0, 0};
			var @print = new[] {1, 1, 2, 2, 2, 2};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 2, 2, 2};

			return new Palette(c, "Accent", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette Set1 (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(228,026,028), RGB(055,126,184), RGB(077,175,074)}; break;
			case 04: c = new[] {RGB(228,026,028), RGB(055,126,184), RGB(077,175,074), RGB(152,078,163)}; break;
			case 05: c = new[] {RGB(228,026,028), RGB(055,126,184), RGB(077,175,074), RGB(152,078,163), RGB(255,127,000)}; break;
			case 06: c = new[] {RGB(228,026,028), RGB(055,126,184), RGB(077,175,074), RGB(152,078,163), RGB(255,127,000), RGB(255,255,051)}; break;
			case 07: c = new[] {RGB(228,026,028), RGB(055,126,184), RGB(077,175,074), RGB(152,078,163), RGB(255,127,000), RGB(255,255,051), RGB(166,086,040)}; break;
			case 08: c = new[] {RGB(228,026,028), RGB(055,126,184), RGB(077,175,074), RGB(152,078,163), RGB(255,127,000), RGB(255,255,051), RGB(166,086,040), RGB(247,129,191)}; break;
			default: c = new[] { RGB(228, 026, 028), RGB(055, 126, 184), RGB(077, 175, 074), RGB(152, 078, 163), RGB(255, 127, 000), RGB(255, 255, 051), RGB(166, 086, 040), RGB(247, 129, 191), RGB(153, 153, 153) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Qualitative;
			var @blind = new[] {2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2};
			var @print = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};

			return new Palette(c, "Set1", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette Set3 (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(141,211,199), RGB(255,255,179), RGB(190,186,218)}; break;
			case 04: c = new[] {RGB(141,211,199), RGB(255,255,179), RGB(190,186,218), RGB(251,128,114)}; break;
			case 05: c = new[] {RGB(141,211,199), RGB(255,255,179), RGB(190,186,218), RGB(251,128,114), RGB(128,177,211)}; break;
			case 06: c = new[] {RGB(141,211,199), RGB(255,255,179), RGB(190,186,218), RGB(251,128,114), RGB(128,177,211), RGB(253,180,098)}; break;
			case 07: c = new[] {RGB(141,211,199), RGB(255,255,179), RGB(190,186,218), RGB(251,128,114), RGB(128,177,211), RGB(253,180,098), RGB(179,222,105)}; break;
			case 08: c = new[] {RGB(141,211,199), RGB(255,255,179), RGB(190,186,218), RGB(251,128,114), RGB(128,177,211), RGB(253,180,098), RGB(179,222,105), RGB(252,205,229)}; break;
			case 09: c = new[] {RGB(141,211,199), RGB(255,255,179), RGB(190,186,218), RGB(251,128,114), RGB(128,177,211), RGB(253,180,098), RGB(179,222,105), RGB(252,205,229), RGB(217,217,217)}; break;
			case 10: c = new[] {RGB(141,211,199), RGB(255,255,179), RGB(190,186,218), RGB(251,128,114), RGB(128,177,211), RGB(253,180,098), RGB(179,222,105), RGB(252,205,229), RGB(217,217,217), RGB(188,128,189)}; break;
			case 11: c = new[] {RGB(141,211,199), RGB(255,255,179), RGB(190,186,218), RGB(251,128,114), RGB(128,177,211), RGB(253,180,098), RGB(179,222,105), RGB(252,205,229), RGB(217,217,217), RGB(188,128,189), RGB(204,235,197)}; break;
			default: c = new[] { RGB(141, 211, 199), RGB(255, 255, 179), RGB(190, 186, 218), RGB(251, 128, 114), RGB(128, 177, 211), RGB(253, 180, 098), RGB(179, 222, 105), RGB(252, 205, 229), RGB(217, 217, 217), RGB(188, 128, 189), RGB(204, 235, 197), RGB(255, 237, 111) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Qualitative;
			var @blind = new[] {2, 2, 0, 0, 0, 0, 0, 0, 0, 0};
			var @print = new[] {1, 1, 1, 1, 1, 1, 2, 0, 0, 0};
			var @xerox = new[] {1, 2, 2, 2, 2, 2, 2, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 2, 2, 2, 0, 0, 0, 0};

			return new Palette(c, "Set3", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette Dark2 (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(027,158,119), RGB(217,095,002), RGB(117,112,179)}; break;
			case 04: c = new[] {RGB(027,158,119), RGB(217,095,002), RGB(117,112,179), RGB(231,041,138)}; break;
			case 05: c = new[] {RGB(027,158,119), RGB(217,095,002), RGB(117,112,179), RGB(231,041,138), RGB(102,166,030)}; break;
			case 06: c = new[] {RGB(027,158,119), RGB(217,095,002), RGB(117,112,179), RGB(231,041,138), RGB(102,166,030), RGB(230,171,002)}; break;
			case 07: c = new[] {RGB(027,158,119), RGB(217,095,002), RGB(117,112,179), RGB(231,041,138), RGB(102,166,030), RGB(230,171,002), RGB(166,118,029)}; break;
			default: c = new[] { RGB(027, 158, 119), RGB(217, 095, 002), RGB(117, 112, 179), RGB(231, 041, 138), RGB(102, 166, 030), RGB(230, 171, 002), RGB(166, 118, 029), RGB(102, 102, 102) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Qualitative;
			var @blind = new[] {1, 2, 2, 2, 0, 0};
			var @print = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};

			return new Palette(c, "Dark2", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette Paired (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(166,206,227), RGB(031,120,180), RGB(178,223,138)}; break;
			case 04: c = new[] {RGB(166,206,227), RGB(031,120,180), RGB(178,223,138), RGB(051,160,044)}; break;
			case 05: c = new[] {RGB(166,206,227), RGB(031,120,180), RGB(178,223,138), RGB(051,160,044), RGB(251,154,153)}; break;
			case 06: c = new[] {RGB(166,206,227), RGB(031,120,180), RGB(178,223,138), RGB(051,160,044), RGB(251,154,153), RGB(227,026,028)}; break;
			case 07: c = new[] {RGB(166,206,227), RGB(031,120,180), RGB(178,223,138), RGB(051,160,044), RGB(251,154,153), RGB(227,026,028), RGB(253,191,111)}; break;
			case 08: c = new[] {RGB(166,206,227), RGB(031,120,180), RGB(178,223,138), RGB(051,160,044), RGB(251,154,153), RGB(227,026,028), RGB(253,191,111), RGB(255,127,000)}; break;
			case 09: c = new[] {RGB(166,206,227), RGB(031,120,180), RGB(178,223,138), RGB(051,160,044), RGB(251,154,153), RGB(227,026,028), RGB(253,191,111), RGB(255,127,000), RGB(202,178,214)}; break;
			case 10: c = new[] {RGB(166,206,227), RGB(031,120,180), RGB(178,223,138), RGB(051,160,044), RGB(251,154,153), RGB(227,026,028), RGB(253,191,111), RGB(255,127,000), RGB(202,178,214), RGB(106,061,154)}; break;
			case 11: c = new[] {RGB(166,206,227), RGB(031,120,180), RGB(178,223,138), RGB(051,160,044), RGB(251,154,153), RGB(227,026,028), RGB(253,191,111), RGB(255,127,000), RGB(202,178,214), RGB(106,061,154), RGB(255,255,153)}; break;
			default: c = new[] {RGB(166,206,227), RGB(031,120,180), RGB(178,223,138), RGB(051,160,044), RGB(251,154,153), RGB(227,026,028), RGB(253,191,111), RGB(255,127,000), RGB(202,178,214), RGB(106,061,154), RGB(255,255,153), RGB(177,089,040)}; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Qualitative;
			var @blind = new[] {1, 1, 2, 2, 2, 2, 0, 0, 0, 0};
			var @print = new[] {1, 1, 1, 1, 1, 2, 2, 2, 2, 2};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 1, 1, 1, 1, 1, 2, 2};

			return new Palette(c, "Paired", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette Pastel2 (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(179,226,205), RGB(253,205,172), RGB(203,213,232)}; break;
			case 04: c = new[] {RGB(179,226,205), RGB(253,205,172), RGB(203,213,232), RGB(244,202,228)}; break;
			case 05: c = new[] {RGB(179,226,205), RGB(253,205,172), RGB(203,213,232), RGB(244,202,228), RGB(230,245,201)}; break;
			case 06: c = new[] {RGB(179,226,205), RGB(253,205,172), RGB(203,213,232), RGB(244,202,228), RGB(230,245,201), RGB(255,242,174)}; break;
			case 07: c = new[] {RGB(179,226,205), RGB(253,205,172), RGB(203,213,232), RGB(244,202,228), RGB(230,245,201), RGB(255,242,174), RGB(241,226,204)}; break;
			default: c = new[] { RGB(179, 226, 205), RGB(253, 205, 172), RGB(203, 213, 232), RGB(244, 202, 228), RGB(230, 245, 201), RGB(255, 242, 174), RGB(241, 226, 204), RGB(204, 204, 204) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Qualitative;
			var @blind = new[] {2, 0, 0, 0, 0, 0};
			var @print = new[] {2, 0, 0, 0, 0, 0};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {2, 2, 0, 0, 0, 0};

			return new Palette(c, "Pastel2", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette Pastel1 (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(251,180,174), RGB(179,205,227), RGB(204,235,197)}; break;
			case 04: c = new[] {RGB(251,180,174), RGB(179,205,227), RGB(204,235,197), RGB(222,203,228)}; break;
			case 05: c = new[] {RGB(251,180,174), RGB(179,205,227), RGB(204,235,197), RGB(222,203,228), RGB(254,217,166)}; break;
			case 06: c = new[] {RGB(251,180,174), RGB(179,205,227), RGB(204,235,197), RGB(222,203,228), RGB(254,217,166), RGB(255,255,204)}; break;
			case 07: c = new[] {RGB(251,180,174), RGB(179,205,227), RGB(204,235,197), RGB(222,203,228), RGB(254,217,166), RGB(255,255,204), RGB(229,216,189)}; break;
			case 08: c = new[] {RGB(251,180,174), RGB(179,205,227), RGB(204,235,197), RGB(222,203,228), RGB(254,217,166), RGB(255,255,204), RGB(229,216,189), RGB(253,218,236)}; break;
			default: c = new[] { RGB(251, 180, 174), RGB(179, 205, 227), RGB(204, 235, 197), RGB(222, 203, 228), RGB(254, 217, 166), RGB(255, 255, 204), RGB(229, 216, 189), RGB(253, 218, 236), RGB(242, 242, 242) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Qualitative;
			var @blind = new[] {2, 0, 0, 0, 0, 0, 0};
			var @print = new[] {2, 2, 2, 0, 0, 0, 0};
			var @xerox = new[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {2, 2, 2, 2, 0, 0, 0};

			return new Palette(c, "Pastel1", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		/*** Sequential ***/
		public static Palette OrRd (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(254,232,200), RGB(253,187,132), RGB(227,074,051)}; break;
			case 04: c = new[] {RGB(254,240,217), RGB(253,204,138), RGB(252,141,089), RGB(215,048,031)}; break;
			case 05: c = new[] {RGB(254,240,217), RGB(253,204,138), RGB(252,141,089), RGB(227,074,051), RGB(179,000,0)}; break;
			case 06: c = new[] {RGB(254,240,217), RGB(253,212,158), RGB(253,187,132), RGB(252,141,089), RGB(227,074,051), RGB(179,000,0)}; break;
			case 07: c = new[] {RGB(254,240,217), RGB(253,212,158), RGB(253,187,132), RGB(252,141,089), RGB(239,101,072), RGB(215,048,031), RGB(153,000,0)}; break;
			case 08: c = new[] {RGB(255,247,236), RGB(254,232,200), RGB(253,212,158), RGB(253,187,132), RGB(252,141,089), RGB(239,101,072), RGB(215,048,031), RGB(153,000,0)}; break;
			default: c = new[] { RGB(255, 247, 236), RGB(254, 232, 200), RGB(253, 212, 158), RGB(253, 187, 132), RGB(252, 141, 089), RGB(239, 101, 072), RGB(215, 048, 031), RGB(179, 000, 0), RGB(127, 000, 0) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 0, 0, 0, 0, 0};
			var @xerox = new[] {1, 1, 2, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 0, 0, 0, 0};

			return new Palette(c, "OrRd", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette PuBu (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(236,231,242), RGB(166,189,219), RGB(043,140,190)}; break;
			case 04: c = new[] {RGB(241,238,246), RGB(189,201,225), RGB(116,169,207), RGB(005,112,176)}; break;
			case 05: c = new[] {RGB(241,238,246), RGB(189,201,225), RGB(116,169,207), RGB(043,140,190), RGB(004,090,141)}; break;
			case 06: c = new[] {RGB(241,238,246), RGB(208,209,230), RGB(166,189,219), RGB(116,169,207), RGB(043,140,190), RGB(004,090,141)}; break;
			case 07: c = new[] {RGB(241,238,246), RGB(208,209,230), RGB(166,189,219), RGB(116,169,207), RGB(054,144,192), RGB(005,112,176), RGB(003,078,123)}; break;
			case 08: c = new[] {RGB(255,247,251), RGB(236,231,242), RGB(208,209,230), RGB(166,189,219), RGB(116,169,207), RGB(054,144,192), RGB(005,112,176), RGB(003,078,123)}; break;
			default: c = new[] { RGB(255, 247, 251), RGB(236, 231, 242), RGB(208, 209, 230), RGB(166, 189, 219), RGB(116, 169, 207), RGB(054, 144, 192), RGB(005, 112, 176), RGB(004, 090, 141), RGB(002, 056, 088) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 2, 2, 0, 0, 0, 0};
			var @xerox = new[] {1, 2, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 2, 0, 0, 0, 0};

			return new Palette(c, "PuBu", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette BuPu (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(224,236,244), RGB(158,188,218), RGB(136,086,167)}; break;
			case 04: c = new[] {RGB(237,248,251), RGB(179,205,227), RGB(140,150,198), RGB(136,065,157)}; break;
			case 05: c = new[] {RGB(237,248,251), RGB(179,205,227), RGB(140,150,198), RGB(136,086,167), RGB(129,015,124)}; break;
			case 06: c = new[] {RGB(237,248,251), RGB(191,211,230), RGB(158,188,218), RGB(140,150,198), RGB(136,086,167), RGB(129,015,124)}; break;
			case 07: c = new[] {RGB(237,248,251), RGB(191,211,230), RGB(158,188,218), RGB(140,150,198), RGB(140,107,177), RGB(136,065,157), RGB(110,001,107)}; break;
			case 08: c = new[] {RGB(247,252,253), RGB(224,236,244), RGB(191,211,230), RGB(158,188,218), RGB(140,150,198), RGB(140,107,177), RGB(136,065,157), RGB(110,001,107)}; break;
			default: c = new[] { RGB(247, 252, 253), RGB(224, 236, 244), RGB(191, 211, 230), RGB(158, 188, 218), RGB(140, 150, 198), RGB(140, 107, 177), RGB(136, 065, 157), RGB(129, 015, 124), RGB(077, 000, 075) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 2, 2, 0, 0, 0};
			var @xerox = new[] {1, 2, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 0, 0, 0, 0};

			return new Palette(c, "BuPu", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette Oranges (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(254,230,206), RGB(253,174,107), RGB(230,085,013)}; break;
			case 04: c = new[] {RGB(254,237,222), RGB(253,190,133), RGB(253,141,060), RGB(217,071,001)}; break;
			case 05: c = new[] {RGB(254,237,222), RGB(253,190,133), RGB(253,141,060), RGB(230,085,013), RGB(166,054,003)}; break;
			case 06: c = new[] {RGB(254,237,222), RGB(253,208,162), RGB(253,174,107), RGB(253,141,060), RGB(230,085,013), RGB(166,054,003)}; break;
			case 07: c = new[] {RGB(254,237,222), RGB(253,208,162), RGB(253,174,107), RGB(253,141,060), RGB(241,105,019), RGB(217,072,001), RGB(140,045,004)}; break;
			case 08: c = new[] {RGB(255,245,235), RGB(254,230,206), RGB(253,208,162), RGB(253,174,107), RGB(253,141,060), RGB(241,105,019), RGB(217,072,001), RGB(140,045,004)}; break;
			default: c = new[] { RGB(255, 245, 235), RGB(254, 230, 206), RGB(253, 208, 162), RGB(253, 174, 107), RGB(253, 141, 060), RGB(241, 105, 019), RGB(217, 072, 001), RGB(166, 054, 003), RGB(127, 039, 004) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 2, 0, 0, 0, 0, 0};
			var @xerox = new[] {1, 2, 2, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 0, 0, 0, 0};

			return new Palette(c, "Oranges", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette BuGn (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(229,245,249), RGB(153,216,201), RGB(044,162,095)}; break;
			case 04: c = new[] {RGB(237,248,251), RGB(178,226,226), RGB(102,194,164), RGB(035,139,069)}; break;
			case 05: c = new[] {RGB(237,248,251), RGB(178,226,226), RGB(102,194,164), RGB(044,162,095), RGB(000,109,044)}; break;
			case 06: c = new[] {RGB(237,248,251), RGB(204,236,230), RGB(153,216,201), RGB(102,194,164), RGB(044,162,095), RGB(000,109,044)}; break;
			case 07: c = new[] {RGB(237,248,251), RGB(204,236,230), RGB(153,216,201), RGB(102,194,164), RGB(065,174,118), RGB(035,139,069), RGB(000,088,036)}; break;
			case 08: c = new[] {RGB(247,252,253), RGB(229,245,249), RGB(204,236,230), RGB(153,216,201), RGB(102,194,164), RGB(065,174,118), RGB(035,139,069), RGB(000,088,036)}; break;
			default: c = new[] { RGB(247, 252, 253), RGB(229, 245, 249), RGB(204, 236, 230), RGB(153, 216, 201), RGB(102, 194, 164), RGB(065, 174, 118), RGB(035, 139, 069), RGB(000, 109, 044), RGB(000, 068, 027) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 2, 0, 0, 0, 0};
			var @xerox = new[] {1, 2, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 2, 0, 0, 0, 0, 0};

			return new Palette(c, "BuGn", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette YlOrBr (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(255,247,188), RGB(254,196,079), RGB(217,095,014)}; break;
			case 04: c = new[] {RGB(255,255,212), RGB(254,217,142), RGB(254,153,041), RGB(204,076,002)}; break;
			case 05: c = new[] {RGB(255,255,212), RGB(254,217,142), RGB(254,153,041), RGB(217,095,014), RGB(153,052,004)}; break;
			case 06: c = new[] {RGB(255,255,212), RGB(254,227,145), RGB(254,196,079), RGB(254,153,041), RGB(217,095,014), RGB(153,052,004)}; break;
			case 07: c = new[] {RGB(255,255,212), RGB(254,227,145), RGB(254,196,079), RGB(254,153,041), RGB(236,112,020), RGB(204,076,002), RGB(140,045,004)}; break;
			case 08: c = new[] {RGB(255,255,229), RGB(255,247,188), RGB(254,227,145), RGB(254,196,079), RGB(254,153,041), RGB(236,112,020), RGB(204,076,002), RGB(140,045,004)}; break;
			default: c = new[] { RGB(255, 255, 229), RGB(255, 247, 188), RGB(254, 227, 145), RGB(254, 196, 079), RGB(254, 153, 041), RGB(236, 112, 020), RGB(204, 076, 002), RGB(153, 052, 004), RGB(102, 037, 006) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 2, 0, 0, 0, 0};
			var @xerox = new[] {1, 2, 2, 0, 0, 0, 0};
			var @panel = new[] {1, 2, 0, 0, 0, 0, 0};

			return new Palette(c, "YlOrBr", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette YlGn (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(247,252,185), RGB(173,221,142), RGB(049,163,084)}; break;
			case 04: c = new[] {RGB(255,255,204), RGB(194,230,153), RGB(120,198,121), RGB(035,132,067)}; break;
			case 05: c = new[] {RGB(255,255,204), RGB(194,230,153), RGB(120,198,121), RGB(049,163,084), RGB(000,104,055)}; break;
			case 06: c = new[] {RGB(255,255,204), RGB(217,240,163), RGB(173,221,142), RGB(120,198,121), RGB(049,163,084), RGB(000,104,055)}; break;
			case 07: c = new[] {RGB(255,255,204), RGB(217,240,163), RGB(173,221,142), RGB(120,198,121), RGB(065,171,093), RGB(035,132,067), RGB(000,090,050)}; break;
			case 08: c = new[] {RGB(255,255,229), RGB(247,252,185), RGB(217,240,163), RGB(173,221,142), RGB(120,198,121), RGB(065,171,093), RGB(035,132,067), RGB(000,090,050)}; break;
			default: c = new[] { RGB(255, 255, 229), RGB(247, 252, 185), RGB(217, 240, 163), RGB(173, 221, 142), RGB(120, 198, 121), RGB(065, 171, 093), RGB(035, 132, 067), RGB(000, 104, 055), RGB(000, 069, 041) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 1, 0, 0, 0, 0};
			var @xerox = new[] {1, 2, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 0, 0, 0, 0};

			return new Palette(c, "YlGn", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette Reds (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(254,224,210), RGB(252,146,114), RGB(222,045,038)}; break;
			case 04: c = new[] {RGB(254,229,217), RGB(252,174,145), RGB(251,106,074), RGB(203,024,029)}; break;
			case 05: c = new[] {RGB(254,229,217), RGB(252,174,145), RGB(251,106,074), RGB(222,045,038), RGB(165,015,021)}; break;
			case 06: c = new[] {RGB(254,229,217), RGB(252,187,161), RGB(252,146,114), RGB(251,106,074), RGB(222,045,038), RGB(165,015,021)}; break;
			case 07: c = new[] {RGB(254,229,217), RGB(252,187,161), RGB(252,146,114), RGB(251,106,074), RGB(239,059,044), RGB(203,024,029), RGB(153,000,013)}; break;
			case 08: c = new[] {RGB(255,245,240), RGB(254,224,210), RGB(252,187,161), RGB(252,146,114), RGB(251,106,074), RGB(239,059,044), RGB(203,024,029), RGB(153,000,013)}; break;
			default: c = new[] { RGB(255, 245, 240), RGB(254, 224, 210), RGB(252, 187, 161), RGB(252, 146, 114), RGB(251, 106, 074), RGB(239, 059, 044), RGB(203, 024, 029), RGB(165, 015, 021), RGB(103, 000, 013) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 2, 2, 0, 0, 0, 0};
			var @xerox = new[] {1, 2, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 2, 0, 0, 0, 0, 0};

			return new Palette(c, "Reds", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette RdPu (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(253,224,221), RGB(250,159,181), RGB(197,027,138)}; break;
			case 04: c = new[] {RGB(254,235,226), RGB(251,180,185), RGB(247,104,161), RGB(174,001,126)}; break;
			case 05: c = new[] {RGB(254,235,226), RGB(251,180,185), RGB(247,104,161), RGB(197,027,138), RGB(122,001,119)}; break;
			case 06: c = new[] {RGB(254,235,226), RGB(252,197,192), RGB(250,159,181), RGB(247,104,161), RGB(197,027,138), RGB(122,001,119)}; break;
			case 07: c = new[] {RGB(254,235,226), RGB(252,197,192), RGB(250,159,181), RGB(247,104,161), RGB(221,052,151), RGB(174,001,126), RGB(122,001,119)}; break;
			case 08: c = new[] {RGB(255,247,243), RGB(253,224,221), RGB(252,197,192), RGB(250,159,181), RGB(247,104,161), RGB(221,052,151), RGB(174,001,126), RGB(122,001,119)}; break;
			default: c = new[] { RGB(255, 247, 243), RGB(253, 224, 221), RGB(252, 197, 192), RGB(250, 159, 181), RGB(247, 104, 161), RGB(221, 052, 151), RGB(174, 001, 126), RGB(122, 001, 119), RGB(073, 000, 106) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 1, 2, 0, 0, 0};
			var @xerox = new[] {1, 2, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 0, 0, 0, 0};

			return new Palette(c, "RdPu", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette Greens (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(229,245,224), RGB(161,217,155), RGB(049,163,084)}; break;
			case 04: c = new[] {RGB(237,248,233), RGB(186,228,179), RGB(116,196,118), RGB(035,139,069)}; break;
			case 05: c = new[] {RGB(237,248,233), RGB(186,228,179), RGB(116,196,118), RGB(049,163,084), RGB(000,109,044)}; break;
			case 06: c = new[] {RGB(237,248,233), RGB(199,233,192), RGB(161,217,155), RGB(116,196,118), RGB(049,163,084), RGB(000,109,044)}; break;
			case 07: c = new[] {RGB(237,248,233), RGB(199,233,192), RGB(161,217,155), RGB(116,196,118), RGB(065,171,093), RGB(035,139,069), RGB(000,090,050)}; break;
			case 08: c = new[] {RGB(247,252,245), RGB(229,245,224), RGB(199,233,192), RGB(161,217,155), RGB(116,196,118), RGB(065,171,093), RGB(035,139,069), RGB(000,090,050)}; break;
			default: c = new[] { RGB(247, 252, 245), RGB(229, 245, 224), RGB(199, 233, 192), RGB(161, 217, 155), RGB(116, 196, 118), RGB(065, 171, 093), RGB(035, 139, 069), RGB(000, 109, 044), RGB(000, 068, 027) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 0, 0, 0, 0, 0, 0};
			var @xerox = new[] {1, 2, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 2, 0, 0, 0, 0, 0};

			return new Palette(c, "Greens", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette YlGnBu (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(237,248,177), RGB(127,205,187), RGB(044,127,184)}; break;
			case 04: c = new[] {RGB(255,255,204), RGB(161,218,180), RGB(065,182,196), RGB(034,094,168)}; break;
			case 05: c = new[] {RGB(255,255,204), RGB(161,218,180), RGB(065,182,196), RGB(044,127,184), RGB(037,052,148)}; break;
			case 06: c = new[] {RGB(255,255,204), RGB(199,233,180), RGB(127,205,187), RGB(065,182,196), RGB(044,127,184), RGB(037,052,148)}; break;
			case 07: c = new[] {RGB(255,255,204), RGB(199,233,180), RGB(127,205,187), RGB(065,182,196), RGB(029,145,192), RGB(034,094,168), RGB(012,044,132)}; break;
			case 08: c = new[] {RGB(255,255,217), RGB(237,248,177), RGB(199,233,180), RGB(127,205,187), RGB(065,182,196), RGB(029,145,192), RGB(034,094,168), RGB(012,044,132)}; break;
			default: c = new[] { RGB(255, 255, 217), RGB(237, 248, 177), RGB(199, 233, 180), RGB(127, 205, 187), RGB(065, 182, 196), RGB(029, 145, 192), RGB(034, 094, 168), RGB(037, 052, 148), RGB(008, 029, 088) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 1, 2, 2, 2, 0};
			var @xerox = new[] {1, 2, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 2, 0, 0, 0, 0};

			return new Palette(c, "YlGnBu", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette Purples (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(239,237,245), RGB(188,189,220), RGB(117,107,177)}; break;
			case 04: c = new[] {RGB(242,240,247), RGB(203,201,226), RGB(158,154,200), RGB(106,081,163)}; break;
			case 05: c = new[] {RGB(242,240,247), RGB(203,201,226), RGB(158,154,200), RGB(117,107,177), RGB(084,039,143)}; break;
			case 06: c = new[] {RGB(242,240,247), RGB(218,218,235), RGB(188,189,220), RGB(158,154,200), RGB(117,107,177), RGB(084,039,143)}; break;
			case 07: c = new[] {RGB(242,240,247), RGB(218,218,235), RGB(188,189,220), RGB(158,154,200), RGB(128,125,186), RGB(106,081,163), RGB(074,020,134)}; break;
			case 08: c = new[] {RGB(252,251,253), RGB(239,237,245), RGB(218,218,235), RGB(188,189,220), RGB(158,154,200), RGB(128,125,186), RGB(106,081,163), RGB(074,020,134)}; break;
			default: c = new[] { RGB(252, 251, 253), RGB(239, 237, 245), RGB(218, 218, 235), RGB(188, 189, 220), RGB(158, 154, 200), RGB(128, 125, 186), RGB(106, 081, 163), RGB(084, 039, 143), RGB(063, 000, 125) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 0, 0, 0, 0, 0, 0};
			var @xerox = new[] {1, 2, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 0, 0, 0, 0, 0, 0};

			return new Palette(c, "Purples", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette GnBu (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(224,243,219), RGB(168,221,181), RGB(067,162,202)}; break;
			case 04: c = new[] {RGB(240,249,232), RGB(186,228,188), RGB(123,204,196), RGB(043,140,190)}; break;
			case 05: c = new[] {RGB(240,249,232), RGB(186,228,188), RGB(123,204,196), RGB(067,162,202), RGB(008,104,172)}; break;
			case 06: c = new[] {RGB(240,249,232), RGB(204,235,197), RGB(168,221,181), RGB(123,204,196), RGB(067,162,202), RGB(008,104,172)}; break;
			case 07: c = new[] {RGB(240,249,232), RGB(204,235,197), RGB(168,221,181), RGB(123,204,196), RGB(078,179,211), RGB(043,140,190), RGB(008,088,158)}; break;
			case 08: c = new[] {RGB(247,252,240), RGB(224,243,219), RGB(204,235,197), RGB(168,221,181), RGB(123,204,196), RGB(078,179,211), RGB(043,140,190), RGB(008,088,158)}; break;
			default: c = new[] { RGB(247, 252, 240), RGB(224, 243, 219), RGB(204, 235, 197), RGB(168, 221, 181), RGB(123, 204, 196), RGB(078, 179, 211), RGB(043, 140, 190), RGB(008, 104, 172), RGB(008, 064, 129) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 1, 2, 2, 2, 0};
			var @xerox = new[] {1, 2, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 2, 0, 0, 0, 0};

			return new Palette(c, "GnBu", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette Greys (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(240,240,240), RGB(189,189,189), RGB(099,099,099)}; break;
			case 04: c = new[] {RGB(247,247,247), RGB(204,204,204), RGB(150,150,150), RGB(082,082,082)}; break;
			case 05: c = new[] {RGB(247,247,247), RGB(204,204,204), RGB(150,150,150), RGB(099,099,099), RGB(037,037,037)}; break;
			case 06: c = new[] {RGB(247,247,247), RGB(217,217,217), RGB(189,189,189), RGB(150,150,150), RGB(099,099,099), RGB(037,037,037)}; break;
			case 07: c = new[] {RGB(247,247,247), RGB(217,217,217), RGB(189,189,189), RGB(150,150,150), RGB(115,115,115), RGB(082,082,082), RGB(037,037,037)}; break;
			case 08: c = new[] {RGB(255,255,255), RGB(240,240,240), RGB(217,217,217), RGB(189,189,189), RGB(150,150,150), RGB(115,115,115), RGB(082,082,082), RGB(037,037,037)}; break;
			default: c = new[] { RGB(255, 255, 255), RGB(240, 240, 240), RGB(217, 217, 217), RGB(189, 189, 189), RGB(150, 150, 150), RGB(115, 115, 115), RGB(082, 082, 082), RGB(037, 037, 037), RGB(000, 0, 000) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 2, 0, 0, 0, 0};
			var @xerox = new[] {1, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 2, 0, 0, 0, 0, 0};

			return new Palette(c, "Greys", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette YlOrRd (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(255,237,160), RGB(254,178,076), RGB(240,059,032)}; break;
			case 04: c = new[] {RGB(255,255,178), RGB(254,204,092), RGB(253,141,060), RGB(227,026,028)}; break;
			case 05: c = new[] {RGB(255,255,178), RGB(254,204,092), RGB(253,141,060), RGB(240,059,032), RGB(189,000,038)}; break;
			case 06: c = new[] {RGB(255,255,178), RGB(254,217,118), RGB(254,178,076), RGB(253,141,060), RGB(240,059,032), RGB(189,000,038)}; break;
			case 07: c = new[] {RGB(255,255,178), RGB(254,217,118), RGB(254,178,076), RGB(253,141,060), RGB(252,078,042), RGB(227,026,028), RGB(177,000,038)}; break;
			case 08: c = new[] {RGB(255,255,204), RGB(255,237,160), RGB(254,217,118), RGB(254,178,076), RGB(253,141,060), RGB(252,078,042), RGB(227,026,028), RGB(177,000,038)}; break;
			default: c = new[] { RGB(255, 255, 204), RGB(255, 237, 160), RGB(254, 217, 118), RGB(254, 178, 076), RGB(253, 141, 060), RGB(252, 078, 042), RGB(227, 026, 028), RGB(189, 000, 038), RGB(128, 000, 038) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 2, 2, 0, 0, 0};
			var @xerox = new[] {1, 2, 2, 0, 0, 0, 0};
			var @panel = new[] {1, 2, 2, 0, 0, 0, 0};

			return new Palette(c, "YlOrRd", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette PuRd (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(231,225,239), RGB(201,148,199), RGB(221,028,119)}; break;
			case 04: c = new[] {RGB(241,238,246), RGB(215,181,216), RGB(223,101,176), RGB(206,018,086)}; break;
			case 05: c = new[] {RGB(241,238,246), RGB(215,181,216), RGB(223,101,176), RGB(221,028,119), RGB(152,000,067)}; break;
			case 06: c = new[] {RGB(241,238,246), RGB(212,185,218), RGB(201,148,199), RGB(223,101,176), RGB(221,028,119), RGB(152,000,067)}; break;
			case 07: c = new[] {RGB(241,238,246), RGB(212,185,218), RGB(201,148,199), RGB(223,101,176), RGB(231,041,138), RGB(206,018,086), RGB(145,000,063)}; break;
			case 08: c = new[] {RGB(247,244,249), RGB(231,225,239), RGB(212,185,218), RGB(201,148,199), RGB(223,101,176), RGB(231,041,138), RGB(206,018,086), RGB(145,000,063)}; break;
			default: c = new[] {RGB(247,244,249), RGB(231,225,239), RGB(212,185,218), RGB(201,148,199), RGB(223,101,176), RGB(231,041,138), RGB(206,018,086), RGB(152,000,067), RGB(103,000,031)}; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 1, 1, 0, 0, 0, 0};
			var @xerox = new[] {1, 2, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 1, 0, 0, 0, 0};

			return new Palette(c, "PuRd", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette Blues (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(222,235,247), RGB(158,202,225), RGB(049,130,189)}; break;
			case 04: c = new[] {RGB(239,243,255), RGB(189,215,231), RGB(107,174,214), RGB(033,113,181)}; break;
			case 05: c = new[] {RGB(239,243,255), RGB(189,215,231), RGB(107,174,214), RGB(049,130,189), RGB(008,081,156)}; break;
			case 06: c = new[] {RGB(239,243,255), RGB(198,219,239), RGB(158,202,225), RGB(107,174,214), RGB(049,130,189), RGB(008,081,156)}; break;
			case 07: c = new[] {RGB(239,243,255), RGB(198,219,239), RGB(158,202,225), RGB(107,174,214), RGB(066,146,198), RGB(033,113,181), RGB(008,069,148)}; break;
			case 08: c = new[] {RGB(247,251,255), RGB(222,235,247), RGB(198,219,239), RGB(158,202,225), RGB(107,174,214), RGB(066,146,198), RGB(033,113,181), RGB(008,069,148)}; break;
			default: c = new[] { RGB(247, 251, 255), RGB(222, 235, 247), RGB(198, 219, 239), RGB(158, 202, 225), RGB(107, 174, 214), RGB(066, 146, 198), RGB(033, 113, 181), RGB(008, 081, 156), RGB(008, 048, 107) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 2, 0, 0, 0, 0, 0};
			var @xerox = new[] {1, 0, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 2, 0, 0, 0, 0, 0};

			return new Palette(c, "Blues", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);

		}
		public static Palette PuBuGn (int size) {
			Color32[] c;
			switch (size) {
			case 03: c = new[] {RGB(236,226,240), RGB(166,189,219), RGB(028,144,153)}; break;
			case 04: c = new[] {RGB(246,239,247), RGB(189,201,225), RGB(103,169,207), RGB(002,129,138)}; break;
			case 05: c = new[] {RGB(246,239,247), RGB(189,201,225), RGB(103,169,207), RGB(028,144,153), RGB(001,108,089)}; break;
			case 06: c = new[] {RGB(246,239,247), RGB(208,209,230), RGB(166,189,219), RGB(103,169,207), RGB(028,144,153), RGB(001,108,089)}; break;
			case 07: c = new[] {RGB(246,239,247), RGB(208,209,230), RGB(166,189,219), RGB(103,169,207), RGB(054,144,192), RGB(002,129,138), RGB(001,100,080)}; break;
			case 08: c = new[] {RGB(255,247,251), RGB(236,226,240), RGB(208,209,230), RGB(166,189,219), RGB(103,169,207), RGB(054,144,192), RGB(002,129,138), RGB(001,100,080)}; break;
			default: c = new[] { RGB(255, 247, 251), RGB(236, 226, 240), RGB(208, 209, 230), RGB(166, 189, 219), RGB(103, 169, 207), RGB(054, 144, 192), RGB(002, 129, 138), RGB(001, 108, 089), RGB(001, 070, 054) }; break;
			//default: goto case 03;
			}
			if (size > c.Length) size = c.Length;
			var @type = Palette.Kind.Sequential;
			var @blind = new[] {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
			var @print = new[] {1, 2, 2, 0, 0, 0, 0};
			var @xerox = new[] {1, 2, 0, 0, 0, 0, 0};
			var @panel = new[] {1, 1, 2, 0, 0, 0, 0};

			return new Palette(c, "PuBuGn", @type, (Palette.Is) @blind[size-3], (Palette.Is) @print[size-3], (Palette.Is) @xerox[size-3], (Palette.Is) @panel[size-3]);
		}




	}

}