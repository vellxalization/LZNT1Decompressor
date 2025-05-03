# A small library to decompress LZNT1-compressed data

## A quick rundown of how the algorithm works
The general idea is quite simple: algorithm splits the data into fixed-size (512, 1024, 2048, or 4096 bytes; 4096 is the most common value, used by Windows and is recommended by Microsoft) chunks, and tries to compress each chunk by finding repeating byte sequences and replacing them with a 2-byte backreference. If compressed size of the chunk >= original chunk size (data didn't compress well), then the original chunk is used without any modifications, otherwise - compressed data is used.

## Some important concepts
* **Compressed data** consists of at least one **data chunk**. 
* Each data chunk consists of a 2-byte _chunk header_ and _chunk data_. If the chunk is not compressed, it's data is copied as is.
* **Compressed chunk** consists of at least one **compressed group**.
* Compressed group contains a 1-byte _group flags_ and _group data_.

Chunk header is 16 bits long and, starting with the least significant bit, contains:
* 0-11: chunk length. It's stored as true length - 1, so in order to get the actual length of the chunk you need to add 1 to this value;
* 12-14: signature chunk value;
* 15: compression flag. If set, the data in the chunk is compressed.

Group flags byte describes how to read data from the group's data. Starting from the least significant bit:
* If it's set to 0, then the corresponding byte should be copied as is;
* If it's set to 1, then the corresponding 2 bytes should be treated as a _backreference_.
<br>For example, we have a byte 0x22 (0010_0010 in binary). The first bit is set to 0, meaning the first byte after the byte flag should be copied to the buffer. Next bit is set to 1, meaning the next 2 byte should be read and parsed as a backreference (reading data from a backreference described below). Next three bits are zeroes, they're copied to the buffer without any changes, the next one is 1 - a backreference. The last two are also zeroes and must be copied.

Backreference is a 2-byte structure containing an offset and a size that is used to copy existing data from the decompressed data. The amount of bits allocated to the offset and size is dynamic and depends on the index within the current decompression chunk. It's described by the following code:
```C#
(int OffsetSize, int LengthSize) GetOffsetAndLengthSizes(int indexInDecompressedChunk)
{
    // maximum chunk size is 4096
    if (indexInDecompressedChunk is < 0 or > 4095)
        throw new ArgumentException("Index must be in a range from 0 to 4095"); 
    
    // at index 0, 4 bits are allocated to the offset and 12 to the length
    var offsetSize = 4;
    var lengthSize = 12;

    for (var i = indexInDecompressedChunk - 1; i >= 0x10; i >>= 1)
    {
        offsetSize++;
        lengthSize--;
    }
        
    return (offsetSize, lengthSize);
}
```
... or this table:
<table>
    <tr>
        <th>Index Range</th>
        <th>Offset bits</th>
        <th>Length bits</th>
    </tr>
    <tr>
        <th>0-15</th>
        <th>4</th>
        <th>12</th>
    </tr>
    <tr>
        <th>16-31</th>
        <th>5</th>
        <th>11</th>
    </tr>
    <tr>
        <th>32-63</th>
        <th>6</th>
        <th>10</th>
    </tr>
    <tr>
        <th>64-127</th>
        <th>7</th>
        <th>9</th>
    </tr>
    <tr>
        <th>128-255</th>
        <th>8</th>
        <th>8</th>
    </tr>
    <tr>
        <th>256-511</th>
        <th>9</th>
        <th>7</th>
    </tr>
    <tr>
        <th>512-1023</th>
        <th>10</th>
        <th>6</th>
    </tr>
    <tr>
        <th>1024-2047</th>
        <th>11</th>
        <th>5</th>
    </tr>
    <tr>
        <th>2048-4095</th>
        <th>12</th>
        <th>4</th>
    </tr>
</table>

Once we know the number of bits allocated to offset and size, we can get the actual values:
* Offset - number of positions needed to go back to copy an existing array sequence. Since offset cannot be 0, it's stored as true offset - 1, so in order to get actual offset we need to add 1 to this value;
* Size - number of bytes we need to copy starting from current index - true offset. Because backreference has a size of 2 bytes, it's pointless to try to compress anything less than 3 bytes long. Therefore size is stored as true size - 3, so in order to get the actual size we need to add 3 to this value.

For example: we have a backreference value of 0x5001 (0b_01010000_00000001). 
* We have decompressed 23 bytes in the current chunk, so our current index is 24. 
* This means that 5 bytes are allocated to the offset and 11 to the size. 
* We read the values and get 0b_01010 (10) - offset, 0b_00000000001 (1) - size.
* We add 1 and 3 to the offset and size respectively to get the actual values: 11 and 4.
* That means we need to copy 4 bytes starting from the index 13 (24 - 11) at the current index of 24.

## Decompression algorithm
1. Create a decompression buffer;
2. Read a chunk header from the compressed data;
<br> 2.1. If the chunk size is set to 0, break;
<br> 2.2. If the compression flag is set to 0, copy the whole chunk to the decompression buffer without any changes;
<br> 2.3. Otherwise - decompress it;
3. Repeat until there are no more compressed data left.

## Decompressing a chunk
1. Read a flags byte;
2. For each bit in flags, starting from the least significant:
<br>2.1. If there is no data for the bit (i.e. the compressed chunk has ended before each bit was iterated), break;
<br>2.2. If the bit is to 0, copy the corresponding byte to the decompression buffer;
<br>2.3. Otherwise, read and parse 2 corresponding bytes as a backreference and use it copy existing data from the decompression buffer;
3. Repeat until the chunk is not fully decompressed.

## Sources
* [MS-XCA] LZNT1: https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-xca/94164d22-2928-4417-876e-d193766c4db6
* libyal/libfwnt: https://github.com/libyal/libfwnt/blob/main/documentation/Compression%20methods.asciidoc