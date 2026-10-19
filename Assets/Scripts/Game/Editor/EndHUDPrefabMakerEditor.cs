using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


// Creates End as an independently editable phase HUD and installs its
// prefab instances into the persistent HUD_Game shell.
public static class EndHUDPrefabMakerEditor
{
    private const string SourceFolder = "Assets/Prefabs/HUD/PreBattle";
    private const string EndFolder = "Assets/Prefabs/HUD/End";
    private const string HudGamePath = "Assets/Prefabs/HUD/HUD_Game.prefab";
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string SessionSetupKey = "Admirals.EndHudSetupComplete";


    [InitializeOnLoadMethod]
    private static void ScheduleSetup()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += CreateMissingAutomatically;
    }


    private static void OnPlayModeStateChanged(PlayModeStateChange State)
    {
        if (State == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += CreateMissingAutomatically;
    }


    private static void CreateMissingAutomatically()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        try
        {
            if (SessionState.GetBool(SessionSetupKey, false))
                return;
            CreateAndWire();
            SessionState.SetBool(SessionSetupKey, true);
        }
        catch (Exception Exception)
        {
            Debug.LogException(Exception);
        }
    }


    [MenuItem("Admirals/HUD/Create and Wire End HUD Prefabs")]
    public static void CreateAndWire()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException(
                "Exit Play Mode before wiring End HUD prefabs.");

        EnsureFolder();
        EnsurePrefab("Top");
        EnsurePrefab("Bottom");
        InstallHudPrefabInstances();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        WireGameSceneInteraction();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SessionState.SetBool(SessionSetupKey, true);
        Debug.Log("End HUD prefabs and Game scene interaction are wired.");
    }


    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(EndFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs/HUD", "End");
    }


    private static void EnsurePrefab(string Suffix)
    {
        string Destination = $"{EndFolder}/HUD_End_{Suffix}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Destination) == null &&
            !AssetDatabase.CopyAsset(
                $"{SourceFolder}/HUD_PreBattle_{Suffix}.prefab",
                Destination))
            throw new InvalidOperationException(
                $"Could not create {Destination}.");

        GameObject Root = PrefabUtility.LoadPrefabContents(Destination);
        try
        {
            Root.name = $"HUD_End_{Suffix}";
            PrefabUtility.SaveAsPrefabAsset(Root, Destination);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(Root);
        }
    }


    private static void InstallHudPrefabInstances()
    {
        GameObject Root = PrefabUtility.LoadPrefabContents(HudGamePath);
        try
        {
            Transform TopParent = FindDescendant(Root.transform, "TopBar");
            Transform BottomParent = FindDescendant(Root.transform, "BottomBar");
            Transform Top = Install(Root, TopParent, "Top");
            Transform Bottom = Install(Root, BottomParent, "Bottom");
            if (Top == null || Bottom == null)
                throw new InvalidOperationException(
                    "HUD_Game is missing its TopBar or BottomBar roots.");

            PrefabUtility.SaveAsPrefabAsset(Root, HudGamePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(Root);
        }
    }


    private static Transform Install(GameObject Root, Transform Parent,
        string Suffix)
    {
        if (Parent == null)
            return null;
        string Name = $"HUD_End_{Suffix}";
        Transform Existing = FindDirectChild(Parent, Name);
        if (Existing != null)
            return Existing;
        GameObject Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            $"{EndFolder}/{Name}.prefab");
        GameObject Instance = (GameObject)PrefabUtility.InstantiatePrefab(
            Prefab, Parent);
        Instance.name = Name;
        Instance.SetActive(false);
        return Instance.transform;
    }


    private static void WireGameSceneInteraction()
    {
        Scene Scene = SceneManager.GetSceneByPath(GameScenePath);
        bool Opened = !Scene.isLoaded;
        if (Opened)
            Scene = EditorSceneManager.OpenScene(
                GameScenePath, OpenSceneMode.Additive);
        try
        {
            HumanGameplayInput Input = FindInScene<HumanGameplayInput>(Scene);
            MovementMapInteraction Movement =
                FindInScene<MovementMapInteraction>(Scene);
            GameplayHUD Hud = FindInScene<GameplayHUD>(Scene);
            if (Input == null || Movement == null || Hud == null)
                throw new InvalidOperationException(
                    "Game scene requires HumanGameplayInput, MovementMapInteraction and GameplayHUD.");

            WireHud(Hud);

            EndMapInteraction End = Input.GetComponent<EndMapInteraction>();
            if (End == null)
                End = Input.gameObject.AddComponent<EndMapInteraction>();
            SerializedObject Source = new SerializedObject(Movement);
            SerializedObject Target = new SerializedObject(End);
            foreach (string Name in new[] {
                "_GameMaster", "_HumanGameplayInput",
                "_UnitPresentationCatalog", "_GameplayCamera" })
                SetReference(Target, Name,
                    Source.FindProperty(Name).objectReferenceValue);
            SetReference(Target, "_GameplayHUD", Hud);
            Target.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(Scene);
            EditorSceneManager.SaveScene(Scene);
        }
        finally
        {
            if (Opened && Scene.isLoaded)
                EditorSceneManager.CloseScene(Scene, true);
        }
    }


    private static void WireHud(GameplayHUD Hud)
    {
        Transform SourceTop = FindDescendant(
            Hud.transform, "HUD_PreBattle_Top");
        Transform SourceBottom = FindDescendant(
            Hud.transform, "HUD_PreBattle_Bottom");
        Transform Top = FindDescendant(Hud.transform, "HUD_End_Top");
        Transform Bottom = FindDescendant(Hud.transform, "HUD_End_Bottom");
        if (SourceTop == null || SourceBottom == null ||
            Top == null || Bottom == null)
            throw new InvalidOperationException(
                "The Game-scene HUD instance is missing its Pre-Battle or End phase roots.");

        SerializedObject Serialized = new SerializedObject(Hud);
        SetReference(Serialized, "_EndTopRoot", Top.gameObject);
        SetReference(Serialized, "_EndBottomRoot", Bottom.gameObject);
        CopyEquivalent(Serialized, SourceTop, Top,
            "_PreBattlePlayerNameText", "_EndPlayerNameText");
        CopyEquivalent(Serialized, SourceTop, Top,
            "_PreBattlePhaseText", "_EndPhaseText");
        CopyEquivalent(Serialized, SourceTop, Top,
            "_PreBattleActionCountText", "_EndActionCountText");
        CopyEquivalent(Serialized, SourceTop, Top,
            "_PreBattleTimerText", "_EndTimerText");
        CopyEquivalent(Serialized, SourceTop, Top,
            "_EndPreBattleButton", "_FinishEndButton");
        CopyEquivalent(Serialized, SourceBottom, Bottom,
            "_PreBattleUnitDetails", "_EndUnitDetails");
        Serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(Hud);
    }


    private static void CopyEquivalent(SerializedObject Serialized,
        Transform SourceRoot, Transform DestinationRoot,
        string SourceProperty, string DestinationProperty)
    {
        Component Source = Serialized.FindProperty(SourceProperty)
            .objectReferenceValue as Component;
        if (Source == null)
            throw new InvalidOperationException(
                $"Missing source HUD reference {SourceProperty}.");
        string Path = AnimationUtility.CalculateTransformPath(
            Source.transform, SourceRoot);
        Transform Destination = string.IsNullOrEmpty(Path)
            ? DestinationRoot
            : DestinationRoot.Find(Path);
        if (Destination == null)
            throw new InvalidOperationException(
                $"End HUD is missing copied path '{Path}'.");
        SetReference(Serialized, DestinationProperty,
            Destination.GetComponent(Source.GetType()));
    }


    private static void SetReference(SerializedObject Object, string Name,
        UnityEngine.Object Value)
    {
        SerializedProperty Property = Object.FindProperty(Name);
        if (Property == null)
            throw new InvalidOperationException(
                $"Missing serialized property {Name}.");
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
