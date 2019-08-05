using System;
using UnityEngine;
using UnityOpus;
using System.Threading;

[RequireComponent(typeof(AudioSource))]
public class Speaker : MonoBehaviour
{
    const NumChannels channels = NumChannels.Mono;
    const SamplingFrequency frequency = SamplingFrequency.Frequency_24000;

    int audioClipLength = (int)frequency; // 1.0[sec]
    AudioSource source;
    int samplePos = 0;
    float[] audioClipData;

    Decoder decoder;
    readonly float[] pcmBuffer = new float[Decoder.maximumPacketDuration * (int)channels];
    SynchronizationContext context;

    void OnEnable()
    {
        context = SynchronizationContext.Current;
        decoder = new Decoder(frequency, NumChannels.Mono);

        source = GetComponent<AudioSource>();
        source.clip = AudioClip.Create("AudioStreamPlayer", audioClipLength, (int)channels, (int)frequency, false);
        source.loop = true;
    }

    void OnDisable()
    {
        source.Stop();
        decoder.Dispose();
        decoder = null;
    }

    public void ReceiveBytes(byte[] encodedData, int length)
    {
        if (decoder != null)
        {
            context.Post(_ => SetData(encodedData, length), null);
        }
    }

    void SetData(byte[] encodedData, int length)
    {
        var pcmLength = decoder.Decode(encodedData, length, pcmBuffer);

        if (audioClipData == null || audioClipData.Length != pcmLength)
        {
            // assume that pcmLength will not change.
            audioClipData = new float[pcmLength];
        }
        Array.Copy(pcmBuffer, audioClipData, pcmLength);

        source.clip.SetData(audioClipData, samplePos);
        samplePos += pcmLength;
        if (!source.isPlaying && samplePos > audioClipLength / 2)
        {
            Debug.Log("source.Play");
            source.Play();
        }
        samplePos %= audioClipLength;
    }


    void Play(int pcmLength)
    {
        source.clip.SetData(audioClipData, samplePos);
        samplePos += pcmLength;
        if (!source.isPlaying && samplePos > audioClipLength / 2)
        {
            Debug.Log("source.Play");
            source.Play();
        }
        samplePos %= audioClipLength;
    }

    public float GetAveragedVolume()
    {
        int size = 1024;
        float[] data = new float[size];
        float a = 0;
        source.GetOutputData(data, 0);
        foreach (float s in data)
        {
            a += Mathf.Abs(s);
        }
        return a / size;
    }
}
