using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// Participant-facing presentation for the selected Unit during Movement.
// It consumes filtered snapshots and authored definitions; it owns no gameplay state.
[DisallowMultipleComponent]
public sealed class MovementUnitDetailsUI : MonoBehaviour
{
    private const int HealthSegmentsPerRow = 10;
    private const float HealthSegmentHeight = 32f;
    private const float HealthSegmentVerticalSpacing = 4f;

    private enum Tab
    {
        Status = 0,
        Systems = 1,
        Weapons = 2,
        Abilities = 3
    }


    [Header("Tabs")]
    [SerializeField] private Button _StatusTabButton;
    [SerializeField] private Button _SystemsTabButton;
    [SerializeField] private Button _WeaponsTabButton;
    [SerializeField] private Button _AbilitiesTabButton;
    [SerializeField] private TMP_Text _StatusTabText;
    [SerializeField] private TMP_Text _SystemsTabText;
    [SerializeField] private TMP_Text _WeaponsTabText;
    [SerializeField] private TMP_Text _AbilitiesTabText;

    [Header("Pages")]
    [SerializeField] private GameObject _StatusPage;
    [SerializeField] private GameObject _SystemsPage;
    [SerializeField] private GameObject _WeaponsPage;
    [SerializeField] private GameObject _AbilitiesPage;

    [Header("Responsive Identity Placement")]
    [SerializeField] private RectTransform _SelectedUnitIdentity;
    [SerializeField] private RectTransform _UnitNameAndBadges;
    [SerializeField] private RectTransform _InstanceName;
    [SerializeField] private RectTransform _StatusNameParent;
    [SerializeField] private RectTransform _StatusEffectsParent;

    [Header("Status")]
    [SerializeField] private Image _UnitPreview;
    [SerializeField] private TMP_Text _UnitNameText;
    [SerializeField] private TMP_Text _InstanceNameText;
    [SerializeField] private TMP_Text _HealthText;
    [SerializeField] private RectTransform _UnitTypeBadges;
    [SerializeField] private RectTransform _HealthSegments;
    [SerializeField] private RectTransform _ActiveEffectsContent;
    [SerializeField] private GameObject _ActiveEffectTemplate;

    [Header("Card Trays")]
    [SerializeField] private RectTransform _SystemsContent;
    [SerializeField] private RectTransform _WeaponsContent;
    [SerializeField] private RectTransform _AbilitiesContent;
    [SerializeField] private GameObject _SystemCardTemplate;
    [SerializeField] private GameObject _WeaponCardTemplate;
    [SerializeField] private GameObject _AbilityCardTemplate;

    [Header("Health Segment Sprites")]
    [SerializeField] private Sprite _HealthSingleFull;
    [SerializeField] private Sprite _HealthSingleEmpty;
    [SerializeField] private Sprite _HealthHeadFull;
    [SerializeField] private Sprite _HealthHeadEmpty;
    [SerializeField] private Sprite _HealthMiddleFull;
    [SerializeField] private Sprite _HealthMiddleEmpty;
    [SerializeField] private Sprite _HealthTailFull;
    [SerializeField] private Sprite _HealthTailEmpty;

    [Header("Charge Segment Sprites")]
    [SerializeField] private Sprite _ChargeSingleFull;
    [SerializeField] private Sprite _ChargeSingleEmpty;
    [SerializeField] private Sprite _ChargeHeadFull;
    [SerializeField] private Sprite _ChargeHeadEmpty;
    [SerializeField] private Sprite _ChargeMiddleFull;
    [SerializeField] private Sprite _ChargeMiddleEmpty;
    [SerializeField] private Sprite _ChargeTailFull;
    [SerializeField] private Sprite _ChargeTailEmpty;

    [Header("Domain Badges")]
    [SerializeField] private Sprite _AirDomainBadge;
    [SerializeField] private Sprite _SurfaceDomainBadge;
    [SerializeField] private Sprite _SubsurfaceDomainBadge;

    [Header("Generated Visual Sizes")]
    [SerializeField, Min(1f)] private float _ChargeSegmentHeight = 14f;
    [SerializeField, Min(1f)] private float _BadgeHeight = 24f;
    [SerializeField, Min(1f)] private float _CardTitleFontSize = 20f;
    [SerializeField, Min(1f)] private float _CardBodyFontSize = 17f;
    [SerializeField, Min(1f)] private float _ChargeCountFontSize = 18f;
    [SerializeField, Min(1f)] private float _AbilityButtonFontSize = 18f;
    [SerializeField, Min(0f)] private float _BadgeSpacing = 6f;


    private ParticipantUnitSnapshot _Unit;
    private Definition_Unit _Definition;
    private GameplayPhase _Phase;
    private Action<int, int> _UseSystem;
    private Action<int> _UseAbility;
    private string _BoundRuntimeUnitID = string.Empty;
    private Tab _CurrentTab;
    private bool _ButtonsBound;


