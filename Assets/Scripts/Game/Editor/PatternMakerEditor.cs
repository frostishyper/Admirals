using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


// Unity Editor Script - Not Attached To A GameObject
// Visual Authoring Tool For OriginPoint And FootprintMovement Patterns.
[CustomEditor(typeof(Definition_Pattern))]
public class PatternMakerEditor :
    Editor
{
    private SerializedProperty _Name;
    private SerializedProperty _ID;
    private SerializedProperty _Mode;
    private SerializedProperty _Unit;
    private SerializedProperty _Layers;


    private int _SelectedLayer;

    private int _GridRadius =
        8;


    private const float CellSize =
        22f;


    private void OnEnable()
    {
        _Name =
            serializedObject.FindProperty(
                "_Name"
            );

        _ID =
            serializedObject.FindProperty(
                "_ID"
            );

        _Mode =
            serializedObject.FindProperty(
                "_Mode"
            );

        _Unit =
            serializedObject.FindProperty(
                "_Unit"
            );

        _Layers =
            serializedObject.FindProperty(
                "_Layers"
            );
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        EditorGUILayout.LabelField(
            "Pattern",
            EditorStyles.boldLabel
        );


        EditorGUILayout.PropertyField(
            _Name
        );

        EditorGUILayout.PropertyField(
            _ID
        );

        EditorGUILayout.PropertyField(
            _Mode
        );


        PatternMode CurrentMode =
            (PatternMode)_Mode.intValue;


        EditorGUILayout.Space(8);


        DrawModeSettings(
            CurrentMode
        );


        EditorGUILayout.Space(8);


        DrawLayerEditor();


        EditorGUILayout.Space(8);


        _GridRadius =
            EditorGUILayout.IntSlider(
                "Grid Radius",
                _GridRadius,
                2,
                16
            );


        EditorGUILayout.Space(4);


        DrawPatternGrid(
            CurrentMode
        );


        serializedObject.ApplyModifiedProperties();
    }


    private void DrawModeSettings(
        PatternMode Mode
    )
    {
        switch (Mode)
        {
            case PatternMode.OriginPoint:

                EditorGUILayout.HelpBox(
                    "Painted cells are exact offsets projected from the Unit's Origin Point.",
                    MessageType.Info
                );

                break;


            case PatternMode.FootprintMovement:

                EditorGUILayout.LabelField(
                    "Unit",
                    EditorStyles.boldLabel
                );


                EditorGUILayout.PropertyField(
                    _Unit
                );


                Definition_Unit Unit =
                    GetUnit();


                if (Unit == null)
                {
                    EditorGUILayout.HelpBox(
                        "Assign the Unit that uses this Movement Pattern. " +
                        "Its Footprint will remain visible while the Pattern is authored.",
                        MessageType.Warning
                    );

                    break;
                }


                EditorGUILayout.HelpBox(
                    $"Showing {Unit.Name}'s {Unit.FootprintSize.x}×{Unit.FootprintSize.y} Footprint. " +
                    "The outline is only an authoring reference and does not create Pattern tiles.",
                    MessageType.Info
                );

                break;
        }
    }


    private void DrawLayerEditor()
    {
        EditorGUILayout.LabelField(
            "Layers",
            EditorStyles.boldLabel
        );


        if (_Layers.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "Add a Layer before painting.",
                MessageType.Warning
            );
        }


        for (
            int i = 0;
            i < _Layers.arraySize;
            i++
        )
        {
            SerializedProperty Layer =
                _Layers.GetArrayElementAtIndex(
                    i
                );


            SerializedProperty LayerName =
                Layer.FindPropertyRelative(
                    "_Name"
                );

            SerializedProperty Strength =
                Layer.FindPropertyRelative(
                    "_Strength"
                );

            SerializedProperty Offsets =
                Layer.FindPropertyRelative(
                    "_Offsets"
                );


            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );


            EditorGUILayout.BeginHorizontal();


            bool Selected =
                _SelectedLayer ==
                i;


            if (GUILayout.Toggle(
                    Selected,
                    $"Layer {i + 1}",
                    "Button"))
            {
                _SelectedLayer =
                    i;
            }


            if (GUILayout.Button(
                    "Remove",
                    GUILayout.Width(
                        70f
                    )))
            {
                _Layers.DeleteArrayElementAtIndex(
                    i
                );


                _SelectedLayer =
                    Mathf.Clamp(
                        _SelectedLayer,
                        0,
                        Mathf.Max(
                            0,
                            _Layers.arraySize - 1
                        )
                    );


                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                return;
            }


            EditorGUILayout.EndHorizontal();


            EditorGUILayout.PropertyField(
                LayerName
            );

            EditorGUILayout.PropertyField(
                Strength
            );


            EditorGUILayout.LabelField(
                "Painted Cells",
                Offsets.arraySize.ToString()
            );


            EditorGUILayout.EndVertical();
        }


        EditorGUILayout.BeginHorizontal();


        if (GUILayout.Button(
                "Add Layer"))
        {
            AddLayer();
        }


        EditorGUI.BeginDisabledGroup(
            _Layers.arraySize == 0
        );


        if (GUILayout.Button(
                "Clear Selected Layer"))
        {
            SerializedProperty SelectedLayer =
                _Layers.GetArrayElementAtIndex(
                    _SelectedLayer
                );


            SelectedLayer
                .FindPropertyRelative(
                    "_Offsets"
                )
                .ClearArray();
        }


        EditorGUI.EndDisabledGroup();


        EditorGUILayout.EndHorizontal();
    }


    // Explicitly initializes a fresh Layer instead of
    // inheriting Unity's duplicated array data.
    private void AddLayer()
    {
        int NewIndex =
            _Layers.arraySize;


        _Layers.InsertArrayElementAtIndex(
            NewIndex
        );


        SerializedProperty NewLayer =
            _Layers.GetArrayElementAtIndex(
                NewIndex
            );


        NewLayer
            .FindPropertyRelative(
                "_Name"
            )
            .stringValue =
            $"Layer {NewIndex + 1}";


        NewLayer
            .FindPropertyRelative(
                "_Strength"
            )
            .intValue =
            1;


        NewLayer
            .FindPropertyRelative(
                "_Offsets"
            )
            .ClearArray();


        _SelectedLayer =
            NewIndex;
    }


    private void DrawPatternGrid(
        PatternMode Mode
    )
    {
        if (_Layers.arraySize == 0)
        {
            return;
        }


        _SelectedLayer =
            Mathf.Clamp(
                _SelectedLayer,
                0,
                _Layers.arraySize - 1
            );


        int Diameter =
            _GridRadius * 2 + 1;


        float GridSize =
            Diameter *
            CellSize;


        Rect GridRect =
            GUILayoutUtility.GetRect(
                GridSize,
                GridSize,
                GUILayout.ExpandWidth(
                    false
                )
            );


        Dictionary<Vector2Int, int> OffsetLayers =
            BuildOffsetLayerMap();


        HashSet<Vector2Int> UnitFootprint =
            null;


        Vector2Int MovementPivot =
            Vector2Int.zero;


        if (Mode ==
            PatternMode.FootprintMovement)
        {
            Definition_Unit Unit =
                GetUnit();


            if (Unit != null)
            {
                MovementPivot =
                    Unit.MovementPivot;


                UnitFootprint =
                    BuildUnitFootprint(
                        Unit
                    );
            }
        }


        for (
            int GridY = -_GridRadius;
            GridY <= _GridRadius;
            GridY++
        )
        {
            for (
                int GridX = -_GridRadius;
                GridX <= _GridRadius;
                GridX++
            )
            {
                Vector2Int Offset =
                    new Vector2Int(
                        GridX,
                        GridY
                    );


                int DisplayX =
                    GridX +
                    _GridRadius;


                int DisplayY =
                    _GridRadius -
                    GridY;


                Rect CellRect =
                    new Rect(
                        GridRect.x +
                        DisplayX *
                        CellSize,

                        GridRect.y +
                        DisplayY *
                        CellSize,

                        CellSize - 1f,
                        CellSize - 1f
                    );


                DrawCell(
                    CellRect,
                    Offset,
                    Mode,
                    OffsetLayers,
                    UnitFootprint
                );


                if (GUI.Button(
                        CellRect,
                        GUIContent.none,
                        GUIStyle.none))
                {
                    ToggleOffset(
                        Offset
                    );
                }


                DrawReferenceMarker(
                    CellRect,
                    Offset,
                    Mode,
                    UnitFootprint,
                    MovementPivot
                );
            }
        }
    }


    // Only explicitly painted cells receive Layer coloring.
    // Unit Footprint support is drawn separately as an outline.
    private void DrawCell(
        Rect CellRect,
        Vector2Int Offset,
        PatternMode Mode,
        Dictionary<Vector2Int, int> OffsetLayers,
        HashSet<Vector2Int> UnitFootprint
    )
    {
        Color Background =
            new Color(
                0.16f,
                0.16f,
                0.16f,
                1f
            );


        if (OffsetLayers.TryGetValue(
                Offset,
                out int Layer))
        {
            Background =
                GetLayerColor(
                    Layer
                );
        }


        EditorGUI.DrawRect(
            CellRect,
            Background
        );


        DrawBorder(
            CellRect,
            new Color(
                0.05f,
                0.05f,
                0.05f,
                1f
            ),
            1f
        );


        // The footprint is always visible for FootprintMovement Patterns,
        // but it never changes which Pattern tiles are actually authored.
        if (Mode ==
                PatternMode.FootprintMovement &&
            UnitFootprint != null &&
            UnitFootprint.Contains(
                Offset))
        {
            DrawBorder(
                ShrinkRect(
                    CellRect,
                    2f
                ),
                new Color(
                    0.85f,
                    0.85f,
                    0.85f,
                    1f
                ),
                2f
            );
        }
    }


    private void DrawReferenceMarker(
        Rect CellRect,
        Vector2Int Offset,
        PatternMode Mode,
        HashSet<Vector2Int> UnitFootprint,
        Vector2Int MovementPivot
    )
    {
        GUIStyle MarkerStyle =
            new GUIStyle(
                EditorStyles.centeredGreyMiniLabel
            );


        MarkerStyle.alignment =
            TextAnchor.MiddleCenter;

        MarkerStyle.normal.textColor =
            Color.white;


        if (Mode ==
            PatternMode.OriginPoint)
        {
            if (Offset ==
                Vector2Int.zero)
            {
                GUI.Label(
                    CellRect,
                    "O",
                    MarkerStyle
                );
            }


            return;
        }


        if (Mode !=
                PatternMode.FootprintMovement ||
            UnitFootprint == null)
        {
            return;
        }


        // The Unit footprint is positioned so its Movement Pivot sits at grid zero.
        if (Offset ==
            Vector2Int.zero)
        {
            GUI.Label(
                CellRect,
                "P",
                MarkerStyle
            );
        }
    }


    private Definition_Unit GetUnit()
    {
        if (_Unit == null)
        {
            return null;
        }


        return
            _Unit.objectReferenceValue
            as Definition_Unit;
    }


    // Places the Unit's actual rectangular Footprint around grid zero.
    // Grid zero represents the Unit's Movement Pivot.
    private HashSet<Vector2Int> BuildUnitFootprint(
        Definition_Unit Unit
    )
    {
        HashSet<Vector2Int> Footprint =
            new HashSet<Vector2Int>();


        Vector2Int Size =
            Unit.FootprintSize;


        Vector2Int Pivot =
            Unit.MovementPivot;


        for (
            int Y = 0;
            Y < Size.y;
            Y++
        )
        {
            for (
                int X = 0;
                X < Size.x;
                X++
            )
            {
                Footprint.Add(
                    new Vector2Int(
                        X - Pivot.x,
                        Y - Pivot.y
                    )
                );
            }
        }


        return
            Footprint;
    }


    private Dictionary<Vector2Int, int>
        BuildOffsetLayerMap()
    {
        Dictionary<Vector2Int, int> Result =
            new Dictionary<Vector2Int, int>();


        for (
            int LayerIndex = 0;
            LayerIndex < _Layers.arraySize;
            LayerIndex++
        )
        {
            SerializedProperty Layer =
                _Layers.GetArrayElementAtIndex(
                    LayerIndex
                );


            SerializedProperty Offsets =
                Layer.FindPropertyRelative(
                    "_Offsets"
                );


            for (
                int OffsetIndex = 0;
                OffsetIndex < Offsets.arraySize;
                OffsetIndex++
            )
            {
                Vector2Int Offset =
                    Offsets
                        .GetArrayElementAtIndex(
                            OffsetIndex
                        )
                        .vector2IntValue;


                if (!Result.ContainsKey(
                        Offset))
                {
                    Result.Add(
                        Offset,
                        LayerIndex
                    );
                }
            }
        }


        return Result;
    }


    // One Pattern tile can belong to exactly one Layer.
    // Painting it on another Layer moves it there.
    private void ToggleOffset(
        Vector2Int Offset
    )
    {
        Undo.RecordObject(
            target,
            "Edit Pattern"
        );


        serializedObject.Update();


        bool WasInSelectedLayer =
            false;


        for (
            int LayerIndex = 0;
            LayerIndex < _Layers.arraySize;
            LayerIndex++
        )
        {
            SerializedProperty Layer =
                _Layers.GetArrayElementAtIndex(
                    LayerIndex
                );


            SerializedProperty Offsets =
                Layer.FindPropertyRelative(
                    "_Offsets"
                );


            for (
                int OffsetIndex =
                    Offsets.arraySize - 1;
                OffsetIndex >= 0;
                OffsetIndex--
            )
            {
                SerializedProperty OffsetProperty =
                    Offsets.GetArrayElementAtIndex(
                        OffsetIndex
                    );


                if (OffsetProperty.vector2IntValue !=
                    Offset)
                {
                    continue;
                }


                if (LayerIndex ==
                    _SelectedLayer)
                {
                    WasInSelectedLayer =
                        true;
                }


                Offsets.DeleteArrayElementAtIndex(
                    OffsetIndex
                );
            }
        }


        if (!WasInSelectedLayer)
        {
            SerializedProperty SelectedLayer =
                _Layers.GetArrayElementAtIndex(
                    _SelectedLayer
                );


            SerializedProperty Offsets =
                SelectedLayer.FindPropertyRelative(
                    "_Offsets"
                );


            int NewIndex =
                Offsets.arraySize;


            Offsets.InsertArrayElementAtIndex(
                NewIndex
            );


            Offsets
                .GetArrayElementAtIndex(
                    NewIndex
                )
                .vector2IntValue =
                Offset;
        }


        serializedObject.ApplyModifiedProperties();


        EditorUtility.SetDirty(
            target
        );
    }


    private Color GetLayerColor(
        int LayerIndex
    )
    {
        float Hue =
            (
                LayerIndex *
                0.17f
            ) %
            1f;


        return
            Color.HSVToRGB(
                Hue,
                0.55f,
                0.95f
            );
    }


    private Rect ShrinkRect(
        Rect Source,
        float Amount
    )
    {
        return
            new Rect(
                Source.x + Amount,
                Source.y + Amount,
                Source.width - Amount * 2f,
                Source.height - Amount * 2f
            );
    }


    private void DrawBorder(
        Rect Rect,
        Color BorderColor,
        float Thickness
    )
    {
        EditorGUI.DrawRect(
            new Rect(
                Rect.x,
                Rect.y,
                Rect.width,
                Thickness
            ),
            BorderColor
        );


        EditorGUI.DrawRect(
            new Rect(
                Rect.x,
                Rect.yMax - Thickness,
                Rect.width,
                Thickness
            ),
            BorderColor
        );


        EditorGUI.DrawRect(
            new Rect(
                Rect.x,
                Rect.y,
                Thickness,
                Rect.height
            ),
            BorderColor
        );


        EditorGUI.DrawRect(
            new Rect(
                Rect.xMax - Thickness,
                Rect.y,
                Thickness,
                Rect.height
            ),
            BorderColor
        );
    }
}