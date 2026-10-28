#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PassAndPlayHandoffPrefabMakerEditor
{
    private const string Folder = "Assets/Prefabs/HUD/PassAndPlay";
    private const string PrefabPath = Folder + "/HUD_PassAndPlay_Handoff.prefab";

    [MenuItem("Admirals/HUD/Create and Wire Pass & Play Handoff")]
    public static void CreateAndWire()
    {
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogError("Close Prefab Mode and open the Game scene before wiring Pass & Play.");
            return;
        }

        HumanGameplayInput Input = Object.FindFirstObjectByType<HumanGameplayInput>();
        if (Input == null || !Input.gameObject.scene.IsValid() ||
            Input.gameObject.scene.name != "Game")
        {
            Debug.LogError("Open the Game scene. Its scene-instance HumanGameplayInput is required.");
            return;
        }

        Canvas ParentCanvas = FindGameplayCanvas(Input.gameObject.scene);
        if (ParentCanvas == null)
        {
            Debug.LogError("The loaded Game scene has no Canvas_Game Canvas.");
            return;
        }

        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/HUD");
        EnsureFolder(Folder);

        GameObject Root = BuildPrefabRoot();
        WireView(Root.GetComponent<PassAndPlayHandoffUI>(), Root.transform);
        GameObject Saved = PrefabUtility.SaveAsPrefabAsset(Root, PrefabPath);
        Object.DestroyImmediate(Root);
        if (Saved == null)
        {
            Debug.LogError("Failed to save the Pass & Play handoff prefab.");
            return;
        }

        SerializedObject SerializedInput = new SerializedObject(Input);
        SerializedInput.FindProperty("_PassAndPlayHandoffPrefab").objectReferenceValue =
            Saved.GetComponent<PassAndPlayHandoffUI>();
        SerializedInput.FindProperty("_PassAndPlayHandoffParent").objectReferenceValue =
            ParentCanvas.transform;
        foreach (GameObject SceneRoot in Input.gameObject.scene.GetRootGameObjects())
        {
            GameplayCamera Camera = SceneRoot.GetComponentInChildren<GameplayCamera>(true);
            if (Camera == null)
                continue;
            SerializedInput.FindProperty("_PassAndPlayCamera").objectReferenceValue = Camera;
            break;
        }
        SerializedInput.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(Input);
        EditorSceneManager.MarkSceneDirty(Input.gameObject.scene);
        EditorSceneManager.SaveScene(Input.gameObject.scene);
        AssetDatabase.SaveAssets();
        Selection.activeObject = Saved;
        EditorGUIUtility.PingObject(Saved);
        Debug.Log("Created HUD_PassAndPlay_Handoff and wired the loaded Game scene. Singleplayer will not instantiate it.");
    }

    private static Canvas FindGameplayCanvas(UnityEngine.SceneManagement.Scene Scene)
    {
        foreach (GameObject Root in Scene.GetRootGameObjects())
            foreach (Canvas Canvas in Root.GetComponentsInChildren<Canvas>(true))
                if (Canvas.name == "Canvas_Game")
                    return Canvas;
        return null;
    }

    private static GameObject BuildPrefabRoot()
    {
        GameObject Root = NewUI("HUD_PassAndPlay_Handoff", null);
        Stretch(Root.GetComponent<RectTransform>());
        Root.AddComponent<PassAndPlayHandoffUI>();
        Canvas Canvas = Root.AddComponent<Canvas>();
        Canvas.overrideSorting = true;
        Canvas.sortingOrder = 500;
        Root.AddComponent<GraphicRaycaster>();

        GameObject Privacy = NewUI("PrivacyRoot", Root.transform);
        Stretch(Privacy.GetComponent<RectTransform>());
        Image Blocker = Privacy.AddComponent<Image>();
        Blocker.color = new Color32(7, 20, 34, 255);
        Blocker.raycastTarget = true;

        GameObject Content = NewUI("Content", Privacy.transform);
        RectTransform ContentRect = Content.GetComponent<RectTransform>();
        ContentRect.anchorMin = new Vector2(0.2f, 0.25f);
        ContentRect.anchorMax = new Vector2(0.8f, 0.75f);
        ContentRect.offsetMin = Vector2.zero;
        ContentRect.offsetMax = Vector2.zero;
        VerticalLayoutGroup Layout = Content.AddComponent<VerticalLayoutGroup>();
        Layout.padding = new RectOffset(24, 24, 24, 24);
        Layout.spacing = 16f;
        Layout.childAlignment = TextAnchor.MiddleCenter;
        Layout.childControlWidth = true;
        Layout.childControlHeight = true;
        Layout.childForceExpandWidth = true;
        Layout.childForceExpandHeight = false;

        TMP_Text Reason = AddText(Content.transform, "Text_Reason", 24f);
        TMP_Text Player = AddText(Content.transform, "Text_Player", 42f);
        TMP_Text Instruction = AddText(Content.transform, "Text_Instruction", 22f);

        GameObject ButtonObject = NewUI("Button_Continue", Content.transform);
        Image ButtonImage = ButtonObject.AddComponent<Image>();
        ButtonImage.color = new Color32(245, 171, 55, 255);
        Button Button = ButtonObject.AddComponent<Button>();
        Button.targetGraphic = ButtonImage;
        HorizontalLayoutGroup ButtonLayout = ButtonObject.AddComponent<HorizontalLayoutGroup>();
        ButtonLayout.padding = new RectOffset(10, 10, 10, 10);
        ButtonLayout.childAlignment = TextAnchor.MiddleCenter;
        ButtonLayout.childControlWidth = true;
        ButtonLayout.childControlHeight = true;
        ButtonLayout.childForceExpandWidth = true;
        ButtonLayout.childForceExpandHeight = false;
        AddText(ButtonObject.transform, "Text_Label", 26f).text = "CONTINUE";

        Reason.text = "PHASE COMPLETE";
        Player.text = "PASS TO PLAYER";
        Instruction.text = "Press Continue when the next player is ready.";
        return Root;
    }

    private static void WireView(PassAndPlayHandoffUI View, Transform Root)
    {
        SerializedObject Serialized = new SerializedObject(View);
        Serialized.FindProperty("_PrivacyRoot").objectReferenceValue = Find(Root, "PrivacyRoot").gameObject;
        Serialized.FindProperty("_ReasonText").objectReferenceValue = Find(Root, "Text_Reason").GetComponent<TMP_Text>();
        Serialized.FindProperty("_PlayerText").objectReferenceValue = Find(Root, "Text_Player").GetComponent<TMP_Text>();
        Serialized.FindProperty("_InstructionText").objectReferenceValue = Find(Root, "Text_Instruction").GetComponent<TMP_Text>();
        Serialized.FindProperty("_ContinueButton").objectReferenceValue = Find(Root, "Button_Continue").GetComponent<Button>();
        Serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_Text AddText(Transform Parent, string Name, float Size)
    {
        GameObject Object = NewUI(Name, Parent);
        TextMeshProUGUI Text = Object.AddComponent<TextMeshProUGUI>();
        Text.font = TMP_Settings.defaultFontAsset;
        Text.fontSize = Size;
        Text.color = Color.white;
        Text.alignment = TextAlignmentOptions.Center;
        Text.textWrappingMode = TextWrappingModes.Normal;
        Text.raycastTarget = false;
        ContentSizeFitter Fitter = Object.AddComponent<ContentSizeFitter>();
        Fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return Text;
    }

    private static GameObject NewUI(string Name, Transform Parent)
    {
        GameObject Object = new GameObject(Name, typeof(RectTransform));
        Object.layer = LayerMask.NameToLayer("UI");
        Object.transform.SetParent(Parent, false);
        return Object;
    }

    private static void Stretch(RectTransform Rect)
    {
        Rect.anchorMin = Vector2.zero;
        Rect.anchorMax = Vector2.one;
        Rect.offsetMin = Vector2.zero;
        Rect.offsetMax = Vector2.zero;
    }

    private static Transform Find(Transform Root, string Name)
    {
        if (Root.name == Name)
            return Root;
        for (int i = 0; i < Root.childCount; i++)
        {
            Transform Found = Find(Root.GetChild(i), Name);
            if (Found != null)
                return Found;
        }
        return null;
    }

    private static void EnsureFolder(string Path)
    {
        if (AssetDatabase.IsValidFolder(Path))
            return;
        int Slash = Path.LastIndexOf('/');
        AssetDatabase.CreateFolder(Path.Substring(0, Slash), Path.Substring(Slash + 1));
    }
}
#endif
