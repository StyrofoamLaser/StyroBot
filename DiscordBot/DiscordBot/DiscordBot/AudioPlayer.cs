using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NAudio.Wave;
using DiscordSharp;

// Class to handle playing audio to the discord server, based on LuigiBot and improved from there

namespace DiscordBot
{
    class AudioPlayer
    {
        WaveCallbackInfo callbackInfo;
        WaveOut outputDevice;
        BufferedWaveProvider bufferedWaveProvider;
        DiscordVoiceConfig config;

        public AudioPlayer(DiscordVoiceConfig __config)
        {
            config = __config;
            callbackInfo = WaveCallbackInfo.FunctionCallback();
            outputDevice = new WaveOut(callbackInfo);
            bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(48000, 16, config.Channels));
        }

        public byte[] GetBytesFromMP3(string file)
        {
            return File.ReadAllBytes(file);
        }

        public void EnqueueBytes(byte[] bytes)
        {
            bufferedWaveProvider.AddSamples(bytes, 0, bytes.Length);
        }

        public Task PlayAudio()
        {
            return Task.Run(() =>
            {
                outputDevice.Init(bufferedWaveProvider);
                outputDevice.Play();
            });
        }

        public void StopAudio()
        {
            outputDevice.Stop();
        }
    }
}
