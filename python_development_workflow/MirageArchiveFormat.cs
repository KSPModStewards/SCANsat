using System;
using System.IO;

namespace Mirage.VirtualTexture;

// ─────────────────────────────────────────────────────────────────────────────
// Mirage Tile Archive — container format (the frozen §5/§3 byte spec).
//
// This file is the SINGLE SOURCE OF TRUTH for the on-disk byte layout. It is
// compiled into BOTH the runtime (Mirage.dll) and the offline packer
// (tools/ArchivePacker), so the writer and the reader can never drift. It is
// deliberately dependency-free — only System / System.IO, NO UnityEngine and NO
// KSP — so the standalone console packer can link this exact source.
//
// A body's canonical archive is a set of per-layer, per-level file pairs:
//   canonical.<layer>.L<N>.bin   (blob: header + tightly packed self-describing tiles)
//   canonical.<layer>.L<N>.idx   (index: header + sorted [key -> offset,length] entries)
//   canonical.manifest           (enumerates installed layers/levels + geometry)
// The runtime-generated web tier reuses the same blob/tile/index structs in a
// single append blob (web.color.bin/.idx).
//
// All multi-byte fields are little-endian (BinaryReader/BinaryWriter guarantee
// this on every platform). Offsets in an on-disk index are FILE-LOCAL; the
// in-RAM merged form stamps a fileId (see ArchiveTileSource) — the disk struct is
// unchanged.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Payload layer. Mirrors <c>VTLayer</c> in the runtime (kept separate so
/// this file stays Unity-free).</summary>
public enum ArchiveLayer : byte
{
    Color = 0,
    Height = 1,
    Normal = 2,
}

/// <summary>Per-tile payload codec. v1 writes only <see cref="None"/>; the other
/// values are reserved so adding compression is an index-compatible change (no
/// format bump).</summary>
public enum TileCodec : byte
{
    None = 0,
    Lz4 = 1,
    Zstd = 2,
    HeightPlaneSplitLz4 = 3,
    HeightVDeltaBitpack = 4,
}

/// <summary>Numeric texture-format codes. These are exactly Unity's
/// <c>TextureFormat</c> enum values, stored raw so this file needn't reference
/// UnityEngine; the runtime casts straight to <c>TextureFormat</c> /
/// <c>ExtendedTextureFormat</c>. Only the formats Mirage tiles actually use are
/// named here.</summary>
public static class ArchiveTextureFormat
{
    public const int RGBA32 = 4;
    public const int R16 = 9;
    public const int DXT1 = 10;
    public const int DXT5 = 12;
    public const int BC6H = 24;
    public const int BC7 = 25;
    public const int BC4 = 26;
    public const int BC5 = 27;
}

/// <summary>The frozen container constants + shared helpers (Morton key packing,
/// alignment, CRC32).</summary>
public static class MirageArchiveFormat
{
    public const ushort FormatVersion = 1;

    // Four-char magics (ASCII, written as raw bytes so there is no endian ambiguity).
    public const uint BlobMagic = 0x3141544D; // "MTA1" (M,T,A,1 little-endian)
    public const uint IndexMagic = 0x3149544D; // "MTI1"

    /// <summary>Tile starts are aligned to this many bytes inside a blob: BC blocks
    /// are 16 B, so 16-B alignment keeps CopyTexture/mmap happy and lets a payload
    /// go to the GPU without a realigning copy. Gap is ≤15 B/tile (alignment, not
    /// addressing — see design §3).</summary>
    public const int TileAlignment = 16;

    /// <summary>On-disk size of a framed <see cref="TileHeader"/> (fields padded to a
    /// 16-B multiple so the payload that follows is aligned too).</summary>
    public const int TileHeaderSize = 24;

    /// <summary>On-disk size of one <see cref="IndexEntry"/>.</summary>
    public const int IndexEntrySize = 22;

    // ── Morton key: (face<<60) | (level<<51) | interleave17(x,y) ─────────────────
    // face: 3 bits [60..62]; level: 9 bits [51..59]; interleaved x,y: 34 bits [0..33]
    // (x,y each up to 17 bits → level ≤ 17). Face+level in the high bits keep a
    // level's tiles contiguous in key-space (Morton ordering → read coalescing).

