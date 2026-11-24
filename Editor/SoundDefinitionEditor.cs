using UnityEditor;
using UnityEngine;

namespace Atthree.Audio.Editor
{
    /// <summary>Sound asset'inin Inspector'ı: Play'e basmadan dinleme düğmesi.</summary>
    [CustomEditor(typeof(SoundDefinition)), CanEditMultipleObjects]
    public class SoundDefinitionEditor : UnityEditor.Editor
    {
        static AudioSource previewSource;

        public override void OnInspectorGUI()
        {
            var sound = (SoundDefinition)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(sound.clips == null || sound.clips.Length == 0))
                {
                    if (GUILayout.Button("▶ Dinle", GUILayout.Height(26))) Preview(sound);
                }
                if (GUILayout.Button("■ Durdur", GUILayout.Height(26), GUILayout.Width(90))) StopPreview();
            }
            if (sound.clips == null || sound.clips.Length == 0)
                EditorGUILayout.HelpBox("Klip ekle: Project penceresinden ses dosyalarını aşağıdaki Clips listesine sürükle.", MessageType.Info);
            else if (sound.channel == AudioChannel.Master)
                EditorGUILayout.HelpBox("Master kanalında ses çalınamaz; Sfx olarak çalınacak.", MessageType.Warning);

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }

        /// <summary>Editörde rastgele klip/ses/pitch ile bir kez çalar (her basışta farklı varyasyon).</summary>
        static void Preview(SoundDefinition sound)
        {
            var clip = sound.PickClip();
            if (clip == null) return;
            if (previewSource == null)
            {
                var go = EditorUtility.CreateGameObjectWithHideFlags("[Sound Preview]", HideFlags.HideAndDontSave, typeof(AudioSource));
                previewSource = go.GetComponent<AudioSource>();
            }
            previewSource.Stop();
            previewSource.clip = clip;
            previewSource.volume = sound.PickVolume();
            previewSource.pitch = sound.PickPitch();
            previewSource.loop = sound.loop;
            previewSource.spatialBlend = 0f;
            previewSource.Play();
        }

        static void StopPreview()
        {
            if (previewSource != null) previewSource.Stop();
        }

        void OnDisable() => StopPreview();
    }
}
