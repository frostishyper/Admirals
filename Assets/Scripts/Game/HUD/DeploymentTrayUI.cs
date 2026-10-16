using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


// Presentation for the initial Deployment card tray.
//
// This component owns card creation, layout and scrolling only.
// Map interaction and tentative UnitView lifecycle belong to
// DeploymentMapInteraction.
[DisallowMultipleComponent]
public sealed class DeploymentTrayUI :
    MonoBehaviour
{
    [Header("Presentation")]
    [SerializeField]
    private UnitPresentationCatalog _UnitPresentationCatalog;


    [Header("Tray")]
    [SerializeField]
    private RectTransform _Content;

    [SerializeField]
    private ScrollRect _ScrollRect;

    [SerializeField]
    private DeploymentUnitCard _CardPrefab;


    private readonly List<DeploymentUnitCard> _Cards =
        new List<DeploymentUnitCard>();


    private GameDeployment.ParticipantView _Plan;

    private DeploymentMapInteraction _Interaction;


    private bool _Built;


    public bool Initialize(
        GameDeployment.ParticipantView Plan,
        DeploymentMapInteraction Interaction
    )
    {
        if (Plan == null)
        {
            Debug.LogError(
                "Deployment Tray UI received no Deployment plan.",
                this
            );

            return false;
        }


        if (Interaction == null)
        {
            Debug.LogError(
                "Deployment Tray UI received no Deployment Map Interaction.",
                this
            );

            return false;
        }


        if (_UnitPresentationCatalog == null)
        {
            Debug.LogError(
                "Deployment Tray UI has no Unit Presentation Catalog.",
                this
            );

            return false;
        }


        if (_Content == null)
        {
            Debug.LogError(
                "Deployment Tray UI has no Content RectTransform.",
                this
            );

            return false;
        }


        if (_CardPrefab == null)
        {
            Debug.LogError(
                "Deployment Tray UI has no Unit Card prefab.",
                this
            );

            return false;
        }


        bool ParticipantChanged =
            _Built &&
            !ReferenceEquals(_Plan, Plan);


        _Plan =
            Plan;

        _Interaction =
            Interaction;


        if (ParticipantChanged)
        {
            ClearCards();
            BuildCards();
        }
        else if (!_Built)
        {
            BuildCards();

            _Built =
                true;
        }


        Refresh();

        ResetScroll();

        return true;
    }


    public void Refresh()
    {
        if (!_Built ||
            _Plan == null)
        {
            return;
        }


        for (int Index = 0;
            Index < _Cards.Count;
            Index++)
        {
            DeploymentUnitCard Card =
                _Cards[Index];


            if (Card == null ||
                Card.Placement == null)
            {
                continue;
            }


            Card.gameObject.SetActive(
                !Card.Placement.IsPlaced
            );
        }


        if (_Content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                _Content
            );
        }
    }


    public void ResetScroll()
    {
        if (_ScrollRect == null)
        {
            return;
        }


        _ScrollRect.StopMovement();

        _ScrollRect.horizontalNormalizedPosition =
            0f;
    }


    internal void BeginCardDrag(
        DeploymentUnitCard Card,
        PointerEventData EventData
    )
    {
        if (!_Built ||
            _Interaction == null ||
            Card == null ||
            Card.Placement == null ||
            Card.Placement.IsPlaced ||
            !_Cards.Contains(
                Card))
        {
            return;
        }


        _Interaction.BeginCardDrag(
            Card,
            EventData
        );
    }


    internal void UpdateCardDrag(
        DeploymentUnitCard Card,
        PointerEventData EventData
    )
    {
        if (!_Built ||
            _Interaction == null ||
            Card == null ||
            !_Cards.Contains(
                Card))
        {
            return;
        }


        _Interaction.UpdateCardDrag(
            Card,
            EventData
        );
    }


    internal void EndCardDrag(
        DeploymentUnitCard Card,
        PointerEventData EventData
    )
    {
        if (!_Built ||
            _Interaction == null ||
            Card == null ||
            !_Cards.Contains(
                Card))
        {
            return;
        }


        _Interaction.EndCardDrag(
            Card,
            EventData
        );
    }


    private void ClearCards()
    {
        foreach (DeploymentUnitCard Card in _Cards)
        {
            if (Card == null)
                continue;

            Card.gameObject.SetActive(false);
            Destroy(Card.gameObject);
        }

        _Cards.Clear();
    }


    private void BuildCards()
    {
        IReadOnlyList<GameDeployment.PlacementView> Placements =
            _Plan.Placements;


        for (int Index = 0;
            Index < Placements.Count;
            Index++)
        {
            DeploymentUnitCard Card =
                Instantiate(
                    _CardPrefab,
                    _Content
                );


            Card.Bind(
                Placements[Index],
                _UnitPresentationCatalog,
                this
            );


            _Cards.Add(
                Card
            );
        }
    }
}


