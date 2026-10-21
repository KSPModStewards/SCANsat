"""
Mirage Texture Parser

Python decoder of BallisticFox's Mirage files, designed to allow for decompressing textures from the binary / index files.
"""

from enum import Enum
from typing import Literal
from os.path import getsize
from pathlib import Path

import numpy as np
import texture2ddecoder
import matplotlib.pyplot as plt

from PIL import Image
from numpy.typing import NDArray

VDeltaBlockLog = 6  # 64 residuals per block: ~0.2% width-table overhead
VDeltaHeaderBytes = 5


class ArchiveLayer(Enum):
    COLOUR = 0
    HEIGHT = 1
    NORMAL = 2


class TileCodec(Enum):
    NO_CODEC = 0
    Lz4 = 1
    Zstd = 2
    HeightPlaneSplitLz4 = 3
    HeightVDeltaBitpack = 4


class ArchiveTextureFormat(Enum):
    RGBA32 = 4
    R16 = 9
    DXT1 = 10
    DXT5 = 12
    BC6H = 24
    BC7 = 25
    BC4 = 26
    BC5 = 27


def blockCount(width: int, height: int) -> int:
    """Calculate the number of blocks in an image."""
    return int(((width + 3) / 4) * ((height + 3) / 4))


def usePlaneSplit(format: ArchiveTextureFormat) -> bool:
    """Should plane splitting be applied?"""
    return format == ArchiveTextureFormat.R16


def planeSplitR16(r16: list[int]) -> bytes:
    """Split a single plane of uint16_t into two planes of all high bytes and all low bytes."""
    n = len(r16)
    low_plane = [r16[i] & 0xFF for i in range(n)]
    high_plane = [r16[i] >> 8 for i in range(n)]

    return bytes(low_plane + high_plane)


def planeUnsplitR16(planed: bytes) -> list[int]:
    """Merge two byte planes into a 16-bit list."""
    n = int(len(planed) / 2)
    r16 = [planed[2 * i] + planed[2 * i + 1] * 256 for i in range(n)]
    return r16


def loadR16(b: bytes | bytearray, i: int) -> int:
    """Convert two bytes at the provided index into a single 16-bit unsigned int."""
    return b[2 * i] | (b[2 * i + 1] << 8)


def bitsFor(v: int) -> int:
    n = 0
    while v != 0:
        n += 1
        v >>= 1

    return n


def vDeltaBitpackDecode(
    src: bytes, srcOffset: int, srcLen: int, width: int, height: int
) -> bytes:
    """
    Inverse of vDeltaBitpackEncode.

    Unpack into a raw little-endian r16 buffer. Single pass — each texel's predictor is read back from the part of r16 already written (the row above, or the texel to the left on row 0), so no scratch buffer is needed.
    """

    if srcLen < VDeltaHeaderBytes:
        raise ValueError("vdelta: truncated header")

    w0 = src[srcOffset] | (src[srcOffset + 1] << 8)
    h0 = src[srcOffset + 2] | (src[srcOffset + 3] << 8)
    blockLog = src[srcOffset + 4]
    if w0 != width or h0 != height:
        raise ValueError(f"vdelta: payload dims {w0}x{h0} != expected {width}x{height}")
    if blockLog < 1 or blockLog > 16:
        raise ValueError(f"vdelta: bad blockLog {blockLog}")

    count = width * height
    r16 = bytearray([0 for _ in range(count * 2)])

    blockSize = 1 << blockLog
    nblocks = int((count + blockSize - 1) / blockSize)
    wp = srcOffset + VDeltaHeaderBytes
    if srcLen < VDeltaHeaderBytes + nblocks:
        raise ValueError("vdelta: truncated block-width table")

    p = wp + nblocks
    sEnd = srcOffset + srcLen
    acc = 0
    accBits = 0

    for b in range(nblocks):
        bw = src[wp + b]
        if bw > 16:
            raise ValueError(f"vdelta: bad block width {bw}")
        s = b * blockSize
        e = min(s + blockSize, count)
        x = s % width
        y = s // width  # two divisions per block, not per texel
        for i in range(s, e):
            z = 0

            if bw != 0:
                while accBits < bw:
                    if p >= sEnd:
                        raise ValueError("vdelta: bitstream underrun")
                    acc |= src[p] << accBits
                    p += 1
                    accBits += 8

                z = acc & ((1 << bw) - 1)
                acc >>= bw
                accBits -= bw

            d = ((z >> 1) & 0xFFFF) ^ (0xFFFF if (z & 1) else 0)  # un-zigzag to uint16
            if y == 0:
                pred = 0 if x == 0 else loadR16(r16, i - 1)
            else:
                pred = loadR16(r16, i - width)
            v = pred + d
            r16[2 * i] = v & 0xFF
            r16[2 * i + 1] = (v >> 8) & 0xFF

            x += 1
            if x == width:
                x = 0
                y += 1

    return bytes(r16)


