using UnityEditor;
using UnityEngine;

public static class PlayerLocomotionSetup
{
    [MenuItem("DIVIDED/Player/Open Animation Controller")]
    public static void OpenController()
    {
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Prefabs/Player.controller");
        if (controller == null) return;
        Selection.activeObject = controller;
        AssetDatabase.OpenAsset(controller);
    }
}
