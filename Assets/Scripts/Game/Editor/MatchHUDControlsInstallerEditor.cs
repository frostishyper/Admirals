#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public static class MatchHUDControlsInstallerEditor
{
    private const string HudPrefabPath =
        "Assets/Prefabs/HUD/HUD_Game.prefab";
    private const string GameScenePath =
        "Assets/Scenes/Game.unity";


    [MenuItem("Admirals/HUD/Create and Wire Match Controls")]
    public static void Install()
    {
        BuildHudPrefab();
        WireGameScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Match Controls Installed",
            "Added the shared turn counter and confirmed Exit Match flow " +
            "to HUD_Game, wired the Game scene, and saved both assets.",
            "OK");
    }


    private static void BuildHudPrefab()
    {
        GameObject Root = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        if (Root == null)
            throw new InvalidOperationException(
                $"Could not load '{HudPrefabPath}'.");

        try
        {
            TMP_Text TextTemplate =
                Root.GetComponentInChildren<TMP_Text>(true);
            Button ButtonTemplate =
                Root.GetComponentInChildren<Button>(true);
            Transform TopBar = FindDescendant(Root.transform, "TopBar");
            Image PanelTemplate =
                TopBar != null ? TopBar.GetComponent<Image>() : null;

            if (TextTemplate == null || ButtonTemplate == null)
            {
                throw new InvalidOperationException(
                    "HUD_Game requires an existing TMP label and Button " +
                    "so the shared controls can preserve its visual style.");
            }

            if (TopBar == null)
            {
                throw new InvalidOperationException(
                    "HUD_Game requires its existing TopBar transform.");
            }

            DestroyChild(Root.transform, "MatchControlsRow");
            DestroyChild(Root.transform, "ExitConfirmation");

            BuildMatchControlsRow(
                Root.transform,
                TextTemplate,
                ButtonTemplate,
                PanelTemplate);
            BuildExitConfirmation(
                Root.transform,
                TextTemplate,
                ButtonTemplate,
                PanelTemplate);

            PrefabUtility.SaveAsPrefabAsset(Root, HudPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(Root);
        }
    }


    private static void BuildMatchControlsRow(
        Transform Parent,
        TMP_Text TextTemplate,
        Button ButtonTemplate,
        Image PanelTemplate)
    {
        GameObject RowObject = CreateUIObject(
            "MatchControlsRow", Parent);
        SetIgnoreLayout(RowObject);
        RectTransform Row = RowObject.GetComponent<RectTransform>();
        Row.anchorMin = new Vector2(0f, 1f);
        Row.anchorMax = new Vector2(0f, 1f);
        Row.pivot = new Vector2(0.5f, 0.5f);
        Row.anchoredPosition = new Vector2(290f, -42.5f);
        Row.sizeDelta = new Vector2(260f, 52f);

        Image Background = RowObject.AddComponent<Image>();
        CopyImageStyle(PanelTemplate, Background);
        Background.color = new Color(1f, 1f, 1f, 0f);
        Background.raycastTarget = false;

        HorizontalLayoutGroup Layout =
            RowObject.AddComponent<HorizontalLayoutGroup>();
        Layout.padding = new RectOffset(10, 10, 6, 6);
        Layout.spacing = 8f;
        Layout.childAlignment = TextAnchor.MiddleCenter;
        Layout.childControlWidth = true;
        Layout.childControlHeight = true;
        Layout.childForceExpandWidth = false;
        Layout.childForceExpandHeight = true;

        TMP_Text Turn = CreateText(
            "Text_TurnNumber", Row, TextTemplate,
            "TURN --", 20f, TextAlignmentOptions.Center);
        ConfigureLayout(Turn.gameObject, 90f, 40f, 90f, 40f, 0f, 0f);

        Button Exit = CreateButton(
            "Button_ExitMatch", Row, ButtonTemplate,
            TextTemplate, "EXIT MATCH", 20f);
        ConfigureLayout(Exit.gameObject, 140f, 40f, 140f, 40f, 0f, 0f);

        Row.SetAsLastSibling();
    }


    private static void BuildExitConfirmation(
        Transform Parent,
        TMP_Text TextTemplate,
        Button ButtonTemplate,
        Image PanelTemplate)
    {
        GameObject ConfirmationObject = CreateUIObject(
            "ExitConfirmation", Parent);
        SetIgnoreLayout(ConfirmationObject);
        RectTransform Confirmation =
            ConfirmationObject.GetComponent<RectTransform>();
        Stretch(Confirmation);

        Image Blocker = ConfirmationObject.AddComponent<Image>();
        Blocker.color = new Color(0f, 0f, 0f, 0.78f);
        Blocker.raycastTarget = true;

        GameObject PanelObject = CreateUIObject(
            "Panel", Confirmation);
        RectTransform Panel = PanelObject.GetComponent<RectTransform>();
        Panel.anchorMin = new Vector2(0.5f, 0.5f);
        Panel.anchorMax = new Vector2(0.5f, 0.5f);
        Panel.pivot = new Vector2(0.5f, 0.5f);
        Panel.anchoredPosition = Vector2.zero;
        Panel.sizeDelta = new Vector2(660f, 280f);

        Image PanelImage = PanelObject.AddComponent<Image>();
        CopyImageStyle(PanelTemplate, PanelImage);
        PanelImage.raycastTarget = true;

        VerticalLayoutGroup PanelLayout =
            PanelObject.AddComponent<VerticalLayoutGroup>();
        PanelLayout.padding = new RectOffset(24, 24, 24, 24);
        PanelLayout.spacing = 14f;
        PanelLayout.childAlignment = TextAnchor.MiddleCenter;
        PanelLayout.childControlWidth = true;
        PanelLayout.childControlHeight = true;
        PanelLayout.childForceExpandWidth = true;
        PanelLayout.childForceExpandHeight = false;

        TMP_Text Title = CreateText(
            "Text_Title", Panel, TextTemplate,
            "EXIT MATCH?", 30f, TextAlignmentOptions.Center);
        ConfigureLayout(Title.gameObject, -1f, 40f, -1f, 40f, 1f, 0f);

        TMP_Text Message = CreateText(
            "Text_Message", Panel, TextTemplate,
            "Current match progress will be lost.",
            22f, TextAlignmentOptions.Center);
        ConfigureLayout(Message.gameObject, -1f, 80f, -1f, 80f, 1f, 0f);

        GameObject ButtonRowObject = CreateUIObject(
            "ButtonRow", Panel);
        RectTransform ButtonRow =
            ButtonRowObject.GetComponent<RectTransform>();
        ConfigureLayout(ButtonRowObject, -1f, 52f, -1f, 52f, 1f, 0f);

        HorizontalLayoutGroup ButtonLayout =
            ButtonRowObject.AddComponent<HorizontalLayoutGroup>();
        ButtonLayout.spacing = 12f;
        ButtonLayout.childAlignment = TextAnchor.MiddleCenter;
        ButtonLayout.childControlWidth = true;
        ButtonLayout.childControlHeight = true;
        ButtonLayout.childForceExpandWidth = true;
        ButtonLayout.childForceExpandHeight = true;

        Button Cancel = CreateButton(
            "Button_CancelExit", ButtonRow, ButtonTemplate,
            TextTemplate, "KEEP PLAYING", 22f);
        ConfigureLayout(Cancel.gameObject, 220f, 48f, -1f, 48f, 1f, 0f);

        Button Confirm = CreateButton(
            "Button_ConfirmExit", ButtonRow, ButtonTemplate,
            TextTemplate, "EXIT TO LOBBY", 22f);
        ConfigureLayout(Confirm.gameObject, 220f, 48f, -1f, 48f, 1f, 0f);

        Confirmation.SetAsLastSibling();
        ConfirmationObject.SetActive(false);
    }


    private static void WireGameScene()
    {
        Scene GameScene = SceneManager.GetSceneByPath(GameScenePath);
        bool OpenedForInstall = !GameScene.isLoaded;

        if (OpenedForInstall)
        {
            GameScene = EditorSceneManager.OpenScene(
                GameScenePath,
                OpenSceneMode.Additive);
        }

        try
        {
            HumanGameplayInput Input =
                FindInScene<HumanGameplayInput>(GameScene);
            GameplayHUD Hud =
                FindInScene<GameplayHUD>(GameScene);
            Transform HudRoot = Hud != null ? Hud.transform : null;

            if (Input == null || HudRoot == null)
            {
                throw new InvalidOperationException(
                    "Game scene requires HumanGameplayInput and the " +
                    "scene-owned GameplayHUD component on the HUD prefab instance.");
            }

            Transform MatchRow =
                FindDescendant(HudRoot, "MatchControlsRow");
            Transform Confirmation =
                FindDescendant(HudRoot, "ExitConfirmation");

            if (MatchRow == null || Confirmation == null)
            {
                throw new InvalidOperationException(
                    "The Game scene HUD instance did not receive the new " +
                    "MatchControlsRow and ExitConfirmation prefab children.");
            }

            RectTransform MatchRowRect =
                MatchRow.GetComponent<RectTransform>();
            if (MatchRowRect != null &&
                PrefabUtility.IsPartOfPrefabInstance(MatchRowRect))
            {
                PrefabUtility.RevertObjectOverride(
                    MatchRowRect,
                    InteractionMode.AutomatedAction);
            }

            MatchHUDControls Controls =
                Input.GetComponent<MatchHUDControls>();
            if (Controls == null)
                Controls = Undo.AddComponent<MatchHUDControls>(Input.gameObject);

            SerializedObject Serialized = new SerializedObject(Controls);
            SetReference(Serialized, "_HumanGameplayInput", Input);
            SetReference(Serialized, "_TurnNumberText",
                FindDescendant(MatchRow, "Text_TurnNumber")
                    ?.GetComponent<TMP_Text>());
            SetReference(Serialized, "_ExitMatchButton",
                FindDescendant(MatchRow, "Button_ExitMatch")
                    ?.GetComponent<Button>());
            SetReference(Serialized, "_ExitConfirmationRoot",
                Confirmation.gameObject);
            SetReference(Serialized, "_ConfirmExitButton",
                FindDescendant(Confirmation, "Button_ConfirmExit")
                    ?.GetComponent<Button>());
            SetReference(Serialized, "_CancelExitButton",
                FindDescendant(Confirmation, "Button_CancelExit")
                    ?.GetComponent<Button>());

            SerializedProperty SceneName =
                Serialized.FindProperty("_FrontendSceneName");
            SceneName.stringValue = "Frontend";
            Serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(Controls);
            EditorSceneManager.MarkSceneDirty(GameScene);
            EditorSceneManager.SaveScene(GameScene);
        }
        finally
        {
            if (OpenedForInstall && GameScene.isLoaded)
                EditorSceneManager.CloseScene(GameScene, true);
        }
    }


    private static GameObject CreateUIObject(
        string Name,
        Transform Parent)
    {
        GameObject Object = new GameObject(
            Name,
            typeof(RectTransform),
            typeof(CanvasRenderer));
        Object.layer = LayerMask.NameToLayer("UI");
        Object.transform.SetParent(Parent, false);
        return Object;
    }


    private static TMP_Text CreateText(
        string Name,
        Transform Parent,
        TMP_Text Template,
        string Value,
        float FontSize,
        TextAlignmentOptions Alignment)
    {
        GameObject Object = CreateUIObject(Name, Parent);
        TextMeshProUGUI Text = Object.AddComponent<TextMeshProUGUI>();
        Text.font = Template.font;
        Text.fontSharedMaterial = Template.fontSharedMaterial;
        Text.color = Template.color;
        Text.fontSize = FontSize;
        Text.enableAutoSizing = false;
        Text.textWrappingMode = TextWrappingModes.NoWrap;
        Text.overflowMode = TextOverflowModes.Overflow;
        Text.alignment = Alignment;
        Text.raycastTarget = false;
        Text.text = Value;
        return Text;
    }


    private static Button CreateButton(
        string Name,
        Transform Parent,
        Button Template,
        TMP_Text TextTemplate,
        string Label,
        float FontSize)
    {
        GameObject Object = CreateUIObject(Name, Parent);
        Image Image = Object.AddComponent<Image>();
        Image TemplateImage = Template.targetGraphic as Image;
        CopyImageStyle(TemplateImage, Image);
        Image.raycastTarget = true;

        Button Button = Object.AddComponent<Button>();
        Button.targetGraphic = Image;
        Button.transition = Template.transition;
        Button.colors = Template.colors;
        Button.spriteState = Template.spriteState;
        Button.navigation = Template.navigation;

        TMP_Text Text = CreateText(
            "Text_Label", Object.transform, TextTemplate,
            Label, FontSize, TextAlignmentOptions.Center);
        Stretch(Text.rectTransform);
        return Button;
    }


    private static void CopyImageStyle(Image Source, Image Destination)
    {
        if (Source == null || Destination == null)
            return;

        Destination.sprite = Source.sprite;
        Destination.overrideSprite = Source.overrideSprite;
        Destination.type = Source.type;
        Destination.preserveAspect = Source.preserveAspect;
        Destination.fillCenter = Source.fillCenter;
        Destination.color = Source.color;
        Destination.material = Source.material;
    }


    private static void ConfigureLayout(
        GameObject Object,
        float MinWidth,
        float MinHeight,
        float PreferredWidth,
        float PreferredHeight,
        float FlexibleWidth,
        float FlexibleHeight)
    {
        LayoutElement Layout = Object.GetComponent<LayoutElement>();
        if (Layout == null)
            Layout = Object.AddComponent<LayoutElement>();

        Layout.minWidth = MinWidth;
        Layout.minHeight = MinHeight;
        Layout.preferredWidth = PreferredWidth;
        Layout.preferredHeight = PreferredHeight;
        Layout.flexibleWidth = FlexibleWidth;
        Layout.flexibleHeight = FlexibleHeight;
    }


    private static void SetIgnoreLayout(GameObject Object)
    {
        LayoutElement Layout = Object.GetComponent<LayoutElement>();
        if (Layout == null)
            Layout = Object.AddComponent<LayoutElement>();
        Layout.ignoreLayout = true;
    }


    private static void Stretch(RectTransform Rect)
    {
        Rect.anchorMin = Vector2.zero;
        Rect.anchorMax = Vector2.one;
        Rect.pivot = new Vector2(0.5f, 0.5f);
        Rect.anchoredPosition = Vector2.zero;
        Rect.sizeDelta = Vector2.zero;
    }


    private static void DestroyChild(Transform Parent, string Name)
    {
        Transform Existing = FindDescendant(Parent, Name);
        if (Existing != null && Existing != Parent)
            UnityEngine.Object.DestroyImmediate(Existing.gameObject);
    }


    private static void SetReference(
        SerializedObject Serialized,
        string PropertyName,
        UnityEngine.Object Value)
    {
        SerializedProperty Property =
            Serialized.FindProperty(PropertyName);
        if (Property == null)
            throw new InvalidOperationException(
                $"MatchHUDControls has no serialized field '{PropertyName}'.");
        if (Value == null)
            throw new InvalidOperationException(
                $"Could not resolve MatchHUDControls field '{PropertyName}'.");
        Property.objectReferenceValue = Value;
    }


    private static T FindInScene<T>(Scene Scene)
        where T : Component
    {
        foreach (GameObject Root in Scene.GetRootGameObjects())
        {
            T Found = Root.GetComponentInChildren<T>(true);
            if (Found != null)
                return Found;
        }

        return null;
    }


    private static Transform FindDescendant(Transform Root, string Name)
    {
        if (Root == null)
            return null;
        if (Root.name == Name)
            return Root;

        for (int i = 0; i < Root.childCount; i++)
        {
            Transform Found = FindDescendant(Root.GetChild(i), Name);
            if (Found != null)
                return Found;
        }

        return null;
    }
}
#endif