def rawPayloadBytes(format: ArchiveTextureFormat, width: int, height: int) -> int:
    """Calculate the number of bytes in a compressed image blob."""
    match format:
        case ArchiveTextureFormat.DXT1:
            raise NotImplementedError
        case ArchiveTextureFormat.BC4:
            return blockCount(width, height) * 8
        case ArchiveTextureFormat.DXT5:
            raise NotImplementedError
        case ArchiveTextureFormat.BC5:
            raise NotImplementedError
        case ArchiveTextureFormat.BC6H:
            raise NotImplementedError
        case ArchiveTextureFormat.BC7:
            return blockCount(width, height) * 16
        case ArchiveTextureFormat.R16:
            return width * height * 2
        case ArchiveTextureFormat.RGBA32:
            return width * height * 4


def Lz4DecompressBlock(src: bytes, srcOffset: int, srcLen: int) -> bytes:
    """
    Decompress one LZ4 <b>block</b> (not frame) format buffer. Standard LZ4 sequences:
        - a token byte (high nibble = literal length, low nibble = match length-4)
        - optional extended-length bytes (0xFF continues)
        - the literals
        - then a 2-byte little-endian back-offset and
        - the match copy (byte-wise to honour overlap).
    Interoperates with K4os <c>LZ4Codec.Encode</c> used by the packer. Returns the number of decoded bytes.
    """

    s = srcOffset
    sEnd = srcOffset + srcLen
    dst = []

    while s < sEnd:
        token = src[s]
        s += 1

        litLen = token >> 4
        if litLen == 0xF:
            b = 0xFF
            while b == 0xFF:
                b = src[s]
                s += 1
                litLen += b

        if s + litLen > sEnd:
            raise BufferError("LZ4: corrupt literal run")

        for _ in range(litLen):
            dst.append(src[s])
            s += 1

        if s >= sEnd:
            break  # final sequence is literals-only (no match)

        offset = src[s] | (src[s + 1] << 8)
        s += 2
        if offset == 0:
            raise ValueError("LZ4: bad match offset")

        matchLen = token & 0xF
        if matchLen == 0xF:
            b = 0xFF
            while b == 0xFF:
                b = src[s]
                s += 1
                matchLen += b
        matchLen += 4  # minmatch

        for _ in range(matchLen):
            dst.append(dst[-offset])

    return bytes(dst)


class Index:
    """Handle index behaviour."""

    def __init__(self, fpath: Path, idx: int) -> None:
        fpath = fpath if isinstance(fpath, Path) else Path(fpath)
        self.idx = idx

        with open(fpath, "rb") as f:
            f.seek(23 + 22 * idx)
            index_data = f.read(22)

        self.key = int.from_bytes(index_data[:8], byteorder="little", signed=False)
        self.offset = int.from_bytes(index_data[8:16], byteorder="little", signed=False)
        self.length = int.from_bytes(
            index_data[16:20], byteorder="little", signed=False
        )
        self.codec = TileCodec(index_data[20])
        self.format = ArchiveTextureFormat(index_data[21])

        self.face = (int)((self.key >> 60) & 0x7)
        self.level = (int)((self.key >> 51) & 0x1FF)
        interleaved = self.key & ((1 << 34) - 1)
        self.x = self.compact1By1(interleaved)
        self.y = self.compact1By1(interleaved >> 1)

    def compact1By1(self, x: int) -> int:
        """Unpack the interlaced X, Y coordinates from the key."""
        x &= 0x5555555555555555
        x = (x | (x >> 1)) & 0x3333333333333333
        x = (x | (x >> 2)) & 0x0F0F0F0F0F0F0F0F
        x = (x | (x >> 4)) & 0x00FF00FF00FF00FF
        x = (x | (x >> 8)) & 0x0000FFFF0000FFFF
        x = (x | (x >> 16)) & 0x00000000FFFFFFFF
        return x

    def __str__(self) -> str:
        output = f"Index {self.idx}:\n"
        output += f"\tOffset: {self.offset}\n"
        output += f"\tLength: {self.length}\n"
        output += f"\tCodec: {self.codec.name}, Format: {self.format.name}\n"
        output += f"\tLevel {self.level}, Face {self.face}, (X: {self.x}, Y: {self.y})"
        return output


