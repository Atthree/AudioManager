using UnityEngine;

namespace Atthree.Audio
{
    /// <summary>
    /// Sahne açılınca (ya da bir alana girilince) müziği değiştirir. Önceki müzik yumuşakça kısılır.
    /// Her sahneye bir tane koy: menü müziği, orman müziği, boss müziği...
    /// </summary>
    [AddComponentMenu("Audio Manager/Play Music")]
    public class PlayMusic : MonoBehaviour
    {
        public SoundDefinition music;
        [Tooltip("Geçiş süresi (sn).")]
        public float fadeSeconds = 2f;
        [Tooltip("Açık: sahne başlayınca çal. Kapalı: bu objenin trigger'ına oyuncu girince çal (müzik alanı).")]
        public bool playOnStart = true;
        [Tooltip("Trigger modunda: sadece bu tag'e sahip obje tetiklesin (boş = hepsi).")]
        public string triggerTag = "Player";

        void Start()
        {
            if (playOnStart) AudioManager.PlayMusic(music, fadeSeconds);
        }

        void OnTriggerEnter(Collider other)
        {
            if (playOnStart) return;
            if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;
            AudioManager.PlayMusic(music, fadeSeconds);
        }
    }
}
