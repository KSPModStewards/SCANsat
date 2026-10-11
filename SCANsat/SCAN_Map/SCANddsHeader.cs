#region license
/*
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANddsHeader - just enough of a DDS file to load one mip level of it
 *
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.IO;
using System.Text;
using KSPTextureLoader;

namespace SCANsat.SCAN_Map
{
	/// <summary>
	/// Reads enough of a DDS file to load a single mip level of it through KSPTextureLoader's
	/// owned-texture API (LoadOwnedTexture2D(config, path, offset, length)): dimensions, mip count,
	/// pixel format and the byte range of each mip. The loader's own DDS parser is internal, so this
	/// is deliberately minimal and only claims the formats planet packs actually ship - BC1/3/4/5/6H/7
	/// by FourCC or DX10 header, and 32/24/8-bit uncompressed. Anything else is reported, not guessed.
	/// </summary>
	public sealed class SCANddsHeader
	{
		public int Width { get; private set; }
		public int Height { get; private set; }
		public int MipCount { get; private set; }
		public ExtendedTextureFormat Format { get; private set; }
		public bool Srgb { get; private set; }          // DX10 *_SRGB formats
		public bool Compressed { get; private set; }
		public int BlockBytes { get; private set; }     // per 4x4 block when compressed, else per pixel
		public long DataOffset { get; private set; }    // first byte of mip 0
		public long FileLength { get; private set; }    // what the mip ranges below have to fit inside
		public string FormatName { get; private set; }

		private const uint DDS_MAGIC = 0x20534444;      // "DDS "
		private const uint DDPF_FOURCC = 0x4;
		private const uint DDPF_RGB = 0x40;
		private const uint DDPF_LUMINANCE = 0x20000;
		private const uint DDSCAPS2_CUBEMAP = 0x200;    // dwCaps2, file offset 112
		private const uint DDSCAPS2_VOLUME = 0x200000;
		private const uint DDS_DIMENSION_TEXTURE2D = 3; // DX10 resourceDimension, file offset 132
		private const uint DDS_MISC_TEXTURECUBE = 0x4;  // DX10 miscFlag, file offset 136

		public static bool TryRead(string path, out SCANddsHeader header, out string error)
		{
			header = null;
			error = null;

			try
			{
				if (!File.Exists(path))
				{
					error = "file not found";
					return false;
				}

				byte[] h = new byte[148];
				int read;
				long fileLength;

				using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					fileLength = fs.Length;
					read = fs.Read(h, 0, h.Length);
				}

				if (read < 128 || BitConverter.ToUInt32(h, 0) != DDS_MAGIC || BitConverter.ToUInt32(h, 4) != 124)
				{
					error = "not a DDS file";
					return false;
				}

				SCANddsHeader d = new SCANddsHeader();
				d.Height = (int)BitConverter.ToUInt32(h, 12);
				d.Width = (int)BitConverter.ToUInt32(h, 16);
				d.MipCount = Math.Max(1, (int)BitConverter.ToUInt32(h, 28));
				d.DataOffset = 128;
				d.FileLength = fileLength;

				// A cubemap, a volume or an array does not lay its data out as the one mip chain running
				// from DataOffset that MipOffset walks: a cubemap repeats the chain per face, a volume
				// stores dwDepth slices per level, an array repeats it per slice. Every offset computed
				// below would land in the middle of the wrong face or slice, so say so instead of
				// handing the loader a plausible-looking byte range. dwDepth is 0 on a plain 2D file
				// and 1 on one written with DDSD_DEPTH, so only more than one slice is a volume.
				uint depth = BitConverter.ToUInt32(h, 24);
				uint caps2 = BitConverter.ToUInt32(h, 112);

				if ((caps2 & DDSCAPS2_CUBEMAP) != 0)
				{
					error = "cubemap DDS files are not supported";
					return false;
				}

				if ((caps2 & DDSCAPS2_VOLUME) != 0 || depth > 1)
				{
					error = "volume DDS files are not supported (depth " + depth + ")";
					return false;
				}

				uint pfFlags = BitConverter.ToUInt32(h, 80);
				string fourCC = Encoding.ASCII.GetString(h, 84, 4);
				uint bitCount = BitConverter.ToUInt32(h, 88);
				uint rMask = BitConverter.ToUInt32(h, 92);
				uint gMask = BitConverter.ToUInt32(h, 96);
				uint bMask = BitConverter.ToUInt32(h, 100);

				if ((pfFlags & DDPF_FOURCC) != 0 && fourCC == "DX10")
				{
					if (read < 148)
					{
						error = "truncated DX10 header";
						return false;
					}

					uint dxgi = BitConverter.ToUInt32(h, 128);
					uint dimension = BitConverter.ToUInt32(h, 132);
					uint miscFlag = BitConverter.ToUInt32(h, 136);
					uint arraySize = BitConverter.ToUInt32(h, 140);
					d.DataOffset = 148;

					// The DX10 header carries its own say on all three, and the legacy dwCaps2 of such a
					// file is usually 0 whatever it holds.
					if (dimension != DDS_DIMENSION_TEXTURE2D)
					{
						error = "DX10 resource dimension " + dimension + " is not a 2D texture";
						return false;
					}

					if ((miscFlag & DDS_MISC_TEXTURECUBE) != 0)
					{
						error = "cubemap DDS files are not supported";
						return false;
					}

					if (arraySize > 1)
					{
						error = "texture array DDS files are not supported (" + arraySize + " slices)";
						return false;
					}

					if (!d.setDxgi(dxgi))
					{
						error = "unsupported DXGI format " + dxgi;
						return false;
					}
				}
				else if ((pfFlags & DDPF_FOURCC) != 0)
				{
					if (!d.setFourCC(fourCC))
					{
						error = "unsupported FourCC " + fourCC;
						return false;
					}
				}
				else if ((pfFlags & DDPF_RGB) != 0 && bitCount == 32)
				{
					if (rMask == 0x00ff0000 && gMask == 0x0000ff00 && bMask == 0x000000ff)
					{
						d.set(ExtendedTextureFormat.BGRA32, "BGRA32", false, 4);
					}
					else if (rMask == 0x000000ff && gMask == 0x0000ff00 && bMask == 0x00ff0000)
					{
						d.set(ExtendedTextureFormat.RGBA32, "RGBA32", false, 4);
					}
					else
					{
						error = "unsupported 32-bit channel masks";
						return false;
					}
				}
				else if ((pfFlags & DDPF_RGB) != 0 && bitCount == 24)
				{
					d.set(ExtendedTextureFormat.RGB24, "RGB24", false, 3);
				}
				else if ((pfFlags & DDPF_LUMINANCE) != 0 && bitCount == 8)
				{
					d.set(ExtendedTextureFormat.R8, "R8", false, 1);
				}
				else
				{
					error = "unsupported pixel format (flags 0x" + pfFlags.ToString("X") + ", " + bitCount + " bpp)";
					return false;
				}

				if (d.Width <= 0 || d.Height <= 0)
				{
					error = "bad dimensions " + d.Width + "x" + d.Height;
					return false;
				}

				// A file too short for its own base level is unusable whichever level is asked for.
				// Levels further down the chain are checked one at a time by TryMipRange, so a file
				// missing only its tail still serves the levels it does hold.
				long needed = d.DataOffset + d.MipBytes(0);

				if (d.FileLength < needed)
				{
					error = "file is " + d.FileLength + " bytes, short of the " + needed + " a "
						+ d.Width + "x" + d.Height + " " + d.FormatName + " level 0 needs";
					return false;
				}

				header = d;
				return true;
			}
			catch (Exception e)
			{
				error = e.GetType().Name + ": " + e.Message;
				return false;
			}
		}

		private void set(ExtendedTextureFormat format, string name, bool compressed, int blockBytes, bool srgb = false)
		{
			Format = format;
			FormatName = name;
			Compressed = compressed;
			BlockBytes = blockBytes;
			Srgb = srgb;
		}

		private bool setFourCC(string fourCC)
		{
			switch (fourCC)
			{
				case "DXT1": set(ExtendedTextureFormat.DXT1, "DXT1/BC1", true, 8); return true;
				case "DXT5": set(ExtendedTextureFormat.DXT5, "DXT5/BC3", true, 16); return true;
				case "ATI1":
				case "BC4U": set(ExtendedTextureFormat.BC4, "BC4", true, 8); return true;
				case "ATI2":
				case "BC5U": set(ExtendedTextureFormat.BC5, "BC5", true, 16); return true;
				default: return false;
			}
		}

		private bool setDxgi(uint dxgi)
		{
			switch (dxgi)
			{
				case 71: set(ExtendedTextureFormat.DXT1, "BC1", true, 8); return true;
				case 72: set(ExtendedTextureFormat.DXT1, "BC1 sRGB", true, 8, true); return true;
				case 77: set(ExtendedTextureFormat.DXT5, "BC3", true, 16); return true;
				case 78: set(ExtendedTextureFormat.DXT5, "BC3 sRGB", true, 16, true); return true;
				case 80: set(ExtendedTextureFormat.BC4, "BC4", true, 8); return true;
				case 83: set(ExtendedTextureFormat.BC5, "BC5", true, 16); return true;
				case 95:
				case 96: set(ExtendedTextureFormat.BC6H, "BC6H", true, 16); return true;
				case 98: set(ExtendedTextureFormat.BC7, "BC7", true, 16); return true;
				case 99: set(ExtendedTextureFormat.BC7, "BC7 sRGB", true, 16, true); return true;
				case 28: set(ExtendedTextureFormat.RGBA32, "RGBA32", false, 4); return true;
				case 29: set(ExtendedTextureFormat.RGBA32, "RGBA32 sRGB", false, 4, true); return true;
				case 87: set(ExtendedTextureFormat.BGRA32, "BGRA32", false, 4); return true;
				case 91: set(ExtendedTextureFormat.BGRA32, "BGRA32 sRGB", false, 4, true); return true;
				case 61: set(ExtendedTextureFormat.R8, "R8", false, 1); return true;
				default: return false;
			}
		}

		public int MipWidth(int mip)
		{
			return Math.Max(1, Width >> mip);
		}

		public int MipHeight(int mip)
		{
			return Math.Max(1, Height >> mip);
		}

		public long MipBytes(int mip)
		{
			if (Compressed)
			{
				long blocksWide = Math.Max(1, (MipWidth(mip) + 3) / 4);
				long blocksHigh = Math.Max(1, (MipHeight(mip) + 3) / 4);
				return blocksWide * blocksHigh * BlockBytes;
			}

			return (long)MipWidth(mip) * MipHeight(mip) * BlockBytes;
		}

		public long MipOffset(int mip)
		{
			long offset = DataOffset;

			for (int i = 0; i < mip; i++)
			{
				offset += MipBytes(i);
			}

			return offset;
		}

		/// <summary>
		/// The byte range of one mip level, checked against the file's actual length. dwMipMapCount is
		/// the file's own claim and nothing verifies that the data behind it was written, so a truncated
		/// or lying file is caught here rather than by handing the loader a range past the end.
		/// </summary>
		public bool TryMipRange(int mip, out long offset, out long length, out string error)
		{
			offset = 0;
			length = 0;
			error = null;

			if (mip < 0 || mip >= MipCount)
			{
				error = "mip " + mip + " is outside the file's " + MipCount + " level(s)";
				return false;
			}

			offset = MipOffset(mip);
			length = MipBytes(mip);

			if (offset + length > FileLength)
			{
				error = "mip " + mip + " (" + MipWidth(mip) + "x" + MipHeight(mip) + " " + FormatName
					+ ") needs bytes " + offset + " to " + (offset + length) + ", past the end of a "
					+ FileLength + " byte file";
				return false;
			}

			return true;
		}

		/// <summary>Smallest mip (largest index) whose width still covers targetWidth.</summary>
		public int MipForWidth(int targetWidth)
		{
			int mip = 0;

			while (mip + 1 < MipCount && MipWidth(mip + 1) >= targetWidth)
			{
				mip++;
			}

			return mip;
		}

		public Texture2DConfig ConfigForMip(int mip, bool linear)
		{
			return new Texture2DConfig
			{
				Width = MipWidth(mip),
				Height = MipHeight(mip),
				MipCount = 1,               // one level; Validate() would turn 0 into a full chain
				Format = Format,
				Readable = false,
				Linear = linear && !Srgb,
			};
		}
	}
}
