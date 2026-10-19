using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Unity Editor Script - Not Attached To A GameObject
// Provides A Visual Tile Editor For PatternDefinition Assets
[CustomEditor(typeof(PatternDefinition))]
public class TilePatternMaker : Editor
{
    private const float CellSize = 28f;

    private SerializedProperty _PatternName;
    private SerializedProperty _PatternID;
    private SerializedProperty _Layers;

    private int _SelectedLayerIndex;
    private int _GridRadius = 10;

    private readonly Dictionary<Vector2Int, int> _CellLayerLookup = new();

    private GUIStyle _CellLabelStyle;


    private GUIStyle CellLabelStyle
    {
        get
        {
            if (_CellLabelStyle == null)
            {
                _CellLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
            }

            return _CellLabelStyle;
        }
    }


    private void OnEnable()
    {
        _PatternName = serializedObject.FindProperty("_PatternName");
        _PatternID = serializedObject.FindProperty("_PatternID");
        _Layers = serializedObject.FindProperty("_Layers");
    }


    public override void OnInspectorGUI()
    {
        serializedObject.UpdateIfRequiredOrScript();

        EditorGUILayout.PropertyField(_PatternName);
        EditorGUILayout.PropertyField(_PatternID);

        EditorGUILayout.Space();

        DrawLayerControls();

        if (_Layers.arraySize <= 0)
        {
            EditorGUILayout.HelpBox(
                "Add a Pattern Layer to begin editing the pattern.",
                MessageType.Info
            );

            serializedObject.ApplyModifiedProperties();
            return;
        }

        _SelectedLayerIndex = Mathf.Clamp(
            _SelectedLayerIndex,
            0,
            _Layers.arraySize - 1
        );

        SerializedProperty selectedLayer =
            _Layers.GetArrayElementAtIndex(_SelectedLayerIndex);

        SerializedProperty intensity =
            selectedLayer.FindPropertyRelative("_Intensity");

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(intensity);

        _GridRadius = EditorGUILayout.IntSlider(
            "Grid Radius",
            _GridRadius,
            1,
            12
        );

        EditorGUILayout.Space();

        BuildCellLookup();

        DrawGrid();

        serializedObject.ApplyModifiedProperties();
    }


