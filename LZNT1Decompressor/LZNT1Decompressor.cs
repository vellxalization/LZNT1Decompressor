namespace LZNT1Decompressor;

/// <summary>
/// A class used to decompress LZNT1-compressed data
/// </summary>
public partial class LZNT1Decompressor
{
    /// <summary>
    /// Decompress LZNT1-compressed data
    /// </summary>
    /// <param name="compressedData">Data to decompress</param>
    /// <param name="decompressionBufferSize">Size of the decompression buffer. Using small value might result in exception if the decompressed data is too big</param>
    /// <param name="bytesDecompressed">Amount of bytes decompressed. Use to trim unused zeroes from buffer</param>
    /// <returns>Decompressed data</returns>
    public virtual byte[] Decompress(byte[] compressedData, int decompressionBufferSize, out int bytesDecompressed)
    {
        var compressedSpan = compressedData.AsSpan();
        var decompressionBuffer = new DecompressionBuffer(decompressionBufferSize);
        int pointer = 0;
        while (pointer < compressedSpan.Length)
        {
            var rawChunkHeader = compressedSpan.Slice(pointer, 2);
            var chunkHeader = new CompressionChunkHeader(rawChunkHeader);
            if (chunkHeader.ChunkSize == 0)
                break;
            
            pointer += 2;
            var chunk = compressedSpan.Slice(pointer, chunkHeader.ChunkSize);
            if (chunkHeader.IsCompressed)
                DecompressChunk(decompressionBuffer, chunk);
            else
                decompressionBuffer.InsertRange(chunk);
            
            decompressionBuffer.CloseCurrentChunk();
            pointer += chunkHeader.ChunkSize;
        }

        bytesDecompressed = decompressionBuffer.InsertPointer;
        return decompressionBuffer.Buffer;
    }

    protected void DecompressChunk(DecompressionBuffer buffer, Span<byte> compressedChunk)
    {
        var pointer = 0;
        while (pointer < compressedChunk.Length)
        {
            var flags = compressedChunk[pointer++];
            if (flags == 0) // group doesn't contain any backreferences
            {
                var diff = compressedChunk.Length - pointer;
                var length = diff <= 8 ? diff : 8;
                var uncompressedGroup = compressedChunk.Slice(pointer, length);
                buffer.InsertRange(uncompressedGroup);
                pointer += length;
                continue;
            }

            var bitMask = 1;
            for (int i = 0; i < 8 && pointer < compressedChunk.Length; ++i)
            {
                if ((flags & bitMask) == 0)
                {
                    buffer.InsertByte(compressedChunk[pointer++]);
                }
                else
                {
                    var rawBackreference = compressedChunk.Slice(pointer, 2);
                    var backreference = new Backreference(rawBackreference, buffer.BlockPointer);
                    pointer += 2;
                    buffer.InsertFromBackreference(backreference);
                }

                bitMask <<= 1;
            }
        }
    }
}