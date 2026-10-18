using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PreBattleHUDPrefabMakerEditor
{
    private const string MovementFolder = "Assets/Prefabs/HUD/Movement";
    private const string PreBattleFolder = "Assets/Prefabs/HUD/PreBattle";
    private const string HudGamePath = "Assets/Prefabs/HUD/HUD_Game.prefab";
    private const string GameScenePath = "Assets/Scenes/Game.unity";

    [InitializeOnLoadMethod]
    private static void ScheduleMissingPrefabSetup()
    {
        EditorApplication.delayCall += CreateMissingPrefabsAutomatically;
    }

    private static void CreateMissingPrefabsAutomatically()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool AssetsMissing =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PreBattleFolder}/HUD_PreBattle_Top.prefab") == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PreBattleFolder}/HUD_PreBattle_Overlay.prefab") == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PreBattleFolder}/HUD_PreBattle_Bottom.prefab") == null;
        if (!AssetsMissing)
            return;

        try
        {
            CreateAndWire();
            Debug.Log(
                "Created, installed, wired, and saved the independent Pre-Battle HUD prefabs.");
        }
        catch (Exception Exception)
        {
            Debug.LogException(Exception);
        }
    }

    [MenuItem("Admirals/HUD/Create and Wire Pre-Battle HUD Prefabs")]
    public static void CreateAndWire()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Pre-Battle HUD",
                "Exit Play Mode before creating or wiring HUD prefabs.", "OK");
            return;
        }

        EnsureFolder();
        EnsureIndependentPrefab("Top");
        EnsureIndependentPrefab("Overlay");
        EnsureIndependentPrefab("Bottom");
        InstallPrefabInstances();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Scene GameScene = SceneManager.GetSceneByPath(GameScenePath);
        bool OpenedForSetup = !GameScene.isLoaded;
        if (OpenedForSetup)
            GameScene = EditorSceneManager.OpenScene(
                GameScenePath,
                OpenSceneMode.Additive
            );

        try
        {
            GameplayHUD Hud = FindGameplayHud(GameScene);
            if (Hud == null)
                throw new InvalidOperationException(
                    "No GameplayHUD component was found in the Game scene.");

            WireSceneHud(Hud);
            EditorSceneManager.MarkSceneDirty(GameScene);
            EditorSceneManager.SaveScene(GameScene);
            if (!OpenedForSetup)
                Selection.activeGameObject = Hud.gameObject;
        }
        finally
        {
            if (OpenedForSetup && GameScene.isLoaded)
                EditorSceneManager.CloseScene(GameScene, true);
        }

        EditorUtility.DisplayDialog("Pre-Battle HUD Ready",
            "Created/retained the independent Pre-Battle prefabs, installed them " +
            "in HUD_Game, wired GameplayHUD, and saved the Game scene.", "OK");
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(PreBattleFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs/HUD", "PreBattle");
    }

    private static void EnsureIndependentPrefab(string Suffix)
    {
        string Source = $"{MovementFolder}/HUD_Movement_{Suffix}.prefab";
        string Destination = $"{PreBattleFolder}/HUD_PreBattle_{Suffix}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Destination) != null)
            return;
        if (!AssetDatabase.CopyAsset(Source, Destination))
            throw new InvalidOperationException($"Could not copy {Source} to {Destination}.");

        GameObject Root = PrefabUtility.LoadPrefabContents(Destination);
        try
        {
            Root.name = $"HUD_PreBattle_{Suffix}";
            if (Suffix == "Overlay")
            {
                Transform MoveColumn = FindDescendant(Root.transform, "MoveColumn");
                if (MoveColumn != null)
                    MoveColumn.gameObject.SetActive(false);
            }
            PrefabUtility.SaveAsPrefabAsset(Root, Destination);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(Root);
        }
    }

    private static void InstallPrefabInstances()
    {
        GameObject HudRoot = PrefabUtility.LoadPrefabContents(HudGamePath);
        try
        {
            InstallInstance(HudRoot, "TopBar", "Top");
            InstallInstance(HudRoot, "PlayAreaUI", "Overlay");
            InstallInstance(HudRoot, "BottomBar", "Bottom");
            PrefabUtility.SaveAsPrefabAsset(HudRoot, HudGamePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(HudRoot);
        }
    }

    private static void InstallInstance(GameObject HudRoot, string ParentName, string Suffix)
    {
        Transform Parent = FindDescendant(HudRoot.transform, ParentName);
        if (Parent == null)
            throw new InvalidOperationException($"HUD_Game has no {ParentName} transform.");

        string InstanceName = $"HUD_PreBattle_{Suffix}";
        if (FindDirectChild(Parent, InstanceName) != null)
            return;

        GameObject Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            $"{PreBattleFolder}/{InstanceName}.prefab");
        GameObject Instance = (GameObject)PrefabUtility.InstantiatePrefab(Prefab, Parent);
        Instance.name = InstanceName;
        Instance.SetActive(false);
    }

    private static void WireSceneHud(GameplayHUD Hud)
    {
        Transform MovementTop = FindDescendant(Hud.transform, "HUD_Movement_Top");
        Transform MovementOverlay = FindDescendant(Hud.transform, "HUD_Movement_Overlay");
        Transform MovementBottom = FindDescendant(Hud.transform, "HUD_Movement_Bottom");
        Transform PreBattleTop = FindDescendant(Hud.transform, "HUD_PreBattle_Top");
        Transform PreBattleOverlay = FindDescendant(Hud.transform, "HUD_PreBattle_Overlay");
        Transform PreBattleBottom = FindDescendant(Hud.transform, "HUD_PreBattle_Bottom");

        if (MovementTop == null || MovementOverlay == null || MovementBottom == null ||
            PreBattleTop == null || PreBattleOverlay == null || PreBattleBottom == null)
            throw new InvalidOperationException(
                "The Movement or Pre-Battle HUD prefab instances are missing from HUD_Game.");

        SerializedObject SerializedHud = new SerializedObject(Hud);
        SetReference(SerializedHud, "_PreBattleTopRoot", PreBattleTop.gameObject);
        SetReference(SerializedHud, "_PreBattleOverlayRoot", PreBattleOverlay.gameObject);
        SetReference(SerializedHud, "_PreBattleBottomRoot", PreBattleBottom.gameObject);

        CopyEquivalent(SerializedHud, MovementTop, PreBattleTop,
            "_MovementPlayerNameText", "_PreBattlePlayerNameText");
        CopyEquivalent(SerializedHud, MovementTop, PreBattleTop,
            "_MovementPhaseText", "_PreBattlePhaseText");
        CopyEquivalent(SerializedHud, MovementTop, PreBattleTop,
            "_MovementCountText", "_PreBattleActionCountText");
        CopyEquivalent(SerializedHud, MovementTop, PreBattleTop,
            "_MovementTimerText", "_PreBattleTimerText");
        CopyEquivalent(SerializedHud, MovementTop, PreBattleTop,
            "_EndMovementButton", "_EndPreBattleButton");
        CopyEquivalent(SerializedHud, MovementOverlay, PreBattleOverlay,
            "_MovementRotateLeftButton", "_PreBattleRotateLeftButton");
        CopyEquivalent(SerializedHud, MovementOverlay, PreBattleOverlay,
            "_MovementRotateRightButton", "_PreBattleRotateRightButton");
        CopyEquivalent(SerializedHud, MovementOverlay, PreBattleOverlay,
            "_ConfirmMovementButton", "_ConfirmPreBattleActionButton");
        CopyEquivalent(SerializedHud, MovementOverlay, PreBattleOverlay,
            "_CancelMovementButton", "_CancelPreBattleActionButton");
        CopyEquivalent(SerializedHud, MovementBottom, PreBattleBottom,
            "_MovementUnitDetails", "_PreBattleUnitDetails");

        SerializedHud.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(Hud);
    }

    private static void CopyEquivalent(SerializedObject SerializedHud,
        Transform SourceRoot, Transform DestinationRoot,
        string SourcePropertyName, string DestinationPropertyName)
    {
        Component SourceComponent = SerializedHud.FindProperty(SourcePropertyName)
            .objectReferenceValue as Component;
        if (SourceComponent == null)
            throw new InvalidOperationException($"GameplayHUD.{SourcePropertyName} is not wired.");

        string RelativePath = AnimationUtility.CalculateTransformPath(
            SourceComponent.transform, SourceRoot);
        Transform DestinationTransform = string.IsNullOrEmpty(RelativePath)
            ? DestinationRoot
            : DestinationRoot.Find(RelativePath);
        if (DestinationTransform == null)
            throw new InvalidOperationException(
                $"Could not find {RelativePath} inside {DestinationRoot.name}.");

        SetReference(SerializedHud, DestinationPropertyName,
            DestinationTransform.GetComponent(SourceComponent.GetType()));
    }

    private static void SetReference(SerializedObject SerializedHud,
        string PropertyName, UnityEngine.Object Value)
    {
        SerializedProperty Property = SerializedHud.FindProperty(PropertyName);
        if (Property == null)
            throw new InvalidOperationException(
                $"GameplayHUD has no serialized field {PropertyName}.");
        Property.objectReferenceValue = Value;
    }

    private static GameplayHUD FindGameplayHud(Scene Scene)
    {
        foreach (GameObject Root in Scene.GetRootGameObjects())
        {
            GameplayHUD Hud = Root.GetComponentInChildren<GameplayHUD>(true);
            if (Hud != null)
                return Hud;
        }
        return null;
    }

    private static Transform FindDirectChild(Transform Parent, string Name)
    {
        for (int Index = 0; Index < Parent.childCount; Index++)
        {
            Transform Child = Parent.GetChild(Index);
            if (Child.name == Name)
                return Child;
        }
        return null;
    }

    private static Transform FindDescendant(Transform Root, string Name)
    {
        if (Root == null)
            return null;
        if (Root.name == Name)
            return Root;
        for (int Index = 0; Index < Root.childCount; Index++)
        {
            Transform Found = FindDescendant(Root.GetChild(Index), Name);
            if (Found != null)
                return Found;
        }
        return null;
    }
}
