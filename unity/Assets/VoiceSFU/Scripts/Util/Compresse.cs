using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System;

public class Compresse
{
    public static byte[] DeflateEncode(byte[] data)
    {
        MemoryStream ms = new MemoryStream();
        DeflateStream CompressedStream = new DeflateStream(ms, CompressionMode.Compress, true);
        CompressedStream.Write(data, 0, data.Length);
        CompressedStream.Close();
        var encode = ms.ToArray();
        ms.Close();
        return encode;
    }

    public static byte[] DeflateDecode(byte[] data)
    {
        MemoryStream mssrc = new MemoryStream(data);
        MemoryStream outstream = new MemoryStream();
        byte[] buffer = new byte[1024];
        DeflateStream uncompressStream = new DeflateStream(mssrc, CompressionMode.Decompress);
        while (true)
        {
            int readSize = uncompressStream.Read(buffer, 0, buffer.Length);
            if (readSize == 0) break;
            outstream.Write(buffer, 0, readSize);
        }
        uncompressStream.Close();
        mssrc.Close();
        return outstream.ToArray();
    }
}
