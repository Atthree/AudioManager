using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Atthree.Audio.Tests
{
    public class AudioManagerTests
    {
        AudioManagerSettings settings;
        readonly List<Object> cleanup = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<AudioManagerSettings>();
            settings.saveVolumes = false;
            settings.initialSources = 4;
            settings.maxVoices = 8;
            AudioManager.Configure(settings);
            AudioManager.SetPaused(false);
        }

        [TearDown]
        public void TearDown()
        {
            AudioManager.SetPaused(false);
            foreach (var o in cleanup) if (o != null) Object.Destroy(o);
            cleanup.Clear();
            AudioManager.Configure(null);
        }

        // ------------------------------------------------------------------ yardımcılar
        static AudioClip Clip(float seconds, float frequency = 440f, string name = "test")
        {
            int samples = Mathf.Max(1, (int)(seconds * 44100));
            var clip = AudioClip.Create(name, samples, 1, 44100, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++) data[i] = 0.2f * Mathf.Sin(2 * Mathf.PI * frequency * i / 44100f);
            clip.SetData(data, 0);
            return clip;
        }

        SoundDefinition Sound(float seconds = 1f, AudioChannel channel = AudioChannel.Sfx, bool loop = false, int clips = 1)
        {
            var s = ScriptableObject.CreateInstance<SoundDefinition>();
            s.clips = Enumerable.Range(0, clips).Select(i => Clip(seconds, 300 + i * 100, "clip" + i)).ToArray();
            s.channel = channel;
            s.loop = loop;
            s.pitchVariation = 0f;
            cleanup.Add(s);
            return s;
        }

        static AudioSource Source(SoundHandle h)
        {
            Assert.IsTrue(AudioManager.Runtime.TryGet(h.Id, out var v), "ses çalmıyor");
            return v.source;
        }

        static IEnumerator Wait(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        // ------------------------------------------------------------------ çalma ve seviye
        [UnityTest]
        public IEnumerator Play_Is2D_AndAppliesChannelAndMasterVolume()
        {
            var s = Sound();
            s.volume = 0.5f;
            AudioManager.SetVolume(AudioChannel.Sfx, 0.5f);
            AudioManager.SetVolume(AudioChannel.Master, 0.8f);

            var h = AudioManager.Play(s);
            Assert.IsTrue(h.IsPlaying);
            var src = Source(h);
            Assert.AreEqual(0f, src.spatialBlend, "Play 2D olmalı");
            Assert.AreEqual(0.5f * 0.5f * 0.8f, src.volume, 0.001f);

            // Çalarken kanal seviyesi değişince hemen uygulanmalı.
            AudioManager.SetVolume(AudioChannel.Sfx, 1f);
            Assert.AreEqual(0.5f * 1f * 0.8f, src.volume, 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayAt_Is3D_AndPlayAttached_FollowsTarget()
        {
            var s = Sound(2f);
            var at = AudioManager.PlayAt(s, new Vector3(5, 0, 0));
            Assert.AreEqual(1f, Source(at).spatialBlend);
            Assert.AreEqual(new Vector3(5, 0, 0), Source(at).transform.position);

            var target = new GameObject("target");
            cleanup.Add(target);
            var attached = AudioManager.PlayAttached(s, target.transform);
            target.transform.position = new Vector3(0, 3, 7);
            yield return null;
            Assert.AreEqual(new Vector3(0, 3, 7), Source(attached).transform.position, "objeyi takip etmeli");

            // Takip edilen obje yok olunca ses susmalı.
            Object.Destroy(target);
            yield return null;
            yield return null;
            Assert.IsFalse(attached.IsPlaying);
        }

        [Test]
        public void Variations_NoImmediateRepeat_AndPitchInRange()
        {
            var s = Sound(clips: 3);
            s.pitch = 1f;
            s.pitchVariation = 0.2f;
            s.maxInstances = 0;
            AudioClip last = null;
            var used = new HashSet<AudioClip>();
            for (int i = 0; i < 40; i++)
            {
                var src = Source(AudioManager.Play(s));
                Assert.AreNotEqual(last, src.clip, "aynı klip art arda gelmemeli");
                Assert.That(src.pitch, Is.InRange(0.8f, 1.2f));
                last = src.clip;
                used.Add(src.clip);
                AudioManager.StopAll(); // havuz dolmasın
            }
            Assert.AreEqual(3, used.Count, "tüm varyasyonlar kullanılmalı");
        }

        // ------------------------------------------------------------------ spam koruması
        [UnityTest]
        public IEnumerator Cooldown_BlocksRapidReplays()
        {
            var s = Sound();
            s.cooldown = 0.3f;
            Assert.IsTrue(AudioManager.Play(s).IsPlaying);
            Assert.IsFalse(AudioManager.Play(s).IsPlaying, "bekleme süresi içinde çalmamalı");
            yield return Wait(0.35f);
            Assert.IsTrue(AudioManager.Play(s).IsPlaying);
        }

        [Test]
        public void MaxInstances_KeepsNewest()
        {
            var s = Sound(5f);
            s.maxInstances = 3;
            var handles = Enumerable.Range(0, 5).Select(_ => AudioManager.Play(s)).ToList();
            Assert.AreEqual(3, AudioManager.ActiveVoiceCount);
            Assert.IsFalse(handles[0].IsPlaying, "en eskisi susmalı");
            Assert.IsTrue(handles[4].IsPlaying);
        }

        [Test]
        public void VoiceLimit_StealsLessImportantSounds()
        {
            var unimportant = Sound(5f, loop: true);
            unimportant.priority = 200;
            for (int i = 0; i < 8; i++) AudioManager.Play(unimportant);
            Assert.AreEqual(8, AudioManager.ActiveVoiceCount);

            var important = Sound(5f, loop: true);
            important.priority = 10;
            var h = AudioManager.Play(important);
            Assert.IsTrue(h.IsPlaying, "önemli ses yer bulmalı");
            Assert.AreEqual(8, AudioManager.ActiveVoiceCount, "sınır aşılmamalı");

            // Her yer önemli seslerle doluysa, önemsiz ses çalmamalı.
            AudioManager.StopAll();
            for (int i = 0; i < 8; i++) AudioManager.Play(important);
            Assert.IsFalse(AudioManager.Play(unimportant).IsPlaying);
        }

        // ------------------------------------------------------------------ yaşam döngüsü
        [UnityTest]
        public IEnumerator FinishedSound_ReturnsToPool()
        {
            var h = AudioManager.Play(Sound(0.1f));
            Assert.IsTrue(h.IsPlaying);
            yield return Wait(0.6f);
            Debug.Log($"[Audio] after finish: playing={h.IsPlaying} active={AudioManager.ActiveVoiceCount}");
            Assert.IsFalse(h.IsPlaying, "bitince havuza dönmeli");
            Assert.AreEqual(0, AudioManager.ActiveVoiceCount);
        }

        [UnityTest]
        public IEnumerator Stop_WithFade_FadesThenStops()
        {
            var h = AudioManager.Play(Sound(5f, loop: true));
            var src = Source(h);
            float start = src.volume;
            h.Stop(0.4f);
            yield return Wait(0.2f);
            Assert.IsTrue(h.IsPlaying, "fade sürerken çalmaya devam etmeli");
            Assert.Less(src.volume, start * 0.8f, "sesi kısılmalı");
            yield return Wait(0.4f);
            Assert.IsFalse(h.IsPlaying);
        }

        [Test]
        public void Handle_None_IsSafe()
        {
            var none = SoundHandle.None;
            Assert.IsFalse(none.IsPlaying);
            Assert.DoesNotThrow(() => { none.Stop(); none.SetVolume(1); none.SetPitch(1); none.FadeTo(0, 1); });
            Assert.IsFalse(AudioManager.Play(null).IsPlaying);
        }

        // ------------------------------------------------------------------ müzik
        [UnityTest]
        public IEnumerator Music_Crossfades_AndDoesNotRestartSameTrack()
        {
            var a = Sound(10f, AudioChannel.Music, loop: true);
            var b = Sound(10f, AudioChannel.Music, loop: true);

            var ha = AudioManager.PlayMusic(a, 0f);
            Assert.AreEqual(ha.Id, AudioManager.PlayMusic(a, 1f).Id, "aynı parça baştan başlamamalı");

            var hb = AudioManager.PlayMusic(b, 0.6f);
            yield return Wait(0.3f);
            Assert.IsTrue(ha.IsPlaying && hb.IsPlaying, "geçiş sırasında ikisi de çalmalı");
            float va = Source(ha).volume, vb = Source(hb).volume;
            Debug.Log($"[Audio] crossfade mid: A={va:F2} B={vb:F2}");
            Assert.That(va, Is.InRange(0.05f, 0.65f));
            Assert.That(vb, Is.InRange(0.05f, 0.65f));

            yield return Wait(0.5f);
            Assert.IsFalse(ha.IsPlaying, "eski parça bitmeli");
            Assert.AreEqual(settings.defaultMusic, Source(hb).volume, 0.01f, "yeni parça tam seviyede");
            Assert.AreEqual(hb.Id, AudioManager.CurrentMusic.Id);
        }

        // ------------------------------------------------------------------ duraklatma
        [UnityTest]
        public IEnumerator Pause_StopsEffects_ButNotMusicOrUi()
        {
            var sfx = AudioManager.Play(Sound(0.2f));
            var ui = AudioManager.Play(Sound(5f, AudioChannel.Ui));
            var music = AudioManager.PlayMusic(Sound(5f, AudioChannel.Music, loop: true), 0f);

            AudioManager.SetPaused(true);
            Assert.IsTrue(AudioListener.pause);
            Assert.IsFalse(Source(sfx).ignoreListenerPause, "efekt duraklamalı");
            Assert.IsTrue(Source(ui).ignoreListenerPause, "UI devam etmeli");
            Assert.IsTrue(Source(music).ignoreListenerPause, "müzik devam etmeli");

            yield return Wait(0.5f);
            Assert.IsTrue(sfx.IsPlaying, "duraklatılmış ses bitmiş sayılıp havuza verilmemeli");
            AudioManager.SetPaused(false);
        }

        // ------------------------------------------------------------------ kalıcı seviyeler
        [Test]
        public void Volumes_ArePersisted()
        {
            string key = AudioRuntime.PrefsKey(AudioChannel.Music);
            float before = PlayerPrefs.GetFloat(key, -1f);
            try
            {
                settings.saveVolumes = true;
                AudioManager.Configure(settings);
                AudioManager.SetVolume(AudioChannel.Music, 0.33f);

                AudioManager.Configure(settings); // oyun yeniden açılmış gibi
                Assert.AreEqual(0.33f, AudioManager.GetVolume(AudioChannel.Music), 0.001f);
            }
            finally
            {
                if (before < 0f) PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetFloat(key, before);
            }
        }

        // ------------------------------------------------------------------ bileşenler
        [UnityTest]
        public IEnumerator PlaySound_OnCollision_PlaysWithImpactVolume()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            cleanup.Add(ground);
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cleanup.Add(box);
            box.transform.position = new Vector3(0, 3, 0);
            box.AddComponent<Rigidbody>();
            var ps = box.AddComponent<PlaySound>();
            ps.sound = Sound(1f);
            ps.playOn = PlaySound.When.Collision;
            ps.minImpactSpeed = 1f;
            ps.fullVolumeSpeed = 20f;

            Assert.AreEqual(0, AudioManager.ActiveVoiceCount);
            float timeout = Time.time + 3f;
            while (AudioManager.ActiveVoiceCount == 0 && Time.time < timeout) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, AudioManager.ActiveVoiceCount, "yere çarpınca ses çalmalı");

            var src = AudioManager.Runtime.GetComponentsInChildren<AudioSource>().First(a => a.clip != null);
            Debug.Log($"[Audio] impact volume={src.volume:F2} at {src.transform.position}");
            Assert.Less(src.volume, 0.9f, "orta hızdaki çarpma tam ses olmamalı");
            Assert.Less(Mathf.Abs(src.transform.position.y), 0.1f, "ses çarpma noktasında");
        }
    }
}
