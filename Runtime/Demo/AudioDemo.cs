using UnityEngine;

namespace Atthree.Audio.Demo
{
    /// <summary>
    /// Ses yöneticisini denemek için ekran paneli: kanal seviyeleri, efektler, 3D ses, müzik geçişi,
    /// duraklatma ve spam koruması. Tools > Audio Manager > Create Demo Scene ile kurulur.
    /// </summary>
    [AddComponentMenu("Audio Manager/Demo/Audio Demo Panel")]
    public class AudioDemo : MonoBehaviour
    {
        public SoundDefinition click, confirm, coin, jump, explosion, musicCalm, musicAction;

        static readonly AudioChannel[] Channels = { AudioChannel.Master, AudioChannel.Music, AudioChannel.Sfx, AudioChannel.Ui };
        GUIStyle label, title;
        string message = "";

        void OnGUI()
        {
            label ??= new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            title ??= new GUIStyle(label) { fontSize = 20, fontStyle = FontStyle.Bold };

            GUILayout.BeginArea(new Rect(12, 12, 420, Screen.height - 24), GUI.skin.box);
            GUILayout.Label("Audio Manager Demo", title);
            GUILayout.Label($"Çalan ses: {AudioManager.ActiveVoiceCount}   ·   Duraklatıldı: {(AudioManager.Paused ? "EVET" : "hayır")}", label);

            GUILayout.Space(6);
            GUILayout.Label("Kanal seviyeleri (kaydedilir, oyunu tekrar açınca geri gelir):", label);
            foreach (var channel in Channels)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(channel.ToString(), label, GUILayout.Width(70));
                float v = AudioManager.GetVolume(channel);
                float nv = GUILayout.HorizontalSlider(v, 0f, 1f, GUILayout.Width(220));
                GUILayout.Label($"{nv * 100:0}%", label, GUILayout.Width(50));
                GUILayout.EndHorizontal();
                if (!Mathf.Approximately(v, nv)) AudioManager.SetVolume(channel, nv);
            }

            GUILayout.Space(8);
            GUILayout.Label("Efektler:", label);
            GUILayout.BeginHorizontal();
            if (Button("Tık (UI)")) { }
            if (Button("Altın (2 varyasyon)")) AudioManager.Play(coin);
            if (Button("Zıpla")) AudioManager.Play(jump);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Button("Patlama (3D, rastgele yer)")) Explode();
            if (Button("Kutuları düşür")) DropCrates();
            GUILayout.EndHorizontal();
            if (Button("Spam: 30 altın aynı anda"))
            {
                for (int i = 0; i < 30; i++) AudioManager.Play(coin);
                message = $"30 kez çağrıldı ama coin sesinde en fazla {coin.maxInstances} kopya ve {coin.cooldown:0.00} sn bekleme var: kulak patlamaz.";
            }

            GUILayout.Space(8);
            GUILayout.Label("Müzik (yumuşak geçiş):", label);
            GUILayout.BeginHorizontal();
            if (Button("Sakin")) AudioManager.PlayMusic(musicCalm, 2f);
            if (Button("Hareketli")) AudioManager.PlayMusic(musicAction, 2f);
            if (Button("Durdur")) AudioManager.StopMusic(1.5f);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            if (Button(AudioManager.Paused ? "Devam Et" : "Duraklat (efektler durur, müzik ve UI devam eder)"))
            {
                AudioManager.SetPaused(!AudioManager.Paused);
                Time.timeScale = AudioManager.Paused ? 0f : 1f;
            }

            GUILayout.Space(6);
            GUILayout.Label("Dönen kapsül motor sesi çalıyor: yaklaşınca yükselir, sağdan/soldan duyulur.", label);
            GUILayout.Label(message, label);
            GUILayout.EndArea();
        }

        bool Button(string text)
        {
            bool pressed = GUILayout.Button(text, GUILayout.Height(28));
            if (pressed) AudioManager.Play(click); // her düğme UI kanalında tık sesi
            return pressed;
        }

        void Explode()
        {
            var cam = Camera.main != null ? Camera.main.transform : transform;
            Vector3 position = cam.position + cam.forward * 12f + cam.right * Random.Range(-12f, 12f);
            AudioManager.PlayAt(explosion, position);
            message = position.x < cam.position.x ? "Patlama solda." : "Patlama sağda.";
        }

        void DropCrates()
        {
            foreach (var body in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                body.position += Vector3.up * Random.Range(2f, 5f);
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Random.insideUnitSphere * 3f;
            }
            message = "Kutular yere çarpınca, çarpma hızıyla orantılı ses çıkarır (PlaySound: Collision).";
        }
    }
}
