using System.Collections.Generic;
using UnityEngine;

namespace Atthree.Audio
{
    /// <summary>
    /// Sahnede görünmez çalışan ses motoru. <see cref="AudioManager"/> ilk kullanımda kendiliğinden
    /// oluşturur ("[Audio Manager]" objesi), sahne değişince yok olmaz.
    ///
    /// Görevleri:
    ///  - AudioSource HAVUZU: her ses için yeni obje yaratıp silmek yerine hazır kaynakları tekrar kullanır.
    ///  - SES SINIRI: kaynaklar dolunca en önemsiz (priority değeri en yüksek, en eski) sesi susturur.
    ///  - Her kare: takip edilen objeye git, fade uygula, kanal seviyesini uygula, biten sesi havuza geri ver.
    ///  - MÜZİK: iki ayrı kaynak arasında yumuşak geçiş (crossfade).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [AddComponentMenu("")] // Add Component menüsünde görünmesin
    public class AudioRuntime : MonoBehaviour
    {
        internal class Voice
        {
            public int id;
            public AudioSource source;
            public SoundDefinition sound;
            public AudioChannel channel;
            public float baseVolume;     // sesin kendi seviyesi (tanım * rastgele * çarpan)
            public float fade = 1f;      // 0..1
            public float fadeTarget = 1f;
            public float fadeSpeed;      // saniyede
            public bool stopWhenSilent;
            public Transform follow;
            public bool hasFollow;
            public float startTime;
            public int priority;
        }

        AudioManagerSettings settings;
        readonly List<Voice> active = new List<Voice>();
        readonly Stack<Voice> free = new Stack<Voice>();
        readonly Dictionary<int, Voice> byId = new Dictionary<int, Voice>();
        readonly Dictionary<SoundDefinition, float> lastPlayTime = new Dictionary<SoundDefinition, float>();
        readonly float[] volumes = new float[System.Enum.GetValues(typeof(AudioChannel)).Length];
        int nextId = 1;
        int sourceCount;

        // Müzik: iki kaynak, biri çalarken diğeri yeni parçayla açılır.
        Voice musicCurrent;

        internal AudioManagerSettings Settings => settings;
        internal int ActiveCount => active.Count;
        internal int SourceCount => sourceCount;

        internal void Initialize(AudioManagerSettings value)
        {
            settings = value;
            foreach (AudioChannel channel in System.Enum.GetValues(typeof(AudioChannel)))
                volumes[(int)channel] = LoadVolume(channel);
            for (int i = 0; i < settings.initialSources; i++) free.Push(CreateVoice());
        }

        Voice CreateVoice()
        {
            var go = new GameObject("Source " + sourceCount);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            sourceCount++;
            return new Voice { source = source };
        }

        // ================================================================== çalma
        internal SoundHandle Play(SoundDefinition sound, Vector3? position, Transform follow, float volumeScale)
        {
            if (sound == null) return SoundHandle.None;
            if (sound.channel == AudioChannel.Master)
            {
                Debug.LogWarning($"[AudioManager] '{sound.name}': Master kanalında ses çalınamaz, Sfx kullanılıyor.", sound);
            }

            var clip = sound.PickClip();
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] '{sound.name}' içinde klip yok.", sound);
                return SoundHandle.None;
            }

            // Spam koruması: bekleme süresi.
            float now = Time.unscaledTime;
            if (sound.cooldown > 0f && lastPlayTime.TryGetValue(sound, out float last) && now - last < sound.cooldown)
                return SoundHandle.None;

            // Spam koruması: aynı sesten en fazla N kopya → en eskisini sustur.
            if (sound.maxInstances > 0)
            {
                Voice oldest = null;
                int count = 0;
                foreach (var v in active)
                {
                    if (v.sound != sound) continue;
                    count++;
                    if (oldest == null || v.startTime < oldest.startTime) oldest = v;
                }
                if (count >= sound.maxInstances && oldest != null) Release(oldest);
            }

            var voice = Acquire(sound.priority);
            if (voice == null) return SoundHandle.None; // daha önemli seslerle dolu

            lastPlayTime[sound] = now;
            var channel = sound.channel == AudioChannel.Master ? AudioChannel.Sfx : sound.channel;
            voice.id = nextId++;
            voice.sound = sound;
            voice.channel = channel;
            voice.baseVolume = sound.PickVolume() * volumeScale;
            voice.fade = voice.fadeTarget = 1f;
            voice.fadeSpeed = 0f;
            voice.stopWhenSilent = false;
            voice.follow = follow;
            voice.hasFollow = follow != null;
            voice.startTime = now;
            voice.priority = sound.priority;

            var source = voice.source;
            source.clip = clip;
            source.loop = sound.loop;
            source.pitch = sound.PickPitch();
            source.priority = sound.priority;
            source.outputAudioMixerGroup = sound.outputOverride != null ? sound.outputOverride : settings.Group(channel);
            source.ignoreListenerPause = IgnoresPause(channel);

            bool spatial = position.HasValue || follow != null;
            source.spatialBlend = spatial ? sound.spatialBlend : 0f;
            source.minDistance = sound.minDistance;
            source.maxDistance = sound.maxDistance;
            source.rolloffMode = sound.rolloff;
            source.transform.position = follow != null ? follow.position : position ?? Vector3.zero;

            ApplyVolume(voice);
            source.Play();