class IndexFile:
    """Index File Parser - decode header and allocate indices."""

    def __init__(self, fpath: Path | str) -> None:
        self.fpath = fpath if isinstance(fpath, Path) else Path(fpath)
        self.fsize = getsize(fpath)

        with open(self.fpath, "rb") as f:
            header = f.read(23)

        assert header[:4] == b"MTI1", "Magic Bytes Incorrect"

        self.version = int.from_bytes(header[4:6], byteorder="little", signed=False)
        self.archive = ArchiveLayer(header[6])
        self.level = int.from_bytes(header[7:11], byteorder="little", signed=True)
        self.num_entries = int.from_bytes(
            header[11:15], byteorder="little", signed=True
        )
        self.blob_length = int.from_bytes(
            header[15:23], byteorder="little", signed=True
        )

    def get_idx(self, idx: int) -> Index:
        """Return the tile index object for a given index integer."""
        return Index(self.fpath, idx)

    def __str__(self) -> str:
        output = f"File Size: {self.fsize} bytes.\n"
        output += f"Version: {self.version}\n"
        output += f"Archive Type: {self.archive.name}\n"
        output += f"Level: {self.level}\n"
        output += f"Number of Entries: {self.num_entries}\n"
        output += f"Blob Length: {self.blob_length}"
        return output


class Tile:
    """Texture tile from the blob."""

    def __init__(self, fpath: Path | str, offset: int) -> None:
        fpath = fpath if isinstance(fpath, Path) else Path(fpath)
        self.offset = offset

        with open(fpath, "rb") as f:
            f.seek(offset)
            tile_header = f.read(24)

        self.key = int.from_bytes(tile_header[:8], byteorder="little", signed=False)
        self.payload_len = int.from_bytes(
            tile_header[8:12], byteorder="little", signed=False
        )
        self.codec = TileCodec(tile_header[12])
        self.format = ArchiveTextureFormat(tile_header[13])
        self.crc32 = int.from_bytes(
            tile_header[14:18], byteorder="little", signed=False
        )

        with open(fpath, "rb") as f:
            f.seek(offset + 24)
            self.payload = f.read(self.payload_len)

    def decodeTilePayload(self) -> bytes:
        """
        Decode a stored tile payload (as read from the blob) into its raw texture bytes.
        The raw length is derived from the format + tile dims by the caller (not stored).
        Throws on a malformed stream.
        """

        match self.codec:
            case TileCodec.NO_CODEC:
                return self.payload
            case TileCodec.Lz4:
                return Lz4DecompressBlock(self.payload, 0, self.payload_len)
            case TileCodec.HeightPlaneSplitLz4:
                planed = Lz4DecompressBlock(self.payload, 0, self.payload_len)
                if len(planed) != self.payload_len:
                    raise ValueError(
                        f"LZ4 decode produced {len(planed)} bytes, expected {self.payload_len}"
                    )
                return bytes(planeUnsplitR16(bytes(planed)))
            case TileCodec.HeightVDeltaBitpack:
                # Dims come from the payload's own header, so this needs no extra caller context.
                if self.payload_len < VDeltaHeaderBytes:
                    raise ValueError("vdelta: truncated header")
                w = self.payload[0] | (self.payload[1] << 8)
                h = self.payload[2] | (self.payload[3] << 8)
                # if w * h * 2 != self.payload_len:
                #     raise ValueError(
                #         f"vdelta: payload dims {w}x{h} imply {w * h * 2} raw bytes, received {self.payload_len}"
                #     )
                return vDeltaBitpackDecode(self.payload, 0, self.payload_len, w, h)
            case _:
                raise ValueError(f"Unsupported Tile Codec {self.codec.name}")

    def __str__(self) -> str:
        output = f"Tile Payload Length: {self.payload_len}\n"
        output += f"\tCodec: {self.codec.name}, Format: {self.format.name}\n"
        return output


class BlobFile:
    """Wrapper for a binary blob file."""

    def __init__(self, fpath: Path | str) -> None:
        self.fpath = fpath if isinstance(fpath, Path) else Path(fpath)
        self.fsize = getsize(fpath)

        with open(self.fpath, "rb") as f:
            header = f.read(20)

        assert header[:4] == b"MTA1", "Magic Bytes Incorrect"

        self.version = int.from_bytes(header[4:6], byteorder="little", signed=False)
        self.layer = ArchiveLayer(header[6])
        self.format = ArchiveTextureFormat(
            int.from_bytes(header[7:11], byteorder="little", signed=True)
        )
        self.tile_size = int.from_bytes(header[11:13], byteorder="little", signed=False)
        self.border_px = int.from_bytes(header[13:15], byteorder="little", signed=False)
        self.face_count = header[15]
        self.flags = int.from_bytes(header[16:20], byteorder="little", signed=False)

    def get_tile(self, idx: Index) -> Tile:
        """Return the tile object for a given index integer."""
        return Tile(self.fpath, idx.offset)

    def __str__(self) -> str:
        output = f"File Size: {self.fsize} bytes.\n"
        output += f"Version: {self.version}\n"
        output += f"Texture Type: {self.layer.name}, Format: {self.format.name}\n"
        output += f"Tile Size: {self.tile_size}, Border Pixels: {self.border_px}\n"
        output += f"Face Count: {self.face_count}\n"
        output += f"Flags: {self.flags:08X}"
        return output


