﻿using SimplePeerConnectionM;
using System.Collections.Generic;
using UnityEngine;
using Utf8Json;
using System;

namespace WebRTC
{

    public class Signaling
    {
        public delegate void OnData(string s);
        public OnData OnDataMethod;

        public delegate void OnSdp(string type, string sdp);


        public OnSdp OnSdpMethod;

        public delegate void IOnRemoteVideo(int id,
      IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
      int strideY, int strideU, int strideV, int strideA,
      uint width, uint height);

        public IOnRemoteVideo OnRemoteVideo;

        public delegate void OnConnect(string json);
        public OnConnect OnConnectMethod;

        public PeerConnectionM peer;


        string roomId;

        public Signaling(string room)
        {
#if UNITY_EDITOR
#elif UNITY_ANDROID
        AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaClass utilityClass = new AndroidJavaClass("org.webrtc.UnityUtility");
        utilityClass.CallStatic("InitializePeerConncectionFactory", new object[1] { activity });
#endif
            roomId = room;
            InitPeer();
        }

        void InitPeer()
        {
            List<string> servers = new List<string>();
            servers.Add("stun: stun.l.google.com:19302");
            peer = new PeerConnectionM(servers, "", "");
            peer.OnLocalSdpReadytoSend += OnLocalSdpReadytoSend;
            peer.OnIceCandiateReadytoSend += OnIceCandidate;
            peer.AddDataChannel();
            peer.OnLocalDataChannelReady += Connected;
            peer.OnDataFromDataChannelReady += Received;
            peer.OnRemoteVideoFrameReady += OnI420RemoteFrameReady;
        }

        void OnI420RemoteFrameReady(int id,
        IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
        int strideY, int strideU, int strideV, int strideA,
        uint width, uint height)
        {
            OnRemoteVideo(id, dataY, dataU, dataV, dataA, strideY, strideU, strideV, strideA, width, height);
        }

        void OnLocalSdpReadytoSend(int id, string type, string sdp)
        {
            OnSdpMethod(type, sdp);
        }

        void OnIceCandidate(int id, string candidate, int sdpMlineIndex, string sdpMid)
        {
            OnSdpMethod("candidate", candidate);
        }

        class RoomJson
        {
            public string type;
            public string roomId;
        }

        public void Connected(int id)
        {
            var data = new RoomJson { type = "connect", roomId = roomId };
            var json = JsonUtility.ToJson(data);
            OnConnectMethod(json);
        }

        public void Received(int id, string s)
        {
            OnDataMethod(s);
        }


        public void SetSdp(string s)
        {
            Debug.Log("setsdp " + s);
            var arr = s.Split('%');

            switch (arr[0])
            {
                case "offer":
                    peer.SetRemoteDescription(arr[0], arr[1]);
                    peer.CreateAnswer();
                    break;
                case "answer":
                    peer.SetRemoteDescription(arr[0], arr[1]);
                    break;
                case "ice":
                    peer.AddIceCandidate(arr[1], int.Parse(arr[2]), arr[3]);
                    break;
            }
        }
        public void Close()
        {
            peer.ClosePeerConnection();
        }
    }
}
