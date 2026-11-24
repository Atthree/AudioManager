using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Atthree.Audio.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Atthree.Audio.Editor
{
    public static class AudioMenus
    {
        const string PackageSounds = "Packages/com.atthree.audio-manager/Sounds";

        // ------------------------------------------------------------------ klipten ses
        /// <summary>
        /// Seçili ses kliplerinden Sound asset'i oluşturur. Adı sadece sonundaki numarayla ayrılan klipler
        /// (Footstep_01, Footstep_02...) tek bir sese varyasyon olarak toplanır.
        /// </summary>
        [MenuItem("Assets/Create/Audio Manager/Sound From Selected Clips", priority = 1)]
        [MenuItem("Tools/Audio Manager/Create Sounds From Selected Clips", priority = 10)]
        static void CreateFromSelection()
        {
            var clips = Selection.GetFiltered<AudioClip>(SelectionMode.Assets);
            if (clips.Length == 0)
            {
                EditorUtility.DisplayDialog("Audio Manager", "Önce Project penceresinde bir ya da daha çok ses dosyası seç.", "Tamam");
                return;
            }

            var created = new List<Object>();
            foreach (var group in clips.GroupBy(c => Regex.Replace(c.name, @"[_\- ]?\d+$", "")))
            {
                string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(group.First()));
                if (folder.StartsWith("Packages/")) folder = "Assets";
                var sound = ScriptableObject.CreateInstance<SoundDefinition>();
                sound.clips = group.OrderBy(c => c.name).ToArray();
                sound.loop = group.First().length > 5f && group.Key.ToLowerInvariant().Contains("loop");
                string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{group.Key}.asset");
                AssetDatabase.CreateAsset(sound, path);
                created.Add(sound);
            }
            AssetDatabase.SaveAssets();
            Selection.objects = created.ToArray();
            Debug.Log($"[AudioManager] {created.Count} ses oluşturuldu.");
        }

        [MenuItem("Assets/Create/Audio Manager/Sound From Selected Clips", true)]
        [MenuItem("Tools/Audio Manager/Create Sounds From Selected Clips", true)]
        static bool CanCreateFromSelection() => Selection.GetFiltered<AudioClip>(SelectionMode.Assets).Length > 0;

        // ------------------------------------------------------------------ ayarlar
        [MenuItem("Tools/Audio Manager/Create Settings Asset", priority = 20)]
        static void CreateSettings()
        {
            const string folder = "Assets/Resources";
            string path = $"{folder}/{AudioManagerSettings.ResourcesName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AudioManagerSettings>(path);
            if (existing == null)
            {
                if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Resources");
                existing = ScriptableObject.CreateInstance<AudioManagerSettings>();
                AssetDatabase.CreateAsset(existing, path);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
        }

        [MenuItem("Tools/Audio Manager/Reset Saved Volumes", priority = 21)]
        static void ResetVolumes()
        {
            foreach (AudioChannel channel in System.Enum.GetValues(typeof(AudioChannel)))
                PlayerPrefs.DeleteKey(AudioRuntime.PrefsKey(channel));
            PlayerPrefs.Save();
            Debug.Log("[AudioManager] Kayıtlı ses seviyeleri silindi.");
        }

        // ------------------------------------------------------------------ demo
        [MenuItem("Tools/Audio Manager/Create Demo Scene", priority = 40)]
        static void CreateDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            SoundDefinition Load(string name) => AssetDatabase.LoadAssetAtPath<SoundDefinition>($"{PackageSounds}/{name}.asset");

            var cam = Camera.main;
            cam.transform.SetPositionAndRotation(new Vector3(0f, 6f, -10f), Quaternion.Euler(28f, 0f, 0f));

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(3f, 1f, 3f);

            // Düşüp çarpınca ses çıkaran kutular (PlaySound: Collision).
            var impact = Load("Impact");
            for (int i = 0; i < 5; i++)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "Crate " + i;
                box.transform.position = new Vector3(-4f + i * 2f, 0.5f, 3f);
                box.AddComponent<Rigidbody>().mass = 10f;
                var sound = box.AddComponent<PlaySound>();
                sound.sound = impact;
                sound.playOn = PlaySound.When.Collision;
            }

            // Motor sesi çalan, daire çizen bir "araba" (3D ses + pitch).
            var car = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            car.name = "Orbiting Engine";
            car.transform.localScale = new Vector3(1f, 0.5f, 1f);
            Object.DestroyImmediate(car.GetComponent<Collider>());
            var orbit = car.AddComponent<DemoOrbit>();
            orbit.engine = Load("Engine");

            // Panel.
            var panel = new GameObject("Audio Demo");
            var demo = panel.AddComponent<AudioDemo>();
            demo.click = Load("UI_Click");
            demo.confirm = Load("UI_Confirm");
            demo.coin = Load("Coin");
            demo.jump = Load("Jump");
            demo.explosion = Load("Explosion");
            demo.musicCalm = Load("Music_Calm");
            demo.musicAction = Load("Music_Action");

            // Sahne açılınca müzik.
            var music = panel.AddComponent<PlayMusic>();
            music.music = demo.musicCalm;
            music.fadeSeconds = 2f;

            Selection.activeGameObject = panel;
            Debug.Log("[AudioManager] Demo sahnesi kuruldu. Play'e bas.");
        }
    }
}
