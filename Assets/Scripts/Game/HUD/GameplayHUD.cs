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
    [Header("Deployment - Top")]
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
    private ScrollRect _DeploymentTray;

    [SerializeField]
    private RectTransform _DeploymentTrayContent;


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


    public void SetPlayerName(
        string PlayerName
    )
    {
        if (_PlayerNameText == null)
        {
            return;
        }


        _PlayerNameText.text =
            string.IsNullOrWhiteSpace(
                PlayerName)
                ? string.Empty
                : PlayerName;
    }


    public void ShowDeploymentPhase()
    {
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
        SetDeploymentObjectsVisible(
            false
        );


        if (_PhaseText != null)
        {
            _PhaseText.text =
                "1 Movement";
        }
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
}
