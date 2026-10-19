using UnityEngine;


[CreateAssetMenu(
    fileName = "Tag_New",
    menuName = "Admirals/Unit Tag"
)]
public sealed class Definition_UnitTag :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string _Name;

    [SerializeField]
    private string _ID;


    [Header("Domain Effect")]
    [SerializeField]
    private bool _OverridesDomain;

    [SerializeField]
    private Domain _DomainOverride;


    public string Name =>
        _Name;

    public string ID =>
        _ID;

    public bool OverridesDomain =>
        _OverridesDomain;

    public Domain DomainOverride =>
        _DomainOverride;


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                _Name))
        {
            ValidationMessage =
                "Unit Tag Name is empty.";

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                _ID))
        {
            ValidationMessage =
                "Unit Tag ID is empty.";

            return false;
        }


        if (_OverridesDomain &&
            _DomainOverride != Domain.Air &&
            _DomainOverride != Domain.Surface &&
            _DomainOverride != Domain.Subsurface)
        {
            ValidationMessage =
                "A Unit Tag Domain override must be one physical Domain.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        if (!Validate(
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Invalid Unit Tag '{name}': {ValidationMessage}",
                this
            );
        }
    }

#endif
}
