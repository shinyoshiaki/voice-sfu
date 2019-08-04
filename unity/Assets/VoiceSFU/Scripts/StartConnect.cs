using System;
using UnityEngine;
using UniRx;
using System.IO;
using System.IO.Compression;

public class StartConnect : MonoBehaviour
{
    MicRecorder micRecorder;
    Speaker speaker;
    Connect connect;

    void Start()
    {
        var s = Scheduler.MainThread;

        connect = GetComponent<Connect>();
        micRecorder = GetComponent<MicRecorder>();
        speaker = GetComponent<Speaker>();

        connect.StartConnect("192.168.1.7");
        connect.OnConnectd += OnConnect;
        connect.OnData += OnData;
    }

    void OnConnect()
    {
        micRecorder.OnEncoded += OnEncode;
    }

    class OpusJson
    {
        public string type;
        public int length;
        public string data;
    }

    void OnEncode(byte[] data, int length)
    {
        MemoryStream ms = new MemoryStream();
        DeflateStream CompressedStream = new DeflateStream(ms, CompressionMode.Compress, true);
        CompressedStream.Write(data, 0, data.Length);
        CompressedStream.Close();
        var encode = Convert.ToBase64String(ms.ToArray());
        ms.Close();
        var json = JsonUtility.ToJson(new OpusJson { type = "opus", length = length, data = encode });
        // connect.Send(json);
        OnData(json);
    }

    void OnData(string msg)
    {
        var json = JsonUtility.FromJson<OpusJson>(msg);
        var compress = Convert.FromBase64String(json.data);
        MemoryStream mssrc = new MemoryStream(compress);
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
        byte[] outByte = outstream.ToArray();

        speaker.ReceiveBytes(outByte, json.length);
    }
}