    private void Awake()
    {
        ConfigureCardTemplates();
        ConfigureTrayContent(_SystemsContent);
        ConfigureTrayContent(_WeaponsContent);
        ConfigureTrayContent(_AbilitiesContent);
        ConfigureContentSizedButton(_StatusTabButton, 14, 14, 6, 6);
        ConfigureContentSizedButton(_SystemsTabButton, 14, 14, 6, 6);
        ConfigureContentSizedButton(_WeaponsTabButton, 14, 14, 6, 6);
        ConfigureContentSizedButton(_AbilitiesTabButton, 14, 14, 6, 6);
        BindTabButtons();
        ShowTab(Tab.Status);
    }


    private void ConfigureCardTemplates()
    {
        ConfigureCardTemplate(_SystemCardTemplate, false);
        ConfigureCardTemplate(_WeaponCardTemplate, false);
        ConfigureCardTemplate(_AbilityCardTemplate, true);
    }


    private static void ConfigureTrayContent(RectTransform Content)
    {
        if (Content == null)
            return;

        HorizontalLayoutGroup Group =
            Content.GetComponent<HorizontalLayoutGroup>();
        if (Group == null)
            Group = Content.gameObject.AddComponent<HorizontalLayoutGroup>();

        Group.childControlWidth = true;
        Group.childControlHeight = true;
        Group.childForceExpandWidth = false;
        Group.childForceExpandHeight = true;

        ContentSizeFitter Fitter =
            Content.GetComponent<ContentSizeFitter>();
        if (Fitter == null)
            Fitter = Content.gameObject.AddComponent<ContentSizeFitter>();

        Fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        Fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }


    private static void ConfigureContentSizedButton(
        Button Button,
        int LeftPadding,
        int RightPadding,
        int TopPadding,
        int BottomPadding)
    {
        if (Button == null)
            return;

        HorizontalLayoutGroup Group =
            Button.GetComponent<HorizontalLayoutGroup>();
        if (Group == null)
            Group = Button.gameObject.AddComponent<HorizontalLayoutGroup>();

        Group.padding = new RectOffset(
            LeftPadding,
            RightPadding,
            TopPadding,
            BottomPadding
        );
        Group.childAlignment = TextAnchor.MiddleCenter;
        Group.childControlWidth = true;
        Group.childControlHeight = true;
        Group.childForceExpandWidth = false;
        Group.childForceExpandHeight = false;

        ContentSizeFitter Fitter =
            Button.GetComponent<ContentSizeFitter>();
        if (Fitter == null)
            Fitter = Button.gameObject.AddComponent<ContentSizeFitter>();

        Fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        Fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        LayoutElement ButtonLayout =
            Button.GetComponent<LayoutElement>();
        if (ButtonLayout != null)
        {
            ButtonLayout.minWidth = -1f;
            ButtonLayout.preferredWidth = -1f;
            ButtonLayout.flexibleWidth = 0f;
        }

        TMP_Text Label = Button.GetComponentInChildren<TMP_Text>(true);
        if (Label == null)
            return;

        Label.textWrappingMode = TextWrappingModes.NoWrap;
        Label.overflowMode = TextOverflowModes.Overflow;
        Label.alignment = TextAlignmentOptions.Center;

        LayoutElement LabelLayout = Label.GetComponent<LayoutElement>();
        if (LabelLayout != null)
        {
            LabelLayout.minWidth = -1f;
            LabelLayout.preferredWidth = -1f;
            LabelLayout.flexibleWidth = 0f;
        }
    }


    private void ConfigureCardTemplate(
        GameObject Template,
        bool HasUseButton)
    {
        if (Template == null)
            return;

        ClearFixedChildWidths(Template.transform);

        LayoutElement CardLayout = Template.GetComponent<LayoutElement>();
        if (CardLayout != null)
        {
            CardLayout.minWidth = -1f;
            CardLayout.preferredWidth = -1f;
            CardLayout.flexibleWidth = 0f;
            CardLayout.minHeight = 0f;
            CardLayout.preferredHeight = 0f;
            CardLayout.flexibleHeight = 1f;
        }

        ContentSizeFitter CardFitter =
            Template.GetComponent<ContentSizeFitter>();
        if (CardFitter == null)
            CardFitter = Template.AddComponent<ContentSizeFitter>();

        CardFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        CardFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        VerticalLayoutGroup CardGroup = Template.GetComponent<VerticalLayoutGroup>();
        if (CardGroup != null)
        {
            CardGroup.padding = new RectOffset(10, 10, 6, 6);
            CardGroup.spacing = 4f;
            CardGroup.childControlHeight = true;
            CardGroup.childForceExpandHeight = false;
        }

        ConfigureTemplateText(Template.transform, "Text_Name", _CardTitleFontSize);
        ConfigureTemplateText(Template.transform, "Text_Charges", _ChargeCountFontSize);
        ConfigureTemplateText(Template.transform, "Text_Replenish", _CardBodyFontSize);

        Transform TitleTransform = Find(Template.transform, "Text_Name");
        if (TitleTransform != null &&
            TitleTransform.TryGetComponent(out TMP_Text TitleText))
        {
            TitleText.textWrappingMode = TextWrappingModes.NoWrap;
            TitleText.overflowMode = TextOverflowModes.Overflow;
        }

        Transform ChargeRow = Find(Template.transform, "ChargeRow");
        LayoutElement ChargeRowLayout = ChargeRow != null
            ? ChargeRow.GetComponent<LayoutElement>()
            : null;
        if (ChargeRowLayout != null)
        {
            ChargeRowLayout.minHeight = 28f;
            ChargeRowLayout.preferredHeight = 28f;
            ChargeRowLayout.flexibleHeight = 0f;
        }

        Transform Footer = Find(Template.transform, "Footer");
        ConfigureCardFooter(
            Footer,
            HasUseButton ? 34f : _BadgeHeight);

        if (HasUseButton)
        {
            Transform UseButton = Find(Template.transform, "Button_Use");
            if (UseButton != null)
            {
                ConfigureTemplateText(UseButton, "Text_Label", _AbilityButtonFontSize);
                ConfigureContentSizedButton(
                    UseButton.GetComponent<Button>(),
                    10,
                    10,
                    10,
                    10
                );
            }
        }
    }