class MirageTextureLoader:
    """Wrapper to handle texture collection from Mirage."""

    def __init__(self, folder: Path | str, level: int = 0) -> None:
        self.folder = folder if isinstance(folder, Path) else Path(folder)
        self.level = level

        self.colour_idx = IndexFile(
            self.folder / Path(f"Level_{level}/canonical.color.L{level}.idx")
        )
        self.colour_blob = BlobFile(
            self.folder / Path(f"Level_{level}/canonical.color.L{level}.bin")
        )
        self.normal_idx = IndexFile(
            self.folder / Path(f"Level_{level}/canonical.normal.L{level}.idx")
        )
        self.normal_blob = BlobFile(
            self.folder / Path(f"Level_{level}/canonical.normal.L{level}.bin")
        )
        self.height_idx = IndexFile(
            self.folder / Path(f"Level_{level}/canonical.height.L{level}.idx")
        )
        self.height_blob = BlobFile(
            self.folder / Path(f"Level_{level}/canonical.height.L{level}.bin")
        )

    def get_texture(
        self, index: int, texture_type: Literal["colour", "normal", "height"]
    ) -> NDArray:
        if texture_type == "colour":
            return self.get_colour_texture(index)
        if texture_type == "normal":
            return self.get_normal_texture(index)
        return self.get_height_texture(index)

    def get_colour_texture(self, index: int) -> NDArray:
        """Get the tile texture at the given index."""
        tile = self.colour_blob.get_tile(self.colour_idx.get_idx(index))
        dds_bytestream = tile.decodeTilePayload()
        square = self.colour_blob.tile_size + 2 * self.colour_blob.border_px
        img_bytes = texture2ddecoder.decode_bc7(dds_bytestream, square, square)
        img = Image.frombytes("RGBA", (square, square), img_bytes, "raw", ("BGRA"))
        img_array = np.asarray(img, copy=True)
        img_array[:, :, 3] = 255
        return img_array

    def get_normal_texture(self, index: int) -> NDArray:
        """Get the tile texture at the given index."""
        tile = self.normal_blob.get_tile(self.normal_idx.get_idx(index))
        dds_bytestream = tile.decodeTilePayload()
        square = self.normal_blob.tile_size + 2 * self.normal_blob.border_px
        img_bytes = texture2ddecoder.decode_bc5(dds_bytestream, square, square)
        img = Image.frombytes("RGBA", (square, square), img_bytes, "raw", ("BGRA"))
        img_array = np.asarray(img, copy=True)
        img_array[:, :, 3] = 255
        return img_array

    def get_height_texture(self, index: int) -> NDArray:
        """Get the tile texture at the given index."""
        tile = self.height_blob.get_tile(self.height_idx.get_idx(index))
        dds_bytestream = tile.decodeTilePayload()
        square = self.height_blob.tile_size + 2 * self.height_blob.border_px
        img = Image.frombytes("I;16L", (square, square), dds_bytestream, "raw")
        img_array = np.asarray(img, copy=True)
        return img_array


def main():
    earth_folder = Path(
        r"C:\Users\rweld\Documents\Kerbal Space Program 1\KSP Mirage Beta\GameData\Sol-Textures\PluginData\03_Earth-System\03_Earth\Terrain"
    )
    mirage = MirageTextureLoader(earth_folder, 0)

    print("Index Blob")
    print(mirage.height_idx)
    print("-------")
    print("Binary Blob")
    print(mirage.height_blob)
    print("-------")

    for i in range(mirage.height_idx.num_entries):
        img_array = mirage.get_texture(i, "height")
        print(f"Array is {img_array.shape}")
        print(f"Min is {np.min(img_array)}, Max is {np.max(img_array)}")
        print(img_array)
        plt.imshow(img_array, cmap="gray", vmin=0, vmax=65535)
        plt.show()


if __name__ == "__main__":
    main()
