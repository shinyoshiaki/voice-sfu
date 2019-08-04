using System;
using UnityEngine;
using UniRx;

public class StartConnect : MonoBehaviour
{
    MicRecorder micRecorder;
    Speaker speaker;

    void Start()
    {
        var s = Scheduler.MainThread;

        var connect = GetComponent<Connect>();
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
        Debug.Log(length + ":" + data.Length);
        var encode = Convert.ToBase64String(data);
        var json = JsonUtility.ToJson(new OpusJson { type = "opus", length = length, data = encode });
    }

    void OnData(string msg)
    {
        var json = JsonUtility.FromJson<OpusJson>(msg);
        var data = Convert.FromBase64String(json.data);
        speaker.ReceiveBytes(data, json.length);
    }
}