    private static void ClearFixedChildWidths(Transform Root)
    {
        if (Root == null)
            return;

        LayoutElement Layout = Root.GetComponent<LayoutElement>();
        if (Layout != null)
        {
            Layout.minWidth = -1f;
            Layout.preferredWidth = -1f;
            Layout.flexibleWidth = 0f;
        }

        for (int i = 0; i < Root.childCount; i++)
            ClearFixedChildWidths(Root.GetChild(i));
    }


    private static void ConfigureTemplateText(
        Transform Root,
        string Name,
        float FontSize,
        float PreferredHeight = -1f)
    {
        Transform TextTransform = Find(Root, Name);
        TMP_Text Text = TextTransform != null
            ? TextTransform.GetComponent<TMP_Text>()
            : null;
        if (Text != null)
        {
            Text.fontSize = FontSize;
            if (PreferredHeight > 0f)
            {
                Text.textWrappingMode = TextWrappingModes.Normal;
                Text.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        if (TextTransform == null)
            return;

        LayoutElement Layout = TextTransform.GetComponent<LayoutElement>();
        if (Layout != null)
        {
            Layout.minHeight = PreferredHeight > 0f
                ? PreferredHeight
                : -1f;
            Layout.preferredHeight = PreferredHeight > 0f
                ? PreferredHeight
                : -1f;
            Layout.flexibleHeight = 0f;
        }
    }


    private static void ConfigureCardFooter(
        Transform Footer,
        float Height)
    {
        if (Footer == null)
            return;

        HorizontalLayoutGroup Group =
            Footer.GetComponent<HorizontalLayoutGroup>();
        if (Group != null)
        {
            Group.childAlignment = TextAnchor.MiddleLeft;
            Group.spacing = 6f;
            Group.childControlWidth = true;
            Group.childControlHeight = true;
            Group.childForceExpandWidth = false;
            Group.childForceExpandHeight = true;
        }

        LayoutElement Layout = Footer.GetComponent<LayoutElement>();
        if (Layout == null)
            Layout = Footer.gameObject.AddComponent<LayoutElement>();

        Layout.minHeight = Height;
        Layout.preferredHeight = Height;
        Layout.flexibleHeight = 0f;
    }


    public void Bind(
        ParticipantUnitSnapshot Unit,
        Definition_Unit Definition,
        UnitPresentationCatalog PresentationCatalog,
        GameplayPhase Phase,
        bool CanAct,
        Action<int, int> UseSystem,
        Action<int> UseAbility)
    {
        bool SelectionChanged =
            Unit == null ||
            !string.Equals(
                _BoundRuntimeUnitID,
                Unit.RuntimeUnitID,
                StringComparison.Ordinal);

        _Unit = Unit;
        _Definition = Definition;
        _Phase = Phase;
        _UseSystem = UseSystem;
        _UseAbility = UseAbility;
        _BoundRuntimeUnitID = Unit?.RuntimeUnitID ?? string.Empty;

        if (_Unit == null || _Definition == null)
        {
            Clear();
            return;
        }

        if (SelectionChanged)
            ShowTab(Tab.Status);

        BindIdentity(PresentationCatalog);
        RebuildHealth();
        RebuildEffects();
        RebuildSystems(CanAct);
        RebuildWeapons();
        RebuildAbilities(CanAct);
        RefreshTabLabels();
    }


    public void Clear()
    {
        _Unit = null;
        _Definition = null;
        _Phase = GameplayPhase.None;
        _UseSystem = null;
        _UseAbility = null;
        _BoundRuntimeUnitID = string.Empty;

        ClearGenerated(_UnitTypeBadges, null);
        ClearGenerated(_HealthSegments, null);
        ClearGenerated(_ActiveEffectsContent, _ActiveEffectTemplate);
        ClearGenerated(_SystemsContent, _SystemCardTemplate);
        ClearGenerated(_WeaponsContent, _WeaponCardTemplate);
        ClearGenerated(_AbilitiesContent, _AbilityCardTemplate);
    }


    private void BindIdentity(UnitPresentationCatalog PresentationCatalog)
    {
        _UnitNameText.text = _Unit.DefinitionName;
        _InstanceNameText.text = _Unit.InstanceName;
        _HealthText.text = $"{_Unit.CurrentHealth} / {_Unit.MaxHealth}";

        _UnitPreview.sprite = null;
        _UnitPreview.enabled = false;

        if (PresentationCatalog != null &&
            PresentationCatalog.TryGetPrefab(_Definition, out UnitView Prefab))
        {
            SpriteRenderer Renderer = Prefab.GetComponent<SpriteRenderer>();

            if (Renderer != null && Renderer.sprite != null)
            {
                _UnitPreview.sprite = Renderer.sprite;
                _UnitPreview.preserveAspect = true;
                _UnitPreview.enabled = true;
            }
        }

        ClearGenerated(_UnitTypeBadges, null);

        Definition_UnitType[] Types = _Definition.UnitTypes;
        if (Types == null)
            return;

        for (int i = 0; i < Types.Length; i++)
        {
            if (Types[i] != null && Types[i].Badge != null)
                CreateBadge(_UnitTypeBadges, Types[i].Badge, Types[i].Name);
        }
    }


    private void RebuildHealth()
    {
        ClearGenerated(_HealthSegments, null);

        int Maximum = Mathf.Max(0, _Unit.MaxHealth);
        int Current = Mathf.Clamp(_Unit.CurrentHealth, 0, Maximum);

        LayoutElement HealthLayout = _HealthSegments.GetComponent<LayoutElement>();
        if (HealthLayout != null)
        {
            int RowCount = Maximum > 0
                ? Mathf.CeilToInt(Maximum / (float)HealthSegmentsPerRow)
                : 0;
            float RequiredHeight = RowCount > 0
                ? RowCount * HealthSegmentHeight +
                  (RowCount - 1) * HealthSegmentVerticalSpacing
                : 0f;

            HealthLayout.minHeight = RequiredHeight;
            HealthLayout.preferredHeight = RequiredHeight;
        }

        for (int i = 0; i < Maximum; i++)
        {
            bool Full = i < Current;
            Sprite Segment = ResolveSegmentSprite(
                i,
                Maximum,
                Full,
                _HealthSingleFull,
                _HealthSingleEmpty,
                _HealthHeadFull,
                _HealthHeadEmpty,
                _HealthMiddleFull,
                _HealthMiddleEmpty,
                _HealthTailFull,
                _HealthTailEmpty);

            CreateSegment(
                _HealthSegments,
                Segment,
                HealthSegmentHeight);
        }
    }


    private void RebuildEffects()
    {
        ClearGenerated(_ActiveEffectsContent, _ActiveEffectTemplate);

        ParticipantStatusSnapshot[] Statuses = _Unit.Statuses;
        if (Statuses != null)
        {
            for (int i = 0; i < Statuses.Length; i++)
            {
                ParticipantStatusSnapshot Status = Statuses[i];
                if (Status == null)
                    continue;

                string Duration = Status.RemainingTurns > 0
                    ? $"{Status.RemainingTurns}T left"
                    : "Permanent";

                CreateEffectEntry(Status.Name, Duration);
            }
        }

        ParticipantModifierSnapshot[] Modifiers = _Unit.Modifiers;
        if (Modifiers == null)
            return;

        for (int i = 0; i < Modifiers.Length; i++)
        {
            ParticipantModifierSnapshot Modifier = Modifiers[i];
            if (Modifier == null)
                continue;

            string Sign = Modifier.Value >= 0 ? "+" : string.Empty;
            string Duration = Modifier.DurationMode == DurationMode.Permanent
                ? "Permanent"
                : Modifier.RemainingTurns > 0
                    ? $"{Modifier.RemainingTurns}T left"
                    : string.Empty;

            CreateEffectEntry(
                $"{Modifier.Type} {Sign}{Modifier.Value}",
                Duration);
        }
    }


    private void RebuildSystems(bool CanAct)
    {
        ClearGenerated(_SystemsContent, _SystemCardTemplate);

        ParticipantSystemSnapshot[] Systems = _Unit.Systems;
        if (Systems == null)
            return;

        for (int i = 0; i < Systems.Length; i++)
        {
            ParticipantSystemSnapshot Snapshot = Systems[i];
            if (Snapshot == null)
                continue;

            GameObject Card = CreateCard(_SystemCardTemplate, _SystemsContent);
            SetText(Card, "Text_Name", Snapshot.Name);
            BindCharges(Card, Snapshot.CurrentCharges, Snapshot.MaxCharges);

            Definition_System Definition = TryGetSystemDefinition(Snapshot.SlotIndex);
            SetText(Card, "Text_Replenish", FormatReplenishment(
                Definition?.ReplenishRate ?? 0,
                Definition?.ReplenishInterval ?? 0,
                Snapshot.ReplenishmentProgress));

            RectTransform Footer = CreateSystemFooter(Card);
            RectTransform Badges = CreateBadgeStrip(Footer);
            if (Definition != null &&
                Definition.Type != null &&
                Definition.Type.Badge != null)
            {
                CreateBadge(Badges, Definition.Type.Badge, Definition.Type.Name);
            }

            ConfigureBadgeStrip(Badges);

            Transform Replenish = Find(Card.transform, "Text_Replenish");
            if (Replenish != null)
            {
                Replenish.SetParent(Footer, false);
                ConfigureFooterText(Replenish);
            }


            bool SupportsMineDeployment =
                Definition != null &&
                (Definition.OperationalPhases & GameplayPhase.PreBattle) != 0 &&
                Definition.MineDeployment != null &&
                Definition.MineDeployment.Enabled;

            bool SupportsUnitDeployment =
                Definition != null &&
                (Definition.OperationalPhases & GameplayPhase.PreBattle) != 0 &&
                Definition.UnitDeployment != null &&
                Definition.UnitDeployment.Enabled;

            int CarriedUnitIndex = -1;
            int AvailableCarriedUnits = 0;

            if (SupportsUnitDeployment && Snapshot.CarriedUnits != null)
            {
                for (int CarriedIndex = 0;
                    CarriedIndex < Snapshot.CarriedUnits.Length;
                    CarriedIndex++)
                {
                    ParticipantCarriedUnitSnapshot Carried =
                        Snapshot.CarriedUnits[CarriedIndex];

                    if (Carried != null &&
                        Carried.State == RuntimeCarriedUnitState.Carried)
                    {
                        AvailableCarriedUnits++;

                        if (CarriedUnitIndex < 0)
                            CarriedUnitIndex = Carried.CarriedUnitIndex;
                    }
                }
            }

            bool SupportsUse =
                SupportsMineDeployment || SupportsUnitDeployment;


            if (SupportsUse)
            {
                bool Usable =
                    CanAct &&
                    _Phase == GameplayPhase.PreBattle &&
                    (!Definition.UsesCharges || Snapshot.CurrentCharges > 0) &&
                    (!SupportsUnitDeployment || CarriedUnitIndex >= 0);

                Button UseButton = CreateSystemUseButton(Card);
                if (UseButton != null)
                {
                    SetText(
                        UseButton.gameObject,
                        "Text_Label",
                        SupportsUnitDeployment
                            ? $"Deploy ({AvailableCarriedUnits})"
                            : "Lay Mine"
                    );
                    UseButton.interactable = Usable;
                    int SlotIndex = Snapshot.SlotIndex;
                    int SelectedCarriedUnitIndex = CarriedUnitIndex;
                    UseButton.onClick.AddListener(() =>
                    {
                        if (UseButton.interactable)
                            _UseSystem?.Invoke(SlotIndex, SelectedCarriedUnitIndex);
                    });
                }
            }
        }
    }


    private void RebuildWeapons()
    {
        ClearGenerated(_WeaponsContent, _WeaponCardTemplate);

        ParticipantWeaponSnapshot[] Weapons = _Unit.Weapons;
        if (Weapons == null)
            return;

        for (int i = 0; i < Weapons.Length; i++)
        {
            ParticipantWeaponSnapshot Snapshot = Weapons[i];
            if (Snapshot == null)
                continue;

            GameObject Card = CreateCard(_WeaponCardTemplate, _WeaponsContent);
            SetText(Card, "Text_Name", Snapshot.Name);
            BindCharges(Card, Snapshot.CurrentCharges, Snapshot.MaxCharges);

            Definition_Weapon Definition = TryGetWeaponDefinition(Snapshot.SlotIndex);
            SetText(Card, "Text_Replenish", FormatReplenishment(
                Definition?.ReplenishRate ?? 0,
                Definition?.ReplenishInterval ?? 0,
                Snapshot.ReplenishmentProgress));

            Transform Replenish = Find(Card.transform, "Text_Replenish");
            if (Replenish != null)
                ConfigureFooterText(Replenish);

            RectTransform Badges = Find(Card.transform, "BadgeStrip") as RectTransform;
            ClearGenerated(Badges, null);

            if (Definition != null)
            {
                Definition_WeaponType[] Types = Definition.Types;
                if (Types != null)
                {
                    for (int TypeIndex = 0; TypeIndex < Types.Length; TypeIndex++)
                    {
                        if (Types[TypeIndex] != null && Types[TypeIndex].Badge != null)
                            CreateBadge(Badges, Types[TypeIndex].Badge, Types[TypeIndex].Name);
                    }
                }

                AddDomainBadges(Badges, Definition.TargetDomains);
            }

            ConfigureBadgeStrip(Badges);
        }
    }


    private void RebuildAbilities(bool CanAct)
    {
        ClearGenerated(_AbilitiesContent, _AbilityCardTemplate);

        ParticipantAbilitySnapshot[] Abilities = _Unit.Abilities;
        if (Abilities == null)
            return;

        for (int i = 0; i < Abilities.Length; i++)
        {
            ParticipantAbilitySnapshot Snapshot = Abilities[i];
            if (Snapshot == null)
                continue;

            GameObject Card = CreateCard(_AbilityCardTemplate, _AbilitiesContent);
            SetText(Card, "Text_Name", Snapshot.Name);

            Definition_Ability Definition = TryGetAbilityDefinition(Snapshot.SlotIndex);
            bool Usable =
                CanAct &&
                Definition != null &&
                (Definition.UsablePhases & _Phase) != 0;

            Button UseButton = Find(Card.transform, "Button_Use").GetComponent<Button>();
            UseButton.interactable = Usable;

            int SlotIndex = Snapshot.SlotIndex;
            UseButton.onClick.AddListener(() =>
            {
                if (UseButton.interactable)
                    _UseAbility?.Invoke(SlotIndex);
            });
        }
    }


    private Button CreateSystemUseButton(GameObject Card)
    {
        Transform Source = _AbilityCardTemplate != null
            ? Find(_AbilityCardTemplate.transform, "Button_Use")
            : null;

        Transform ChargeRow = Card != null
            ? Find(Card.transform, "ChargeRow")
            : null;


        if (Source == null || ChargeRow == null)
            return null;


        GameObject ButtonObject = Instantiate(
            Source.gameObject,
            ChargeRow
        );

        ButtonObject.name = "Button_Use";
        ButtonObject.SetActive(true);
        SetText(ButtonObject, "Text_Label", "Use");
        Button Button = ButtonObject.GetComponent<Button>();
        ConfigureContentSizedButton(Button, 10, 10, 10, 10);
        return Button;
    }


    private RectTransform CreateSystemFooter(GameObject Card)
    {
        GameObject Footer = new GameObject(
            "Footer",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement));

        Footer.layer = Card.layer;
        Footer.transform.SetParent(Card.transform, false);
        ConfigureCardFooter(Footer.transform, _BadgeHeight);
        return Footer.GetComponent<RectTransform>();
    }


    private static void ConfigureFooterText(Transform TextTransform)
    {
        if (TextTransform.TryGetComponent(out TMP_Text Text))
        {
            Text.alignment = TextAlignmentOptions.MidlineRight;
            Text.textWrappingMode = TextWrappingModes.NoWrap;
            Text.overflowMode = TextOverflowModes.Ellipsis;
        }

        LayoutElement Layout = TextTransform.GetComponent<LayoutElement>();
        if (Layout == null)
            Layout = TextTransform.gameObject.AddComponent<LayoutElement>();

        Layout.minWidth = -1f;
        Layout.preferredWidth = -1f;
        Layout.flexibleWidth = 0f;
        Layout.minHeight = -1f;
        Layout.preferredHeight = -1f;
        Layout.flexibleHeight = 0f;
    }


    private void CreateEffectEntry(string Name, string Duration)
    {
        GameObject Entry = CreateCard(_ActiveEffectTemplate, _ActiveEffectsContent);
        SetText(Entry, "Text_Name", Name);
        SetText(Entry, "Text_Duration", Duration);
    }


    private void BindCharges(GameObject Card, int Current, int Maximum)
    {
        RectTransform Strip = Find(Card.transform, "ChargeStrip") as RectTransform;
        ClearGenerated(Strip, null);

        Transform ChargeTextTransform = Find(Card.transform, "Text_Charges");
        TMP_Text ChargeText = ChargeTextTransform != null
            ? ChargeTextTransform.GetComponent<TMP_Text>()
            : null;
        LayoutElement ChargeTextLayout = ChargeTextTransform != null
            ? ChargeTextTransform.GetComponent<LayoutElement>()
            : null;

        if (Maximum <= 0)
        {
            if (Strip != null)
                Strip.gameObject.SetActive(false);

            if (ChargeText != null)
            {
                ChargeText.alignment = TextAlignmentOptions.MidlineLeft;
                ChargeText.textWrappingMode = TextWrappingModes.NoWrap;
                ChargeText.overflowMode = TextOverflowModes.Overflow;
            }

            if (ChargeTextLayout != null)
            {
                ChargeTextLayout.minWidth = -1f;
                ChargeTextLayout.preferredWidth = -1f;
                ChargeTextLayout.flexibleWidth = 0f;
            }

            SetText(Card, "Text_Charges", "No charges");
            return;
        }

        if (Strip != null)
            Strip.gameObject.SetActive(true);

        if (ChargeText != null)
        {
            ChargeText.alignment = TextAlignmentOptions.MidlineRight;
            ChargeText.textWrappingMode = TextWrappingModes.NoWrap;
            ChargeText.overflowMode = TextOverflowModes.Overflow;
        }

        if (ChargeTextLayout != null)
        {
            ChargeTextLayout.minWidth = -1f;
            ChargeTextLayout.preferredWidth = -1f;
            ChargeTextLayout.flexibleWidth = 0f;
        }

        Current = Mathf.Clamp(Current, 0, Maximum);
        SetText(Card, "Text_Charges", $"{Current}/{Maximum}");

        for (int i = 0; i < Maximum; i++)
        {
            bool Full = i < Current;
            Sprite Segment = ResolveSegmentSprite(
                i,
                Maximum,
                Full,
                _ChargeSingleFull,
                _ChargeSingleEmpty,
                _ChargeHeadFull,
                _ChargeHeadEmpty,
                _ChargeMiddleFull,
                _ChargeMiddleEmpty,
                _ChargeTailFull,
                _ChargeTailEmpty);

            CreateSegment(
                Strip,
                Segment,
                _ChargeSegmentHeight,
                false);
        }
    }


    private void AddDomainBadges(RectTransform Parent, Domain Domains)
    {
        if ((Domains & Domain.Air) != 0 && _AirDomainBadge != null)
            CreateBadge(Parent, _AirDomainBadge, "Air");
        if ((Domains & Domain.Surface) != 0 && _SurfaceDomainBadge != null)
            CreateBadge(Parent, _SurfaceDomainBadge, "Surface");
        if ((Domains & Domain.Subsurface) != 0 && _SubsurfaceDomainBadge != null)
            CreateBadge(Parent, _SubsurfaceDomainBadge, "Subsurface");
    }


    private RectTransform CreateBadgeStrip(Transform Parent)
    {
        GameObject Strip = new GameObject(
            "BadgeStrip",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement));

        Strip.layer = Parent.gameObject.layer;
        Strip.transform.SetParent(Parent, false);

        RectTransform Rect = Strip.GetComponent<RectTransform>();
        LayoutElement Layout = Strip.GetComponent<LayoutElement>();
        Layout.minHeight = _BadgeHeight;
        Layout.preferredHeight = _BadgeHeight;

        return Rect;
    }