    public const int MaxCoordBits = 17;

    public static ulong PackKey(int face, int level, int x, int y)
    {
        if ((uint)face > 5u)
            throw new ArgumentOutOfRangeException(nameof(face));
        if ((uint)level > 511u)
            throw new ArgumentOutOfRangeException(nameof(level));
        if ((uint)x >= (1u << MaxCoordBits) || (uint)y >= (1u << MaxCoordBits))
            throw new ArgumentOutOfRangeException($"tile coord out of range: {x},{y}");

        ulong interleaved = Part1By1((uint)x) | (Part1By1((uint)y) << 1);
        return ((ulong)face << 60) | ((ulong)level << 51) | interleaved;
    }

    public static void UnpackKey(ulong key, out int face, out int level, out int x, out int y)
    {
        face = (int)((key >> 60) & 0x7);
        level = (int)((key >> 51) & 0x1FF);
        ulong interleaved = key & ((1UL << 34) - 1);
        x = (int)Compact1By1(interleaved);
        y = (int)Compact1By1(interleaved >> 1);
    }

    public static int KeyFace(ulong key) => (int)((key >> 60) & 0x7);

    public static int KeyLevel(ulong key) => (int)((key >> 51) & 0x1FF);

    // Spread the low 17 bits of x into even bit positions (0,2,4,...). The masks
    // are the standard Morton magic constants (they cover a full 32-bit input; a
    // 17-bit input is simply a subset).
    private static ulong Part1By1(uint v)
    {
        ulong x = v;
        x &= 0x1FFFF; // keep 17 bits
        x = (x | (x << 16)) & 0x0000FFFF0000FFFFUL;
        x = (x | (x << 8)) & 0x00FF00FF00FF00FFUL;
        x = (x | (x << 4)) & 0x0F0F0F0F0F0F0F0FUL;
        x = (x | (x << 2)) & 0x3333333333333333UL;
        x = (x | (x << 1)) & 0x5555555555555555UL;
        return x;
    }

    // Inverse of Part1By1: gather even bit positions back into a contiguous integer.
    private static uint Compact1By1(ulong x)
    {
        x &= 0x5555555555555555UL;
        x = (x | (x >> 1)) & 0x3333333333333333UL;
        x = (x | (x >> 2)) & 0x0F0F0F0F0F0F0F0FUL;
        x = (x | (x >> 4)) & 0x00FF00FF00FF00FFUL;
        x = (x | (x >> 8)) & 0x0000FFFF0000FFFFUL;
        x = (x | (x >> 16)) & 0x00000000FFFFFFFFUL;
        return (uint)x;
    }

    // ── Alignment ────────────────────────────────────────────────────────────────
    public static long AlignUp(long value, int alignment)
    {
        long m = value % alignment;
        return m == 0 ? value : value + (alignment - m);
    }

    // ── CRC32 (IEEE 802.3, poly 0xEDB88320) ──────────────────────────────────────
    // Implemented here rather than via System.IO.Hashing so the format stays
    // net4.8-compatible with no extra package.
    private static readonly uint[] s_Crc32Table = BuildCrc32Table();

