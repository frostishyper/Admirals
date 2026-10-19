using System;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


// Builds the persistent Battle engagement screen and wires its controller.
// Runtime code only populates the authored templates with current choices/results.
public static class BattleEngagementPrefabMakerEditor
{
    private const string Folder = "Assets/Prefabs/HUD/Battle";
    private const string PrefabPath = Folder + "/HUD_Battle_Engagement.prefab";
    private const string HudGamePath = "Assets/Prefabs/HUD/HUD_Game.prefab";
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string UiClipPath = Folder + "/HUD_D20_Roll.anim";
    private const string UiControllerPath = Folder + "/HUD_D20.controller";

    [InitializeOnLoadMethod]
    private static void Schedule()
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

        GameObject Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (Prefab == null || NeedsLayoutMigration(Prefab.transform))
            CreateAndWire();
    }


    private static bool NeedsLayoutMigration(Transform Root)
    {
        return FindDescendant(Root, "ActionRow") != null ||
               FindDescendant(Root, "Handoff") != null ||
               FindDescendant(Root, "AttackerActionContent") == null ||
               FindDescendant(Root, "DefenderActionContent") == null ||
               FindDescendant(Root, "Button_AttackerLock") == null ||
               FindDescendant(Root, "Button_DefenderLock") == null ||
               HasFlexibleHeight(FindDescendant(Root, "Text_Instruction")) ||
               HasFlexibleHeight(FindDescendant(Root, "IdentityRow")) ||
               HasFlexibleHeight(FindDescendant(Root, "CombatSummary")) ||
               NeedsDynamicChoiceCard(FindDescendant(Root, "ChoiceTemplate")) ||
               NeedsCompactSummary(FindDescendant(Root, "CombatSummary"));
    }


    private static bool NeedsDynamicChoiceCard(Transform Target)
    {
        if (Target == null)
            return true;

        LayoutElement Card = Target.GetComponent<LayoutElement>();
        Transform Name = FindDescendant(Target, "Text_ChoiceName");
        LayoutElement NameLayout = Name != null
            ? Name.GetComponent<LayoutElement>() : null;
        return Card == null || Card.preferredHeight >= 0f ||
               NameLayout == null || NameLayout.preferredHeight >= 0f;
    }


    private static bool HasFlexibleHeight(Transform Target)
    {
        if (Target == null)
            return true;
        LayoutElement Element = Target.GetComponent<LayoutElement>();
        return Element == null || !Mathf.Approximately(Element.flexibleHeight, 0f);
    }


    private static bool NeedsCompactSummary(Transform Target)
    {
        if (Target == null)
            return true;
        VerticalLayoutGroup Layout = Target.GetComponent<VerticalLayoutGroup>();
        return Layout == null || Layout.padding.top != 8 ||
               Layout.padding.bottom != 8 || !Mathf.Approximately(Layout.spacing, 4f);
    }


    [MenuItem("Admirals/HUD/Create and Wire Battle Engagement Prefab")]
    public static void CreateAndWire()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before creating Battle engagement UI.");
        EnsureFolder();
        EnsureUiDiceAssets();
        EnsurePrefab();
        InstallInHudGame();

        Scene Scene = SceneManager.GetSceneByPath(GameScenePath);
        if (Scene.isLoaded)
        {
            HumanGameplayInput Input = FindInScene<HumanGameplayInput>(Scene);
            GameplayHUD Hud = FindWiredGameplayHudInScene(Scene);
            MovementMapInteraction Movement = FindInScene<MovementMapInteraction>(Scene);
            if (Input == null || Hud == null || Movement == null)
                throw new InvalidOperationException(
                    "Game scene requires HumanGameplayInput, GameplayHUD and MovementMapInteraction.");
            Transform View = FindDescendant(Hud.transform, "HUD_Battle_Engagement");
            if (View == null)
                throw new InvalidOperationException("Battle engagement prefab instance is missing.");
            WireController(Input.gameObject, View, Hud, Movement);
            EditorSceneManager.MarkSceneDirty(Scene);
            EditorSceneManager.SaveScene(Scene);
        }
        else
            Debug.LogWarning(
                "Battle engagement prefab rebuilt. Game scene was not loaded, so scene wiring was left untouched.");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Battle engagement prefab creation and Game scene wiring completed.");
    }


    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Prefabs/HUD", "Battle");
    }


    private static void EnsureUiDiceAssets()
    {
        AnimationClip Clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UiClipPath);
        if (Clip == null)
        {
            AnimationClip Source = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/GameData/VFX/D20_Roll.anim");
            Clip = new AnimationClip { name = "HUD_D20_Roll", frameRate = Source.frameRate };
            EditorCurveBinding[] Bindings = AnimationUtility.GetObjectReferenceCurveBindings(Source);
            if (Bindings.Length > 0)
            {
                ObjectReferenceKeyframe[] Keys =
                    AnimationUtility.GetObjectReferenceCurve(Source, Bindings[0]);
                EditorCurveBinding Binding = EditorCurveBinding.PPtrCurve(
                    string.Empty, typeof(Image), "m_Sprite");
                AnimationUtility.SetObjectReferenceCurve(Clip, Binding, Keys);
            }
            AnimationClipSettings Settings = AnimationUtility.GetAnimationClipSettings(Clip);
            Settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(Clip, Settings);
            AssetDatabase.CreateAsset(Clip, UiClipPath);
        }

        AnimatorController Controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(UiControllerPath);
        if (Controller == null)
        {
            Controller = AnimatorController.CreateAnimatorControllerAtPath(UiControllerPath);
            Controller.AddMotion(Clip);
        }
    }


    private static void EnsurePrefab()
    {
        GameObject Root = NewUI("HUD_Battle_Engagement", null);
        Stretch(Root.GetComponent<RectTransform>());
        Root.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -70f);
        LayoutElement RootElement = Root.AddComponent<LayoutElement>();
        RootElement.ignoreLayout = true;
        Image RootImage = Root.AddComponent<Image>();
        CopyPanelArt(RootImage);
        RootImage.raycastTarget = true;

        VerticalLayoutGroup RootLayout = Root.AddComponent<VerticalLayoutGroup>();
        RootLayout.padding = new RectOffset(24, 24, 20, 20);
        RootLayout.spacing = 14f;
        RootLayout.childControlWidth = true;
        RootLayout.childControlHeight = true;
        RootLayout.childForceExpandWidth = true;
        RootLayout.childForceExpandHeight = false;

        TMP_Text Heading = AddText(Root.transform, "Text_Instruction",
            "CHOOSE YOUR ACTION", 26f, TextAlignmentOptions.Center, 40f);

        GameObject Identity = AddRow(Root.transform, "IdentityRow", 54f, 20f);
        GameObject AttackerIdentity = AddColumn(Identity.transform, "AttackerIdentity", 1f);
        AddText(AttackerIdentity.transform, "Text_AttackerName", "Attacker", 24f,
            TextAlignmentOptions.Left, 32f);
        AddText(AttackerIdentity.transform, "Text_AttackerHealth", "HP", 18f,
            TextAlignmentOptions.Left, 24f);
        GameObject DefenderIdentity = AddColumn(Identity.transform, "DefenderIdentity", 1f);
        AddText(DefenderIdentity.transform, "Text_DefenderName", "Defender", 24f,
            TextAlignmentOptions.Right, 32f);
        AddText(DefenderIdentity.transform, "Text_DefenderHealth", "HP", 18f,
            TextAlignmentOptions.Right, 24f);

        GameObject Summary = AddPanelColumn(Root.transform, "CombatSummary", 1f);
        VerticalLayoutGroup SummaryLayout = Summary.GetComponent<VerticalLayoutGroup>();
        SummaryLayout.padding = new RectOffset(8, 8, 8, 8);
        SummaryLayout.spacing = 4f;
        LayoutElement SummaryElement = Summary.GetComponent<LayoutElement>();
        SummaryElement.minHeight = 118f;
        SummaryElement.preferredHeight = 118f;
        SummaryElement.flexibleHeight = 0f;
        AddText(Summary.transform, "Text_SelectedAttack", "ATTACK NOT LOCKED", 19f,
            TextAlignmentOptions.Center, 28f);
        GameObject DiceRow = AddRow(Summary.transform, "DiceRow", 68f, 20f);
        CreateDice(DiceRow.transform, "AttackerDie", "Text_AttackerRoll");
        CreateDice(DiceRow.transform, "DefenderDie", "Text_DefenderRoll");

        GameObject Body = AddRow(Root.transform, "Body", -1f, 14f);
        Body.GetComponent<LayoutElement>().flexibleHeight = 1f;
        GameObject Left = AddPanelColumn(Body.transform, "AttackerPanel", 1f);
        AddText(Left.transform, "Text_AttackerHeader", "ATTACKER ACTIONS", 19f,
            TextAlignmentOptions.Left, 30f);
        RectTransform AttackerActionContent = AddScroll(
            Left.transform, "AttackerActionContent");
        RectTransform AttackContent = AddVerticalContainer(
            AttackerActionContent, "AttackerChoices");
        GameObject AttackerFooter = AddRow(Left.transform,
            "AttackerActionFooter", 48f, 8f);
        AddButton(AttackerFooter.transform, "Button_Cancel", "CANCEL");
        AddButton(AttackerFooter.transform, "Button_AttackerLock", "LOCK IN CHOICE");

        GameObject Center = AddPanelColumn(Body.transform, "ResolutionPanel", 1.25f);
        AddText(Center.transform, "Text_ResolutionHeader", "RESOLUTION", 19f,
            TextAlignmentOptions.Left, 30f);
        RectTransform ResolutionContent = AddScroll(Center.transform, "ResolutionContent");
        ResolutionContent.GetComponent<VerticalLayoutGroup>().spacing = 14f;
        TMP_Text LineTemplate = AddText(ResolutionContent, "ResolutionLineTemplate",
            "RESULT", 18f, TextAlignmentOptions.Left, 30f);
        LineTemplate.margin = new Vector4(0f, 4f, 0f, 4f);
        LineTemplate.gameObject.SetActive(false);
        Button Continue = AddButton(Center.transform, "Button_Continue", "CONTINUE");
        Continue.gameObject.SetActive(false);

        GameObject Right = AddPanelColumn(Body.transform, "DefenderPanel", 1f);
        AddText(Right.transform, "Text_DefenderHeader", "DEFENDER ACTIONS", 19f,
            TextAlignmentOptions.Left, 30f);
        RectTransform DefenderActionContent = AddScroll(
            Right.transform, "DefenderActionContent");
        RectTransform DefenseContent = AddVerticalContainer(
            DefenderActionContent, "DefenderChoices");
        AddText(DefenderActionContent, "Text_SupportHeader", "ALLY SUPPORT - OPTIONAL", 17f,
            TextAlignmentOptions.Left, 28f);
        RectTransform SupportContent = AddVerticalContainer(
            DefenderActionContent, "SupportChoices");
        AddButton(Right.transform, "Button_DefenderLock", "LOCK IN CHOICE");

        Button ChoiceTemplate = AddChoiceTemplate(Root.transform);
        ChoiceTemplate.gameObject.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(Root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(Root);
    }


    private static void InstallInHudGame()
    {
        GameObject Root = PrefabUtility.LoadPrefabContents(HudGamePath);
        try
        {
            if (FindDescendant(Root.transform, "HUD_Battle_Engagement") == null)
            {
                GameObject Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                GameObject Instance = (GameObject)PrefabUtility.InstantiatePrefab(Prefab, Root.transform);
                Instance.name = "HUD_Battle_Engagement";
                Instance.transform.SetAsLastSibling();
                Instance.SetActive(false);
            }
            PrefabUtility.SaveAsPrefabAsset(Root, HudGamePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(Root);
        }
    }


    private static void WireController(GameObject Host, Transform Root,
        GameplayHUD Hud, MovementMapInteraction Movement)
    {
        BattleEngagementUI Controller = Host.GetComponent<BattleEngagementUI>();
        if (Controller == null)
            Controller = Host.AddComponent<BattleEngagementUI>();
        SerializedObject Serialized = new SerializedObject(Controller);
        Set(Serialized, "_Root", Root.gameObject);
        Set(Serialized, "_GameplayHUD", Hud);
        SerializedObject MovementSerialized = new SerializedObject(Movement);
        Set(Serialized, "_UnitPresentationCatalog",
            MovementSerialized.FindProperty("_UnitPresentationCatalog").objectReferenceValue);
        Set(Serialized, "_AttackerName", Component<TMP_Text>(Root, "Text_AttackerName"));
        Set(Serialized, "_AttackerHealth", Component<TMP_Text>(Root, "Text_AttackerHealth"));
        Set(Serialized, "_DefenderName", Component<TMP_Text>(Root, "Text_DefenderName"));
        Set(Serialized, "_DefenderHealth", Component<TMP_Text>(Root, "Text_DefenderHealth"));
        Set(Serialized, "_Instruction", Component<TMP_Text>(Root, "Text_Instruction"));
        Set(Serialized, "_SelectedAttack", Component<TMP_Text>(Root, "Text_SelectedAttack"));
        Set(Serialized, "_AttackerChoices", FindDescendant(Root, "AttackerChoices") as RectTransform);
        Set(Serialized, "_DefenderChoices", FindDescendant(Root, "DefenderChoices") as RectTransform);
        Set(Serialized, "_SupportChoices", FindDescendant(Root, "SupportChoices") as RectTransform);
        Set(Serialized, "_SupportHeader", FindDescendant(Root, "Text_SupportHeader")?.gameObject);
        Set(Serialized, "_ChoiceTemplate", Component<Button>(Root, "ChoiceTemplate"));
        Set(Serialized, "_AttackerLockButton",
            Component<Button>(Root, "Button_AttackerLock"));
        Set(Serialized, "_DefenderLockButton",
            Component<Button>(Root, "Button_DefenderLock"));
        Set(Serialized, "_CancelButton", Component<Button>(Root, "Button_Cancel"));
        Set(Serialized, "_ResolutionContent", FindDescendant(Root, "ResolutionContent") as RectTransform);
        Set(Serialized, "_ResolutionLineTemplate", Component<TMP_Text>(Root, "ResolutionLineTemplate"));
        Set(Serialized, "_AttackerDie", Component<Animator>(Root, "AttackerDie"));
        Set(Serialized, "_DefenderDie", Component<Animator>(Root, "DefenderDie"));
        Set(Serialized, "_AttackerRollText", Component<TMP_Text>(Root, "Text_AttackerRoll"));
        Set(Serialized, "_DefenderRollText", Component<TMP_Text>(Root, "Text_DefenderRoll"));
        Set(Serialized, "_ContinueButton", Component<Button>(Root, "Button_Continue"));
        MovementUnitDetailsUI Details = FindDescendant(Hud.transform,
            "HUD_Battle_Bottom")?.GetComponent<MovementUnitDetailsUI>();
        if (Details == null)
            Details = FindDescendant(Hud.transform,
                "HUD_Battle_Bottom")?.GetComponentInChildren<MovementUnitDetailsUI>(true);
        if (Details != null)
        {
            SerializedObject DetailSerialized = new SerializedObject(Details);
            CopyReference(DetailSerialized, Serialized, "_AirDomainBadge");
            CopyReference(DetailSerialized, Serialized, "_SurfaceDomainBadge");
            CopyReference(DetailSerialized, Serialized, "_SubsurfaceDomainBadge");
        }
        Serialized.ApplyModifiedPropertiesWithoutUndo();
    }


    private static GameObject NewUI(string Name, Transform Parent)
    {
        GameObject Object = new GameObject(Name, typeof(RectTransform));
        Object.layer = LayerMask.NameToLayer("UI");
        if (Parent != null)
            Object.transform.SetParent(Parent, false);
        return Object;
    }


    private static GameObject AddRow(Transform Parent, string Name,
        float Height, float Spacing)
    {
        GameObject Row = NewUI(Name, Parent);
        HorizontalLayoutGroup Layout = Row.AddComponent<HorizontalLayoutGroup>();
        Layout.spacing = Spacing;
        Layout.childControlWidth = true;
        Layout.childControlHeight = true;
        Layout.childForceExpandWidth = true;
        Layout.childForceExpandHeight = true;
        Layout.childAlignment = TextAnchor.MiddleCenter;
        LayoutElement Element = Row.AddComponent<LayoutElement>();
        if (Height > 0f)
        {
            Element.minHeight = Height;
            Element.preferredHeight = Height;
            Element.flexibleHeight = 0f;
        }
        return Row;
    }


    private static GameObject AddColumn(Transform Parent, string Name, float FlexibleWidth)
    {
        GameObject Column = NewUI(Name, Parent);
        VerticalLayoutGroup Layout = Column.AddComponent<VerticalLayoutGroup>();
        Layout.spacing = 6f;
        Layout.childControlWidth = true;
        Layout.childControlHeight = true;
        Layout.childForceExpandWidth = true;
        Layout.childForceExpandHeight = false;
        LayoutElement Element = Column.AddComponent<LayoutElement>();
        Element.flexibleWidth = FlexibleWidth;
        return Column;
    }


    private static RectTransform AddVerticalContainer(Transform Parent, string Name)
    {
        GameObject Container = NewUI(Name, Parent);
        VerticalLayoutGroup Layout = Container.AddComponent<VerticalLayoutGroup>();
        Layout.spacing = 8f;
        Layout.childControlWidth = true;
        Layout.childControlHeight = true;
        Layout.childForceExpandWidth = true;
        Layout.childForceExpandHeight = false;
        ContentSizeFitter Fitter = Container.AddComponent<ContentSizeFitter>();
        Fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return Container.GetComponent<RectTransform>();
    }


    private static GameObject AddPanelColumn(Transform Parent, string Name,
        float FlexibleWidth)
    {
        GameObject Panel = AddColumn(Parent, Name, FlexibleWidth);
        Image Image = Panel.AddComponent<Image>();
        CopyPanelArt(Image);
        VerticalLayoutGroup Layout = Panel.GetComponent<VerticalLayoutGroup>();
        Layout.padding = new RectOffset(12, 12, 12, 12);
        Layout.spacing = 10f;
        return Panel;
    }


    private static TMP_Text AddText(Transform Parent, string Name, string Value,
        float Size, TextAlignmentOptions Alignment, float Height)
    {
        GameObject Object = NewUI(Name, Parent);
        TMP_Text Text = Object.AddComponent<TextMeshProUGUI>();
        Text.font = TMP_Settings.defaultFontAsset;
        Text.text = Value;
        Text.fontSize = Size;
        Text.alignment = Alignment;
        Text.textWrappingMode = TextWrappingModes.Normal;
        Text.raycastTarget = false;
        LayoutElement Element = Object.AddComponent<LayoutElement>();
        Element.minHeight = Height;
        Element.preferredHeight = Height;
        Element.flexibleHeight = 0f;
        return Text;
    }


    private static Button AddChoiceTemplate(Transform Parent)
    {
        GameObject SourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/HUD/Search/HUD_Search_Overlay.prefab");
        Transform Source = FindDescendant(SourcePrefab.transform, "Button_ConfirmMove");
        GameObject Object = UnityEngine.Object.Instantiate(Source.gameObject, Parent);
        Object.name = "ChoiceTemplate";
        Object.SetActive(true);
        for (int i = Object.transform.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(Object.transform.GetChild(i).gameObject);

        // The source button uses a HorizontalLayoutGroup. Unity permits only
        // one LayoutGroup per GameObject, so discard the source composition
        // before authoring the vertical Battle action card.
        LayoutGroup[] SourceLayouts = Object.GetComponents<LayoutGroup>();
        for (int i = 0; i < SourceLayouts.Length; i++)
            UnityEngine.Object.DestroyImmediate(SourceLayouts[i]);

        VerticalLayoutGroup Group = Object.AddComponent<VerticalLayoutGroup>();
        Group.padding = new RectOffset(12, 12, 8, 8);
        Group.spacing = 4f;
        Group.childControlWidth = true;
        Group.childControlHeight = true;
        Group.childForceExpandWidth = true;
        Group.childForceExpandHeight = false;

        LayoutElement Element = Object.GetComponent<LayoutElement>();
        if (Element == null)
            Element = Object.AddComponent<LayoutElement>();
        Element.minHeight = 100f;
        // The vertical layout group supplies the preferred height. This keeps
        // short cards compact while allowing wrapped names (notably allied
        // support's "unit - action" label) to grow without crushing metadata.
        Element.preferredHeight = -1f;
        Element.flexibleWidth = 1f;
        Element.flexibleHeight = 0f;

        TMP_Text Name = AddText(Object.transform, "Text_ChoiceName", "ACTION", 19f,
            TextAlignmentOptions.Left, 30f);
        Name.GetComponent<LayoutElement>().preferredHeight = -1f;
        GameObject Meta = AddRow(Object.transform, "ChoiceMetaRow", 24f, 8f);
        TMP_Text Charges = AddText(Meta.transform, "Text_ChoiceCharges",
            "CHARGES", 15f, TextAlignmentOptions.Left, 24f);
        LayoutElement ChargesLayout = Charges.GetComponent<LayoutElement>();
        ChargesLayout.flexibleWidth = 1f;

        GameObject Badges = NewUI("BadgeStrip", Meta.transform);
        HorizontalLayoutGroup BadgeLayout = Badges.AddComponent<HorizontalLayoutGroup>();
        BadgeLayout.spacing = 6f;
        BadgeLayout.childControlWidth = true;
        BadgeLayout.childControlHeight = true;
        BadgeLayout.childForceExpandWidth = false;
        BadgeLayout.childForceExpandHeight = false;
        BadgeLayout.childAlignment = TextAnchor.MiddleLeft;
        LayoutElement BadgeElement = Badges.AddComponent<LayoutElement>();
        BadgeElement.minHeight = 24f;
        BadgeElement.preferredHeight = 24f;
        BadgeElement.flexibleWidth = 1f;

        AddText(Object.transform, "Text_ChoiceDetail", "D20 OUTCOME", 14f,
            TextAlignmentOptions.Left, 20f);
        return Object.GetComponent<Button>();
    }


    private static Button AddButton(Transform Parent, string Name, string Label)
    {
        GameObject SourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/HUD/Search/HUD_Search_Overlay.prefab");
        Transform Source = FindDescendant(SourcePrefab.transform, "Button_ConfirmMove");
        GameObject Object = UnityEngine.Object.Instantiate(Source.gameObject, Parent);
        Object.name = Name;
        Object.SetActive(true);
        Button Button = Object.GetComponent<Button>();
        TMP_Text Text = Button.GetComponentInChildren<TMP_Text>(true);
        Text.text = Label;
        Text.textWrappingMode = TextWrappingModes.NoWrap;
        LayoutElement Element = Object.GetComponent<LayoutElement>();
        if (Element == null)
            Element = Object.AddComponent<LayoutElement>();
        Element.minHeight = 44f;
        Element.preferredHeight = 50f;
        Element.flexibleWidth = 1f;
        Element.flexibleHeight = 0f;
        return Button;
    }


    private static RectTransform AddScroll(Transform Parent, string Name)
    {
        GameObject Scroll = NewUI(Name + "Scroll", Parent);
        ScrollRect ScrollRect = Scroll.AddComponent<ScrollRect>();
        ScrollRect.horizontal = false;
        ScrollRect.vertical = true;
        ScrollRect.scrollSensitivity = 25f;
        Image Image = Scroll.AddComponent<Image>();
        Image.color = new Color(0f, 0f, 0f, 0.08f);
        Image.raycastTarget = true;
        LayoutElement Layout = Scroll.AddComponent<LayoutElement>();
        Layout.flexibleHeight = 1f;
        Layout.minHeight = 90f;

        GameObject Viewport = NewUI("Viewport", Scroll.transform);
        Stretch(Viewport.GetComponent<RectTransform>());
        Viewport.AddComponent<RectMask2D>();
        GameObject Content = NewUI(Name, Viewport.transform);
        RectTransform Rect = Content.GetComponent<RectTransform>();
        Rect.anchorMin = new Vector2(0f, 1f);
        Rect.anchorMax = new Vector2(1f, 1f);
        Rect.pivot = new Vector2(0.5f, 1f);
        Rect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup Group = Content.AddComponent<VerticalLayoutGroup>();
        Group.spacing = 8f;
        Group.childControlWidth = true;
        Group.childControlHeight = true;
        Group.childForceExpandWidth = true;
        Group.childForceExpandHeight = false;
        ContentSizeFitter Fitter = Content.AddComponent<ContentSizeFitter>();
        Fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect.viewport = Viewport.GetComponent<RectTransform>();
        ScrollRect.content = Rect;
        return Rect;
    }


    private static void CreateDice(Transform Parent, string Name, string RollName)
    {
        GameObject Dice = NewUI(Name, Parent);
        Image Image = Dice.AddComponent<Image>();
        Image.preserveAspect = true;
        Image.raycastTarget = false;
        AnimationClip Clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UiClipPath);
        EditorCurveBinding[] Bindings = AnimationUtility.GetObjectReferenceCurveBindings(Clip);
        if (Bindings.Length > 0)
        {
            ObjectReferenceKeyframe[] Keys =
                AnimationUtility.GetObjectReferenceCurve(Clip, Bindings[0]);
            if (Keys.Length > 0)
                Image.sprite = Keys[0].value as Sprite;
        }
        Animator Animator = Dice.AddComponent<Animator>();
        Animator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(UiControllerPath);
        LayoutElement Layout = Dice.AddComponent<LayoutElement>();
        Layout.minWidth = 54f;
        Layout.preferredWidth = 60f;
        Layout.minHeight = 54f;
        Layout.preferredHeight = 60f;
        Layout.flexibleHeight = 0f;
        TMP_Text Roll = AddText(Dice.transform, RollName, string.Empty, 28f,
            TextAlignmentOptions.Center, 52f);
        Stretch(Roll.rectTransform);
    }


    private static void CopyPanelArt(Image Destination)
    {
        GameObject Source = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/HUD/Search/HUD_Search_Bottom.prefab");
        Image Image = Source != null ? Source.GetComponent<Image>() : null;
        if (Image != null)
        {
            Destination.sprite = Image.sprite;
            Destination.type = Image.type;
            Destination.color = Image.color;
            Destination.material = Image.material;
        }
        else
            Destination.color = new Color(0.03f, 0.08f, 0.13f, 1f);
    }


    private static void Stretch(RectTransform Rect)
    {
        Rect.anchorMin = Vector2.zero;
        Rect.anchorMax = Vector2.one;
        Rect.offsetMin = Vector2.zero;
        Rect.offsetMax = Vector2.zero;
    }


    private static T Component<T>(Transform Root, string Name) where T : Component =>
        FindDescendant(Root, Name)?.GetComponent<T>();


    private static void Set(SerializedObject Object, string Name,
        UnityEngine.Object Value)
    {
        SerializedProperty Property = Object.FindProperty(Name);
        if (Property == null)
            throw new InvalidOperationException($"Missing serialized property {Name}.");
        Property.objectReferenceValue = Value;
    }


    private static void CopyReference(SerializedObject Source,
        SerializedObject Destination, string Name)
    {
        SerializedProperty SourceProperty = Source.FindProperty(Name);
        if (SourceProperty != null)
            Set(Destination, Name, SourceProperty.objectReferenceValue);
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
