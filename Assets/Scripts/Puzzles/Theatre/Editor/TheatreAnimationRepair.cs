using System.Linq;
using UnityEditor;
using UnityEngine;

// Repair the copied Generic Angry clip after its Mixamo source is imported as Humanoid.
public class TheatreAnimationRepair : AssetPostprocessor
{
    private const string SourcePath = "Assets/Prefabs/X Bot@Angry.fbx";
    private const string ClipPath = "Assets/Prefabs/Angry.anim";

    [InitializeOnLoadMethod]
    private static void ScheduleRepair() => EditorApplication.delayCall += Repair;

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] oldPaths)
    {
        if (imported.Contains(SourcePath)) ScheduleRepair();
    }

    [MenuItem("DIVIDED/Theatre/Repair Angry Humanoid Clip")]
    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var target = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (target == null || target.humanMotion) return;
        var source = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__") && clip.humanMotion);
        if (source == null)
        {
            Debug.LogWarning("Theatre: Angry is still Generic. Wait for its FBX Humanoid import, then use DIVIDED/Theatre/Repair Angry Humanoid Clip.");
            return;
        }
        string clipName = target.name;
        EditorUtility.CopySerialized(source, target);
        target.name = clipName;
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssetIfDirty(target);
        Debug.Log("Theatre: rebuilt Angry.anim as Humanoid and preserved its scene references.", target);
    }
}
