using UnityEditor;
using UnityEngine;

// Unity Editor Script - Not Attached To A GameObject
// Provides A Visual Grid For Building PatternDefinition Assets
[CustomEditor(typeof(PatternDefinition))]
public class TilePatternMaker : Editor
{
    // Currently Selected Pattern Layer
    private int _SelectedLayer = 0;

    // Controls How Much Of The Pattern Grid Is Visible
    // Radius 5 = 11x11 Grid
    private int _GridRadius = 5;

    // Size Of Each Clickable Grid Cell In The Inspector
    private const float CELL_SIZE = 28f;


    public override void OnInspectorGUI()
    {
        // Load The Current Serialized Values From The Asset
        serializedObject.Update();


        // Find The Fields Inside PatternDefinition
        SerializedProperty PatternName =
            serializedObject.FindProperty("_PatternName");

        SerializedProperty PatternID =
            serializedObject.FindProperty("_PatternID");

        SerializedProperty Layers =
            serializedObject.FindProperty("_Layers");


        // Basic Pattern Information
        EditorGUILayout.PropertyField(PatternName);
        EditorGUILayout.PropertyField(PatternID);

        EditorGUILayout.Space(10);


        // Layer Controls
        DrawLayerControls(Layers);

        EditorGUILayout.Space(10);


        // Only Draw The Grid If A Layer Exists
        if (Layers.arraySize > 0)
        {
            DrawPatternGrid(Layers);
        }


        // Save Any Changes Back Into The PatternDefinition Asset
        serializedObject.ApplyModifiedProperties();
    }


    private void DrawLayerControls(SerializedProperty Layers)
    {
        EditorGUILayout.LabelField(
            "Pattern Layers",
            EditorStyles.boldLabel
        );


        // Add A New Pattern Layer
        if (GUILayout.Button("Add Layer"))
        {
            Layers.InsertArrayElementAtIndex(Layers.arraySize);

            SerializedProperty NewLayer =
                Layers.GetArrayElementAtIndex(Layers.arraySize - 1);

            SerializedProperty Intensity =
                NewLayer.FindPropertyRelative("_Intensity");

            SerializedProperty Offsets =
                NewLayer.FindPropertyRelative("_Offsets");


            // New Layers Start Clean
            Intensity.intValue = 0;
            Offsets.arraySize = 0;

            _SelectedLayer = Layers.arraySize - 1;
        }


        // Nothing Else To Show If There Are No Layers
        if (Layers.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "Add A Layer To Begin Building The Pattern.",
                MessageType.Info
            );

            return;
        }


        // Prevent Selected Layer From Going Out Of Range
        _SelectedLayer =
            Mathf.Clamp(
                _SelectedLayer,
                0,
                Layers.arraySize - 1
            );


        EditorGUILayout.Space(5);


        // Select Which Layer Is Currently Being Painted
        string[] LayerNames = new string[Layers.arraySize];

        for (int i = 0; i < Layers.arraySize; i++)
        {
            SerializedProperty Layer =
                Layers.GetArrayElementAtIndex(i);

            SerializedProperty Intensity =
                Layer.FindPropertyRelative("_Intensity");

            LayerNames[i] =
                "Layer " + (i + 1) +
                "  |  Intensity " +
                Intensity.intValue;
        }


        _SelectedLayer =
            EditorGUILayout.Popup(
                "Selected Layer",
                _SelectedLayer,
                LayerNames
            );


        SerializedProperty SelectedLayer =
            Layers.GetArrayElementAtIndex(_SelectedLayer);

        SerializedProperty SelectedIntensity =
            SelectedLayer.FindPropertyRelative("_Intensity");


        // Edit The Shared Intensity For The Entire Selected Layer
        EditorGUILayout.PropertyField(
            SelectedIntensity,
            new GUIContent("Intensity")
        );


        EditorGUILayout.BeginHorizontal();


        // Remove The Currently Selected Layer
        if (GUILayout.Button("Remove Layer"))
        {
            Layers.DeleteArrayElementAtIndex(_SelectedLayer);

            _SelectedLayer =
                Mathf.Max(0, _SelectedLayer - 1);

            EditorGUILayout.EndHorizontal();
            return;
        }


        // Remove Every Cell From The Selected Layer
        if (GUILayout.Button("Clear Layer"))
        {
            SerializedProperty Offsets =
                SelectedLayer.FindPropertyRelative("_Offsets");

            Offsets.arraySize = 0;
        }


        EditorGUILayout.EndHorizontal();


        EditorGUILayout.Space(5);