            active.Add(voice);
            byId[voice.id] = voice;
            return new SoundHandle(voice.id);
        }

        /// <summary>Boş bir kaynak bulur; yoksa büyütür; sınırdaysa daha önemsiz bir sesi susturur.</summary>
        Voice Acquire(int priority)
        {
            if (free.Count > 0) return free.Pop();
            if (sourceCount < settings.maxVoices) return CreateVoice();

            // Kurban: en önemsiz (priority değeri en büyük), eşitse en eski. Müzik asla.
            Voice victim = null;
            foreach (var v in active)
            {
                if (v == musicCurrent || v.channel == AudioChannel.Music) continue;
                if (victim == null || v.priority > victim.priority ||
                    (v.priority == victim.priority && v.startTime < victim.startTime))
                    victim = v;
            }
            if (victim == null || victim.priority < priority) return null; // yeni ses daha önemsiz
            Release(victim);
            return free.Pop();
        }

        internal void Release(Voice voice)
        {
            voice.source.Stop();
            voice.source.clip = null;
            byId.Remove(voice.id);
            active.Remove(voice);
            if (voice == musicCurrent) musicCurrent = null;
            voice.id = 0;
            voice.sound = null;
            voice.follow = null;
            voice.hasFollow = false;
            free.Push(voice);
        }

        // ================================================================== her kare
        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var v = active[i];

                if (v.hasFollow)
                {
                    if (v.follow == null) { Release(v); continue; } // takip edilen obje yok oldu
                    v.source.transform.position = v.follow.position;
                }

                if (v.fadeSpeed > 0f)
                {
                    v.fade = Mathf.MoveTowards(v.fade, v.fadeTarget, v.fadeSpeed * dt);
                    if (Mathf.Approximately(v.fade, v.fadeTarget)) v.fadeSpeed = 0f;
                    if (v.stopWhenSilent && v.fade <= 0f) { Release(v); continue; }
                }

                ApplyVolume(v);

                // Bitti mi? (Duraklatılmış sesler bitmiş sayılmaz.)
                bool pausedByListener = AudioListener.pause && !v.source.ignoreListenerPause;
                if (!v.source.loop && !v.source.isPlaying && !pausedByListener && Time.unscaledTime - v.startTime > 0.05f)
                    Release(v);
            }
        }

        void ApplyVolume(Voice v)
        {
            v.source.volume = v.baseVolume * v.fade * volumes[(int)v.channel] * volumes[(int)AudioChannel.Master];
        }

        bool IgnoresPause(AudioChannel channel) =>
            (channel == AudioChannel.Music && settings.musicIgnoresPause) ||
            (channel == AudioChannel.Ui && settings.uiIgnoresPause);

        // ================================================================== kontrol
        internal bool TryGet(int id, out Voice voice) => byId.TryGetValue(id, out voice);

        internal void Stop(Voice voice, float fadeSeconds)
        {
            if (fadeSeconds <= 0f) { Release(voice); return; }
            FadeTo(voice, 0f, fadeSeconds);
            voice.stopWhenSilent = true;
        }

        internal void FadeTo(Voice voice, float target, float seconds)
        {
            voice.fadeTarget = Mathf.Clamp01(target);
            voice.fadeSpeed = seconds <= 0f ? 1000f : Mathf.Abs(voice.fadeTarget - voice.fade) / seconds;
            if (voice.fadeSpeed <= 0f) voice.fadeSpeed = 1000f;
        }

        internal void StopAll(AudioChannel? channel, float fadeSeconds)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var v = active[i];
                if (channel.HasValue && channel.Value != AudioChannel.Master && v.channel != channel.Value) continue;
                Stop(v, fadeSeconds);
            }
        }

        // ================================================================== müzik
        internal SoundHandle PlayMusic(SoundDefinition music, float fadeSeconds)
        {
            // Aynı parça zaten çalıyorsa baştan başlatma.
            if (musicCurrent != null && musicCurrent.sound == music && !musicCurrent.stopWhenSilent)
                return new SoundHandle(musicCurrent.id);

            if (musicCurrent != null) Stop(musicCurrent, fadeSeconds);
            musicCurrent = null;
            if (music == null) return SoundHandle.None;

            var handle = Play(music, null, null, 1f);
            if (!TryGet(handle.Id, out var voice)) return handle;
            voice.channel = AudioChannel.Music;
            voice.source.ignoreListenerPause = settings.musicIgnoresPause;
            voice.source.outputAudioMixerGroup = music.outputOverride != null ? music.outputOverride : settings.Group(AudioChannel.Music);
            voice.priority = 0;
            voice.source.priority = 0;
            if (fadeSeconds > 0f)
            {
                voice.fade = 0f;
                FadeTo(voice, 1f, fadeSeconds);
                ApplyVolume(voice);
            }
            musicCurrent = voice;
            return handle;
        }

        internal void StopMusic(float fadeSeconds)
        {
            if (musicCurrent != null) Stop(musicCurrent, fadeSeconds);
            musicCurrent = null;
        }

        internal SoundHandle CurrentMusic => musicCurrent != null ? new SoundHandle(musicCurrent.id) : SoundHandle.None;

        // ================================================================== ses seviyeleri
        internal float GetVolume(AudioChannel channel) => volumes[(int)channel];

        internal void SetVolume(AudioChannel channel, float value, bool save)
        {
            volumes[(int)channel] = Mathf.Clamp01(value);
            foreach (var v in active) ApplyVolume(v);
            if (save && settings.saveVolumes)
            {
                PlayerPrefs.SetFloat(PrefsKey(channel), volumes[(int)channel]);
                PlayerPrefs.Save();
            }
        }

        float LoadVolume(AudioChannel channel)
        {
            float fallback = settings.DefaultVolume(channel);
            return settings.saveVolumes ? PlayerPrefs.GetFloat(PrefsKey(channel), fallback) : fallback;
        }

        internal static string PrefsKey(AudioChannel channel) => "Atthree.Audio.Volume." + channel;
    }
}