    private static uint[] BuildCrc32Table()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[i] = c;
        }
        return table;
    }

    public static uint Crc32(byte[] data, int offset, int count)
    {
        uint crc = 0xFFFFFFFFu;
        int end = offset + count;
        for (int i = offset; i < end; i++)
            crc = s_Crc32Table[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }

    public static uint Crc32(byte[] data) => Crc32(data, 0, data.Length);

    // ── Per-tile codecs (§8) ──────────────────────────────────────────────────────
    // Tiles are stored compressed only when it beats raw by a margin (adaptive, per-tile), so incompressible
    // detail tiles cost nothing to read. LZ4 (fast decode, ~GB/s) over gzip everywhere. Height additionally
    // byte-plane-splits R16 before LZ4 (deinterleave lo/hi bytes — the hi plane is near-constant on smooth
    // terrain, so it packs to almost nothing). ENCODE is offline (packer, via K4os); DECODE lives here so the
    // runtime ships no third-party LZ4 dependency and the format stays self-describing.

    /// <summary>Raw (decoded) payload size in bytes for a tile of the given format + dimensions. Used to
    /// size the decode target; the raw length is never stored (it's implied by format + tile dims).</summary>
    public static int RawPayloadBytes(int format, int width, int height)
    {
        switch (format)
        {
            case ArchiveTextureFormat.DXT1:
            case ArchiveTextureFormat.BC4:
                return BlockCount(width, height) * 8;
            case ArchiveTextureFormat.DXT5:
            case ArchiveTextureFormat.BC5:
            case ArchiveTextureFormat.BC6H:
            case ArchiveTextureFormat.BC7:
                return BlockCount(width, height) * 16;
            case ArchiveTextureFormat.R16:
                return width * height * 2;
            case ArchiveTextureFormat.RGBA32:
                return width * height * 4;
            default:
                throw new ArgumentException($"RawPayloadBytes: unknown format {format}");
        }
    }

    private static int BlockCount(int width, int height) => ((width + 3) / 4) * ((height + 3) / 4);

    /// <summary>Should this layer's raw payload be plane-split before LZ4 (height R16 only)?</summary>
    public static bool UsePlaneSplit(int format) => format == ArchiveTextureFormat.R16;

    /// <summary>Deinterleave an R16 buffer (lo,hi,lo,hi,…) into [all lo bytes][all hi bytes]. Same length.</summary>
    public static byte[] PlaneSplitR16(byte[] r16)
    {
        int n = r16.Length / 2;
        var outp = new byte[r16.Length];
        for (int i = 0; i < n; i++)
        {
            outp[i] = r16[2 * i];
            outp[n + i] = r16[2 * i + 1];
        }
        return outp;
    }

    /// <summary>Inverse of <see cref="PlaneSplitR16"/>: reinterleave planes back to R16 into <paramref name="r16"/>.</summary>
    public static void PlaneUnsplitR16(byte[] planed, byte[] r16)
    {
        int n = r16.Length / 2;
        for (int i = 0; i < n; i++)
        {
            r16[2 * i] = planed[i];
            r16[2 * i + 1] = planed[n + i];
        }
    }

    // ── vdelta-bitpack (R16 height) ──────────────────────────────────────────────
    // Heightmaps are smooth in 2D — a property generic LZ4 cannot exploit, since it only finds repeated byte
    // strings. This codec instead predicts each texel from the one directly ABOVE it (row 0 from its left
    // neighbour), zigzag-maps the signed residual to unsigned (so small negatives stay small), then bit-packs
    // each block of 64 residuals at exactly that block's required bit width. Gently-sloped blocks collapse to
    // a few bits per texel and perfectly flat ones to width 0 (no bits at all). Decode is a tight shift/mask
    // loop with no match-copies or back-references, so it is markedly faster than LZ4 as well as smaller.
    //
    // All arithmetic is mod-2^16, so the transform is exactly invertible on any input (wrap-safe) — lossless
    // even across a cliff where the residual overflows.
    //
    // Payload layout (self-describing, so DecodeTilePayload needs no dims):
    //   u16 width | u16 height | u8 blockLog | u8 widths[nblocks] | LSB-first bitstream
    private const int VDeltaBlockLog = 6; // 64 residuals per block: ~0.2% width-table overhead
    private const int VDeltaHeaderBytes = 5;

    private static ushort LoadR16(byte[] b, int i) => (ushort)(b[2 * i] | (b[2 * i + 1] << 8));

    private static int BitsFor(ushort v)
    {
        int n = 0;
        while (v != 0)
        {
            n++;
            v >>= 1;
        }
        return n;
    }

    /// <summary>Encode a raw little-endian R16 buffer with vertical-delta + zigzag + per-block bitpacking.</summary>
    public static byte[] VDeltaBitpackEncode(byte[] r16, int width, int height)
    {
        int count = width * height;
        if (r16.Length < count * 2)
            throw new ArgumentException("vdelta: source shorter than width*height*2");

        // Pass 1 — residuals. Needed up front because each block's bit width is the max over its residuals.
        var zz = new ushort[count];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            ushort v = LoadR16(r16, i);
            ushort pred =
                y == 0 ? (x == 0 ? (ushort)0 : LoadR16(r16, i - 1)) : LoadR16(r16, i - width);
            short d = (short)(v - pred); // truncation = mod-2^16 wrap
            zz[i] = (ushort)(((d << 1) ^ (d >> 15)) & 0xFFFF);
        }

        int blockSize = 1 << VDeltaBlockLog;
        int nblocks = (count + blockSize - 1) / blockSize;
        var widths = new byte[nblocks];
        long bits = 0;
        for (int b = 0; b < nblocks; b++)
        {
            int s = b * blockSize,
                e = Math.Min(s + blockSize, count);
            ushort max = 0;
            for (int i = s; i < e; i++)
                if (zz[i] > max)
                    max = zz[i];
            widths[b] = (byte)BitsFor(max);
            bits += (long)widths[b] * (e - s);
        }

        var outp = new byte[VDeltaHeaderBytes + nblocks + (int)((bits + 7) / 8)];
        outp[0] = (byte)width;
        outp[1] = (byte)(width >> 8);
        outp[2] = (byte)height;
        outp[3] = (byte)(height >> 8);
        outp[4] = VDeltaBlockLog;
        Buffer.BlockCopy(widths, 0, outp, VDeltaHeaderBytes, nblocks);

        // Pass 2 — bitstream. accBits stays < 8 after each flush, so accBits + w <= 23 never overflows acc.
        int p = VDeltaHeaderBytes + nblocks;
        ulong acc = 0;
        int accBits = 0;
        for (int b = 0; b < nblocks; b++)
        {
            int w = widths[b];
            if (w == 0)
                continue;
            int s = b * blockSize,
                e = Math.Min(s + blockSize, count);
            for (int i = s; i < e; i++)
            {
                acc |= (ulong)zz[i] << accBits;
                accBits += w;
                while (accBits >= 8)
                {
                    outp[p++] = (byte)acc;
                    acc >>= 8;
                    accBits -= 8;
                }
            }
        }
        if (accBits > 0)
            outp[p++] = (byte)acc;
        return outp;
    }

    /// <summary>Inverse of <see cref="VDeltaBitpackEncode"/>: unpack into a raw little-endian R16 buffer.
    /// Single pass — each texel's predictor is read back from the part of <paramref name="r16"/> already
    /// written (the row above, or the texel to the left on row 0), so no scratch buffer is needed.</summary>
    public static void VDeltaBitpackDecode(
        byte[] src,
        int srcOffset,
        int srcLen,
        byte[] r16,
        int width,
        int height
    )
    {
        if (srcLen < VDeltaHeaderBytes)
            throw new InvalidDataException("vdelta: truncated header");
        int w0 = src[srcOffset] | (src[srcOffset + 1] << 8);
        int h0 = src[srcOffset + 2] | (src[srcOffset + 3] << 8);
        int blockLog = src[srcOffset + 4];
        if (w0 != width || h0 != height)
            throw new InvalidDataException(
                $"vdelta: payload dims {w0}x{h0} != expected {width}x{height}"
            );
        if (blockLog < 1 || blockLog > 16)
            throw new InvalidDataException($"vdelta: bad blockLog {blockLog}");

        int count = width * height;
        if (r16.Length < count * 2)
            throw new ArgumentException("vdelta: destination shorter than width*height*2");

        int blockSize = 1 << blockLog;
        int nblocks = (count + blockSize - 1) / blockSize;
        int wp = srcOffset + VDeltaHeaderBytes;
        if (srcLen < VDeltaHeaderBytes + nblocks)
            throw new InvalidDataException("vdelta: truncated block-width table");

        int p = wp + nblocks;
        int sEnd = srcOffset + srcLen;
        ulong acc = 0;
        int accBits = 0;

        for (int b = 0; b < nblocks; b++)
        {
            int bw = src[wp + b];
            if (bw > 16)
                throw new InvalidDataException($"vdelta: bad block width {bw}");
            int s = b * blockSize,
                e = Math.Min(s + blockSize, count);
            int x = s % width,
                y = s / width; // two divisions per block, not per texel
            for (int i = s; i < e; i++)
            {
                uint z = 0;
                if (bw != 0)
                {
                    while (accBits < bw)
                    {
                        if (p >= sEnd)
                            throw new InvalidDataException("vdelta: bitstream underrun");
                        acc |= (ulong)src[p++] << accBits;
                        accBits += 8;
                    }
                    z = (uint)(acc & ((1UL << bw) - 1));
                    acc >>= bw;
                    accBits -= bw;
                }
                short d = (short)((z >> 1) ^ (uint)(-(int)(z & 1))); // un-zigzag
                ushort pred =
                    y == 0 ? (x == 0 ? (ushort)0 : LoadR16(r16, i - 1)) : LoadR16(r16, i - width);
                ushort v = (ushort)(pred + d);
                r16[2 * i] = (byte)v;
                r16[2 * i + 1] = (byte)(v >> 8);
                if (++x == width)
                {
                    x = 0;
                    y++;
                }
            }
        }
    }

    /// <summary>
    /// Pick the codec for a tile being baked into a <b>web</b> archive at runtime and return the bytes to
    /// store. Unlike the offline packer — which links K4os and can afford to try every codec and keep the
    /// smallest — the runtime ships no third-party compressor, so the choice here is between the codecs whose
    /// ENCODER is in this file. That is exactly one: vdelta-bitpack, which is pure managed C# with no
    /// dependency, and is both smaller and faster to decode than LZ4 on R16 anyway. Everything else (BC7
    /// color, BC5 normals) stores raw: LZ4 is the only thing that helps BCn and we cannot encode it here, and
    /// BCn is already block-compressed, so raw costs nothing but disk.
    /// </summary>
    public static byte[] EncodeForWeb(
        byte[] raw,
        int format,
        int width,
        int height,
        out TileCodec codec
    )
    {
        if (format == ArchiveTextureFormat.R16)
        {
            byte[] packed = VDeltaBitpackEncode(raw, width, height);
            // Pathological input (pure noise) can bit-pack larger than the source; store raw if so.
            if (packed.Length < raw.Length)
            {
                codec = TileCodec.HeightVDeltaBitpack;
                return packed;
            }
        }
        codec = TileCodec.None;
        return raw;
    }

    /// <summary>Decode a stored tile payload (as read from the blob) into its raw texture bytes. The raw
    /// length is derived from the format + tile dims by the caller (not stored). Throws on a malformed stream.</summary>
    public static void DecodeTilePayload(
        TileCodec codec,
        byte[] stored,
        int storedLen,
        byte[] raw,
        int rawLen
    )
    {
        switch (codec)
        {
            case TileCodec.None:
                Array.Copy(stored, 0, raw, 0, rawLen);
                return;
            case TileCodec.Lz4:
            {
                int n = Lz4DecompressBlock(stored, 0, storedLen, raw, rawLen);
                if (n != rawLen)
                    throw new InvalidDataException(
                        $"LZ4 decode produced {n} bytes, expected {rawLen}"
                    );
                return;
            }
            case TileCodec.HeightPlaneSplitLz4:
            {
                var planed = new byte[rawLen]; // plane-split is length-preserving
                int n = Lz4DecompressBlock(stored, 0, storedLen, planed, rawLen);
                if (n != rawLen)
                    throw new InvalidDataException(
                        $"LZ4 decode produced {n} bytes, expected {rawLen}"
                    );
                PlaneUnsplitR16(planed, raw);
                return;
            }
            case TileCodec.HeightVDeltaBitpack:
            {
                // Dims come from the payload's own header, so this needs no extra caller context.
                if (storedLen < VDeltaHeaderBytes)
                    throw new InvalidDataException("vdelta: truncated header");
                int w = stored[0] | (stored[1] << 8);
                int h = stored[2] | (stored[3] << 8);
                if (w * h * 2 != rawLen)
                    throw new InvalidDataException(
                        $"vdelta: payload dims {w}x{h} imply {w * h * 2} raw bytes, expected {rawLen}"
                    );
                VDeltaBitpackDecode(stored, 0, storedLen, raw, w, h);
                return;
            }
            default:
                throw new InvalidDataException($"unknown tile codec {codec}");
        }
    }

    /// <summary>
    /// Decompress one LZ4 <b>block</b> (not frame) format buffer. Standard LZ4 sequences: a token byte
    /// (high nibble = literal length, low nibble = match length−4), optional extended-length bytes (0xFF
    /// continues), the literals, then a 2-byte little-endian back-offset and the match copy (byte-wise to
    /// honour overlap). Interoperates with K4os <c>LZ4Codec.Encode</c> used by the packer. Returns the number
    /// of decoded bytes.
    /// </summary>
    public static int Lz4DecompressBlock(
        byte[] src,
        int srcOffset,
        int srcLen,
        byte[] dst,
        int dstCap
    )
    {
        int s = srcOffset;
        int sEnd = srcOffset + srcLen;
        int d = 0;

        while (s < sEnd)
        {
            int token = src[s++];

            int litLen = token >> 4;
            if (litLen == 0xF)
            {
                int b;
                do
                {
                    b = src[s++];
                    litLen += b;
                } while (b == 0xFF);
            }

            if (d + litLen > dstCap || s + litLen > sEnd)
                throw new InvalidDataException("LZ4: corrupt literal run");
            for (int i = 0; i < litLen; i++)
                dst[d++] = src[s++];

            if (s >= sEnd)
                break; // final sequence is literals-only (no match)

            int offset = src[s] | (src[s + 1] << 8);
            s += 2;
            if (offset == 0 || offset > d)
                throw new InvalidDataException("LZ4: bad match offset");

            int matchLen = token & 0xF;
            if (matchLen == 0xF)
            {
                int b;
                do
                {
                    b = src[s++];
                    matchLen += b;
                } while (b == 0xFF);
            }
            matchLen += 4; // minmatch

            if (d + matchLen > dstCap)
                throw new InvalidDataException("LZ4: match overruns output");
            int m = d - offset;
            for (int i = 0; i < matchLen; i++)
                dst[d++] = dst[m++]; // byte-wise: handles overlapping (offset < matchLen) runs
        }
        return d;
    }

    // ── Magic helpers ────────────────────────────────────────────────────────────
    internal static void WriteMagic(BinaryWriter w, uint magic) => w.Write(magic);

    internal static void ExpectMagic(BinaryReader r, uint expected, string what)
    {
        uint got = r.ReadUInt32();
        if (got != expected)
            throw new InvalidDataException(
                $"Mirage archive: bad {what} magic 0x{got:X8} (expected 0x{expected:X8})"
            );
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Blob header — one per <layer>.L<N>.bin, followed by tightly packed tiles.
// ─────────────────────────────────────────────────────────────────────────────
public struct BlobHeader
{
    public ushort version;
    public ArchiveLayer layer;
    public int format; // Unity TextureFormat code (ArchiveTextureFormat.*)
    public ushort tileSize; // inner tile dim (no border), e.g. 256
    public ushort borderPx; // per-side border, e.g. 4
    public byte faceCount; // 6
    public uint flags;

    public void Write(BinaryWriter w)
    {
        MirageArchiveFormat.WriteMagic(w, MirageArchiveFormat.BlobMagic);
        w.Write(version);
        w.Write((byte)layer);
        w.Write(format);
        w.Write(tileSize);
        w.Write(borderPx);
        w.Write(faceCount);
        w.Write(flags);
    }

    public static BlobHeader Read(BinaryReader r)
    {
        MirageArchiveFormat.ExpectMagic(r, MirageArchiveFormat.BlobMagic, "blob");
        return new BlobHeader
        {
            version = r.ReadUInt16(),
            layer = (ArchiveLayer)r.ReadByte(),
            format = r.ReadInt32(),
            tileSize = r.ReadUInt16(),
            borderPx = r.ReadUInt16(),
            faceCount = r.ReadByte(),
            flags = r.ReadUInt32(),
        };
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Tile header — precedes every payload in a blob (self-describing so the web
// index can be rebuilt from the blob alone; harmless overhead on canonical).
// 24 bytes total; the payload that follows starts 16-B aligned.
// ─────────────────────────────────────────────────────────────────────────────
public struct TileHeader
{
    public ulong key; // Morton(face,level,x,y)
    public uint payloadLen; // compressed/stored byte length that follows
    public TileCodec codec;
    public byte format; // Unity TextureFormat code (fits in a byte for all Mirage formats)
    public uint crc32; // CRC32 over the payload bytes

    public void Write(BinaryWriter w)
    {
        w.Write(key); // 8
        w.Write(payloadLen); // 4
        w.Write((byte)codec); // 1
        w.Write(format); // 1
        w.Write(crc32); // 4
        w.Write((ushort)0); // pad  2
        w.Write((ushort)0); // pad  2
        w.Write((ushort)0); // pad  2  → 24 total
    }

    public static TileHeader Read(BinaryReader r)
    {
        var h = new TileHeader
        {
            key = r.ReadUInt64(),
            payloadLen = r.ReadUInt32(),
            codec = (TileCodec)r.ReadByte(),
            format = r.ReadByte(),
            crc32 = r.ReadUInt32(),
        };
        r.ReadUInt16();
        r.ReadUInt16();
        r.ReadUInt16(); // pad
        return h;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Index — one per <layer>.L<N>.{idx}. Header + entryCount sorted entries.
// blobLength is the staleness sentinel: it must equal the paired .bin file size.
// ─────────────────────────────────────────────────────────────────────────────
public struct IndexHeader
{
    public ushort version;
    public ArchiveLayer layer;
    public int level;
    public int entryCount;
    public long blobLength; // size of the paired .bin, for the staleness check

    public void Write(BinaryWriter w)
    {
        MirageArchiveFormat.WriteMagic(w, MirageArchiveFormat.IndexMagic);
        w.Write(version);
        w.Write((byte)layer);
        w.Write(level);
        w.Write(entryCount);
        w.Write(blobLength);
    }

    public static IndexHeader Read(BinaryReader r)
    {
        MirageArchiveFormat.ExpectMagic(r, MirageArchiveFormat.IndexMagic, "index");
        return new IndexHeader
        {
            version = r.ReadUInt16(),
            layer = (ArchiveLayer)r.ReadByte(),
            level = r.ReadInt32(),
            entryCount = r.ReadInt32(),
            blobLength = r.ReadInt64(),
        };
    }
}

/// <summary>On-disk index entry (22 B). The file-local <see cref="offset"/> points
/// at the tile's <see cref="TileHeader"/> in the paired blob.</summary>
public struct IndexEntry
{
    public ulong key;
    public ulong offset; // file-local byte offset of the TileHeader
    public uint length; // payload length (bytes after the 24-B tile header)
    public TileCodec codec;
    public byte format;

    public void Write(BinaryWriter w)
    {
        w.Write(key); // 8
        w.Write(offset); // 8
        w.Write(length); // 4
        w.Write((byte)codec); // 1
        w.Write(format); // 1  → 22
    }

    public static IndexEntry Read(BinaryReader r) =>
        new IndexEntry
        {
            key = r.ReadUInt64(),
            offset = r.ReadUInt64(),
            length = r.ReadUInt32(),
            codec = (TileCodec)r.ReadByte(),
            format = r.ReadByte(),
        };
}

// No manifest: installed layers + each layer's finest level K are discovered by probing which
// Level_<N>/canonical.<layer>.L<N>.idx files are present (contiguous from 0). "Presence of a file is the
// config" — a user installs a subset by copying only the Level_<N> folders they want, and K auto-drops
// with no file to regenerate or keep in sync. See TileArchivePaths.DetectMaxLevel.
