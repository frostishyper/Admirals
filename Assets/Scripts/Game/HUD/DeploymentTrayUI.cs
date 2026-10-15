using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[DisallowMultipleComponent]
public sealed class DeploymentTrayUI :
    MonoBehaviour
{
    [Header("Game")]
    [SerializeField]
    private GameMaster _GameMaster;

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


    private bool _Built;


    private IEnumerator Start()
    {
        if (_GameMaster == null)
        {
            Debug.LogError(
                "Deployment Tray UI has no GameMaster.",
                this
            );


            yield break;
        }


        while (!_GameMaster.InitializationComplete)
        {
            if (!_GameMaster.enabled)
            {
                Debug.LogError(
                    "Deployment Tray UI could not initialize because GameMaster initialization failed.",
                    this
                );


                yield break;
            }


            yield return null;
        }


        Build();
    }


    public void Build()
    {
        if (_Built)
        {
            Refresh();

            return;
        }


        if (_GameMaster.GameDeployment == null ||
            _GameMaster.GameDeployment.ParticipantA == null)
        {
            Debug.LogError(
                "Deployment Tray UI has no initialized Participant A Deployment plan.",
                this
            );


            return;
        }


        if (_UnitPresentationCatalog == null)
        {
            Debug.LogError(
                "Deployment Tray UI has no Unit Presentation Catalog.",
                this
            );


            return;
        }


        if (_Content == null)
        {
            Debug.LogError(
                "Deployment Tray UI has no Content RectTransform.",
                this
            );


            return;
        }


        if (_CardPrefab == null)
        {
            Debug.LogError(
                "Deployment Tray UI has no Unit Card prefab.",
                this
            );


            return;
        }


        IReadOnlyList<GameDeployment.Placement> Placements =
            _GameMaster
                .GameDeployment
                .ParticipantA
                .Placements;


        for (
            int Index = 0;
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
                _UnitPresentationCatalog
            );


            _Cards.Add(
                Card
            );
        }


        _Built =
            true;


        Refresh();


        if (_ScrollRect != null)
        {
            _ScrollRect.horizontalNormalizedPosition =
                0f;
        }
    }


    public void Refresh()
    {
        for (
            int Index = 0;
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
}