    private void ConfigureBadgeStrip(RectTransform Strip)
    {
        if (Strip == null)
            return;

        int BadgeCount = Strip.childCount;
        Strip.gameObject.SetActive(BadgeCount > 0);

        HorizontalLayoutGroup Group = Strip.GetComponent<HorizontalLayoutGroup>();
        if (Group != null)
        {
            Group.childAlignment = TextAnchor.MiddleLeft;
            Group.spacing = _BadgeSpacing;
            Group.childControlWidth = true;
            Group.childControlHeight = true;
            Group.childForceExpandWidth = false;
            Group.childForceExpandHeight = false;
        }

        LayoutElement Layout = Strip.GetComponent<LayoutElement>();
        if (Layout == null)
            return;

        Layout.minHeight = _BadgeHeight;
        Layout.preferredHeight = _BadgeHeight;
        Layout.minWidth = -1f;
        Layout.preferredWidth = -1f;
        Layout.flexibleWidth = 0f;
    }


    private Definition_System TryGetSystemDefinition(int SlotIndex)
    {
        UnitSystemSlot[] Slots = _Definition.SystemSlots;
        return Slots != null && SlotIndex >= 0 && SlotIndex < Slots.Length
            ? Slots[SlotIndex]?.System
            : null;
    }


    private Definition_Weapon TryGetWeaponDefinition(int SlotIndex)
    {
        UnitWeaponSlot[] Slots = _Definition.WeaponSlots;
        return Slots != null && SlotIndex >= 0 && SlotIndex < Slots.Length
            ? Slots[SlotIndex]?.Weapon
            : null;
    }


