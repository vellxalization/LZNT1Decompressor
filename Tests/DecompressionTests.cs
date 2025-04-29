namespace Tests;
using LZNT1Decompressor;

public class DecompressionTests
{
    [Fact]
    public void ShortDecompression()
    {
        var decompressor = new LZNT1Decompressor();
        var originalBytes = File.ReadAllBytes("../../../ShortUncompressed").AsSpan();
        var compressedBytes = File.ReadAllBytes("../../../ShortCompressed");
        var decompressedBytes = decompressor.Decompress(compressedBytes, 5000, out var bytesDecompressed)
            .AsSpan()[..bytesDecompressed];
        Assert.Equal(originalBytes.Length, decompressedBytes.Length);
        for (int i = 0; i < decompressedBytes.Length; ++i)
        {
            Assert.Equal(originalBytes[i], decompressedBytes[i]);
        }
    }
    
    [Fact]
    public void LongDecompression()
    {
        var decompressor = new LZNT1Decompressor();
        var originalBytes = File.ReadAllBytes("../../../LongUncompressed").AsSpan();
        var compressedBytes = File.ReadAllBytes("../../../LongCompressed");
        var decompressedBytes = decompressor.Decompress(compressedBytes, 15000, out var bytesDecompressed)
            .AsSpan()[..bytesDecompressed];
        Assert.Equal(originalBytes.Length, decompressedBytes.Length);
        for (int i = 0; i < decompressedBytes.Length; ++i)
        {
            Assert.Equal(originalBytes[i], decompressedBytes[i]);
        }
    }

    [Fact]
    public void DecompressionException()
    {
        var decompressor = new LZNT1Decompressor();
        var originalBytes = File.ReadAllBytes("../../../LongUncompressed").AsSpan();
        var compressedBytes = File.ReadAllBytes("../../../LongCompressed");
        Assert.Throws<ArgumentException>(() => decompressor.Decompress(compressedBytes, 3000, out var bytesDecompressed));
    }
}