using UnityEngine;

namespace Atthree.Audio
{
    /// <summary>
    /// Ses sisteminin ana kapısı. Her yerden çağrılabilir; ilk çağrıda kendini kurar.
    ///
    ///   AudioManager.Play(clickSound);                       // 2D (UI, arayüz)
    ///   AudioManager.PlayAt(explosionSound, transform.position); // 3D, bir noktada
    ///   AudioManager.PlayAttached(engineLoop, car.transform);    // 3D, objeyi takip eder
    ///   AudioManager.PlayMusic(forestMusic, fadeSeconds: 2f);    // müzik, yumuşak geçişle
    ///   AudioManager.SetVolume(AudioChannel.Music, 0.5f);        // ayarlar menüsü
    ///   AudioManager.SetPaused(true);                             // duraklatma menüsü
    /// </summary>
    public static class AudioManager
    {
        static AudioRuntime runtime;
        static AudioManagerSettings settingsOverride;
        static bool quitting;

        static void OnQuitting() => quitting = true;

        /// <summary>Ses motoru (ilk kullanımda oluşur). Oyun çalışmıyorsa null.</summary>
        internal static AudioRuntime Runtime
        {
            get
            {
                if (runtime != null) return runtime;
                if (!Application.isPlaying || quitting) return null; // kapanırken yeniden yaratma
                var settings = settingsOverride != null ? settingsOverride : Resources.Load<AudioManagerSettings>(AudioManagerSettings.ResourcesName);
                if (settings == null) settings = ScriptableObject.CreateInstance<AudioManagerSettings>();

                var go = new GameObject("[Audio Manager]");
                Object.DontDestroyOnLoad(go);
                runtime = go.AddComponent<AudioRuntime>();
                runtime.Initialize(settings);
                return runtime;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            quitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
            runtime = null;
            settingsOverride = null;
            Paused = false;
            musicForClip.Clear();
        }

        /// <summary>
        /// Ayarları kodla ver (Resources'taki asset yerine). Ses motoru zaten kurulduysa onu yeniden kurar.
        /// </summary>
        public static void Configure(AudioManagerSettings settings)
        {
            settingsOverride = settings;
            if (runtime != null)
            {
                Object.Destroy(runtime.gameObject);
                runtime = null;
            }
        }

        // ------------------------------------------------------------------ çalma
        /// <summary>2D çalar (konumsuz): arayüz, ödül, oyuncunun kendi sesleri.</summary>
        public static SoundHandle Play(SoundDefinition sound, float volumeScale = 1f) =>
            Runtime?.Play(sound, null, null, volumeScale) ?? SoundHandle.None;

        /// <summary>Dünyada bir noktada 3D çalar: patlama, çarpma.</summary>
        public static SoundHandle PlayAt(SoundDefinition sound, Vector3 position, float volumeScale = 1f) =>
            Runtime?.Play(sound, position, null, volumeScale) ?? SoundHandle.None;

        /// <summary>Bir objeyi takip ederek 3D çalar: motor, ayak sesi, konuşan karakter. Obje yok olunca susar.</summary>
        public static SoundHandle PlayAttached(SoundDefinition sound, Transform target, float volumeScale = 1f) =>
            target == null ? SoundHandle.None : Runtime?.Play(sound, null, target, volumeScale) ?? SoundHandle.None;

        /// <summary>Asset oluşturmadan hızlıca bir klip çal.</summary>
        public static SoundHandle PlayClip(AudioClip clip, AudioChannel channel = AudioChannel.Sfx, float volume = 1f) =>
            clip == null ? SoundHandle.None : Play(SoundDefinition.Create(clip, channel, volume));

        public static void StopAll(float fadeSeconds = 0f) => Runtime?.StopAll(null, fadeSeconds);

        public static void StopAll(AudioChannel channel, float fadeSeconds = 0f) => Runtime?.StopAll(channel, fadeSeconds);

        // ------------------------------------------------------------------ müzik
        /// <summary>
        /// Müziği değiştirir: eski parça kısılırken yenisi açılır (crossfade). Aynı parça çalıyorsa bir şey yapmaz.
        /// Tanımın kanalı ne olursa olsun Music kanalında çalar.
        /// </summary>
        public static SoundHandle PlayMusic(SoundDefinition music, float fadeSeconds = 1.5f) =>
            Runtime?.PlayMusic(music, fadeSeconds) ?? SoundHandle.None;

        public static SoundHandle PlayMusic(AudioClip clip, float fadeSeconds = 1.5f)
        {
            if (clip == null) return SoundHandle.None;
            // Aynı klip için hep aynı tanımı kullan; yoksa "aynı parça çalıyor" kontrolü tutmaz.
            if (!musicForClip.TryGetValue(clip, out var sound) || sound == null)
                musicForClip[clip] = sound = SoundDefinition.Create(clip, AudioChannel.Music, 1f, loop: true);
            return PlayMusic(sound, fadeSeconds);
        }

        static readonly System.Collections.Generic.Dictionary<AudioClip, SoundDefinition> musicForClip =
            new System.Collections.Generic.Dictionary<AudioClip, SoundDefinition>();

        public static void StopMusic(float fadeSeconds = 1.5f) => Runtime?.StopMusic(fadeSeconds);

        public static SoundHandle CurrentMusic => Runtime?.CurrentMusic ?? SoundHandle.None;

        // ------------------------------------------------------------------ ses seviyeleri
        /// <summary>Kanalın seviyesi (0..1).</summary>
        public static float GetVolume(AudioChannel channel) => Runtime?.GetVolume(channel) ?? 1f;

        /// <summary>Kanalın seviyesini ayarlar; çalan seslere hemen uygulanır ve (ayarlıysa) kaydedilir.</summary>
        public static void SetVolume(AudioChannel channel, float volume, bool save = true) =>
            Runtime?.SetVolume(channel, volume, save);

        /// <summary>0..1 arası seviyeyi desibele çevirir (AudioMixer parametresi için).</summary>
        public static float ToDecibels(float linear) => linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;

        // ------------------------------------------------------------------ duraklatma
        public static bool Paused { get; private set; }

        /// <summary>
        /// Oyunu sessizce duraklatır: efektler durur, kaldığı yerden devam eder.
        /// Müzik ve UI sesleri (ayarlara göre) çalmaya devam eder.
        /// </summary>
        public static void SetPaused(bool paused)
        {
            Paused = paused;
            AudioListener.pause = paused;
        }

        /// <summary>Şu an çalan ses sayısı (hata ayıklama).</summary>
        public static int ActiveVoiceCount => Runtime?.ActiveCount ?? 0;
    }
}
