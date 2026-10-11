using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Unity Editor Script - Not Attached To A GameObject
// Provides MapBoard Setup And Gameplay Terrain Painting Tools
[CustomEditor(typeof(MapBoard))]
public class MapBoardEditor : Editor
{
    private bool _PickingOrigin;
    private bool _PaintingTerrain;

    private bool _ShowTerrainOverlay = true;
    private bool _ShowDeepWaterOverlay = false;

    private TerrainType _SelectedTerrain =
        TerrainType.DeepWater;


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapBoard Board =
            (MapBoard)target;


        EditorGUILayout.Space();

        DrawBoardSetup(Board);

        EditorGUILayout.Space();

        DrawTerrainTools(Board);
    }


    private void DrawBoardSetup(MapBoard Board)
    {
        EditorGUILayout.LabelField(
            "Board Setup",
            EditorStyles.boldLabel
        );


        if (!_PickingOrigin)
        {
            if (GUILayout.Button(
                    "Pick Bottom-Left Board Cell"
                ))
            {
                _PickingOrigin = true;
                _PaintingTerrain = false;

                SceneView.RepaintAll();
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Click the bottom-left cell of the painted map in the Scene view.",
                MessageType.Info
            );

            if (GUILayout.Button(
                    "Cancel Origin Picking"
                ))
            {
                _PickingOrigin = false;

                SceneView.RepaintAll();
            }
        }
    }


    private void DrawTerrainTools(MapBoard Board)
    {
        EditorGUILayout.LabelField(
            "Gameplay Terrain",
            EditorStyles.boldLabel
        );


        if (!Board.HasTerrainData)
        {
            EditorGUILayout.HelpBox(
                $"Terrain data is not initialized for the current {Board.BoardSize.x}x{Board.BoardSize.y} board.",
                MessageType.Warning
            );

            if (GUILayout.Button(
                    "Initialize Terrain As Deep Water"
                ))
            {
                Undo.RecordObject(
                    Board,
                    "Initialize Map Terrain"
                );

                Board.InitializeTerrainData();

                EditorUtility.SetDirty(Board);

                SceneView.RepaintAll();
            }

            return;
        }


        EditorGUILayout.HelpBox(
            $"Terrain Data Ready: {Board.BoardSize.x * Board.BoardSize.y} Cells",
            MessageType.Info
        );


        _SelectedTerrain =
            (TerrainType)EditorGUILayout.EnumPopup(
                "Selected Terrain",
                _SelectedTerrain
            );


        _ShowTerrainOverlay =
            EditorGUILayout.Toggle(
                "Show Terrain Overlay",
                _ShowTerrainOverlay
            );


        if (_ShowTerrainOverlay)
        {
            _ShowDeepWaterOverlay =
                EditorGUILayout.Toggle(
                    "Show Deep Water",
                    _ShowDeepWaterOverlay
                );
        }


        EditorGUILayout.Space();


        if (!_PaintingTerrain)
        {
            if (GUILayout.Button(
                    "Start Terrain Painting"
                ))
            {
                _PaintingTerrain = true;
                _PickingOrigin = false;

                SceneView.RepaintAll();
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Left-click and drag across cells in the Scene view to paint the selected gameplay terrain.",
                MessageType.Info
            );

            if (GUILayout.Button(
                    "Stop Terrain Painting"
                ))
            {
                _PaintingTerrain = false;

                SceneView.RepaintAll();
            }
        }


        EditorGUILayout.Space();


        if (GUILayout.Button(
                "Fill Entire Board With Deep Water"
            ))
        {
            bool Confirmed =
                EditorUtility.DisplayDialog(
                    "Fill Entire Board?",
                    "This replaces all current gameplay terrain classifications with Deep Water.",
                    "Fill",
                    "Cancel"
                );

            if (Confirmed)
            {
                Undo.RecordObject(
                    Board,
                    "Fill Map Terrain"
                );

                Board.FillTerrain(
                    TerrainType.DeepWater
                );

                EditorUtility.SetDirty(Board);

                SceneView.RepaintAll();
            }
        }
    }


    private void OnSceneGUI()
    {
        MapBoard Board =
            (MapBoard)target;


        if (Board.TerrainTilemap == null)
        {
            return;
        }


        DrawBoardOutline(Board);


        if (_ShowTerrainOverlay &&
            Board.HasTerrainData)
        {
            DrawTerrainOverlay(Board);
        }


        if (_PickingOrigin)
        {
            HandleOriginPicking(Board);
            return;
        }


        if (_PaintingTerrain &&
            Board.HasTerrainData)
        {
            HandleTerrainPainting(Board);
        }
    }


    private void HandleOriginPicking(
        MapBoard Board
    )
    {
        Event CurrentEvent =
            Event.current;


        HandleUtility.AddDefaultControl(
            GUIUtility.GetControlID(
                FocusType.Passive
            )
        );


        if (!TryGetHoveredTilemapCell(
                Board,
                out Vector3Int HoveredCell))
        {
            return;
        }


        DrawTilemapCellHighlight(
            Board.TerrainTilemap,
            HoveredCell,
            new Color(
                1f,
                1f,
                0f,
                0.25f
            ),
            Color.yellow
        );


        if (CurrentEvent.type !=
                EventType.MouseDown ||
            CurrentEvent.button != 0)
        {
            return;
        }


        SerializedProperty OriginProperty =
            serializedObject.FindProperty(
                "_TilemapOrigin"
            );


        Undo.RecordObject(
            Board,
            "Set Map Tilemap Origin"
        );


        OriginProperty.vector2IntValue =
            new Vector2Int(
                HoveredCell.x,
                HoveredCell.y
            );


        serializedObject.ApplyModifiedProperties();

        EditorUtility.SetDirty(Board);


        _PickingOrigin = false;

        CurrentEvent.Use();

        Repaint();
        SceneView.RepaintAll();
    }


    private void HandleTerrainPainting(
        MapBoard Board
    )
    {
        Event CurrentEvent =
            Event.current;


        HandleUtility.AddDefaultControl(
            GUIUtility.GetControlID(
                FocusType.Passive
            )
        );


        if (!TryGetHoveredTilemapCell(
                Board,
                out Vector3Int TilemapCell))
        {
            return;
        }


        Vector2Int BoardCoordinate =
            Board.TilemapCellToBoard(
                TilemapCell
            );


        if (!Board.IsWithinBoard(
                BoardCoordinate))
        {
            return;
        }


        DrawTilemapCellHighlight(
            Board.TerrainTilemap,
            TilemapCell,
            GetTerrainColor(
                _SelectedTerrain,
                0.55f
            ),
            Color.white
        );


        bool IsPainting =
            (
                CurrentEvent.type ==
                EventType.MouseDown ||
                CurrentEvent.type ==
                EventType.MouseDrag
            ) &&
            CurrentEvent.button == 0;


        if (!IsPainting)
        {
            return;
        }


        if (Board.TryGetTerrainType(
                BoardCoordinate,
                out TerrainType CurrentTerrain) &&
            CurrentTerrain == _SelectedTerrain)
        {
            CurrentEvent.Use();
            return;
        }


        Undo.RecordObject(
            Board,
            "Paint Map Terrain"
        );


        Board.SetTerrainType(
            BoardCoordinate,
            _SelectedTerrain
        );


        EditorUtility.SetDirty(Board);

        CurrentEvent.Use();

        Repaint();
        SceneView.RepaintAll();
    }


    private void DrawTerrainOverlay(
        MapBoard Board
    )
    {
        Vector2Int BoardSize =
            Board.BoardSize;


        for (int Y = 0; Y < BoardSize.y; Y++)
        {
            for (int X = 0; X < BoardSize.x; X++)
            {
                Vector2Int BoardCoordinate =
                    new Vector2Int(
                        X,
                        Y
                    );


                if (!Board.TryGetTerrainType(
                        BoardCoordinate,
                        out TerrainType Terrain))
                {
                    continue;
                }


                if (Terrain == TerrainType.DeepWater &&
                    !_ShowDeepWaterOverlay)
                {
                    continue;
                }


                Vector3Int TilemapCell =
                    Board.BoardToTilemapCell(
                        BoardCoordinate
                    );


                DrawTilemapCellHighlight(
                    Board.TerrainTilemap,
                    TilemapCell,
                    GetTerrainColor(
                        Terrain,
                        0.25f
                    ),
                    new Color(
                        1f,
                        1f,
                        1f,
                        0.15f
                    )
                );
            }
        }
    }


    private void DrawBoardOutline(
        MapBoard Board
    )
    {
        if (Board.BoardSize.x <= 0 ||
            Board.BoardSize.y <= 0)
        {
            return;
        }


        Vector3Int BottomLeftCell =
            Board.BoardToTilemapCell(
                Vector2Int.zero
            );


        Vector3Int TopRightCell =
            Board.BoardToTilemapCell(
                new Vector2Int(
                    Board.BoardSize.x,
                    Board.BoardSize.y
                )
            );


        Vector3 BottomLeft =
            Board.TerrainTilemap.CellToWorld(
                BottomLeftCell
            );


        Vector3 TopRight =
            Board.TerrainTilemap.CellToWorld(
                TopRightCell
            );


        Vector3[] Corners =
        {
            new Vector3(
                BottomLeft.x,
                BottomLeft.y,
                BottomLeft.z
            ),

            new Vector3(
                TopRight.x,
                BottomLeft.y,
                BottomLeft.z
            ),

            new Vector3(
                TopRight.x,
                TopRight.y,
                BottomLeft.z
            ),

            new Vector3(
                BottomLeft.x,
                TopRight.y,
                BottomLeft.z
            )
        };


        Handles.DrawSolidRectangleWithOutline(
            Corners,
            Color.clear,
            Color.yellow
        );
    }


    private bool TryGetHoveredTilemapCell(
        MapBoard Board,
        out Vector3Int Cell
    )
    {
        Ray MouseRay =
            HandleUtility.GUIPointToWorldRay(
                Event.current.mousePosition
            );


        Plane MapPlane =
            new Plane(
                Vector3.forward,
                Board.TerrainTilemap
                    .transform.position
            );


        if (!MapPlane.Raycast(
                MouseRay,
                out float Distance))
        {
            Cell = default;
            return false;
        }


        Vector3 WorldPosition =
            MouseRay.GetPoint(
                Distance
            );


        Cell =
            Board.TerrainTilemap.WorldToCell(
                WorldPosition
            );


        return true;
    }


    private void DrawTilemapCellHighlight(
        Tilemap Tilemap,
        Vector3Int Cell,
        Color FillColor,
        Color OutlineColor
    )
    {
        Vector3 BottomLeft =
            Tilemap.CellToWorld(
                Cell
            );


        Vector3 TopRight =
            Tilemap.CellToWorld(
                Cell +
                new Vector3Int(
                    1,
                    1,
                    0
                )
            );


        Vector3[] Corners =
        {
            new Vector3(
                BottomLeft.x,
                BottomLeft.y,
                BottomLeft.z
            ),

            new Vector3(
                TopRight.x,
                BottomLeft.y,
                BottomLeft.z
            ),

            new Vector3(
                TopRight.x,
                TopRight.y,
                BottomLeft.z
            ),

            new Vector3(
                BottomLeft.x,
                TopRight.y,
                BottomLeft.z
            )
        };


        Handles.DrawSolidRectangleWithOutline(
            Corners,
            FillColor,
            OutlineColor
        );
    }


    private Color GetTerrainColor(
        TerrainType Terrain,
        float Alpha
    )
    {
        switch (Terrain)
        {
            case TerrainType.DeepWater:
                return new Color(
                    0.1f,
                    0.4f,
                    1f,
                    Alpha
                );

            case TerrainType.ShallowWater:
                return new Color(
                    0f,
                    1f,
                    1f,
                    Alpha
                );

            case TerrainType.Land:
                return new Color(
                    0.3f,
                    1f,
                    0.2f,
                    Alpha
                );

            case TerrainType.Barrier:
                return new Color(
                    1f,
                    0.15f,
                    0.15f,
                    Alpha
                );

            default:
                return Color.clear;
        }
    }
}