using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;


public static class SearchHUDPrefabMakerEditor
{
    private const string SourceFolder = "Assets/Prefabs/HUD/PreBattle";
    private const string SearchFolder = "Assets/Prefabs/HUD/Search";
    private const string HudGamePath = "Assets/Prefabs/HUD/HUD_Game.prefab";
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string LabelPath = SearchFolder + "/HUD_Search_StrengthLabel.prefab";

    [InitializeOnLoadMethod]
    private static void ScheduleSetup()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += CreateMissingPrefabsAutomatically;
    }


    private static void OnPlayModeStateChanged(PlayModeStateChange State)
    {
        if (State == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += CreateMissingPrefabsAutomatically;
    }


    private static void CreateMissingPrefabsAutomatically()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        try
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    SearchFolder + "/HUD_Search_Top.prefab") == null)
            {
                CreateAndWire();
                Debug.Log("Created and wired the independent Search HUD prefabs.");
                return;
            }

            RepairPhaseHudLayout();
        }
        catch (Exception Exception)
        {
            Debug.LogException(Exception);
        }
    }


    [MenuItem("Admirals/HUD/Create and Wire Search HUD Prefabs")]
    public static void CreateAndWire()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before wiring Search HUD prefabs.");

        EnsureFolder();
        EnsurePrefab("Top");
        EnsurePrefab("Overlay");
        EnsurePrefab("Bottom");
        EnsureStrengthLabelPrefab();
        InstallPrefabInstances();
        RepairPhaseHudLayout();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Scene Scene = SceneManager.GetSceneByPath(GameScenePath);
        bool Opened = !Scene.isLoaded;
        if (Opened)
            Scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
        try
        {
            GameplayHUD Hud = FindInScene<GameplayHUD>(Scene);
            MovementMapInteraction Movement = FindInScene<MovementMapInteraction>(Scene);
            HumanGameplayInput Input = FindInScene<HumanGameplayInput>(Scene);
            if (Hud == null || Movement == null || Input == null)
                throw new InvalidOperationException(
                    "Game scene requires GameplayHUD, MovementMapInteraction and HumanGameplayInput.");

            WireHud(Hud);
            WireInteraction(Input.gameObject, Movement, Hud);
            EditorSceneManager.MarkSceneDirty(Scene);
            EditorSceneManager.SaveScene(Scene);
        }
        finally
        {
            if (Opened && Scene.isLoaded)
                EditorSceneManager.CloseScene(Scene, true);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Search HUD prefab creation and Game scene wiring completed.");
    }


    [MenuItem("Admirals/HUD/Repair Search and Pre-Battle Layout")]
    public static void RepairPhaseHudLayout()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before repairing HUD prefabs.");

        RepairTopPrefab(SourceFolder + "/HUD_PreBattle_Top.prefab");
        RepairTopPrefab(SearchFolder + "/HUD_Search_Top.prefab");
        RepairSearchOverlayPrefab();
        RepairSearchOverlayInHudGame();
        AssetDatabase.SaveAssets();
    }


    private static void RepairTopPrefab(string Path)
    {
        GameObject Root = PrefabUtility.LoadPrefabContents(Path);
        try
        {
            ConfigureSingleLineText(FindDescendant(Root.transform, "Text_MovementCount"));
            ConfigureSingleLineText(FindDescendant(Root.transform, "Text_Timer"));

            Transform Right = FindDescendant(Root.transform, "Right");
            HorizontalLayoutGroup RightLayout =
                Right != null ? Right.GetComponent<HorizontalLayoutGroup>() : null;
            if (RightLayout != null)
            {
                RightLayout.spacing = 24f;
                EditorUtility.SetDirty(RightLayout);
            }

            Transform EndButton = FindDescendant(Root.transform, "Button_EndMovement");
            if (EndButton != null)
            {
                ConfigureSingleLineText(
                    EndButton.GetComponentInChildren<TMP_Text>(true)?.transform);
            }

            PrefabUtility.SaveAsPrefabAsset(Root, Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(Root);
        }
    }


    private static void ConfigureSingleLineText(Transform TextTransform)
    {
        TMP_Text Text = TextTransform != null
            ? TextTransform.GetComponent<TMP_Text>()
            : null;
        if (Text == null)
            return;

        Text.textWrappingMode = TextWrappingModes.NoWrap;
        Text.overflowMode = TextOverflowModes.Overflow;
        EditorUtility.SetDirty(Text);
    }


    private static void RepairSearchOverlayPrefab()
    {
        string Path = SearchFolder + "/HUD_Search_Overlay.prefab";
        GameObject Root = PrefabUtility.LoadPrefabContents(Path);
        try
        {
            ConfigureSearchOverlayRect(Root.GetComponent<RectTransform>());
            ConfigureSearchOverlayLayout(Root.transform);
            PrefabUtility.SaveAsPrefabAsset(Root, Path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(Root);
        }
    }


    private static void RepairSearchOverlayInHudGame()
    {
        GameObject Root = PrefabUtility.LoadPrefabContents(HudGamePath);
        try
        {
            Transform Overlay = FindDescendant(Root.transform, "HUD_Search_Overlay");
            if (Overlay != null)
            {
                ConfigureSearchOverlayRect(Overlay as RectTransform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(Overlay);
            }
            PrefabUtility.SaveAsPrefabAsset(Root, HudGamePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(Root);
        }
    }


    private static void ConfigureSearchOverlayRect(RectTransform Rect)
    {
        if (Rect == null)
            return;

        Rect.anchorMin = new Vector2(1f, 0f);
        Rect.anchorMax = new Vector2(1f, 0f);
        // Pin the content-fitted panel by its lower-right corner so shrinking
        // or growing its content does not move it away from the play-area edge.
        Rect.pivot = new Vector2(1f, 0f);
        Rect.sizeDelta = Vector2.zero;
        Rect.anchoredPosition = Vector2.zero;
        EditorUtility.SetDirty(Rect);
    }


    private static void ConfigureSearchOverlayLayout(Transform Root)
    {
        if (Root == null)
            return;

        VerticalLayoutGroup RootLayout = Root.GetComponent<VerticalLayoutGroup>();
        if (RootLayout != null)
        {
            RootLayout.padding = new RectOffset(10, 10, 10, 10);
            RootLayout.spacing = 8f;
            RootLayout.childForceExpandWidth = false;
            RootLayout.childForceExpandHeight = false;
            RootLayout.childAlignment = TextAnchor.MiddleCenter;
            EditorUtility.SetDirty(RootLayout);
        }

        Transform ControlsRow = FindDescendant(Root, "ControlsRow");
        LayoutElement ControlsLayout = ControlsRow != null
            ? ControlsRow.GetComponent<LayoutElement>() : null;
        if (ControlsLayout != null)
        {
            ControlsLayout.minWidth = -1f;
            ControlsLayout.minHeight = -1f;
            ControlsLayout.preferredWidth = -1f;
            ControlsLayout.preferredHeight = -1f;
            ControlsLayout.flexibleWidth = 0f;
            ControlsLayout.flexibleHeight = 0f;
            EditorUtility.SetDirty(ControlsLayout);
        }

        ConfigurePaddedContentButton(
            FindDescendant(Root, "Button_ConfirmMove"), 10);
    }


    private static void ConfigurePaddedContentButton(
        Transform ButtonTransform, int Padding)
    {
        if (ButtonTransform == null)
            return;

        HorizontalLayoutGroup Group =
            ButtonTransform.GetComponent<HorizontalLayoutGroup>();
        if (Group == null)
            Group = ButtonTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
        Group.padding = new RectOffset(Padding, Padding, Padding, Padding);
        Group.childAlignment = TextAnchor.MiddleCenter;
        Group.childControlWidth = true;
        Group.childControlHeight = true;
        Group.childForceExpandWidth = false;
        Group.childForceExpandHeight = false;

        ContentSizeFitter Fitter =
            ButtonTransform.GetComponent<ContentSizeFitter>();
        if (Fitter == null)
            Fitter = ButtonTransform.gameObject.AddComponent<ContentSizeFitter>();
        Fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        Fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement ButtonLayout =
            ButtonTransform.GetComponent<LayoutElement>();
        if (ButtonLayout != null)
        {
            ButtonLayout.minWidth = -1f;
            ButtonLayout.minHeight = -1f;
            ButtonLayout.preferredWidth = -1f;
            ButtonLayout.preferredHeight = -1f;
            ButtonLayout.flexibleWidth = 0f;
            ButtonLayout.flexibleHeight = 0f;
        }

        TMP_Text Label = ButtonTransform.GetComponentInChildren<TMP_Text>(true);
        if (Label == null)
            return;
        Label.textWrappingMode = TextWrappingModes.NoWrap;
        Label.overflowMode = TextOverflowModes.Overflow;
        Label.alignment = TextAlignmentOptions.Center;
        LayoutElement LabelLayout = Label.GetComponent<LayoutElement>();
        if (LabelLayout != null)
        {
            LabelLayout.minWidth = -1f;
            LabelLayout.minHeight = -1f;
            LabelLayout.preferredWidth = -1f;
            LabelLayout.preferredHeight = -1f;
            LabelLayout.flexibleWidth = 0f;
            LabelLayout.flexibleHeight = 0f;
        }

        EditorUtility.SetDirty(ButtonTransform.gameObject);
    }


    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(SearchFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs/HUD", "Search");
    }


    private static void EnsurePrefab(string Suffix)
    {
        string Destination = $"{SearchFolder}/HUD_Search_{Suffix}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Destination) == null &&
            !AssetDatabase.CopyAsset(
                $"{SourceFolder}/HUD_PreBattle_{Suffix}.prefab", Destination))
            throw new InvalidOperationException($"Could not create {Destination}.");

        GameObject Root = PrefabUtility.LoadPrefabContents(Destination);
        try
        {
            Root.name = $"HUD_Search_{Suffix}";
            if (Suffix == "Overlay")
            {
                Transform MoveColumn = FindDescendant(Root.transform, "MoveColumn");
                Transform Cancel = FindDescendant(Root.transform, "Button_CancelMove");
                if (MoveColumn != null)
                    MoveColumn.gameObject.SetActive(false);
                if (Cancel != null)
                    Cancel.gameObject.SetActive(false);
            }
            PrefabUtility.SaveAsPrefabAsset(Root, Destination);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(Root);
        }
    }


    private static void EnsureStrengthLabelPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(LabelPath) != null)
            return;
        GameObject Label = new GameObject("HUD_Search_StrengthLabel");
        TextMeshPro Text = Label.AddComponent<TextMeshPro>();
        Text.text = "100";
        Text.font = TMP_Settings.defaultFontAsset;
        Text.alignment = TextAlignmentOptions.Center;
        Text.enableAutoSizing = true;
        Text.fontSizeMin = 1f;
        Text.fontSizeMax = 5f;
        Text.rectTransform.sizeDelta = new Vector2(1f, 1f);
        Text.raycastTarget = false;
        MeshRenderer Renderer = Label.GetComponent<MeshRenderer>();
        Renderer.sortingOrder = 75;
        PrefabUtility.SaveAsPrefabAsset(Label, LabelPath);
        UnityEngine.Object.DestroyImmediate(Label);
    }


    private static void InstallPrefabInstances()
    {
        GameObject HudRoot = PrefabUtility.LoadPrefabContents(HudGamePath);
        try
        {
            Install(HudRoot, "TopBar", "Top");
            Install(HudRoot, "PlayAreaUI", "Overlay");
            Install(HudRoot, "BottomBar", "Bottom");
            PrefabUtility.SaveAsPrefabAsset(HudRoot, HudGamePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(HudRoot);
        }
    }


    private static void Install(GameObject Root, string ParentName, string Suffix)
    {
        Transform Parent = FindDescendant(Root.transform, ParentName);
        string Name = $"HUD_Search_{Suffix}";
        if (Parent == null || FindDirectChild(Parent, Name) != null)
            return;
        GameObject Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            $"{SearchFolder}/{Name}.prefab");
        GameObject Instance = (GameObject)PrefabUtility.InstantiatePrefab(Prefab, Parent);
        Instance.name = Name;
        Instance.SetActive(false);
    }


    private static void WireHud(GameplayHUD Hud)
    {
        Transform SourceTop = FindDescendant(Hud.transform, "HUD_PreBattle_Top");
        Transform SourceOverlay = FindDescendant(Hud.transform, "HUD_PreBattle_Overlay");
        Transform SourceBottom = FindDescendant(Hud.transform, "HUD_PreBattle_Bottom");
        Transform Top = FindDescendant(Hud.transform, "HUD_Search_Top");
        Transform Overlay = FindDescendant(Hud.transform, "HUD_Search_Overlay");
        Transform Bottom = FindDescendant(Hud.transform, "HUD_Search_Bottom");
        if (SourceTop == null || SourceOverlay == null || SourceBottom == null ||
            Top == null || Overlay == null || Bottom == null)
            throw new InvalidOperationException("Search HUD instances are missing from HUD_Game.");

        SerializedObject Serialized = new SerializedObject(Hud);
        SetReference(Serialized, "_SearchTopRoot", Top.gameObject);
        SetReference(Serialized, "_SearchOverlayRoot", Overlay.gameObject);
        SetReference(Serialized, "_SearchBottomRoot", Bottom.gameObject);
        CopyEquivalent(Serialized, SourceTop, Top, "_PreBattlePlayerNameText", "_SearchPlayerNameText");
        CopyEquivalent(Serialized, SourceTop, Top, "_PreBattlePhaseText", "_SearchPhaseText");
        CopyEquivalent(Serialized, SourceTop, Top, "_PreBattleActionCountText", "_SearchCountText");
        CopyEquivalent(Serialized, SourceTop, Top, "_PreBattleTimerText", "_SearchTimerText");
        CopyEquivalent(Serialized, SourceTop, Top, "_EndPreBattleButton", "_EndSearchButton");
        CopyEquivalent(Serialized, SourceOverlay, Overlay, "_PreBattleRotateLeftButton", "_SearchRotateLeftButton");
        CopyEquivalent(Serialized, SourceOverlay, Overlay, "_PreBattleRotateRightButton", "_SearchRotateRightButton");
        CopyEquivalent(Serialized, SourceOverlay, Overlay, "_ConfirmPreBattleActionButton", "_CommitSearchButton");
        CopyEquivalent(Serialized, SourceBottom, Bottom, "_PreBattleUnitDetails", "_SearchUnitDetails");
        Serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(Hud);
    }


    private static void WireInteraction(GameObject Host,
        MovementMapInteraction Movement, GameplayHUD Hud)
    {
        SearchMapInteraction Search = Host.GetComponent<SearchMapInteraction>();
        if (Search == null)
            Search = Host.AddComponent<SearchMapInteraction>();
        SerializedObject Source = new SerializedObject(Movement);
        SerializedObject Target = new SerializedObject(Search);
        foreach (string Name in new[] {
            "_GameMaster", "_HumanGameplayInput", "_UnitPresentationCatalog", "_GameplayCamera" })
        {
            SetReference(Target, Name, Source.FindProperty(Name).objectReferenceValue);
        }
        SetReference(Target, "_GameplayHUD", Hud);
        SetReference(Target, "_SearchPreviewTile",
            AssetDatabase.LoadAssetAtPath<TileBase>("Assets/GameData/Maps/TileAssets/Search_01.asset"));
        SetReference(Target, "_SearchAnimatedTile",
            AssetDatabase.LoadAssetAtPath<TileBase>("Assets/GameData/Maps/AnimatedTiles/Search_Animated.asset"));
        SetReference(Target, "_AirContactTile",
            AssetDatabase.LoadAssetAtPath<TileBase>("Assets/GameData/Maps/AnimatedTiles/Contact_Air.asset"));
        SetReference(Target, "_SurfaceContactTile",
            AssetDatabase.LoadAssetAtPath<TileBase>("Assets/GameData/Maps/AnimatedTiles/Contact_Surface.asset"));
        SetReference(Target, "_SubsurfaceContactTile",
            AssetDatabase.LoadAssetAtPath<TileBase>("Assets/GameData/Maps/AnimatedTiles/Contact_SubSurface.asset"));
        GameObject Label = AssetDatabase.LoadAssetAtPath<GameObject>(LabelPath);
        SetReference(Target, "_StrengthLabelPrefab", Label.GetComponent<TMP_Text>());
        Target.ApplyModifiedPropertiesWithoutUndo();
    }


    private static void CopyEquivalent(SerializedObject Serialized,
        Transform SourceRoot, Transform DestinationRoot,
        string SourceProperty, string DestinationProperty)
    {
        Component Source = Serialized.FindProperty(SourceProperty).objectReferenceValue as Component;
        string Path = AnimationUtility.CalculateTransformPath(Source.transform, SourceRoot);
        Transform Destination = string.IsNullOrEmpty(Path)
            ? DestinationRoot : DestinationRoot.Find(Path);
        SetReference(Serialized, DestinationProperty,
            Destination.GetComponent(Source.GetType()));
    }


    private static void SetReference(SerializedObject Object,
        string Name, UnityEngine.Object Value)
    {
        SerializedProperty Property = Object.FindProperty(Name);
        if (Property == null)
            throw new InvalidOperationException($"Missing serialized property {Name}.");
        Property.objectReferenceValue = Value;
    }


    private static T FindInScene<T>(Scene Scene) where T : Component
    {
        foreach (GameObject Root in Scene.GetRootGameObjects())
        {
            T Found = Root.GetComponentInChildren<T>(true);
            if (Found != null)
                return Found;
        }
        return null;
    }


    private static Transform FindDirectChild(Transform Parent, string Name)
    {
        for (int i = 0; i < Parent.childCount; i++)
            if (Parent.GetChild(i).name == Name)
                return Parent.GetChild(i);
        return null;
    }


    private static Transform FindDescendant(Transform Root, string Name)
    {
        if (Root == null || Root.name == Name)
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
