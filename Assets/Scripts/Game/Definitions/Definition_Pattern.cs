using System;
using System.Collections.Generic;
using UnityEngine;


// Embedded Serializable Data - Not A ScriptableObject
[Serializable]
public class PatternLayer
{
    [SerializeField]
    private string _Name;

    [Min(0)]
    [SerializeField]
    private int _Strength;

    [SerializeField]
    private Vector2Int[] _Offsets;


    public string Name
    {
        get
        {
            return _Name;
        }
    }


    public int Strength
    {
        get
        {
            return _Strength;
        }
    }


    public Vector2Int[] Offsets
    {
        get
        {
            return _Offsets;
        }
    }
}


// ScriptableObject Asset - Not Attached To A GameObject
// Defines One Reusable Spatial Pattern.
[CreateAssetMenu(
    fileName = "Pattern_New",
    menuName = "Admirals/Pattern"
)]
public class Definition_Pattern :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string _Name;

    [SerializeField]
    private string _ID;


    [Header("Pattern")]
    [SerializeField]
    private PatternMode _Mode;


    // FootprintMovement Patterns are paired directly with the Unit using them.
    // OriginPoint Patterns do not require a Unit reference.
    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_Unit _Unit;


    [SerializeField]
    private PatternLayer[] _Layers;


    // Runtime-only lookup built from the authored Pattern data.
    private Dictionary<Vector2Int, PatternLayer> _LayerLookup;

    private bool _LayerLookupValid;


    public string Name
    {
        get
        {
            return _Name;
        }
    }


    public string ID
    {
        get
        {
            return _ID;
        }
    }


    public PatternMode Mode
    {
        get
        {
            return _Mode;
        }
    }


    public Definition_Unit Unit
    {
        get
        {
            return _Unit;
        }
    }


    public PatternLayer[] Layers
    {
        get
        {
            return _Layers;
        }
    }


    // Checks that one tile never belongs to more than one Pattern Layer.
    public bool ValidateLayers(
        out string ValidationMessage
    )
    {
        if (_Layers == null)
        {
            ValidationMessage =
                string.Empty;

            return true;
        }


        HashSet<Vector2Int> UsedOffsets =
            new HashSet<Vector2Int>();


        for (
            int LayerIndex = 0;
            LayerIndex < _Layers.Length;
            LayerIndex++
        )
        {
            PatternLayer Layer =
                _Layers[LayerIndex];


            if (Layer == null ||
                Layer.Offsets == null)
            {
                continue;
            }


            for (
                int OffsetIndex = 0;
                OffsetIndex < Layer.Offsets.Length;
                OffsetIndex++
            )
            {
                Vector2Int Offset =
                    Layer.Offsets[OffsetIndex];


                if (!UsedOffsets.Add(
                        Offset))
                {
                    ValidationMessage =
                        $"Offset {Offset} appears in more than one Pattern Layer.";

                    return false;
                }
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    // Validates Pattern-specific authored data.
    public bool Validate(
        out string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                _Name))
        {
            ValidationMessage =
                "Pattern Name is empty.";

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                _ID))
        {
            ValidationMessage =
                "Pattern ID is empty.";

            return false;
        }


        if (_Mode != PatternMode.OriginPoint &&
            _Mode != PatternMode.FootprintMovement)
        {
            ValidationMessage =
                "Pattern Mode is not configured.";

            return false;
        }


        if (_Mode ==
            PatternMode.FootprintMovement)
        {
            if (_Unit == null)
            {
                ValidationMessage =
                    "Footprint Movement Pattern has no Unit.";

                return false;
            }


            // Both sides of the pairing must point at each other.
            if (_Unit.MovementPattern !=
                this)
            {
                ValidationMessage =
                    $"Footprint Movement Pattern is paired with Unit '{_Unit.Name}', " +
                    "but that Unit does not use this Movement Pattern.";

                return false;
            }
        }


        if (!ValidateLayers(
                out ValidationMessage))
        {
            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    // Performs a constant-time lookup for one authored Pattern tile.
    public bool TryGetLayer(
        Vector2Int Offset,
        out PatternLayer FoundLayer
    )
    {
        FoundLayer =
            null;


        if (_LayerLookup == null)
        {
            BuildLayerLookup();
        }


        if (!_LayerLookupValid)
        {
            return false;
        }


        return
            _LayerLookup.TryGetValue(
                Offset,
                out FoundLayer
            );
    }


    // Builds the runtime lookup once instead of rescanning
    // every Pattern Layer for each queried tile.
    private void BuildLayerLookup()
    {
        if (_LayerLookup == null)
        {
            _LayerLookup =
                new Dictionary<Vector2Int, PatternLayer>();
        }
        else
        {
            _LayerLookup.Clear();
        }


        _LayerLookupValid =
            true;


        if (_Layers == null)
        {
            return;
        }


        for (
            int LayerIndex = 0;
            LayerIndex < _Layers.Length;
            LayerIndex++
        )
        {
            PatternLayer Layer =
                _Layers[LayerIndex];


            if (Layer == null ||
                Layer.Offsets == null)
            {
                continue;
            }


            for (
                int OffsetIndex = 0;
                OffsetIndex < Layer.Offsets.Length;
                OffsetIndex++
            )
            {
                Vector2Int Offset =
                    Layer.Offsets[OffsetIndex];


                // Overlapping Layers are invalid.
                // There is deliberately no precedence rule.
                if (_LayerLookup.ContainsKey(
                        Offset))
                {
                    _LayerLookup.Clear();

                    _LayerLookupValid =
                        false;

                    return;
                }


                _LayerLookup.Add(
                    Offset,
                    Layer
                );
            }
        }
    }


    private void OnEnable()
    {
        BuildLayerLookup();
    }


    // Reports invalid authored Pattern data and refreshes the runtime lookup.
    private void OnValidate()
    {
        if (!Validate(
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Invalid Pattern '{name}': {ValidationMessage}",
                this
            );
        }


        BuildLayerLookup();
    }
}