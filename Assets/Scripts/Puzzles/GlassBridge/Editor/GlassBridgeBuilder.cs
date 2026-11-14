using TMPro;
using UnityEditor;
using UnityEngine;

public static class GlassBridgeBuilder
{
    [MenuItem("DIVIDED/Create Glass Bridge Level")]
    private static void Create()
    {
        if (Application.isPlaying) return;
        var hub = Object.FindFirstObjectByType<LevelSelectionManager>();
        if (hub == null)
        {
            EditorUtility.DisplayDialog("Glass bridge", "Open the scene containing LevelSelectionManager first.", "OK");
            return;
        }
        var hubData = new SerializedObject(hub);
        var spawns = hubData.FindProperty("levelSpawns");
        int levelIndex = spawns.arraySize;
        var root = new GameObject("GlassBridge_Level_" + levelIndex);
        Undo.RegisterCreatedObjectUndo(root, "Create glass bridge");
        // Separate from the existing rooms. Move the whole root to reposition it.
        root.transform.position = new Vector3(150f, 20f, 0f);
        var manager = root.AddComponent<GlassBridgePuzzleManager>();
        var data = new SerializedObject(manager);
        data.FindProperty("levelSelectionManager").objectReferenceValue = hub;
        data.FindProperty("bridgeLevelIndex").intValue = levelIndex;
        var glass = GlassMaterial();

        Cube(root.transform, "Start platform", new Vector3(0, -0.3f, -5), new Vector3(7, 0.6f, 6));
        Transform first = Point(root.transform, "Spawn_Player1", new Vector3(-1.4f, 0.2f, -5));
        Transform second = Point(root.transform, "Spawn_Player2", new Vector3(1.4f, 0.2f, -5));
        data.FindProperty("startLeft").objectReferenceValue = Panel(root.transform, "Start_Left", new Vector3(-1.4f, 0, 0), glass, "START");
        data.FindProperty("startRight").objectReferenceValue = Panel(root.transform, "Start_Right", new Vector3(1.4f, 0, 0), glass, "START");
        var rows = data.FindProperty("rows");
        rows.arraySize = 6;
        for (int i = 0; i < rows.arraySize; i++)
        {
            var row = rows.GetArrayElementAtIndex(i);
            float z = (i + 1) * 4.2f;
            row.FindPropertyRelative("left").objectReferenceValue = Panel(root.transform, $"Row_{i + 1}_Left", new Vector3(-1.4f, 0, z), glass, "?");
            row.FindPropertyRelative("right").objectReferenceValue = Panel(root.transform, $"Row_{i + 1}_Right", new Vector3(1.4f, 0, z), glass, "?");
        }
        Cube(root.transform, "Finish platform", new Vector3(0, -0.3f, 30.2f), new Vector3(7, 0.6f, 5));
        var finish = Point(root.transform, "FinishZone", new Vector3(0, 1.5f, 30.2f)).gameObject.AddComponent<BoxCollider>();
        finish.size = new Vector3(7, 3.2f, 4.5f);
        finish.isTrigger = true;
        data.FindProperty("finishZone").objectReferenceValue = finish;
        data.FindProperty("fallBoundary").objectReferenceValue = Point(root.transform, "FallBoundary", new Vector3(0, -6, 0));
        var screen = Cube(root.transform, "Screen backdrop", new Vector3(0, 6, 33), new Vector3(18, 8, 0.3f));
        screen.GetComponent<Renderer>().sharedMaterial = ScreenMaterial();
        data.FindProperty("questionDisplay").objectReferenceValue = Text(root.transform, "QuestionScreen", new Vector3(0, 6.5f, 32.8f), Quaternion.identity, new Vector2(17, 5), 10, "GLASS BRIDGE");
        data.FindProperty("timerDisplay").objectReferenceValue = Text(root.transform, "Timer", new Vector3(0, 3.5f, 32.8f), Quaternion.identity, new Vector2(8, 2), 12, "READY");
        data.ApplyModifiedPropertiesWithoutUndo();

        Undo.RecordObject(hub, "Connect glass bridge to hub");
        spawns.InsertArrayElementAtIndex(levelIndex);
        var entry = spawns.GetArrayElementAtIndex(levelIndex);
        entry.FindPropertyRelative("firstPlayer").objectReferenceValue = first;
        entry.FindPropertyRelative("secondPlayer").objectReferenceValue = second;
        var bridges = hubData.FindProperty("glassBridgePuzzles");
        int bridgeIndex = bridges.arraySize;
        bridges.InsertArrayElementAtIndex(bridgeIndex);
        bridges.GetArrayElementAtIndex(bridgeIndex).objectReferenceValue = manager;
        hubData.ApplyModifiedProperties();
        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log($"Created glass bridge. Set a hub LevelDoor's Level Index to {levelIndex}. Confirm Hub Level Index on the bridge (default 0), then save the scene.", root);
    }

    private static GlassBridgePanel Panel(Transform parent, string name, Vector3 position, Material material, string answer)
    {
        var root = Point(parent, name, position);
        var surface = Cube(root, "Glass", Vector3.zero, new Vector3(2.4f, 0.18f, 3));
        surface.GetComponent<Renderer>().sharedMaterial = material;
        var panel = root.gameObject.AddComponent<GlassBridgePanel>();
        var data = new SerializedObject(panel);
        data.FindProperty("surface").objectReferenceValue = surface.GetComponent<Collider>();
        data.FindProperty("visual").objectReferenceValue = surface;
        data.FindProperty("answerLabel").objectReferenceValue = Text(root, "Answer below glass", new Vector3(0, -0.14f, 0), Quaternion.Euler(90, 0, 0), new Vector2(2.1f, 2.5f), 5, answer);
        data.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    private static Transform Point(Transform parent, string name, Vector3 position)
    {
        var item = new GameObject(name).transform;
        item.SetParent(parent, false);
        item.localPosition = position;
        return item;
    }

    private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale)
    {
        var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        return item;
    }

    private static TMP_Text Text(Transform parent, string name, Vector3 position, Quaternion rotation, Vector2 size, float fontSize, string value)
    {
        var point = Point(parent, name, position);
        point.localRotation = rotation;
        var label = point.gameObject.AddComponent<TextMeshPro>();
        label.rectTransform.sizeDelta = size;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.text = value;
        return label;
    }

    private static Material GlassMaterial()
    {
        const string path = "Assets/Scripts/Puzzles/GlassBridge/BridgeGlass.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetFloat("_Surface", 1);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0);
        material.SetColor("_BaseColor", new Color(0.35f, 0.8f, 0.95f, 0.22f));
        material.SetFloat("_Smoothness", 0.75f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = 3000;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material ScreenMaterial()
    {
        const string path = "Assets/Scripts/Puzzles/GlassBridge/BridgeScreen.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor", new Color(0.015f, 0.025f, 0.045f));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
