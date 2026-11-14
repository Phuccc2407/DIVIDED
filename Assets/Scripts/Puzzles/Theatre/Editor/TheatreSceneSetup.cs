using UnityEditor;
using UnityEngine;

public static class TheatreSceneSetup
{
    [MenuItem("DIVIDED/Theatre/Select Configured Stage")]
    public static void SelectSetup()
    {
        var root = GameObject.Find("Theatre_Level_4_Setup");
        if (root == null) { Debug.LogWarning("Open SampleScene to find Theatre_Level_4_Setup."); return; }
        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    [MenuItem("DIVIDED/Theatre/Validate Open Scene")]
    public static void ValidateOpenScene()
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<TheatrePuzzleManager>();
        if (manager == null) { Debug.LogError("No TheatrePuzzleManager in the open scene."); return; }
        manager.SendMessage("ValidateInEditor", SendMessageOptions.RequireReceiver);
        Selection.activeGameObject = manager.gameObject;
    }

}
