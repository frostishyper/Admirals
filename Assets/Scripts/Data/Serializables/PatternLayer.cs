using System;
using UnityEngine;

// Serializable Layer Used By PatternDefinition
[Serializable]
public class PatternLayer
{
    // Shared Strength / Value For Every Cell In This Layer
    [SerializeField] private int _Intensity;

    // Relative Grid Cells Belonging To This Layer
    [SerializeField] private Vector2Int[] _Offsets;


    // Read-Only Access For Other Systems
    public int Intensity
    {
        get { return _Intensity; }
    }

    public Vector2Int[] Offsets
    {
        get { return _Offsets; }
    }
}