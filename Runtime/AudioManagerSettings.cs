using UnityEngine;
using UnityEngine.Audio;

namespace Atthree.Audio
{
    /// <summary>
    /// Ses yöneticisinin ayarları (isteğe bağlı). Kullanmak için Tools > Audio Manager > Create Settings Asset:
    /// Assets/Resources/AudioManagerSettings.asset oluşturur, oyun açılınca otomatik okunur.
    /// Bu asset yoksa varsayılan değerler kullanılır.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio Manager/Settings", fileName = "AudioManagerSettings", order = 20)]
    public class AudioManagerSettings : ScriptableObject
    {
        public const string ResourcesName = "AudioManagerSettings";

        [Header("Kaynak Havuzu")]
        [Tooltip("Başta hazırlanan AudioSource sayısı.")]
        public int initialSources = 16;
        [Tooltip("Aynı anda çalabilecek en fazla ses. Dolunca en önemsiz ses susturulur.")]
        public int maxVoices = 48;

        [Header("Ses Seviyeleri")]
        [Tooltip("Kanal seviyelerini PlayerPrefs'e kaydet; oyun tekrar açılınca geri gelsin.")]
        public bool saveVolumes = true;
        [Range(0f, 1f)] public float defaultMaster = 1f;
        [Range(0f, 1f)] public float defaultMusic = 0.7f;
        [Range(0f, 1f)] public float defaultSfx = 1f;
        [Range(0f, 1f)] public float defaultUi = 1f;
        [Range(0f, 1f)] public float defaultVoice = 1f;
        [Range(0f, 1f)] public float defaultAmbience = 0.8f;

        [Header("Duraklatma")]
        [Tooltip("Oyun duraklatılınca (SetPaused) müzik çalmaya devam etsin.")]
        public bool musicIgnoresPause = true;
        [Tooltip("UI sesleri duraklatma menüsünde de çalsın.")]
        public bool uiIgnoresPause = true;

        [Header("Mixer (isteğe bağlı)")]
        [Tooltip("Kanalları kendi AudioMixer gruplarına yönlendirmek istersen (efekt, sıkıştırma vb. için).")]
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup uiGroup;
        public AudioMixerGroup voiceGroup;
        public AudioMixerGroup ambienceGroup;

        public float DefaultVolume(AudioChannel channel) => channel switch
        {
            AudioChannel.Master => defaultMaster,
            AudioChannel.Music => defaultMusic,
            AudioChannel.Sfx => defaultSfx,
            AudioChannel.Ui => defaultUi,
            AudioChannel.Voice => defaultVoice,
            AudioChannel.Ambience => defaultAmbience,
            _ => 1f,
        };

        public AudioMixerGroup Group(AudioChannel channel) => channel switch
        {
            AudioChannel.Music => musicGroup,
            AudioChannel.Sfx => sfxGroup,
            AudioChannel.Ui => uiGroup,
            AudioChannel.Voice => voiceGroup,
            AudioChannel.Ambience => ambienceGroup,
            _ => null,
        };
    }
}
