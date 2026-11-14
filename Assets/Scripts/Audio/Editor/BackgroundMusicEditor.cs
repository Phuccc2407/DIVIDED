using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BackgroundMusic))]
public class BackgroundMusicEditor : Editor
{
    private Editor playlistEditor;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        serializedObject.Update();
        var playlist = serializedObject.FindProperty("playlist").objectReferenceValue;
        if (playlist == null)
        {
            EditorGUILayout.HelpBox("Assign the shared BackgroundMusicPlaylist asset to configure music.", MessageType.Info);
            return;
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shared playlist - edits apply to LobbySample and SampleScene", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Game Music = hub. Puzzle 3 (glass bridge) = Level Index 2; puzzle 4 (theatre) = Level Index 3.", MessageType.Info);
        CreateCachedEditor(playlist, null, ref playlistEditor);
        playlistEditor.OnInspectorGUI();
    }

    private void OnDisable()
    {
        if (playlistEditor != null) DestroyImmediate(playlistEditor);
    }
}