    private Definition_Ability TryGetAbilityDefinition(int SlotIndex)
    {
        Definition_Ability[] Abilities = _Definition.Abilities;
        return Abilities != null && SlotIndex >= 0 && SlotIndex < Abilities.Length
            ? Abilities[SlotIndex]
            : null;
    }


    private static string FormatReplenishment(int Rate, int Interval, int Progress)
    {
        if (Rate <= 0 || Interval <= 0)
            return string.Empty;

        int Remaining = Mathf.Max(1, Interval - Mathf.Max(0, Progress));
        return $"+{Rate} in {Remaining}T";
    }


    private void RefreshTabLabels()
    {
        _StatusTabText.text = "STATUS";
        _SystemsTabText.text = $"SYSTEMS: {_Unit.Systems?.Length ?? 0}";
        _WeaponsTabText.text = $"WEAPONS: {_Unit.Weapons?.Length ?? 0}";
        _AbilitiesTabText.text = $"ABILITIES: {_Unit.Abilities?.Length ?? 0}";
    }


    private void BindTabButtons()
    {
        if (_ButtonsBound)
            return;

        _StatusTabButton.onClick.AddListener(() => ShowTab(Tab.Status));
        _SystemsTabButton.onClick.AddListener(() => ShowTab(Tab.Systems));
        _WeaponsTabButton.onClick.AddListener(() => ShowTab(Tab.Weapons));
        _AbilitiesTabButton.onClick.AddListener(() => ShowTab(Tab.Abilities));
        _ButtonsBound = true;
    }


