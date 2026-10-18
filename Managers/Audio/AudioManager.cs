using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMNLibrary.Singleton;
using UnityEngine;

namespace Core.Audio
{
    [DefaultExecutionOrder(-80)]
    [AddComponentMenu("Core/Audio/Audio Manager")]
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] [Range(1, 32)] private int sfxChannelCount = 8;

        private readonly List<AudioSource> _sfxChannels = new();
        private int _nextChannelIndex;

        protected override void Awake()
        {
            base.Awake();
            InitializeSfxChannels();
        }

        private void InitializeSfxChannels()
        {
            _sfxChannels.Clear();

            if (sfxSource != null)
            {
                _sfxChannels.Add(sfxSource);
            }

            var targetCount = Mathf.Max(1, sfxChannelCount);
            while (_sfxChannels.Count < targetCount)
            {
                var channel = gameObject.AddComponent<AudioSource>();
                channel.playOnAwake = false;

                if (sfxSource != null)
                {
                    channel.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
                    channel.spatialBlend = sfxSource.spatialBlend;
                    channel.priority = sfxSource.priority;
                    channel.mute = sfxSource.mute;
                    channel.bypassEffects = sfxSource.bypassEffects;
                    channel.bypassListenerEffects = sfxSource.bypassListenerEffects;
                    channel.bypassReverbZones = sfxSource.bypassReverbZones;
                }

                _sfxChannels.Add(channel);
            }
        }

        public void PlaySfx(AudioCueSO audioCue)
        {
            if (audioCue == null)
            {
                return;
            }

            var clip = audioCue.GetRandomClip();
            if (clip == null)
            {
                return;
            }

            var channel = GetAvailableSfxChannel();
            if (channel != null)
            {
                channel.pitch = audioCue.GetPitch();
                channel.volume = audioCue.Volume;
                channel.clip = clip;
                channel.Play();
            }
        }

        private AudioSource GetAvailableSfxChannel()
        {
            if (_sfxChannels.Count == 0)
            {
                InitializeSfxChannels();
            }

            for (var i = 0; i < _sfxChannels.Count; i++)
            {
                var index = (_nextChannelIndex + i) % _sfxChannels.Count;
                var candidate = _sfxChannels[index];
                if (candidate != null && !candidate.isPlaying)
                {
                    _nextChannelIndex = (index + 1) % _sfxChannels.Count;
                    return candidate;
                }
            }

            var fallback = _sfxChannels[_nextChannelIndex % _sfxChannels.Count];
            _nextChannelIndex = (_nextChannelIndex + 1) % _sfxChannels.Count;
            return fallback;
        }

        public void PlayMusic(AudioClip musicClip, bool loop = true)
        {
            if (musicClip == null || musicSource == null)
            {
                return;
            }

            musicSource.clip = musicClip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }
    }
}

