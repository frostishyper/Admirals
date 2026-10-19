using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// Presentation/controller for one Battle engagement. All choices are submitted
// through participant-bound access; the returned resolution trace is read-only.
[DisallowMultipleComponent]
[RequireComponent(typeof(HumanGameplayInput))]
public sealed class BattleEngagementUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject _Root;
    [SerializeField] private GameplayHUD _GameplayHUD;
    [SerializeField] private UnitPresentationCatalog _UnitPresentationCatalog;
    [SerializeField] private TMP_Text _AttackerName;
    [SerializeField] private TMP_Text _AttackerHealth;
    [SerializeField] private TMP_Text _DefenderName;
    [SerializeField] private TMP_Text _DefenderHealth;
    [SerializeField] private TMP_Text _Instruction;
    [SerializeField] private TMP_Text _SelectedAttack;

    [Header("Choices")]
    [SerializeField] private RectTransform _AttackerChoices;
    [SerializeField] private RectTransform _DefenderChoices;
    [SerializeField] private RectTransform _SupportChoices;
    [SerializeField] private GameObject _SupportHeader;
    [SerializeField] private Button _ChoiceTemplate;
    [SerializeField] private Button _AttackerLockButton;
    [SerializeField] private Button _DefenderLockButton;
    [SerializeField] private Button _CancelButton;

    [Header("Resolution")]
    [SerializeField] private RectTransform _ResolutionContent;
    [SerializeField] private TMP_Text _ResolutionLineTemplate;
    [SerializeField] private Animator _AttackerDie;
    [SerializeField] private Animator _DefenderDie;
    [SerializeField] private TMP_Text _AttackerRollText;
    [SerializeField] private TMP_Text _DefenderRollText;
    [SerializeField] private Button _ContinueButton;

    [Header("Action Card Badges")]
    [SerializeField] private Sprite _AirDomainBadge;
    [SerializeField] private Sprite _SurfaceDomainBadge;
    [SerializeField] private Sprite _SubsurfaceDomainBadge;

    [SerializeField, Min(0f)] private float _LineDelaySeconds = 0.8f;
    [SerializeField, Min(0f)] private float _ContinueDelaySeconds = 6f;

    private HumanGameplayInput _Input;
    private GameParticipantAPI.ParticipantAccess _Access;
    private GameBattle.EngagementView _View;
    private GameBattle.CombatActionKind _SelectedKind;
    private int _SelectedSlot = -1;
    private GameBattle.CombatActionKind _SelectedSupportKind;
    private int _SelectedSupportSlot = -1;
    private string _SelectedSupportUnitID = string.Empty;
    private readonly List<GameObject> _GeneratedChoices = new List<GameObject>();
    private readonly List<GameObject> _GeneratedLines = new List<GameObject>();
    private Coroutine _ResolutionRoutine;


    private void Awake()
    {
        _Input = GetComponent<HumanGameplayInput>();
        _Root?.SetActive(false);
        _ChoiceTemplate?.gameObject.SetActive(false);
        _ResolutionLineTemplate?.gameObject.SetActive(false);
        _AttackerLockButton?.onClick.AddListener(LockChoice);
        _DefenderLockButton?.onClick.AddListener(LockChoice);
        _CancelButton?.onClick.AddListener(CancelEngagement);
        _ContinueButton?.onClick.AddListener(ContinueEngagement);
    }


    private IEnumerator Start()
    {
        while (!_Input.Initialized)
            yield return null;
        _Input.ParticipantChanged += HandleParticipantChanged;
        BindAccess();
        Refresh();
    }


    private void BindAccess()
    {
        if (_Access != null)
            _Access.SnapshotChanged -= Refresh;
        _Access = _Input.ParticipantAccess;
        if (_Access != null)
            _Access.SnapshotChanged += Refresh;
    }


    private void HandleParticipantChanged()
    {
        if (_ResolutionRoutine != null)
        {
            StopCoroutine(_ResolutionRoutine);
            _ResolutionRoutine = null;
        }
        ClearGenerated(_GeneratedLines);
        if (_ContinueButton != null)
        {
            _ContinueButton.interactable = false;
            _ContinueButton.gameObject.SetActive(false);
        }
        BindAccess();
        ResetSelection();
        Refresh();
    }


    private void Refresh()
    {
        if (_Access == null || !_Access.TryGetBattleView(out GameBattle.ParticipantView Battle) ||
            Battle?.Engagement == null || !Battle.Engagement.IsActive)
        {
            _Root?.SetActive(false);
            return;
        }

        _View = Battle.Engagement;
        _Root.SetActive(true);
        _GameplayHUD?.SetBattleEngagementVisible(true);
        _AttackerName.text = _View.AttackerInstanceName;
        _AttackerHealth.text = $"HP {_View.AttackerHealth} / {_View.AttackerMaxHealth}";
        _DefenderName.text = _View.DefenderInstanceName;
        _DefenderHealth.text = $"HP {_View.DefenderHealth} / {_View.DefenderMaxHealth}";
        _SelectedAttack.text = _View.AttackerLocked
            ? $"ATTACK: {_View.SelectedAttackName}" : "ATTACK NOT LOCKED";

        if (_View.Resolved)
        {
            _AttackerLockButton?.gameObject.SetActive(false);
            _DefenderLockButton?.gameObject.SetActive(false);
            _CancelButton?.gameObject.SetActive(false);
            if (_ResolutionRoutine == null)
                _ResolutionRoutine = StartCoroutine(PlayResolution(_View.Resolution));
            return;
        }

        RebuildChoices();
        _AttackerLockButton?.gameObject.SetActive(_View.IsAttacker);
        _DefenderLockButton?.gameObject.SetActive(!_View.IsAttacker);
        if (_View.IsAttacker)
        {
            _Instruction.text = "Choose one weapon or offensive ability";
            if (_AttackerLockButton != null)
                _AttackerLockButton.interactable =
                    !_View.AttackerLocked && _SelectedSlot >= 0;
            _CancelButton?.gameObject.SetActive(!_View.AttackerLocked);
        }
        else
        {
            _Instruction.text = "Choose one defense and optional allied support";
            if (_DefenderLockButton != null)
                _DefenderLockButton.interactable = _View.AttackerLocked;
            _CancelButton?.gameObject.SetActive(false);
        }
    }


    private void RebuildChoices()
    {
        ClearGenerated(_GeneratedChoices);
        if (_View.IsAttacker)
        {
            _SupportHeader?.SetActive(false);
            _SupportChoices?.gameObject.SetActive(false);
            GameBattle.CombatActionKind PreviousKind =
                GameBattle.CombatActionKind.None;
            for (int i = 0; i < _View.AttackerOptions.Length; i++)
            {
                GameBattle.ActionOption Option = _View.AttackerOptions[i];
                if (Option.Kind != PreviousKind)
                {
                    CreateSectionHeader(_AttackerChoices,
                        Option.Kind == GameBattle.CombatActionKind.Weapon
                            ? "WEAPONS" : "OFFENSIVE ABILITIES");
                    PreviousKind = Option.Kind;
                }
                CreateChoice(_AttackerChoices, Option, false,
                    string.Empty, string.Empty, _View.AttackerDefinitionID);
            }
            return;
        }

        CreateSectionHeader(_DefenderChoices, "DEFENDER'S OWN ACTION");
        CreateNoDefenseChoice();
        for (int i = 0; i < _View.DefenderOptions.Length; i++)
            CreateChoice(_DefenderChoices, _View.DefenderOptions[i], false,
                string.Empty, string.Empty, _View.DefenderDefinitionID);
        bool HasSupport = _View.AllySupport.Length > 0;
        _SupportHeader?.SetActive(HasSupport);
        _SupportChoices?.gameObject.SetActive(HasSupport);
        for (int g = 0; g < _View.AllySupport.Length; g++)
        {
            GameBattle.AllySupportGroup Group = _View.AllySupport[g];
            for (int i = 0; i < Group.Options.Length; i++)
                CreateChoice(_SupportChoices, Group.Options[i], true,
                    Group.RuntimeUnitID, Group.UnitName, Group.DefinitionID);
        }
    }


    private void CreateSectionHeader(RectTransform Parent, string Value)
    {
        GameObject Object = new GameObject("Section_" + Value,
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI), typeof(LayoutElement));
        Object.layer = Parent.gameObject.layer;
        Object.transform.SetParent(Parent, false);
        TMP_Text Text = Object.GetComponent<TextMeshProUGUI>();
        Text.font = TMP_Settings.defaultFontAsset;
        Text.text = Value;
        Text.fontSize = 15f;
        Text.alignment = TextAlignmentOptions.MidlineLeft;
        Text.textWrappingMode = TextWrappingModes.NoWrap;
        Text.raycastTarget = false;
        LayoutElement Layout = Object.GetComponent<LayoutElement>();
        Layout.minHeight = 24f;
        Layout.preferredHeight = 24f;
        _GeneratedChoices.Add(Object);
    }


    private void CreateNoDefenseChoice()
    {
        Button Button = Instantiate(_ChoiceTemplate, _DefenderChoices);
        Button.gameObject.SetActive(true);
        SetCardText(Button, "Text_ChoiceName", "NO DEFENSE");
        SetCardText(Button, "Text_ChoiceCharges", "CONSERVE CHARGES");
        SetCardText(Button, "Text_ChoiceDetail", "No defender action");
        Button.onClick.AddListener(() =>
        {
            _SelectedKind = GameBattle.CombatActionKind.None;
            _SelectedSlot = -1;
            SetCurrentLockInteractable(true);
        });
        _GeneratedChoices.Add(Button.gameObject);
    }


    private void CreateChoice(RectTransform Parent, GameBattle.ActionOption Option,
        bool Support, string SupportUnitID, string SupportUnitName = "",
        string UnitDefinitionID = "")
    {
        Button Button = Instantiate(_ChoiceTemplate, Parent);
        Button.gameObject.SetActive(true);
        MakeChoiceCardContentDriven(Button);
        SetCardText(Button, "Text_ChoiceName",
            (Support ? $"{SupportUnitName} - " : string.Empty) + Option.Name);
        SetCardText(Button, "Text_ChoiceCharges", Option.MaxCharges > 0
            ? $"CHARGES  {Option.CurrentCharges}/{Option.MaxCharges}"
            : "NO CHARGES");
        SetCardText(Button, "Text_ChoiceDetail",
            Option.UsesD20 ? "D20 OUTCOME" : "FLAT OUTCOME");
        PopulateBadges(Button, UnitDefinitionID, Option);
        Button.onClick.AddListener(() =>
        {
            if (Support)
            {
                _SelectedSupportKind = Option.Kind;
                _SelectedSupportSlot = Option.SlotIndex;
                _SelectedSupportUnitID = SupportUnitID;
            }
            else
            {
                _SelectedKind = Option.Kind;
                _SelectedSlot = Option.SlotIndex;
            }
            SetCurrentLockInteractable(true);
        });
        _GeneratedChoices.Add(Button.gameObject);
    }


    private static void MakeChoiceCardContentDriven(Button Button)
    {
        LayoutElement Card = Button.GetComponent<LayoutElement>();
        if (Card != null)
            Card.preferredHeight = -1f;

        Transform NameTransform = Find(Button.transform, "Text_ChoiceName");
        LayoutElement Name = NameTransform != null
            ? NameTransform.GetComponent<LayoutElement>() : null;
        if (Name != null)
            Name.preferredHeight = -1f;
    }


    private static void SetChoiceLabel(Button Button, string Text)
    {
        TMP_Text Label = Button.GetComponentInChildren<TMP_Text>(true);
        if (Label != null)
            Label.text = Text;
    }


    private static void SetCardText(Button Button, string Name, string Value)
    {
        Transform Target = Find(Button.transform, Name);
        TMP_Text Text = Target != null ? Target.GetComponent<TMP_Text>() : null;
        if (Text != null)
            Text.text = Value;
        else
            SetChoiceLabel(Button, Value);
    }


    private void PopulateBadges(Button Button, string UnitDefinitionID,
        GameBattle.ActionOption Option)
    {
        Transform Strip = Find(Button.transform, "BadgeStrip");
        if (Strip == null)
            return;
        for (int i = Strip.childCount - 1; i >= 0; i--)
            Destroy(Strip.GetChild(i).gameObject);

        Definition_Unit Unit = null;
        _UnitPresentationCatalog?.TryGetPresentation(
            UnitDefinitionID, out Unit, out _);
        if (Unit == null)
            return;

        if (Option.Kind == GameBattle.CombatActionKind.Weapon &&
            Option.SlotIndex >= 0 && Option.SlotIndex < Unit.WeaponSlots.Length)
        {
            Definition_Weapon Weapon = Unit.WeaponSlots[Option.SlotIndex]?.Weapon;
            if (Weapon != null)
            {
                Definition_WeaponType[] Types = Weapon.Types;
                for (int i = 0; Types != null && i < Types.Length; i++)
                    AddBadge(Strip, Types[i]?.Badge, Types[i]?.Name);
                AddDomainBadges(Strip, Weapon.TargetDomains);
            }
        }
        else if (Option.Kind == GameBattle.CombatActionKind.System &&
            Option.SlotIndex >= 0 && Option.SlotIndex < Unit.SystemSlots.Length)
        {
            Definition_System System = Unit.SystemSlots[Option.SlotIndex]?.System;
            AddBadge(Strip, System?.Type?.Badge, System?.Type?.Name);
            if (System != null)
                AddDomainBadges(Strip, System.Domains);
        }
        else if (Option.Kind == GameBattle.CombatActionKind.Ability &&
            Option.SlotIndex >= 0 && Option.SlotIndex < Unit.Abilities.Length)
        {
            Definition_Ability Ability = Unit.Abilities[Option.SlotIndex];
            if (Ability != null)
                AddDomainBadges(Strip, Ability.TargetDomains);
        }
    }


    private void AddDomainBadges(Transform Parent, Domain Domains)
    {
        if ((Domains & Domain.Air) != 0)
            AddBadge(Parent, _AirDomainBadge, "Air");
        if ((Domains & Domain.Surface) != 0)
            AddBadge(Parent, _SurfaceDomainBadge, "Surface");
        if ((Domains & Domain.Subsurface) != 0)
            AddBadge(Parent, _SubsurfaceDomainBadge, "Subsurface");
    }


    private static void AddBadge(Transform Parent, Sprite Sprite, string Name)
    {
        if (Parent == null || Sprite == null)
            return;
        GameObject Badge = new GameObject($"Badge_{Name}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(LayoutElement));
        Badge.layer = Parent.gameObject.layer;
        Badge.transform.SetParent(Parent, false);
        Image Image = Badge.GetComponent<Image>();
        Image.sprite = Sprite;
        Image.preserveAspect = true;
        Image.raycastTarget = false;
        LayoutElement Layout = Badge.GetComponent<LayoutElement>();
        float Aspect = Sprite.rect.height > 0f
            ? Sprite.rect.width / Sprite.rect.height : 1f;
        Layout.minHeight = 24f;
        Layout.preferredHeight = 24f;
        Layout.minWidth = 24f * Aspect;
        Layout.preferredWidth = 24f * Aspect;
        Layout.flexibleWidth = 0f;
    }


    private void LockChoice()
    {
        if (_View == null)
            return;
        if (_View.IsAttacker)
        {
            if (_View.AttackerLocked || _SelectedSlot < 0)
                return;
            string Message = "The local participant cannot lock this attack.";
            if (_Access == null ||
                !_Access.TryLockBattleAttack(_SelectedKind, _SelectedSlot,
                    out Message))
                Debug.LogWarning(Message, this);
            return;
        }

        if (!_View.AttackerLocked || _View.DefenderLocked)
            return;
        string DefenseMessage = "The local participant cannot lock this defense.";
        if (_Access == null ||
            !_Access.TryLockBattleDefense(_SelectedKind, _SelectedSlot,
                _SelectedSupportUnitID, _SelectedSupportKind,
                _SelectedSupportSlot, out _, out DefenseMessage))
        {
            Debug.LogWarning(DefenseMessage, this);
            return;
        }
        if (_DefenderLockButton != null)
            _DefenderLockButton.interactable = false;
    }


    private void CancelEngagement()
    {
        string Message = "The local participant cannot cancel this engagement.";
        if (_Access == null || !_Access.TryCancelEngagement(out Message))
            Debug.LogWarning(Message, this);
    }


    private void ContinueEngagement()
    {
        string Message = "The local participant cannot continue this engagement.";
        if (_Access == null || !_Access.TryContinueEngagement(out Message))
            Debug.LogWarning(Message, this);
        else
            ResetEngagementState();
    }


    private Button CurrentLockButton() =>
        _View != null && _View.IsAttacker
            ? _AttackerLockButton : _DefenderLockButton;


    private void SetCurrentLockInteractable(bool Interactable)
    {
        Button Button = CurrentLockButton();
        if (Button != null)
            Button.interactable = Interactable;
    }


    private IEnumerator PlayResolution(GameBattle.ResolutionView Resolution)
    {
        ClearGenerated(_GeneratedLines);
        _ContinueButton.gameObject.SetActive(true);
        _ContinueButton.interactable = false;
        _AttackerRollText.text = string.Empty;
        _DefenderRollText.text = string.Empty;
        if (_AttackerDie != null)
            _AttackerDie.Play(0, 0, 0f);
        if (_DefenderDie != null)
            _DefenderDie.Play(0, 0, 0f);
        yield return new WaitForSecondsRealtime(1f);
        _AttackerRollText.text = Resolution.AttackerRoll > 0
            ? Resolution.AttackerRoll.ToString() : "FLAT";
        _DefenderRollText.text = Resolution.AllyRoll > 0
            ? Resolution.AllyRoll.ToString() :
              Resolution.DefenderRoll > 0 ? Resolution.DefenderRoll.ToString() : "FLAT";

        // The defender-side die is deliberately reused when both allied support
        // and the actual defender require rolls.
        if (Resolution.AllyRoll > 0 && Resolution.DefenderRoll > 0)
        {
            yield return new WaitForSecondsRealtime(0.35f);
            _DefenderRollText.text = string.Empty;
            if (_DefenderDie != null)
                _DefenderDie.Play(0, 0, 0f);
            yield return new WaitForSecondsRealtime(1f);
            _DefenderRollText.text = Resolution.DefenderRoll.ToString();
        }

        for (int i = 0; i < Resolution.Events.Length; i++)
        {
            TMP_Text Line = Instantiate(_ResolutionLineTemplate, _ResolutionContent);
            Line.gameObject.SetActive(true);
            Line.text = $"{Resolution.Events[i].Label}: {Resolution.Events[i].Text}";
            Line.ForceMeshUpdate();
            LayoutElement LineLayout = Line.GetComponent<LayoutElement>();
            if (LineLayout != null)
            {
                float AvailableWidth = Mathf.Max(1f, _ResolutionContent.rect.width);
                float PreferredHeight = Line.GetPreferredValues(
                    Line.text, AvailableWidth, 0f).y;
                LineLayout.minHeight = PreferredHeight + 8f;
                LineLayout.preferredHeight = PreferredHeight + 8f;
                LineLayout.flexibleHeight = 0f;
            }
            _GeneratedLines.Add(Line.gameObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_ResolutionContent);
            yield return new WaitForSecondsRealtime(_LineDelaySeconds);
        }
        yield return new WaitForSecondsRealtime(_ContinueDelaySeconds);
        _ContinueButton.interactable = true;
        _ResolutionRoutine = null;
    }


    private void ResetSelection()
    {
        _SelectedKind = GameBattle.CombatActionKind.None;
        _SelectedSlot = -1;
        _SelectedSupportKind = GameBattle.CombatActionKind.None;
        _SelectedSupportSlot = -1;
        _SelectedSupportUnitID = string.Empty;
    }


    private void ResetEngagementState()
    {
        ResetSelection();
        _Root?.SetActive(false);
        ClearGenerated(_GeneratedChoices);
        ClearGenerated(_GeneratedLines);
    }


    private static void ClearGenerated(List<GameObject> Objects)
    {
        for (int i = 0; i < Objects.Count; i++)
            if (Objects[i] != null)
                Destroy(Objects[i]);
        Objects.Clear();
    }


    private static Transform Find(Transform Root, string Name)
    {
        if (Root == null || Root.name == Name)
            return Root;
        for (int i = 0; i < Root.childCount; i++)
        {
            Transform Found = Find(Root.GetChild(i), Name);
            if (Found != null)
                return Found;
        }
        return null;
    }


    private void OnDestroy()
    {
        if (_Input != null)
            _Input.ParticipantChanged -= HandleParticipantChanged;
        if (_Access != null)
            _Access.SnapshotChanged -= Refresh;
    }
}