    private void ShowTab(Tab SelectedTab)
    {
        _CurrentTab = SelectedTab;
        _StatusPage.SetActive(SelectedTab == Tab.Status);
        _SystemsPage.SetActive(SelectedTab == Tab.Systems);
        _WeaponsPage.SetActive(SelectedTab == Tab.Weapons);
        _AbilitiesPage.SetActive(SelectedTab == Tab.Abilities);

        _StatusTabButton.interactable = SelectedTab != Tab.Status;
        _SystemsTabButton.interactable = SelectedTab != Tab.Systems;
        _WeaponsTabButton.interactable = SelectedTab != Tab.Weapons;
        _AbilitiesTabButton.interactable = SelectedTab != Tab.Abilities;

        PlaceIdentity(SelectedTab == Tab.Status);
    }


    private void PlaceIdentity(bool InStatusPage)
    {
        if (_SelectedUnitIdentity == null ||
            _UnitNameAndBadges == null ||
            _InstanceName == null ||
            _StatusNameParent == null ||
            _StatusEffectsParent == null)
        {
            return;
        }

        if (InStatusPage)
        {
            _UnitNameAndBadges.SetParent(_StatusNameParent, false);
            _UnitNameAndBadges.SetSiblingIndex(0);

            _InstanceName.SetParent(_StatusEffectsParent, false);
            _InstanceName.SetSiblingIndex(0);

            _SelectedUnitIdentity.gameObject.SetActive(false);
        }
        else
        {
            _UnitNameAndBadges.SetParent(_SelectedUnitIdentity, false);
            _UnitNameAndBadges.SetSiblingIndex(0);

            _InstanceName.SetParent(_SelectedUnitIdentity, false);
            _InstanceName.SetSiblingIndex(1);

            _SelectedUnitIdentity.gameObject.SetActive(true);
        }
    }


