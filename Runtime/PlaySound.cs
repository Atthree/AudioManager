using UnityEngine;

namespace Atthree.Audio
{
    /// <summary>
    /// Kod yazmadan ses çal: objeye ekle, sesi seç, ne zaman çalacağını seç.
    ///
    ///   Start / Enable / Disable : obje başlarken, açılırken, kapanırken
    ///   Collision                : bir şeye çarpınca; ses darbenin şiddetiyle orantılı (kutu düşmesi, araba kazası)
    ///   TriggerEnter             : bir trigger'a girilince (kapı alanı, toplanan eşya)
    ///   Manual                   : sadece kodla / UnityEvent ile Play() çağrılınca (buton OnClick)
    /// </summary>
    [AddComponentMenu("Audio Manager/Play Sound")]
    public class PlaySound : MonoBehaviour
    {
        public enum When { Start, Enable, Disable, Collision, TriggerEnter, Manual }

        public SoundDefinition sound;
        public When playOn = When.Start;

        [Tooltip("3D: objeyi takip ederek çal. Kapalıysa 2D (konumsuz) çalar.")]
        public bool positional = true;
        [Tooltip("Obje kapanınca / yok olunca (loop) sesi durdur.")]
        public bool stopOnDisable = true;

        [Header("Collision")]
        [Tooltip("Bu hızın (m/sn) altındaki çarpmalar ses çıkarmaz.")]
        public float minImpactSpeed = 1f;
        [Tooltip("Bu hızda ve üstünde tam ses.")]
        public float fullVolumeSpeed = 8f;
        [Tooltip("Sadece bu katmanlardaki objelerle çarpışınca çal.")]
        public LayerMask collisionLayers = ~0;

        SoundHandle handle;

        void Start() { if (playOn == When.Start) Play(); }
        void OnEnable() { if (playOn == When.Enable) Play(); }

        void OnDisable()
        {
            if (playOn == When.Disable) PlayDetached(); // obje kapanıyor: takip etmeden, olduğu yerde çal
            else if (stopOnDisable) handle.Stop(0.1f);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (playOn != When.Collision) return;
            if ((collisionLayers.value & (1 << collision.gameObject.layer)) == 0) return;

            float speed = collision.relativeVelocity.magnitude;
            if (speed < minImpactSpeed) return;
            float volume = Mathf.InverseLerp(minImpactSpeed, Mathf.Max(minImpactSpeed + 0.01f, fullVolumeSpeed), speed);
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            handle = AudioManager.PlayAt(sound, point, Mathf.Lerp(0.2f, 1f, volume));
        }

        void OnTriggerEnter(Collider other)
        {
            if (playOn != When.TriggerEnter) return;
            if ((collisionLayers.value & (1 << other.gameObject.layer)) == 0) return;
            Play();
        }

        /// <summary>Sesi çal (Manual için; Button OnClick'e de bağlanabilir).</summary>
        public void Play()
        {
            handle = positional ? AudioManager.PlayAttached(sound, transform) : AudioManager.Play(sound);
        }

        void PlayDetached()
        {
            handle = positional ? AudioManager.PlayAt(sound, transform.position) : AudioManager.Play(sound);
        }

        public void Stop(float fadeSeconds = 0f) => handle.Stop(fadeSeconds);
    }
}
