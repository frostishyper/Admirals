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


    private bool _ShowingMovement;
    private bool _MovementSelectionVisible;


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


    public void BindMovementUnitDetails(
        ParticipantUnitSnapshot Unit,
        Definition_Unit Definition,
        UnitPresentationCatalog PresentationCatalog,
        bool CanAct,
        Action<int> UseAbility)
    {
        if (_MovementUnitDetails != null)
        {
            _MovementUnitDetails.Bind(
                Unit,
                Definition,
                PresentationCatalog,
                CanAct,
                UseAbility
            );
        }
    }


    public void ClearMovementUnitDetails()
    {
        if (_MovementUnitDetails != null)
        {
            _MovementUnitDetails.Clear();
        }
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
    }


    public void ShowDeploymentPhase()
    {
        _ShowingMovement =
            false;

        _MovementSelectionVisible =
            false;

        SetObjectVisible(_DeploymentTopRoot, true);
        SetObjectVisible(_DeploymentOverlayRoot, true);
        SetObjectVisible(_DeploymentBottomRoot, true);
        SetObjectVisible(_MovementTopRoot, false);
        SetObjectVisible(_MovementOverlayRoot, false);
        SetObjectVisible(_MovementBottomRoot, false);
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

        SetObjectVisible(_DeploymentTopRoot, false);
        SetObjectVisible(_DeploymentOverlayRoot, false);
        SetObjectVisible(_DeploymentBottomRoot, false);
        SetObjectVisible(_MovementTopRoot, true);
        ApplyMovementSelectionVisibility();

        SetDeploymentObjectsVisible(
            false
        );


        if (_MovementPhaseText != null)
        {
            _MovementPhaseText.text =
                "1 Movement";
        }


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
        if (!_ShowingMovement)
        {
            return;
        }


        bool Visible =
            _MovementSelectionVisible;

        SetObjectVisible(_MovementOverlayRoot, Visible);
        SetObjectVisible(_MovementBottomRoot, Visible);
        SetObjectVisible(_BottomBarRoot, Visible);
    }


    public void SetMovementSelectionControls(
        bool CanAdjust,
        bool CanConfirm
    )
    {
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


        SetObjectVisible(_MovementCountText, !Visible && _ShowingMovement);
        SetObjectVisible(_MovementTimerText, !Visible && _ShowingMovement);
        SetObjectVisible(_EndMovementButton, !Visible && _ShowingMovement);
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