    private static GameObject CreateCard(GameObject Template, RectTransform Parent)
    {
        GameObject Card = Instantiate(Template, Parent);
        Card.name = Template.name.Replace("Template", string.Empty).TrimEnd('_');
        Card.SetActive(true);
        return Card;
    }


    private void CreateBadge(RectTransform Parent, Sprite Sprite, string Name)
    {
        GameObject Badge = new GameObject(
            $"Badge_{Name}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement));

        Badge.layer = Parent.gameObject.layer;
        Badge.transform.SetParent(Parent, false);

        Image Image = Badge.GetComponent<Image>();
        Image.sprite = Sprite;
        Image.preserveAspect = true;
        Image.raycastTarget = false;

        LayoutElement Layout = Badge.GetComponent<LayoutElement>();
        float BadgeWidth = GetAspectWidth(Sprite, _BadgeHeight);
        Layout.minWidth = BadgeWidth;
        Layout.preferredWidth = BadgeWidth;
        Layout.flexibleWidth = 0f;
        Layout.minHeight = _BadgeHeight;
        Layout.preferredHeight = _BadgeHeight;
    }


    private static void CreateSegment(
        RectTransform Parent,
        Sprite Sprite,
        float Height,
        bool FlexibleWidth = false)
    {
        GameObject Segment = new GameObject(
            "Segment",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement));

        Segment.layer = Parent.gameObject.layer;
        Segment.transform.SetParent(Parent, false);

        Image Image = Segment.GetComponent<Image>();
        Image.sprite = Sprite;
        Image.preserveAspect = true;
        Image.raycastTarget = false;

        LayoutElement Layout = Segment.GetComponent<LayoutElement>();
        float Width = GetAspectWidth(Sprite, Height);
        Layout.minWidth = FlexibleWidth ? 0f : Width;
        Layout.preferredWidth = Width;
        Layout.flexibleWidth = FlexibleWidth ? 1f : 0f;
        Layout.minHeight = Height;
        Layout.preferredHeight = Height;
    }


