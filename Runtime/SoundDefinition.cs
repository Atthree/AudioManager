using UnityEngine;
using UnityEngine.Audio;

namespace Atthree.Audio
{
    /// <summary>
    /// Bir sesin "tarifi": hangi klipler, ne kadar yüksek, ne kadar çeşitli, nerede duyulur.
    /// Project penceresi > sağ tık > Create > Audio Manager > Sound.
    ///
    /// Neden klip yerine bu? Aynı "adım sesi" her seferinde biraz farklı (rastgele klip, ses, pitch)
    /// çalınca kulak yorulmaz; ayarlar da sesin içinde durur, kodu değiştirmeden düzeltilir.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio Manager/Sound", fileName = "New Sound", order = 0)]
    public class SoundDefinition : ScriptableObject
    {
        [Tooltip("Her çalışta biri rastgele seçilir.")]
        public AudioClip[] clips = new AudioClip[0];
        public AudioChannel channel = AudioChannel.Sfx;

        [Header("Ses ve Çeşitlilik")]
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Her çalışta ses seviyesi bu kadar rastgele azalabilir (0 = hep aynı).")]
        [Range(0f, 0.5f)] public float volumeVariation = 0f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        [Tooltip("Her çalışta pitch ± bu kadar rastgele değişir.")]
        [Range(0f, 0.5f)] public float pitchVariation = 0.05f;
        [Tooltip("Aynı klip art arda iki kez seçilmesin.")]
        public bool avoidRepeat = true;
        public bool loop;

        [Header("3D (PlayAt / PlayAttached ile çalınca)")]
        [Tooltip("0 = konumsuz (2D), 1 = tamamen 3D. Play() ile çalınca her zaman 2D'dir.")]
        [Range(0f, 1f)] public float spatialBlend = 1f;
        public float minDistance = 1f;
        public float maxDistance = 40f;
        public AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;

        [Header("Spam Koruması")]
        [Tooltip("Bu sesin iki çalınışı arasında en az kaç saniye olmalı (0 = sınır yok).")]
        public float cooldown = 0f;
        [Tooltip("Aynı anda en fazla kaç kopyası çalabilir (0 = sınır yok). Dolunca en eskisi susar.")]
        public int maxInstances = 0;
        [Tooltip("0 = en önemli, 256 = en önemsiz. Kaynaklar dolunca önemsiz sesler susturulur.")]
        [Range(0, 256)] public int priority = 128;

        [Header("Gelişmiş")]
        [Tooltip("Boşsa kanalın mixer grubu kullanılır (AudioManagerSettings).")]
        public AudioMixerGroup outputOverride;

        [System.NonSerialized] int lastClipIndex = -1;

        /// <summary>Çalınacak klibi seçer (rastgele, art arda tekrar etmeden).</summary>
        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];
            int index = Random.Range(0, clips.Length);
            if (avoidRepeat && index == lastClipIndex) index = (index + 1 + Random.Range(0, clips.Length - 1)) % clips.Length;
            lastClipIndex = index;
            return clips[index];
        }

        public float PickVolume() => volume * (1f - Random.Range(0f, volumeVariation));
        public float PickPitch() => pitch * (1f + Random.Range(-pitchVariation, pitchVariation));

        /// <summary>Kodla hızlıca ses tanımı oluşturur (asset gerekmeden).</summary>
        public static SoundDefinition Create(AudioClip clip, AudioChannel channel = AudioChannel.Sfx, float volume = 1f, bool loop = false)
        {
            var sound = CreateInstance<SoundDefinition>();
            sound.name = clip != null ? clip.name : "Sound";
            sound.clips = new[] { clip };
            sound.channel = channel;
            sound.volume = volume;
            sound.loop = loop;
            sound.pitchVariation = 0f;
            return sound;
        }
    }
}
