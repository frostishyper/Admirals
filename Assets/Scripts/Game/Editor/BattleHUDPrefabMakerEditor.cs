using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;


// Creates persistent, editable Battle HUD prefabs from the established Search
// composition. This runs only in Edit Mode; Battle UI is never generated at runtime.
public static class BattleHUDPrefabMakerEditor
{
    private const string SourceFolder = "Assets/Prefabs/HUD/Search";
    private const string BattleFolder = "Assets/Prefabs/HUD/Battle";
    private const string HudGamePath = "Assets/Prefabs/HUD/HUD_Game.prefab";
    private const string GameScenePath = "Assets/Scenes/Game.unity";

    [InitializeOnLoadMethod]
    private static void ScheduleSetup()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += CreateAndWireIfMissing;
    }


    private static void OnPlayModeStateChanged(PlayModeStateChange State)
    {
        if (State == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += CreateAndWireIfMissing;
    }


    private static void CreateAndWireIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(
                BattleFolder + "/HUD_Battle_Top.prefab") == null)
            CreateAndWire();
    }


    [MenuItem("Admirals/HUD/Create and Wire Battle HUD Prefabs")]
    public static void CreateAndWire()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before wiring Battle HUD prefabs.");

        EnsureFolder();
        EnsurePrefab("Top");
        EnsurePrefab("Overlay");
        EnsurePrefab("Bottom");
        InstallPrefabInstances();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Scene Scene = SceneManager.GetSceneByPath(GameScenePath);
        bool Opened = !Scene.isLoaded;
        if (Opened)
            Scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
        try
        {
            GameplayHUD Hud = FindWiredGameplayHudInScene(Scene);
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
        BattleEngagementPrefabMakerEditor.CreateAndWire();
        Debug.Log("Battle HUD prefab creation and Game scene wiring completed.");
    }


    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(BattleFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs/HUD", "Battle");
    }


    private static void EnsurePrefab(string Suffix)
    {
        string Destination = $"{BattleFolder}/HUD_Battle_{Suffix}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Destination) == null &&
            !AssetDatabase.CopyAsset(
                $"{SourceFolder}/HUD_Search_{Suffix}.prefab", Destination))
            throw new InvalidOperationException($"Could not create {Destination}.");

        GameObject Root = PrefabUtility.LoadPrefabContents(Destination);
        try
        {
            Root.name = $"HUD_Battle_{Suffix}";
            if (Suffix == "Overlay")
                ConfigureOverlay(Root);
            PrefabUtility.SaveAsPrefabAsset(Root, Destination);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(Root);
        }
    }


    private static void ConfigureOverlay(GameObject Root)
    {
        RectTransform RootRect = Root.GetComponent<RectTransform>();
        RootRect.anchorMin = new Vector2(1f, 0f);
        RootRect.anchorMax = new Vector2(1f, 0f);
        RootRect.pivot = new Vector2(1f, 0f);
        RootRect.anchoredPosition = new Vector2(-20f, 20f);
        RootRect.sizeDelta = new Vector2(390f, 250f);

        Transform MoveColumn = FindDescendant(Root.transform, "MoveColumn");
        Transform Cancel = FindDescendant(Root.transform, "Button_BattleDeselect") ??
            FindDescendant(Root.transform, "Button_CancelMove");
        if (MoveColumn != null)
            MoveColumn.gameObject.SetActive(false);
        if (Cancel != null)
        {
            Cancel.name = "Button_BattleDeselect";
            Cancel.gameObject.SetActive(true);
            SetButtonText(Cancel, "Button_BattleDeselect", "DESELECT");
        }

        Transform Domain = FindDescendant(Root.transform, "Text_BattleDomain") ??
            FindDescendant(Root.transform, "Text_Rotate");
        if (Domain != null)
        {
            Domain.name = "Text_BattleDomain";
            TMP_Text Text = Domain.GetComponent<TMP_Text>();
            Text.text = "SHOW FIRE CONTROL RANGE";
            Text.textWrappingMode = TextWrappingModes.NoWrap;
            Text.overflowMode = TextOverflowModes.Overflow;
        }

        Transform DomainRow = FindDescendant(Root.transform, "RotateRow");
        Transform Air = FindDescendant(Root.transform, "Button_BattleAir") ??
            FindDescendant(Root.transform, "Button_RotateLeft");
        Transform Surface = FindDescendant(Root.transform, "Button_BattleSurface") ??
            FindDescendant(Root.transform, "Button_RotateRight");
        if (Air != null)
        {
            Air.name = "Button_BattleAir";
            Air.gameObject.SetActive(true);
            SetButtonText(Air, "Button_BattleAir", "AIR");
            ConfigureDomainButton(Air);
        }
        if (Surface != null)
        {
            Surface.name = "Button_BattleSurface";
            Surface.gameObject.SetActive(true);
            SetButtonText(Surface, "Button_BattleSurface", "SURFACE");
            ConfigureDomainButton(Surface);
        }
        if (DomainRow != null && Surface != null &&
            FindDescendant(DomainRow, "Button_BattleSubsurface") == null)
        {
            GameObject Subsurface = UnityEngine.Object.Instantiate(
                Surface.gameObject, DomainRow);
            Subsurface.name = "Button_BattleSubsurface";
            Subsurface.SetActive(true);
            SetButtonText(Subsurface.transform, "Button_BattleSubsurface", "SUBSURFACE");
            ConfigureDomainButton(Subsurface.transform);
        }

        Transform RotateColumn = FindDescendant(Root.transform, "RotateColumn");
        if (RotateColumn != null && FindDescendant(RotateColumn, "Text_BattleTarget") == null)
        {
            GameObject Target = new GameObject("Text_BattleTarget", typeof(RectTransform));
            Target.layer = RotateColumn.gameObject.layer;
            Target.transform.SetParent(RotateColumn, false);
            TMP_Text Text = Target.AddComponent<TextMeshProUGUI>();
            Text.font = TMP_Settings.defaultFontAsset;
            Text.text = "Select a valid target";
            Text.fontSize = 18f;
            Text.alignment = TextAlignmentOptions.Center;
            Text.textWrappingMode = TextWrappingModes.Normal;
            Text.raycastTarget = false;
            LayoutElement Layout = Target.AddComponent<LayoutElement>();
            Layout.minHeight = 26f;
            Layout.preferredHeight = 36f;
        }
        Transform TargetText = FindDescendant(Root.transform, "Text_BattleTarget");
        LayoutElement TargetLayout = TargetText != null
            ? TargetText.GetComponent<LayoutElement>() : null;
        if (TargetLayout != null)
        {
            TargetLayout.minHeight = 60f;
            TargetLayout.preferredHeight = 72f;
        }

        SetButtonText(Root.transform, "Button_ConfirmMove", "ENGAGE");
    }


    private static void SetButtonText(Transform Root, string Name, string Text)
    {
        Transform Button = FindDescendant(Root, Name);
        TMP_Text Label = Button != null ? Button.GetComponentInChildren<TMP_Text>(true) : null;
        if (Label != null)
        {
            Label.text = Text;
            Label.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }


    private static void ConfigureDomainButton(Transform Button)
    {
        LayoutElement Layout = Button.GetComponent<LayoutElement>();
        if (Layout == null)
            Layout = Button.gameObject.AddComponent<LayoutElement>();
        Layout.minWidth = 90f;
        Layout.preferredWidth = -1f;
        Layout.flexibleWidth = 1f;
        TMP_Text Label = Button.GetComponentInChildren<TMP_Text>(true);
        if (Label != null)
        {
            Label.fontSize = 15f;
            Label.enableAutoSizing = true;
            Label.fontSizeMin = 11f;
            Label.fontSizeMax = 15f;
        }
    }


    private static void InstallPrefabInstances()
    {
        GameObject Root = PrefabUtility.LoadPrefabContents(HudGamePath);
        try
        {
            // GameplayHUD is intentionally a scene-owned component because its
            // references point at the live HUD_Game prefab instance. Keeping a
            // second copy on the prefab asset produces an unwired component and
            // can also give the scene two competing GameplayHUD components.
            foreach (GameplayHUD PrefabHud in Root.GetComponentsInChildren<GameplayHUD>(true))
                UnityEngine.Object.DestroyImmediate(PrefabHud);

            Install(Root, "TopBar", "Top");
            Install(Root, "PlayAreaUI", "Overlay");
            Install(Root, "BottomBar", "Bottom");
            PrefabUtility.SaveAsPrefabAsset(Root, HudGamePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(Root);
        }
    }


    private static void Install(GameObject Root, string ParentName, string Suffix)
    {
        Transform Parent = FindDescendant(Root.transform, ParentName);
        string Name = $"HUD_Battle_{Suffix}";
        if (Parent == null || FindDirectChild(Parent, Name) != null)
            return;
        GameObject Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            $"{BattleFolder}/{Name}.prefab");
        GameObject Instance = (GameObject)PrefabUtility.InstantiatePrefab(Prefab, Parent);
        Instance.name = Name;
        Instance.SetActive(false);
    }


    private static void WireHud(GameplayHUD Hud)
    {
        Transform SourceTop = FindDescendant(Hud.transform, "HUD_Search_Top");
        Transform SourceOverlay = FindDescendant(Hud.transform, "HUD_Search_Overlay");
        Transform SourceBottom = FindDescendant(Hud.transform, "HUD_Search_Bottom");
        Transform Top = FindDescendant(Hud.transform, "HUD_Battle_Top");
        Transform Overlay = FindDescendant(Hud.transform, "HUD_Battle_Overlay");
        Transform Bottom = FindDescendant(Hud.transform, "HUD_Battle_Bottom");
        if (SourceTop == null || SourceOverlay == null || SourceBottom == null ||
            Top == null || Overlay == null || Bottom == null)
            throw new InvalidOperationException("Battle HUD instances are missing from HUD_Game.");

        SerializedObject Serialized = new SerializedObject(Hud);
        SetReference(Serialized, "_BattleTopRoot", Top.gameObject);
        SetReference(Serialized, "_BattleOverlayRoot", Overlay.gameObject);
        SetReference(Serialized, "_BattleBottomRoot", Bottom.gameObject);
        CopyEquivalent(Serialized, SourceTop, Top, "_SearchPlayerNameText", "_BattlePlayerNameText");
        CopyEquivalent(Serialized, SourceTop, Top, "_SearchPhaseText", "_BattlePhaseText");
        CopyEquivalent(Serialized, SourceTop, Top, "_SearchCountText", "_BattleCountText");
        CopyEquivalent(Serialized, SourceTop, Top, "_SearchTimerText", "_BattleTimerText");
        CopyEquivalent(Serialized, SourceTop, Top, "_EndSearchButton", "_EndBattleButton");
        SetReference(Serialized, "_BattlePreviousDomainButton", null);
        SetReference(Serialized, "_BattleNextDomainButton", null);
        SetReference(Serialized, "_BattleAirDomainButton",
            FindDescendant(Overlay, "Button_BattleAir")?.GetComponent<Button>());
        SetReference(Serialized, "_BattleSurfaceDomainButton",
            FindDescendant(Overlay, "Button_BattleSurface")?.GetComponent<Button>());
        SetReference(Serialized, "_BattleSubsurfaceDomainButton",
            FindDescendant(Overlay, "Button_BattleSubsurface")?.GetComponent<Button>());
        SetReference(Serialized, "_BattleDeselectButton",
            FindDescendant(Overlay, "Button_BattleDeselect")?.GetComponent<Button>());
        CopyEquivalent(Serialized, SourceOverlay, Overlay, "_CommitSearchButton", "_EngageButton");
        CopyEquivalent(Serialized, SourceBottom, Bottom, "_SearchUnitDetails", "_BattleUnitDetails");
        SetReference(Serialized, "_BattleDomainText", null);
        SetReference(Serialized, "_BattleTargetText",
            FindDescendant(Overlay, "Text_BattleTarget")?.GetComponent<TMP_Text>());
        Serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(Hud);
    }


    private static void WireInteraction(GameObject Host,
        MovementMapInteraction Movement, GameplayHUD Hud)
    {
        BattleMapInteraction Battle = Host.GetComponent<BattleMapInteraction>();
        if (Battle == null)
            Battle = Host.AddComponent<BattleMapInteraction>();
        SerializedObject Source = new SerializedObject(Movement);
        SerializedObject Target = new SerializedObject(Battle);
        foreach (string Name in new[] {
            "_GameMaster", "_HumanGameplayInput", "_UnitPresentationCatalog", "_GameplayCamera" })
            SetReference(Target, Name, Source.FindProperty(Name).objectReferenceValue);
        SetReference(Target, "_GameplayHUD", Hud);
        SetReference(Target, "_FireControlTile",
            AssetDatabase.LoadAssetAtPath<TileBase>(
                "Assets/GameData/Maps/AnimatedTiles/Indicator_White_Animated.asset"));
        SetReference(Target, "_CrosshairTile",
            AssetDatabase.LoadAssetAtPath<TileBase>("Assets/GameData/Maps/AnimatedTiles/Crosshair_Animated.asset"));
        SetReference(Target, "_AirContactTile",
            AssetDatabase.LoadAssetAtPath<TileBase>("Assets/GameData/Maps/AnimatedTiles/Contact_Air.asset"));
        SetReference(Target, "_SurfaceContactTile",
            AssetDatabase.LoadAssetAtPath<TileBase>("Assets/GameData/Maps/AnimatedTiles/Contact_Surface.asset"));
        SetReference(Target, "_SubsurfaceContactTile",
            AssetDatabase.LoadAssetAtPath<TileBase>("Assets/GameData/Maps/AnimatedTiles/Contact_SubSurface.asset"));
        Target.ApplyModifiedPropertiesWithoutUndo();
    }


    private static void CopyEquivalent(SerializedObject Serialized,
        Transform SourceRoot, Transform DestinationRoot,
        string SourceProperty, string DestinationProperty)
    {
        SerializedProperty SourceSerialized = Serialized.FindProperty(SourceProperty);
        if (SourceSerialized == null)
            throw new InvalidOperationException(
                $"GameplayHUD has no serialized field {SourceProperty}.");

        Component Source = SourceSerialized.objectReferenceValue as Component;
        if (Source == null)
            throw new InvalidOperationException(
                $"The Game scene GameplayHUD field {SourceProperty} is not wired.");

        string Path = AnimationUtility.CalculateTransformPath(Source.transform, SourceRoot);
        Transform Destination = string.IsNullOrEmpty(Path)
            ? DestinationRoot : DestinationRoot.Find(Path);
        if (Destination == null)
            throw new InvalidOperationException(
                $"HUD Battle copy is missing '{Path}' for {DestinationProperty}.");

        Component DestinationComponent = Destination.GetComponent(Source.GetType());
        if (DestinationComponent == null)
            throw new InvalidOperationException(
                $"HUD Battle object '{Destination.name}' has no {Source.GetType().Name} " +
                $"component for {DestinationProperty}.");

        SetReference(Serialized, DestinationProperty,
            DestinationComponent);
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


    private static GameplayHUD FindWiredGameplayHudInScene(Scene Scene)
    {
        foreach (GameObject Root in Scene.GetRootGameObjects())
        {
            foreach (GameplayHUD Hud in Root.GetComponentsInChildren<GameplayHUD>(true))
            {
                SerializedProperty SearchTop =
                    new SerializedObject(Hud).FindProperty("_SearchTopRoot");
                if (SearchTop?.objectReferenceValue != null)
                    return Hud;
            }
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
