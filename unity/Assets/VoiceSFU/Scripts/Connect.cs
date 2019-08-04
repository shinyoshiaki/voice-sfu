using UnityEngine;
using WebRTC;
using UniRx;
using UniRx.Async;
using System;
using UnityEngine.Networking;
using Utf8Json;
public class Connect : MonoBehaviour
{
    Signaling signaling;

    public delegate void IOnRemoteVideo(int id,
      IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
      int strideY, int strideU, int strideV, int strideA,
      uint width, uint height);

    public IOnRemoteVideo OnRemoteVideo;

    bool connected = false;

    string baseAddress = "";
    string id = "";

    public void StartConnect(string ipAddress)
    {
        var s = Scheduler.MainThread;

#if UNITY_EDITOR
#elif UNITY_ANDROID
        AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaClass utilityClass = new AndroidJavaClass("org.webrtc.UnityUtility");
        utilityClass.CallStatic("InitializePeerConncectionFactory", new object[1] { activity });
#endif

        Debug.Log("start");

        signaling = new Signaling(ipAddress);
        signaling.OnConnectMethod += OnConnet;
        signaling.OnDataMethod += OnData;
        signaling.OnSdpMethod += OnSdp;
        signaling.OnRemoteVideo += OnI420RemoteFrameReady;

        baseAddress = ipAddress;

        Join();
    }

    public class JoinReq
    {
        public string room;
    }

    public class JoinRes
    {
        public Sdp sdp;
        public string uu;
    }

    public class Sdp
    {
        public string type;
        public string sdp;
    }

    async void Join()
    {
        var request = new UnityWebRequest("http://" + baseAddress + ":8080/join", "POST");
        var join = new JoinReq { room = "unity" };
        var json = JsonUtility.ToJson(join);
        byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(postData);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        await request.SendWebRequest();
        var joinres = JsonSerializer.Deserialize<JoinRes>(request.downloadHandler.text);
        id = joinres.uu;
        Debug.Log(joinres.sdp);

        signaling.SetSdp(joinres.sdp.type + "%" + joinres.sdp.sdp);
    }

    void OnConnet(string str)
    {
        if (connected == true) return;
        Debug.Log("connect");
        signaling.peer.SendDataViaDataChannel("test from unity");
        connected = true;
        signaling.peer.AddDataChannel();

        Scheduler.MainThread.Schedule(() =>
                {
                    Observable.Interval(TimeSpan.FromMilliseconds(1000)).Subscribe(_ =>
       {           
           signaling.peer.SendDataViaDataChannel("test from unity");
       }).AddTo(this);
                });

    }

    public void Send(string str)
    {
        if (connected)
            signaling.peer.SendDataViaDataChannel(str);
    }

    void OnData(string s)
    {
        Debug.Log("data " + s);
    }

    public class AnswerReq
    {
        public string room;
        public string uu;
        public string type;
        public string sdp;
    }

    void OnSdp(string _type, string _sdp)
    {
        Scheduler.MainThread.Schedule(() =>
        {
            Debug.Log("onsdp");
            var request = new UnityWebRequest("http://" + baseAddress + ":8080/answer", "POST");
            var join = new AnswerReq { room = "unity", uu = id, type = _type, sdp = _sdp };
            var json = JsonUtility.ToJson(join);
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(postData);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SendWebRequest();
        });
    }


    void OnI420RemoteFrameReady(int id,
       IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
       int strideY, int strideU, int strideV, int strideA,
       uint width, uint height)
    {
        OnRemoteVideo(id, dataY, dataU, dataV, dataA, strideY, strideU, strideV, strideA, width, height);
    }


    void OnDestroy()
    {
        Debug.Log("OnDestroy1");
        signaling.Close();
    }
}
