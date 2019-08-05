using System.Collections.Generic;
using System;
using UnityEngine;
using UnityOpus;
using UniRx;

public class MicRecorder : MonoBehaviour
{
    public bool IsMute = false;

    const SamplingFrequency samplingFrequency = SamplingFrequency.Frequency_24000;
    const int lengthSeconds = 1;

    const int bitrate = (int)samplingFrequency * 2;
    const int frameSize = 120 * 4;
    const int outputBufferSize = frameSize * 4; // at least frameSize * sizeof(float)

    AudioClip clip;
    int samplePos = 0;
    float[] processBuffer = new float[512];
    float[] microphoneBuffer = new float[lengthSeconds * (int)samplingFrequency];

    Encoder encoder;
    Queue<float> pcmQueue = new Queue<float>();
    readonly float[] frameBuffer = new float[frameSize];
    readonly byte[] outputBuffer = new byte[outputBufferSize];

    public delegate void IOnEncoded(byte[] data, int length);
    public IOnEncoded OnEncoded;

    public float GetAveragedVolume()
    {
        float a = 0;
        foreach (float s in processBuffer)
        {
            a += Mathf.Abs(s);
        }
        return a / processBuffer.Length;
    }

    void OnEnable()
    {
        encoder = new Encoder(
            samplingFrequency,
            NumChannels.Mono,
            OpusApplication.Audio)
        {
            Bitrate = bitrate,
            Complexity = 10,
            Signal = OpusSignal.Music
        };
    }

    void OnDisable()
    {
        encoder.Dispose();
        encoder = null;
        pcmQueue.Clear();
    }

    void Start()
    {
        clip = Microphone.Start(null, true, lengthSeconds, (int)samplingFrequency);
        Observable.Interval(TimeSpan.FromMilliseconds(1000 / 10)).Subscribe(_ =>
         {
             Recorde();
         }).AddTo(this);
    }

    void Recorde()
    {
        var position = Microphone.GetPosition(null);
        if (position < 0 || samplePos == position)
        {
            return;
        }

        if (!IsMute)
        {
            clip.GetData(microphoneBuffer, 0);
            while (GetDataLength(microphoneBuffer.Length, samplePos, position) > processBuffer.Length)
            {
                var remain = microphoneBuffer.Length - samplePos;
                if (remain < processBuffer.Length)
                {
                    Array.Copy(microphoneBuffer, samplePos, processBuffer, 0, remain);
                    Array.Copy(microphoneBuffer, 0, processBuffer, remain, processBuffer.Length - remain);
                }
                else
                {
                    Array.Copy(microphoneBuffer, samplePos, processBuffer, 0, processBuffer.Length);
                }

                Encode(processBuffer);

                samplePos += processBuffer.Length;
                if (samplePos > microphoneBuffer.Length)
                {
                    samplePos -= microphoneBuffer.Length;
                }
            }
        }
    }

    void Encode(float[] data)
    {
        foreach (var sample in data)
        {
            pcmQueue.Enqueue(sample);
        }
        while (pcmQueue.Count > frameSize)
        {
            for (int i = 0; i < frameSize; i++)
            {
                frameBuffer[i] = pcmQueue.Dequeue();
            }
            var encodedLength = encoder.Encode(frameBuffer, outputBuffer);
            if (OnEncoded != null) OnEncoded(outputBuffer, encodedLength);
        }
    }

    static int GetDataLength(int bufferLength, int head, int tail)
    {
        if (head < tail)
        {
            return tail - head;
        }
        else
        {
            return bufferLength - head + tail;
        }
    }
}