    private static float GetAspectWidth(Sprite Sprite, float Height)
    {
        if (Sprite == null || Sprite.rect.height <= 0f)
            return Height;

        return Height * Sprite.rect.width / Sprite.rect.height;
    }


    private static Sprite ResolveSegmentSprite(
        int Index,
        int Count,
        bool Full,
        Sprite SingleFull,
        Sprite SingleEmpty,
        Sprite HeadFull,
        Sprite HeadEmpty,
        Sprite MiddleFull,
        Sprite MiddleEmpty,
        Sprite TailFull,
        Sprite TailEmpty)
    {
        if (Count == 1)
            return Full ? SingleFull : SingleEmpty;
        if (Index == 0)
            return Full ? HeadFull : HeadEmpty;
        if (Index == Count - 1)
            return Full ? TailFull : TailEmpty;
        return Full ? MiddleFull : MiddleEmpty;
    }


    private static void SetText(GameObject Root, string Name, string Value)
    {
        Transform Target = Find(Root.transform, Name);
        if (Target != null && Target.TryGetComponent(out TMP_Text Text))
            Text.text = Value ?? string.Empty;
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


    private static void ClearGenerated(RectTransform Parent, GameObject Template)
    {
        if (Parent == null)
            return;

        for (int i = Parent.childCount - 1; i >= 0; i--)
        {
            GameObject Child = Parent.GetChild(i).gameObject;
            if (Child != Template)
                Destroy(Child);
        }
    }
}
