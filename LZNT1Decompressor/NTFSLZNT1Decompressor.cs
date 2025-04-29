namespace LZNT1Decompressor;

public class NTFSLZNT1Decompressor : LZNT1Decompressor
{
    public override byte[] Decompress(byte[] compressedData, int decompressionBufferSize, out int bytesDecompressed)
    {
        var compressedSpan = compressedData.AsSpan();
        var decompressionBuffer = new DecompressionBuffer(decompressionBufferSize);
        int pointer = 0;
        while (pointer < compressedSpan.Length)
        {
            if (compressedSpan[pointer++] == 0)
                continue;
            
            var rawChunkHeader = compressedSpan.Slice(pointer, 2);
            var chunkHeader = new CompressionChunkHeader(rawChunkHeader);
            
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
}