        // Changes Only The Visible Authoring Area
        // It Does Not Change Or Limit The Stored Pattern
        _GridRadius =
            EditorGUILayout.IntSlider(
                "Grid Radius",
                _GridRadius,
                2,
                12
            );
    }


    private void DrawPatternGrid(SerializedProperty Layers)
    {
        EditorGUILayout.LabelField(
            "Pattern Grid",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Click An Empty Cell To Add It To The Selected Layer.\n" +
            "Click A Cell In The Selected Layer To Remove It.\n" +
            "Click A Cell From Another Layer To Move It To The Selected Layer.\n" +
            "O Marks The Pattern Origin At (0, 0).",
            MessageType.None
        );


        // Draw From Positive Y At The Top
        // To Negative Y At The Bottom
        for (int y = _GridRadius; y >= -_GridRadius; y--)
        {
            EditorGUILayout.BeginHorizontal();


            for (int x = -_GridRadius; x <= _GridRadius; x++)
            {
                Vector2Int Offset =
                    new Vector2Int(x, y);


                // Find Which Layer Currently Owns This Cell
                int OwningLayer =
                    FindLayerContainingOffset(
                        Layers,
                        Offset
                    );


                string ButtonText = "";


                // Clearly Mark The Pattern Origin
                if (Offset == Vector2Int.zero)
                {
                    ButtonText = "O";
                }
                else if (OwningLayer >= 0)
                {
                    SerializedProperty Layer =
                        Layers.GetArrayElementAtIndex(OwningLayer);

                    SerializedProperty Intensity =
                        Layer.FindPropertyRelative("_Intensity");

                    // Show The Layer's Intensity Directly On Painted Cells
                    ButtonText =
                        Intensity.intValue.ToString();
                }


                GUIStyle CellStyle =
                    new GUIStyle(GUI.skin.button);


                // Visually Distinguish Cells Belonging
                // To The Currently Selected Layer
                if (OwningLayer == _SelectedLayer)
                {
                    CellStyle.fontStyle = FontStyle.Bold;
                }


                if (GUILayout.Button(
                    ButtonText,
                    CellStyle,
                    GUILayout.Width(CELL_SIZE),
                    GUILayout.Height(CELL_SIZE)))
                {
                    PaintCell(
                        Layers,
                        Offset,
                        OwningLayer
                    );
                }
            }


            EditorGUILayout.EndHorizontal();
        }
    }


    private void PaintCell(
        SerializedProperty Layers,
        Vector2Int Offset,
        int OwningLayer)
    {
        // Clicking A Cell Already In The Selected Layer
        // Removes It From That Layer
        if (OwningLayer == _SelectedLayer)
        {
            RemoveOffsetFromLayer(
                Layers,
                _SelectedLayer,
                Offset
            );

            return;
        }


        // If Another Layer Already Owns This Cell,
        // Remove It From That Layer First
        if (OwningLayer >= 0)
        {
            RemoveOffsetFromLayer(
                Layers,
                OwningLayer,
                Offset
            );
        }


        // Add The Cell To The Currently Selected Layer
        AddOffsetToLayer(
            Layers,
            _SelectedLayer,
            Offset
        );
    }


    private int FindLayerContainingOffset(
        SerializedProperty Layers,
        Vector2Int Offset)
    {
        // Search Every Layer To See Who Owns This Cell
        for (int LayerIndex = 0;
            LayerIndex < Layers.arraySize;
            LayerIndex++)
        {
            SerializedProperty Layer =
                Layers.GetArrayElementAtIndex(LayerIndex);

            SerializedProperty Offsets =
                Layer.FindPropertyRelative("_Offsets");


            for (int OffsetIndex = 0;
                OffsetIndex < Offsets.arraySize;
                OffsetIndex++)
            {
                if (Offsets
                    .GetArrayElementAtIndex(OffsetIndex)
                    .vector2IntValue == Offset)
                {
                    return LayerIndex;
                }
            }
        }


        // -1 Means Nobody Currently Owns The Cell
        return -1;
    }


    private void AddOffsetToLayer(
        SerializedProperty Layers,
        int LayerIndex,
        Vector2Int Offset)
    {
        SerializedProperty Layer =
            Layers.GetArrayElementAtIndex(LayerIndex);

        SerializedProperty Offsets =
            Layer.FindPropertyRelative("_Offsets");


        int NewIndex = Offsets.arraySize;

        Offsets.InsertArrayElementAtIndex(NewIndex);

        Offsets
            .GetArrayElementAtIndex(NewIndex)
            .vector2IntValue = Offset;
    }


    private void RemoveOffsetFromLayer(
        SerializedProperty Layers,
        int LayerIndex,
        Vector2Int Offset)
    {
        SerializedProperty Layer =
            Layers.GetArrayElementAtIndex(LayerIndex);

        SerializedProperty Offsets =
            Layer.FindPropertyRelative("_Offsets");


        for (int i = 0;
            i < Offsets.arraySize;
            i++)
        {
            SerializedProperty StoredOffset =
                Offsets.GetArrayElementAtIndex(i);


            if (StoredOffset.vector2IntValue == Offset)
            {
                Offsets.DeleteArrayElementAtIndex(i);
                return;
            }
        }
    }
}