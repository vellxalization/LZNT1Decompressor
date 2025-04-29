namespace LZNT1Decompressor;

public partial class LZNT1Decompressor
{
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
            var chunk = compressedSpan.Slice(pointer, chunkHeader.ChunkSize + 1);
            if (chunkHeader.IsCompressed)
                DecompressChunk(decompressionBuffer, chunk);
            else
                decompressionBuffer.InsertRange(chunk);
            
            decompressionBuffer.CloseCurrentBlock();
            pointer += chunkHeader.ChunkSize + 1;
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