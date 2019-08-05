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
        var encode = Convert.ToBase64String(Compresse.DeflateEncode(data));
        var json = JsonUtility.ToJson(new OpusJson { type = "opus", length = length, data = encode });
        // connect.Send(json);
        OnData(json);
    }

    void OnData(string msg)
    {
        Debug.Log(msg);
        var json = JsonUtility.FromJson<OpusJson>(msg);
        var compress = Convert.FromBase64String(json.data);
        byte[] outByte = Compresse.DeflateDecode(compress);

        speaker.ReceiveBytes(outByte, json.length);
    }
}
