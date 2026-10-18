using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// Presentation interface for the in-match HUD.
//
// Gameplay systems tell this component what should be displayed.
// This component does not own Deployment gameplay rules.
[DisallowMultipleComponent]
public sealed class GameplayHUD :
    MonoBehaviour
{
    [Header("Shared Regions")]
    [SerializeField]
    private GameObject _BottomBarRoot;


    [Header("Deployment - Top")]
    [SerializeField]
    private GameObject _DeploymentTopRoot;

    [SerializeField]
    private TMP_Text _PlayerNameText;

    [SerializeField]
    private TMP_Text _PhaseText;

    [SerializeField]
    private TMP_Text _PlacedCountText;

    [SerializeField]
    private Button _ResetButton;

    [SerializeField]
    private Button _ConfirmButton;


    [Header("Deployment - Overlay")]
    [SerializeField]
    private GameObject _DeploymentOverlayRoot;

    [SerializeField]
    private Button _RotateLeftButton;

    [SerializeField]
    private Button _RotateRightButton;


    [Header("Deployment - Bottom")]
    [SerializeField]
    private GameObject _DeploymentBottomRoot;

    [SerializeField]
    private ScrollRect _DeploymentTray;

    [SerializeField]
    private RectTransform _DeploymentTrayContent;


    [Header("Movement - Top")]
    [SerializeField]
    private GameObject _MovementTopRoot;

    [SerializeField]
    private TMP_Text _MovementPlayerNameText;

    [SerializeField]
    private TMP_Text _MovementPhaseText;

    [SerializeField]
    private TMP_Text _MovementCountText;

    [SerializeField]
    private TMP_Text _MovementTimerText;

    [SerializeField]
    private Button _EndMovementButton;


    [Header("Movement - Overlay")]
    [SerializeField]
    private GameObject _MovementOverlayRoot;

    [SerializeField]
    private Button _MoveUpButton;

    [SerializeField]
    private Button _MoveDownButton;

    [SerializeField]
    private Button _MoveLeftButton;

    [SerializeField]
    private Button _MoveRightButton;

    [SerializeField]
    private Button _MovementRotateLeftButton;

    [SerializeField]
    private Button _MovementRotateRightButton;

    [SerializeField]
    private Button _ConfirmMovementButton;

    [SerializeField]
    private Button _CancelMovementButton;


    [Header("Movement - Bottom")]
    [SerializeField]
    private GameObject _MovementBottomRoot;

    [SerializeField]
    private MovementUnitDetailsUI _MovementUnitDetails;


    [Header("Pre-Battle - Top")]
    [SerializeField]
    private GameObject _PreBattleTopRoot;

    [SerializeField]
    private TMP_Text _PreBattlePlayerNameText;

    [SerializeField]
    private TMP_Text _PreBattlePhaseText;

    [SerializeField]
    private TMP_Text _PreBattleActionCountText;

    [SerializeField]
    private TMP_Text _PreBattleTimerText;

    [SerializeField]
    private Button _EndPreBattleButton;


    [Header("Pre-Battle - Overlay")]
    [SerializeField]
    private GameObject _PreBattleOverlayRoot;

    [SerializeField]
    private Button _PreBattleRotateLeftButton;

    [SerializeField]
    private Button _PreBattleRotateRightButton;

    [SerializeField]
    private Button _ConfirmPreBattleActionButton;

    [SerializeField]
    private Button _CancelPreBattleActionButton;


    [Header("Pre-Battle - Bottom")]
    [SerializeField]
    private GameObject _PreBattleBottomRoot;

    [SerializeField]
    private MovementUnitDetailsUI _PreBattleUnitDetails;


    [Header("Search - Top")]
    [SerializeField] private GameObject _SearchTopRoot;
    [SerializeField] private TMP_Text _SearchPlayerNameText;
    [SerializeField] private TMP_Text _SearchPhaseText;
    [SerializeField] private TMP_Text _SearchCountText;
    [SerializeField] private TMP_Text _SearchTimerText;
    [SerializeField] private Button _EndSearchButton;

    [Header("Search - Overlay")]
    [SerializeField] private GameObject _SearchOverlayRoot;
    [SerializeField] private Button _SearchRotateLeftButton;
    [SerializeField] private Button _SearchRotateRightButton;
    [SerializeField] private Button _CommitSearchButton;

    [Header("Search - Bottom")]
    [SerializeField] private GameObject _SearchBottomRoot;
    [SerializeField] private MovementUnitDetailsUI _SearchUnitDetails;

    [Header("Battle - Top")]
    [SerializeField] private GameObject _BattleTopRoot;
    [SerializeField] private TMP_Text _BattlePlayerNameText;
    [SerializeField] private TMP_Text _BattlePhaseText;
    [SerializeField] private TMP_Text _BattleCountText;
    [SerializeField] private TMP_Text _BattleTimerText;
    [SerializeField] private Button _EndBattleButton;

    [Header("Battle - Overlay")]
    [SerializeField] private GameObject _BattleOverlayRoot;
    [SerializeField] private Button _BattlePreviousDomainButton;
    [SerializeField] private Button _BattleNextDomainButton;
    [SerializeField] private Button _BattleAirDomainButton;
    [SerializeField] private Button _BattleSurfaceDomainButton;
    [SerializeField] private Button _BattleSubsurfaceDomainButton;
    [SerializeField] private Button _BattleDeselectButton;
    [SerializeField] private Button _EngageButton;
    [SerializeField] private TMP_Text _BattleDomainText;
    [SerializeField] private TMP_Text _BattleTargetText;

    [Header("Battle - Bottom")]
    [SerializeField] private GameObject _BattleBottomRoot;
    [SerializeField] private MovementUnitDetailsUI _BattleUnitDetails;


    private bool _ShowingMovement;
    private bool _ShowingPreBattle;
    private bool _ShowingSearch;
    private bool _ShowingBattle;
    private bool _MovementSelectionVisible;
    private bool _PreBattleTargetingVisible;


    public Button ResetButton
    {
        get
        {
            return _ResetButton;
        }
    }


    public Button ConfirmButton
    {
        get
        {
            return _ConfirmButton;
        }
    }


    public Button RotateLeftButton
    {
        get
        {
            return _RotateLeftButton;
        }
    }


    public Button RotateRightButton
    {
        get
        {
            return _RotateRightButton;
        }
    }


    public RectTransform DeploymentTrayContent
    {
        get
        {
            return _DeploymentTrayContent;
        }
    }


    public Button MoveUpButton => _MoveUpButton;
    public Button MoveDownButton => _MoveDownButton;
    public Button MoveLeftButton => _MoveLeftButton;
    public Button MoveRightButton => _MoveRightButton;
    public Button MovementRotateLeftButton => _MovementRotateLeftButton;
    public Button MovementRotateRightButton => _MovementRotateRightButton;
    public Button ConfirmMovementButton => _ConfirmMovementButton;
    public Button CancelMovementButton => _CancelMovementButton;
    public Button EndMovementButton => _EndMovementButton;
    public Button PreBattleRotateLeftButton => _PreBattleRotateLeftButton;
    public Button PreBattleRotateRightButton => _PreBattleRotateRightButton;
    public Button ConfirmPreBattleActionButton => _ConfirmPreBattleActionButton;
    public Button CancelPreBattleActionButton => _CancelPreBattleActionButton;
    public Button EndPreBattleButton => _EndPreBattleButton;
    public Button SearchRotateLeftButton => _SearchRotateLeftButton;
    public Button SearchRotateRightButton => _SearchRotateRightButton;
    public Button CommitSearchButton => _CommitSearchButton;
    public Button EndSearchButton => _EndSearchButton;
    public Button BattlePreviousDomainButton => _BattlePreviousDomainButton;
    public Button BattleNextDomainButton => _BattleNextDomainButton;
    public Button BattleAirDomainButton => _BattleAirDomainButton;
    public Button BattleSurfaceDomainButton => _BattleSurfaceDomainButton;
    public Button BattleSubsurfaceDomainButton => _BattleSubsurfaceDomainButton;
    public Button BattleDeselectButton => _BattleDeselectButton;
    public Button EngageButton => _EngageButton;
    public Button EndBattleButton => _EndBattleButton;


    public void BindMovementUnitDetails(
        ParticipantUnitSnapshot Unit,
        Definition_Unit Definition,
        UnitPresentationCatalog PresentationCatalog,
        GameplayPhase Phase,
        bool CanAct,
        Action<int, int> UseSystem,
        Action<int> UseAbility,
        Action<int> SelectSensor = null,
        int SelectedSensorSlotIndex = -1)
    {
        MovementUnitDetailsUI Details =
            Phase == GameplayPhase.PreBattle ? _PreBattleUnitDetails :
            Phase == GameplayPhase.Search ? _SearchUnitDetails :
            Phase == GameplayPhase.Battle ? _BattleUnitDetails :
            _MovementUnitDetails;

        if (Details != null)
        {
            Details.Bind(
                Unit,
                Definition,
                PresentationCatalog,
                Phase,
                CanAct,
                UseSystem,
                UseAbility,
                SelectSensor,
                SelectedSensorSlotIndex
            );
        }
    }


    public void ClearMovementUnitDetails()
    {
        if (_MovementUnitDetails != null)
        {
            _MovementUnitDetails.Clear();
        }

        if (_PreBattleUnitDetails != null)
        {
            _PreBattleUnitDetails.Clear();
        }

        if (_SearchUnitDetails != null)
            _SearchUnitDetails.Clear();

        if (_BattleUnitDetails != null)
            _BattleUnitDetails.Clear();
    }


    public void SetPlayerName(
        string PlayerName
    )
    {
        string DisplayName =
            string.IsNullOrWhiteSpace(
                PlayerName)
                ? string.Empty
                : PlayerName;


        if (_PlayerNameText != null)
        {
            _PlayerNameText.text =
                DisplayName;
        }


        if (_MovementPlayerNameText != null)
        {
            _MovementPlayerNameText.text =
                DisplayName;
        }

        if (_PreBattlePlayerNameText != null)
        {
            _PreBattlePlayerNameText.text = DisplayName;
        }

        if (_SearchPlayerNameText != null)
            _SearchPlayerNameText.text = DisplayName;

        if (_BattlePlayerNameText != null)
            _BattlePlayerNameText.text = DisplayName;
    }


    public void ShowDeploymentPhase()
    {
        _ShowingMovement =
            false;

        _ShowingPreBattle =
            false;

        _ShowingSearch = false;
        _ShowingBattle = false;

        _MovementSelectionVisible =
            false;

        SetObjectVisible(_DeploymentTopRoot, true);
        SetObjectVisible(_DeploymentOverlayRoot, true);
        SetObjectVisible(_DeploymentBottomRoot, true);
        SetObjectVisible(_MovementTopRoot, false);
        SetObjectVisible(_MovementOverlayRoot, false);
        SetObjectVisible(_MovementBottomRoot, false);
        SetObjectVisible(_PreBattleTopRoot, false);
        SetObjectVisible(_PreBattleOverlayRoot, false);
        SetObjectVisible(_PreBattleBottomRoot, false);
        HideSearchRoots();
        HideBattleRoots();
        SetObjectVisible(_BottomBarRoot, true);

        SetDeploymentObjectsVisible(
            true
        );


        if (_PhaseText != null)
        {
            _PhaseText.text =
                "0 Deployment";
        }
    }


    public void ShowMovementPhase()
    {
        _ShowingMovement =
            true;

        _ShowingPreBattle =
            false;

        _ShowingSearch = false;
        _ShowingBattle = false;

        SetObjectVisible(_DeploymentTopRoot, false);
        SetObjectVisible(_DeploymentOverlayRoot, false);
        SetObjectVisible(_DeploymentBottomRoot, false);
        SetObjectVisible(_MovementTopRoot, true);
        SetObjectVisible(_PreBattleTopRoot, false);
        SetObjectVisible(_PreBattleOverlayRoot, false);
        SetObjectVisible(_PreBattleBottomRoot, false);
        HideSearchRoots();
        HideBattleRoots();
        ApplyMovementSelectionVisibility();

        SetDeploymentObjectsVisible(
            false
        );


        if (_MovementPhaseText != null)
        {
            _MovementPhaseText.text =
                "1 Movement";
        }


        SetEndButtonLabel("END MOVEMENT");
        SetButtonLabel(_ConfirmMovementButton, "Move");
        SetMovementOverlayColumnsVisible(true);


        SetObjectVisible(
            _MovementCountText,
            true
        );

        SetObjectVisible(
            _MovementTimerText,
            true
        );

        SetObjectVisible(
            _EndMovementButton,
            true
        );

        SetObjectVisible(_MoveUpButton, true);
        SetObjectVisible(_MoveDownButton, true);
        SetObjectVisible(_MoveLeftButton, true);
        SetObjectVisible(_MoveRightButton, true);
        SetObjectVisible(_MovementRotateLeftButton, true);
        SetObjectVisible(_MovementRotateRightButton, true);
    }


    public void ShowPreBattlePhase()
    {
        _ShowingMovement = false;
        _ShowingPreBattle = true;
        _ShowingSearch = false;
        _ShowingBattle = false;

        SetObjectVisible(_DeploymentTopRoot, false);
        SetObjectVisible(_DeploymentOverlayRoot, false);
        SetObjectVisible(_DeploymentBottomRoot, false);
        SetObjectVisible(_MovementTopRoot, false);
        SetObjectVisible(_MovementOverlayRoot, false);
        SetObjectVisible(_MovementBottomRoot, false);
        SetObjectVisible(_PreBattleTopRoot, true);
        HideSearchRoots();
        HideBattleRoots();
        SetDeploymentObjectsVisible(false);
        ApplyMovementSelectionVisibility();

        if (_PreBattlePhaseText != null)
            _PreBattlePhaseText.text = "2 Pre-Battle";

        SetButtonLabel(_EndPreBattleButton, "END PRE-BATTLE");
        SetButtonLabel(_ConfirmPreBattleActionButton, "Deploy");
        ConfigureSingleLineTopText(_PreBattlePhaseText);
        ConfigureSingleLineTopText(_PreBattleActionCountText);
        ConfigureSingleLineTopText(_PreBattleTimerText);
        ConfigureTopBarRightSpacing(_PreBattleActionCountText);
        ConfigureContentWidthButton(_EndPreBattleButton);
        SetObjectVisible(_PreBattleActionCountText, true);
        SetObjectVisible(_PreBattleTimerText, true);
        SetObjectVisible(_EndPreBattleButton, true);
    }


    public void ShowSearchPhase()
    {
        _ShowingMovement = false;
        _ShowingPreBattle = false;
        _ShowingSearch = true;
        _ShowingBattle = false;

        SetObjectVisible(_DeploymentTopRoot, false);
        SetObjectVisible(_DeploymentOverlayRoot, false);
        SetObjectVisible(_DeploymentBottomRoot, false);
        SetObjectVisible(_MovementTopRoot, false);
        SetObjectVisible(_MovementOverlayRoot, false);
        SetObjectVisible(_MovementBottomRoot, false);
        SetObjectVisible(_PreBattleTopRoot, false);
        SetObjectVisible(_PreBattleOverlayRoot, false);
        SetObjectVisible(_PreBattleBottomRoot, false);
        SetObjectVisible(_SearchTopRoot, true);
        SetObjectVisible(_SearchOverlayRoot, false);
        SetObjectVisible(_SearchBottomRoot, false);
        SetObjectVisible(_BottomBarRoot, false);
        HideBattleRoots();
        SetDeploymentObjectsVisible(false);

        if (_SearchPhaseText != null)
            _SearchPhaseText.text = "3 Search";
        SetButtonLabel(_EndSearchButton, "END SEARCH");
        SetButtonLabel(_CommitSearchButton, "SEARCH");
        ConfigureSingleLineTopText(_SearchPhaseText);
        ConfigureSingleLineTopText(_SearchCountText);
        ConfigureSingleLineTopText(_SearchTimerText);
        ConfigureTopBarRightSpacing(_SearchCountText);
        ConfigureContentWidthButton(_EndSearchButton);
    }


    public void SetSearchStatus(int SearchesCommitted, int SearchLimit,
        float SecondsRemaining, bool CanEndSearch)
    {
        if (_SearchCountText != null)
        {
            _SearchCountText.text =
                $"Searched {Mathf.Max(0, SearchesCommitted)} / {Mathf.Max(0, SearchLimit)}";
            ConfigureSingleLineTopText(_SearchCountText);
        }
        if (_SearchTimerText != null)
        {
            int TotalSeconds = Mathf.Max(0, Mathf.CeilToInt(SecondsRemaining));
            _SearchTimerText.text = $"{TotalSeconds / 60:00}:{TotalSeconds % 60:00}";
            ConfigureSingleLineTopText(_SearchTimerText);
        }
        SetButtonInteractable(_EndSearchButton, CanEndSearch);
    }


    public void SetSearchSelectionVisible(bool UnitSelected, bool SensorSelected)
    {
        if (!_ShowingSearch)
            return;
        SetObjectVisible(_SearchBottomRoot, UnitSelected);
        SetObjectVisible(_SearchOverlayRoot, SensorSelected);
        SetObjectVisible(_BottomBarRoot, UnitSelected);
    }


    public void SetSearchControls(bool CanAdjust, bool CanCommit)
    {
        SetButtonInteractable(_SearchRotateLeftButton, CanAdjust);
        SetButtonInteractable(_SearchRotateRightButton, CanAdjust);
        SetButtonInteractable(_CommitSearchButton, CanCommit);
    }


    private void HideSearchRoots()
    {
        SetObjectVisible(_SearchTopRoot, false);
        SetObjectVisible(_SearchOverlayRoot, false);
        SetObjectVisible(_SearchBottomRoot, false);
    }


    public void ShowBattlePhase()
    {
        _ShowingMovement = false;
        _ShowingPreBattle = false;
        _ShowingSearch = false;
        _ShowingBattle = true;

        SetObjectVisible(_DeploymentTopRoot, false);
        SetObjectVisible(_DeploymentOverlayRoot, false);
        SetObjectVisible(_DeploymentBottomRoot, false);
        SetObjectVisible(_MovementTopRoot, false);
        SetObjectVisible(_MovementOverlayRoot, false);
        SetObjectVisible(_MovementBottomRoot, false);
        SetObjectVisible(_PreBattleTopRoot, false);
        SetObjectVisible(_PreBattleOverlayRoot, false);
        SetObjectVisible(_PreBattleBottomRoot, false);
        HideSearchRoots();
        SetObjectVisible(_BattleTopRoot, true);
        SetObjectVisible(_BattleOverlayRoot, false);
        SetObjectVisible(_BattleBottomRoot, false);
        SetObjectVisible(_BottomBarRoot, false);
        SetDeploymentObjectsVisible(false);

        if (_BattlePhaseText != null)
            _BattlePhaseText.text = "4 Battle";
        SetButtonLabel(_EndBattleButton, "END BATTLE");
        SetButtonLabel(_EngageButton, "ENGAGE");
        ConfigureSingleLineTopText(_BattlePhaseText);
        ConfigureSingleLineTopText(_BattleCountText);
        ConfigureSingleLineTopText(_BattleTimerText);
        ConfigureTopBarRightSpacing(_BattleCountText);
        ConfigureContentWidthButton(_EndBattleButton);
    }


    public void SetBattleStatus(int AttacksCommitted, float SecondsRemaining,
        bool CanEndBattle)
    {
        if (_BattleCountText != null)
        {
            _BattleCountText.text = $"Engagements {Mathf.Max(0, AttacksCommitted)}";
            ConfigureSingleLineTopText(_BattleCountText);
        }
        if (_BattleTimerText != null)
        {
            int TotalSeconds = Mathf.Max(0, Mathf.CeilToInt(SecondsRemaining));
            _BattleTimerText.text = $"{TotalSeconds / 60:00}:{TotalSeconds % 60:00}";
            ConfigureSingleLineTopText(_BattleTimerText);
        }
        SetButtonInteractable(_EndBattleButton, CanEndBattle);
    }


    public void SetBattleSelectionVisible(bool UnitSelected)
    {
        if (!_ShowingBattle)
            return;
        SetObjectVisible(_BattleOverlayRoot, UnitSelected);
        SetObjectVisible(_BattleBottomRoot, UnitSelected);
        SetObjectVisible(_BottomBarRoot, UnitSelected);
    }


    public void SetBattleEngagementVisible(bool Visible)
    {
        if (!_ShowingBattle || !Visible)
            return;
        SetObjectVisible(_BattleOverlayRoot, false);
        SetObjectVisible(_BattleBottomRoot, false);
        SetObjectVisible(_BottomBarRoot, false);
    }


    public void SetBattleControls(Domain Domain, bool CanChangeDomain,
        GameBattle.TargetView Target)
    {
        if (_BattleDomainText != null)
            _BattleDomainText.text = Domain == Domain.None
                ? "NO FIRE CONTROL" : Domain.ToString().ToUpperInvariant();
        if (_BattleTargetText != null)
            _BattleTargetText.text = Target == null
                ? "TARGET\nSelect a valid contact or visible enemy"
                : $"TARGET - {Target.Domain.ToString().ToUpperInvariant()}\n" +
                  $"{(Target.IsAbsoluteVisible ? "Enemy in absolute vision" : "Acquired contact")}\n" +
                  $"Weapons able to engage: {Target.LegalWeaponCount}" +
                  (Target.LegalAbilityCount > 0
                      ? $" - Abilities: {Target.LegalAbilityCount}" : string.Empty);
        SetButtonInteractable(_BattlePreviousDomainButton, CanChangeDomain);
        SetButtonInteractable(_BattleNextDomainButton, CanChangeDomain);
        SetButtonInteractable(_BattleAirDomainButton,
            CanChangeDomain && Domain != Domain.Air);
        SetButtonInteractable(_BattleSurfaceDomainButton,
            CanChangeDomain && Domain != Domain.Surface);
        SetButtonInteractable(_BattleSubsurfaceDomainButton,
            CanChangeDomain && Domain != Domain.Subsurface);
        SetButtonInteractable(_BattleDeselectButton, Target != null);
        SetButtonInteractable(_EngageButton, Target != null);
    }


    private void HideBattleRoots()
    {
        SetObjectVisible(_BattleTopRoot, false);
        SetObjectVisible(_BattleOverlayRoot, false);
        SetObjectVisible(_BattleBottomRoot, false);
    }


    public void SetPreBattleStatus(
        int ActionsCommitted,
        float SecondsRemaining,
        bool CanAct
    )
    {
        if (_PreBattleActionCountText != null)
        {
            _PreBattleActionCountText.text = $"Actions {Mathf.Max(0, ActionsCommitted)}";
            ConfigureSingleLineTopText(_PreBattleActionCountText);
        }

        if (_PreBattleTimerText != null)
        {
            int TotalSeconds = Mathf.Max(0, Mathf.CeilToInt(SecondsRemaining));
            _PreBattleTimerText.text = $"{TotalSeconds / 60:00}:{TotalSeconds % 60:00}";
            ConfigureSingleLineTopText(_PreBattleTimerText);
        }

        if (_EndPreBattleButton != null)
            _EndPreBattleButton.interactable = CanAct;
    }


    public void SetPreBattleSelectionVisible(
        bool UnitSelected,
        bool Targeting,
        bool AllowRotation = false
    )
    {
        _MovementSelectionVisible = UnitSelected;
        _PreBattleTargetingVisible = Targeting;
        ApplyMovementSelectionVisibility();

        SetObjectVisible(_MoveUpButton, !_ShowingPreBattle);
        SetObjectVisible(_MoveDownButton, !_ShowingPreBattle);
        SetObjectVisible(_MoveLeftButton, !_ShowingPreBattle);
        SetObjectVisible(_MoveRightButton, !_ShowingPreBattle);
        SetObjectVisible(_PreBattleRotateLeftButton, AllowRotation);
        SetObjectVisible(_PreBattleRotateRightButton, AllowRotation);

        if (_ShowingPreBattle && _PreBattleOverlayRoot != null)
        {
            Transform RotateColumn = FindDescendant(
                _PreBattleOverlayRoot.transform,
                "RotateColumn"
            );

            if (RotateColumn != null)
                RotateColumn.gameObject.SetActive(AllowRotation);
        }
    }


    public void SetMovementStatus(
        int MovesCommitted,
        int MovementLimit,
        float SecondsRemaining,
        bool CanAct
    )
    {
        if (_MovementCountText != null)
        {
            _MovementCountText.text =
                $"Moved {Mathf.Max(0, MovesCommitted)} / {Mathf.Max(0, MovementLimit)}";
        }


        if (_MovementTimerText != null)
        {
            int TotalSeconds =
                Mathf.Max(
                    0,
                    Mathf.CeilToInt(
                        SecondsRemaining
                    )
                );

            _MovementTimerText.text =
                $"{TotalSeconds / 60:00}:{TotalSeconds % 60:00}";
        }


        if (_EndMovementButton != null)
        {
            _EndMovementButton.interactable =
                CanAct;
        }
    }


    public void SetMovementSelectionVisible(
        bool Visible
    )
    {
        _MovementSelectionVisible =
            Visible;

        ApplyMovementSelectionVisibility();
    }


    private void ApplyMovementSelectionVisibility()
    {
        if (!_ShowingMovement && !_ShowingPreBattle)
        {
            return;
        }


        bool Visible =
            _MovementSelectionVisible;

        SetObjectVisible(_MovementOverlayRoot, _ShowingMovement && Visible);
        SetObjectVisible(_MovementBottomRoot, _ShowingMovement && Visible);
        SetObjectVisible(_PreBattleOverlayRoot,
            _ShowingPreBattle && _PreBattleTargetingVisible);
        SetObjectVisible(_PreBattleBottomRoot, _ShowingPreBattle && Visible);
        SetObjectVisible(_BottomBarRoot, Visible);
    }


    private void SetEndButtonLabel(string Label)
    {
        SetButtonLabel(_EndMovementButton, Label);
    }


    private static void SetButtonLabel(
        Button Button,
        string Label
    )
    {
        if (Button == null)
            return;

        TMP_Text Text = Button.GetComponentInChildren<TMP_Text>(true);
        if (Text != null)
            Text.text = Label;
    }


    private static void ConfigureSingleLineTopText(TMP_Text Text)
    {
        if (Text == null)
            return;

        Text.textWrappingMode = TextWrappingModes.NoWrap;
        Text.overflowMode = TextOverflowModes.Overflow;

        LayoutElement Layout = Text.GetComponent<LayoutElement>();
        if (Layout == null)
            return;

        float PreferredWidth = Mathf.Ceil(Text.GetPreferredValues(Text.text).x);
        bool LayoutChanged =
            !Mathf.Approximately(Layout.minWidth, PreferredWidth) ||
            !Mathf.Approximately(Layout.preferredWidth, PreferredWidth) ||
            !Mathf.Approximately(Layout.flexibleWidth, 0f);
        Layout.minWidth = PreferredWidth;
        Layout.preferredWidth = PreferredWidth;
        Layout.flexibleWidth = 0f;

        RectTransform Parent = Text.rectTransform.parent as RectTransform;
        if (LayoutChanged && Parent != null)
            LayoutRebuilder.MarkLayoutForRebuild(Parent);
    }


    private static void ConfigureContentWidthButton(Button Button)
    {
        if (Button == null)
            return;

        TMP_Text Label = Button.GetComponentInChildren<TMP_Text>(true);
        if (Label == null)
            return;

        Label.textWrappingMode = TextWrappingModes.NoWrap;
        Label.overflowMode = TextOverflowModes.Overflow;

        LayoutElement Layout = Button.GetComponent<LayoutElement>();
        if (Layout == null)
            Layout = Button.gameObject.AddComponent<LayoutElement>();

        const float HorizontalPadding = 32f;
        float PreferredWidth =
            Mathf.Ceil(Label.GetPreferredValues(Label.text).x) + HorizontalPadding;
        bool LayoutChanged =
            !Mathf.Approximately(Layout.minWidth, PreferredWidth) ||
            !Mathf.Approximately(Layout.preferredWidth, PreferredWidth) ||
            !Mathf.Approximately(Layout.flexibleWidth, 0f);
        Layout.minWidth = PreferredWidth;
        Layout.preferredWidth = PreferredWidth;
        Layout.flexibleWidth = 0f;

        RectTransform Parent = Button.transform.parent as RectTransform;
        if (LayoutChanged && Parent != null)
            LayoutRebuilder.MarkLayoutForRebuild(Parent);
    }


    private static void ConfigureTopBarRightSpacing(TMP_Text CountText)
    {
        if (CountText == null)
            return;

        HorizontalLayoutGroup Layout =
            CountText.transform.parent != null
                ? CountText.transform.parent.GetComponent<HorizontalLayoutGroup>()
                : null;
        if (Layout == null || Mathf.Approximately(Layout.spacing, 24f))
            return;

        Layout.spacing = 24f;
        LayoutRebuilder.MarkLayoutForRebuild(Layout.transform as RectTransform);
    }


    private void SetMovementOverlayColumnsVisible(bool Visible)
    {
        if (_MovementOverlayRoot == null)
            return;

        Transform MoveColumn = FindDescendant(
            _MovementOverlayRoot.transform,
            "MoveColumn"
        );
        Transform RotateColumn = FindDescendant(
            _MovementOverlayRoot.transform,
            "RotateColumn"
        );

        if (MoveColumn != null)
            MoveColumn.gameObject.SetActive(Visible);
        if (RotateColumn != null)
            RotateColumn.gameObject.SetActive(Visible);
    }


    private static Transform FindDescendant(
        Transform Root,
        string Name
    )
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


    public void SetMovementSelectionControls(
        bool CanAdjust,
        bool CanConfirm
    )
    {
        if (_ShowingPreBattle)
        {
            SetButtonInteractable(_PreBattleRotateLeftButton, CanAdjust);
            SetButtonInteractable(_PreBattleRotateRightButton, CanAdjust);
            SetButtonInteractable(_CancelPreBattleActionButton, CanAdjust);
            SetButtonInteractable(_ConfirmPreBattleActionButton, CanConfirm);
            return;
        }

        SetButtonInteractable(_MoveUpButton, CanAdjust);
        SetButtonInteractable(_MoveDownButton, CanAdjust);
        SetButtonInteractable(_MoveLeftButton, CanAdjust);
        SetButtonInteractable(_MoveRightButton, CanAdjust);
        SetButtonInteractable(_MovementRotateLeftButton, CanAdjust);
        SetButtonInteractable(_MovementRotateRightButton, CanAdjust);
        SetButtonInteractable(_CancelMovementButton, CanAdjust);
        SetButtonInteractable(_ConfirmMovementButton, CanConfirm);
    }


    public void SetDeploymentPlacedCount(
        int Placed,
        int Total
    )
    {
        if (_PlacedCountText == null)
        {
            return;
        }


        int SafeTotal =
            Mathf.Max(
                0,
                Total
            );


        int SafePlaced =
            Mathf.Clamp(
                Placed,
                0,
                SafeTotal
            );


        _PlacedCountText.text =
            $"{SafePlaced} / {SafeTotal}";
    }


    public void SetDeploymentResetAvailable(
        bool Available
    )
    {
        if (_ResetButton != null)
        {
            _ResetButton.interactable =
                Available;
        }
    }


    public void SetDeploymentConfirmAvailable(
        bool Available
    )
    {
        if (_ConfirmButton != null)
        {
            _ConfirmButton.interactable =
                Available;
        }
    }


    public void SetDeploymentRotationAvailable(
        bool Available
    )
    {
        if (_RotateLeftButton != null)
        {
            _RotateLeftButton.interactable =
                Available;
        }


        if (_RotateRightButton != null)
        {
            _RotateRightButton.interactable =
                Available;
        }
    }


    public void SetDeploymentControlsAvailable(
        bool CanReset,
        bool CanConfirm,
        bool CanRotate
    )
    {
        SetDeploymentResetAvailable(
            CanReset
        );


        SetDeploymentConfirmAvailable(
            CanConfirm
        );


        SetDeploymentRotationAvailable(
            CanRotate
        );
    }


    public void ResetDeploymentTrayScroll()
    {
        if (_DeploymentTray == null)
        {
            return;
        }


        _DeploymentTray.horizontalNormalizedPosition =
            0f;
    }


    private void SetDeploymentObjectsVisible(
        bool Visible
    )
    {
        SetObjectVisible(
            _PlacedCountText,
            Visible
        );


        SetObjectVisible(
            _ResetButton,
            Visible
        );


        SetObjectVisible(
            _ConfirmButton,
            Visible
        );


        if (_DeploymentOverlayRoot != null)
        {
            _DeploymentOverlayRoot.SetActive(
                Visible
            );
        }


        SetObjectVisible(
            _DeploymentTray,
            Visible
        );


        bool ShowingGameplayPhase = !Visible && _ShowingMovement;

        SetObjectVisible(_MovementCountText, ShowingGameplayPhase);
        SetObjectVisible(_MovementTimerText, ShowingGameplayPhase);
        SetObjectVisible(_EndMovementButton, ShowingGameplayPhase);

        bool ShowingPreBattle = !Visible && _ShowingPreBattle;
        SetObjectVisible(_PreBattleActionCountText, ShowingPreBattle);
        SetObjectVisible(_PreBattleTimerText, ShowingPreBattle);
        SetObjectVisible(_EndPreBattleButton, ShowingPreBattle);
    }


    private static void SetButtonInteractable(
        Button Button,
        bool Interactable
    )
    {
        if (Button != null)
        {
            Button.interactable =
                Interactable;
        }
    }


    private static void SetObjectVisible(
        Component Component,
        bool Visible
    )
    {
        if (Component != null)
        {
            Component.gameObject.SetActive(
                Visible
            );
        }
    }


    private static void SetObjectVisible(
        GameObject GameObject,
        bool Visible
    )
    {
        if (GameObject != null)
        {
            GameObject.SetActive(
                Visible
            );
        }
    }
}
