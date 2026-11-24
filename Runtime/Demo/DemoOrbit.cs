using UnityEngine;

namespace Atthree.Audio.Demo
{
    /// <summary>
    /// Demo: çember çizen obje, üzerinde motor sesi döngüsü. 3D sesi ve pitch'i hızla değiştirmeyi gösterir.
    /// Gerçek bir arabada pitch'i araç hızına bağlarsın: handle.SetPitch(0.8f + speed / maxSpeed).
    /// </summary>
    [AddComponentMenu("Audio Manager/Demo/Demo Orbit")]
    public class DemoOrbit : MonoBehaviour
    {
        public SoundDefinition engine;
        public float radius = 7f;
        public float baseSpeed = 0.6f;

        SoundHandle handle;
        float angle;

        void Start() => handle = AudioManager.PlayAttached(engine, transform);

        void Update()
        {
            // Hız dalgalansın: hızlanınca pitch yükselir.
            float speed = baseSpeed * (1f + 0.6f * Mathf.Sin(Time.time * 0.7f));
            angle += speed * Time.deltaTime;
            transform.position = new Vector3(Mathf.Cos(angle) * radius, 0.5f, Mathf.Sin(angle) * radius + 2f);
            handle.SetPitch(0.7f + speed);
        }

        void OnDisable() => handle.Stop(0.2f);
    }
}