    private void DrawLayerControls()
    {
        EditorGUILayout.LabelField(
            "Pattern Layers",
            EditorStyles.boldLabel
        );

        if (_Layers.arraySize > 0)
        {
            string[] layerNames =
                new string[_Layers.arraySize];

            for (int i = 0; i < _Layers.arraySize; i++)
            {
                SerializedProperty layer =
                    _Layers.GetArrayElementAtIndex(i);

                SerializedProperty intensity =
                    layer.FindPropertyRelative("_Intensity");

                layerNames[i] =
                    $"Layer {i + 1} - Intensity {intensity.intValue}";
            }

            _SelectedLayerIndex =
                EditorGUILayout.Popup(
                    "Selected Layer",
                    _SelectedLayerIndex,
                    layerNames
                );
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Layer"))
        {
            AddLayer();
        }

        using (
            new EditorGUI.DisabledScope(
                _Layers.arraySize <= 0
            )
        )
        {
            if (GUILayout.Button("Remove Layer"))
            {
                RemoveSelectedLayer();
            }

            if (GUILayout.Button("Clear Layer"))
            {
                ClearSelectedLayer();
            }
        }

        EditorGUILayout.EndHorizontal();
    }


    private void AddLayer()
    {
        int newLayerIndex = _Layers.arraySize;

        _Layers.arraySize++;

        SerializedProperty newLayer =
            _Layers.GetArrayElementAtIndex(
                newLayerIndex
            );

        SerializedProperty intensity =
            newLayer.FindPropertyRelative(
                "_Intensity"
            );

        SerializedProperty offsets =
            newLayer.FindPropertyRelative(
                "_Offsets"
            );

        intensity.intValue =
            newLayerIndex + 1;

        offsets.ClearArray();

        _SelectedLayerIndex =
            newLayerIndex;
    }


    private void RemoveSelectedLayer()
    {
        if (_Layers.arraySize <= 0)
        {
            return;
        }

        _Layers.DeleteArrayElementAtIndex(
            _SelectedLayerIndex
        );

        _SelectedLayerIndex = Mathf.Clamp(
            _SelectedLayerIndex,
            0,
            _Layers.arraySize - 1
        );
    }


    private void ClearSelectedLayer()
    {
        if (_Layers.arraySize <= 0)
        {
            return;
        }

        SerializedProperty layer =
            _Layers.GetArrayElementAtIndex(
                _SelectedLayerIndex
            );

        SerializedProperty offsets =
            layer.FindPropertyRelative(
                "_Offsets"
            );

        offsets.ClearArray();
    }


    private void BuildCellLookup()
    {
        _CellLayerLookup.Clear();

        for (
            int layerIndex = 0;
            layerIndex < _Layers.arraySize;
            layerIndex++
        )
        {
            SerializedProperty layer =
                _Layers.GetArrayElementAtIndex(
                    layerIndex
                );

            SerializedProperty offsets =
                layer.FindPropertyRelative(
                    "_Offsets"
                );

            for (
                int offsetIndex = 0;
                offsetIndex < offsets.arraySize;
                offsetIndex++
            )
            {
                Vector2Int position =
                    offsets
                        .GetArrayElementAtIndex(
                            offsetIndex
                        )
                        .vector2IntValue;

                _CellLayerLookup[position] =
                    layerIndex;
            }
        }
    }


    private void DrawGrid()
    {
        int diameter =
            (_GridRadius * 2) + 1;

        Rect availableRect =
            GUILayoutUtility.GetRect(
                0f,
                diameter * CellSize,
                GUILayout.ExpandWidth(true)
            );

        float actualCellSize =
            Mathf.Min(
                CellSize,
                availableRect.width / diameter
            );

        float gridSize =
            actualCellSize * diameter;

        Rect gridRect = new Rect(
            availableRect.x +
            (
                (availableRect.width - gridSize)
                * 0.5f
            ),
            availableRect.y,
            gridSize,
            gridSize
        );

        HandleGridInput(
            gridRect,
            actualCellSize
        );

        for (
            int row = 0;
            row < diameter;
            row++
        )
        {
            int y =
                _GridRadius - row;

            for (
                int column = 0;
                column < diameter;
                column++
            )
            {
                int x =
                    column - _GridRadius;

                Vector2Int position =
                    new Vector2Int(x, y);

                Rect cellRect =
                    new Rect(
                        gridRect.x +
                        (
                            column *
                            actualCellSize
                        ),
                        gridRect.y +
                        (
                            row *
                            actualCellSize
                        ),
                        actualCellSize,
                        actualCellSize
                    );

                DrawCell(
                    cellRect,
                    position
                );
            }
        }
    }


    private void DrawCell(
        Rect cellRect,
        Vector2Int position
    )
    {
        bool isOrigin =
            position == Vector2Int.zero;

        bool isOccupied =
            _CellLayerLookup.TryGetValue(
                position,
                out int occupyingLayer
            );

        bool isSelectedLayer =
            isOccupied &&
            occupyingLayer ==
            _SelectedLayerIndex;

        Color backgroundColor;

        if (isOrigin)
        {
            backgroundColor =
                new Color(
                    0.35f,
                    0.35f,
                    0.35f
                );
        }
        else if (isSelectedLayer)
        {
            backgroundColor =
                new Color(
                    0.25f,
                    0.55f,
                    0.80f
                );
        }
        else if (isOccupied)
        {
            backgroundColor =
                new Color(
                    0.30f,
                    0.40f,
                    0.50f
                );
        }
        else
        {
            backgroundColor =
                new Color(
                    0.20f,
                    0.20f,
                    0.20f
                );
        }

        EditorGUI.DrawRect(
            cellRect,
            Color.black
        );

        Rect innerRect =
            new Rect(
                cellRect.x + 1f,
                cellRect.y + 1f,
                cellRect.width - 2f,
                cellRect.height - 2f
            );

        EditorGUI.DrawRect(
            innerRect,
            backgroundColor
        );

        string label = "";

        if (isOrigin)
        {
            label = "O";
        }
        else if (isOccupied)
        {
            SerializedProperty layer =
                _Layers.GetArrayElementAtIndex(
                    occupyingLayer
                );

            SerializedProperty intensity =
                layer.FindPropertyRelative(
                    "_Intensity"
                );

            label =
                intensity.intValue.ToString();
        }

        if (!string.IsNullOrEmpty(label))
        {
            GUI.Label(
                innerRect,
                label,
                CellLabelStyle
            );
        }
    }


    private void HandleGridInput(
        Rect gridRect,
        float actualCellSize
    )
    {
        Event currentEvent =
            Event.current;

        if (
            currentEvent.type !=
            EventType.MouseDown ||
            currentEvent.button != 0 ||
            !gridRect.Contains(
                currentEvent.mousePosition
            )
        )
        {
            return;
        }

        int column =
            Mathf.FloorToInt(
                (
                    currentEvent.mousePosition.x
                    - gridRect.x
                )
                / actualCellSize
            );

        int row =
            Mathf.FloorToInt(
                (
                    currentEvent.mousePosition.y
                    - gridRect.y
                )
                / actualCellSize
            );

        int x =
            column - _GridRadius;

        int y =
            _GridRadius - row;

        Vector2Int position =
            new Vector2Int(x, y);

        // Origin Is Reserved For The Unit / Pattern Source
        if (
            position ==
            Vector2Int.zero
        )
        {
            currentEvent.Use();
            return;
        }

        ToggleCell(position);

        serializedObject
            .ApplyModifiedProperties();

        currentEvent.Use();

        Repaint();
    }


    private void ToggleCell(
        Vector2Int position
    )
    {
        if (
            _CellLayerLookup.TryGetValue(
                position,
                out int existingLayer
            )
        )
        {
            // Clicking A Cell Already In The Selected Layer Removes It
            if (
                existingLayer ==
                _SelectedLayerIndex
            )
            {
                RemoveOffsetFromLayer(
                    _SelectedLayerIndex,
                    position
                );

                return;
            }

            // Clicking A Cell From Another Layer Moves It
            RemoveOffsetFromLayer(
                existingLayer,
                position
            );
        }

        AddOffsetToLayer(
            _SelectedLayerIndex,
            position
        );
    }


    private void AddOffsetToLayer(
        int layerIndex,
        Vector2Int position
    )
    {
        SerializedProperty layer =
            _Layers.GetArrayElementAtIndex(
                layerIndex
            );

        SerializedProperty offsets =
            layer.FindPropertyRelative(
                "_Offsets"
            );

        int newIndex =
            offsets.arraySize;

        offsets.arraySize++;

        offsets
            .GetArrayElementAtIndex(
                newIndex
            )
            .vector2IntValue =
            position;
    }


    private void RemoveOffsetFromLayer(
        int layerIndex,
        Vector2Int position
    )
    {
        SerializedProperty layer =
            _Layers.GetArrayElementAtIndex(
                layerIndex
            );

        SerializedProperty offsets =
            layer.FindPropertyRelative(
                "_Offsets"
            );

        for (
            int i = offsets.arraySize - 1;
            i >= 0;
            i--
        )
        {
            if (
                offsets
                    .GetArrayElementAtIndex(i)
                    .vector2IntValue ==
                position
            )
            {
                offsets
                    .DeleteArrayElementAtIndex(i);
            }
        }
    }
}