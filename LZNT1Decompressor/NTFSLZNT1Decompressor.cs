namespace LZNT1Decompressor;

/// <summary>
/// A class for decompressing LZNT1-compressed data. Should only be used to decompress data read from a DATA attribute of an NTFS volume.
/// Only difference from the regular decompressor: it won't stop decompressing data when it encounters a zero length chunk header
/// (because NTFS-compressed files can contain zeros in the middle).
/// </summary>
public class NTFSLZNT1Decompressor : LZNT1Decompressor
{
    public override byte[] Decompress(byte[] compressedData, int decompressionBufferSize, out int bytesDecompressed)
    {
        var compressedSpan = compressedData.AsSpan();
        var decompressionBuffer = new DecompressionBuffer(decompressionBufferSize);
        int pointer = 0;
        while (pointer < compressedSpan.Length)
        {
            if (compressedSpan[pointer] == 0)
            {
                ++pointer;
                continue;
            }
            
            var rawChunkHeader = compressedSpan.Slice(pointer, 2);
            var chunkHeader = new CompressionChunkHeader(rawChunkHeader);
            
            pointer += 2;
            var chunk = compressedSpan.Slice(pointer, chunkHeader.ChunkSize + 1);
            if (chunkHeader.IsCompressed)
                DecompressChunk(decompressionBuffer, chunk);
            else
                decompressionBuffer.InsertRange(chunk);
            
            decompressionBuffer.CloseCurrentChunk();
            pointer += chunkHeader.ChunkSize + 1;
        }

        bytesDecompressed = decompressionBuffer.InsertPointer;
        return decompressionBuffer.Buffer;
    }
}