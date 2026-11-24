namespace Atthree.Audio
{
    /// <summary>
    /// Çalan bir sese uzaktan kumanda. AudioManager.Play(...) döner.
    ///
    ///   var engine = AudioManager.PlayAttached(engineLoop, car.transform);
    ///   engine.SetVolume(0.5f);
    ///   engine.Stop(fadeSeconds: 0.5f);
    ///
    /// Ses bittikten sonra kumanda geçersiz olur (IsPlaying false döner); güvenle çağırmaya devam edebilirsin.
    /// Ses çalınamadıysa (spam koruması, klip yok) <see cref="None"/> döner.
    /// </summary>
    public readonly struct SoundHandle
    {
        public static readonly SoundHandle None = default;

        public int Id { get; }

        public SoundHandle(int id) => Id = id;

        /// <summary>Ses hâlâ çalıyor mu (havuza geri verilmedi mi).</summary>
        public bool IsPlaying => Id != 0 && AudioManager.Runtime != null && AudioManager.Runtime.TryGet(Id, out _);

        public void Stop(float fadeSeconds = 0f)
        {
            if (TryGet(out var v)) AudioManager.Runtime.Stop(v, fadeSeconds);
        }

        /// <summary>Sesin kendi seviyesini değiştirir (kanal ve master ayrıca uygulanır).</summary>
        public void SetVolume(float volume)
        {
            if (TryGet(out var v)) v.baseVolume = volume * (v.sound != null ? v.sound.volume : 1f);
        }

        public void SetPitch(float pitch)
        {
            if (TryGet(out var v)) v.source.pitch = pitch;
        }

        /// <summary>Sesi yumuşakça bir seviyeye götürür (0 = sessiz, 1 = tam). Durdurmaz.</summary>
        public void FadeTo(float level, float seconds)
        {
            if (TryGet(out var v)) AudioManager.Runtime.FadeTo(v, level, seconds);
        }

        bool TryGet(out AudioRuntime.Voice voice)
        {
            voice = null;
            return Id != 0 && AudioManager.Runtime != null && AudioManager.Runtime.TryGet(Id, out voice);
        }

        public override string ToString() => Id == 0 ? "SoundHandle(None)" : $"SoundHandle({Id}, playing={IsPlaying})";
    }
}
