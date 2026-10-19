using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Unity Editor Script - Not Attached To A GameObject
// Provides Scene Tools For Authoring Terrain And Deployment Zones
[CustomEditor(typeof(MapBoard))]
public class MapBoardEditor : Editor
{
    private enum PaintingMode
    {
        None,
        Terrain,
        DeploymentZone
    }


    private MapBoard _Board;

    private PaintingMode _PaintingMode =
        PaintingMode.None;


    private TerrainType _SelectedTerrain =
        TerrainType.DeepWater;

    private DeploymentZone _SelectedDeploymentZone =
        DeploymentZone.ZoneA;


    private bool _ShowTerrainOverlay =
        true;

    private bool _ShowDeepWater =
        false;

    private bool _ShowDeploymentZoneOverlay =
        true;

    private bool _PickingTilemapOrigin =
        false;


    private static readonly Color DeepWaterColor =
        new Color(
            0.10f,
            0.35f,
            1.00f,
            0.20f
        );

    private static readonly Color ShallowWaterColor =
        new Color(
            0.00f,
            0.90f,
            1.00f,
            0.28f
        );

    private static readonly Color LandColor =
        new Color(
            0.15f,
            0.90f,
            0.20f,
            0.28f
        );

    private static readonly Color BarrierColor =
        new Color(
            1.00f,
            0.10f,
            0.10f,
            0.30f
        );


    private static readonly Color ZoneAColor =
        new Color(
            0.15f,
            0.45f,
            1.00f,
            0.30f
        );

    private static readonly Color ZoneBColor =
        new Color(
            1.00f,
            0.20f,
            0.20f,
            0.30f
        );

    private static readonly Color BoardOutlineColor =
        new Color(
            1.00f,
            0.85f,
            0.10f,
            1.00f
        );

    private static readonly Color HoverColor =
        new Color(
            1.00f,
            1.00f,
            1.00f,
            0.45f
        );


    private void OnEnable()
    {
        _Board =
            (MapBoard)target;


        SceneView.duringSceneGui +=
            OnSceneGUI;
    }


    private void OnDisable()
    {
        SceneView.duringSceneGui -=
            OnSceneGUI;
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        DrawDefaultInspector();


        EditorGUILayout.Space(12);


        DrawBoardSetupTools();


        EditorGUILayout.Space(12);


        DrawTerrainTools();


        EditorGUILayout.Space(12);


        DrawDeploymentZoneTools();


        serializedObject.ApplyModifiedProperties();
    }


    private void DrawBoardSetupTools()
    {
        EditorGUILayout.LabelField(
            "Board Setup",
            EditorStyles.boldLabel
        );


        if (_PickingTilemapOrigin)
        {
            EditorGUILayout.HelpBox(
                "Click the bottom-left gameplay cell in the Scene View.",
                MessageType.Info
            );


            if (GUILayout.Button(
                    "Cancel Picking Bottom-Left Cell"))
            {
                _PickingTilemapOrigin =
                    false;

                SceneView.RepaintAll();
            }
        }
        else
        {
            if (GUILayout.Button(
                    "Pick Bottom-Left Board Cell"))
            {
                _PickingTilemapOrigin =
                    true;

                _PaintingMode =
                    PaintingMode.None;

                SceneView.RepaintAll();
            }
        }
    }


    private void DrawTerrainTools()
    {
        EditorGUILayout.LabelField(
            "Terrain Authoring",
            EditorStyles.boldLabel
        );


        if (!_Board.HasTerrainData)
        {
            EditorGUILayout.HelpBox(
                "Terrain data has not been initialized for this board size.",
                MessageType.Warning
            );
        }


        if (GUILayout.Button(
                "Initialize Terrain Data"))
        {
            Undo.RecordObject(
                _Board,
                "Initialize Terrain Data"
            );


            _Board.InitializeTerrainData();


            MarkBoardDirty();
        }


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


        _ShowDeepWater =
            EditorGUILayout.Toggle(
                "Show Deep Water",
                _ShowDeepWater
            );


        bool IsPaintingTerrain =
            _PaintingMode ==
            PaintingMode.Terrain;


        string PaintButtonText =
            IsPaintingTerrain
                ? "Stop Terrain Painting"
                : "Start Terrain Painting";


        if (GUILayout.Button(
                PaintButtonText))
        {
            _PaintingMode =
                IsPaintingTerrain
                    ? PaintingMode.None
                    : PaintingMode.Terrain;


            _PickingTilemapOrigin =
                false;


            SceneView.RepaintAll();
        }


        if (GUILayout.Button(
                "Fill Entire Board With Deep Water"))
        {
            Undo.RecordObject(
                _Board,
                "Fill Board With Deep Water"
            );


            _Board.FillTerrain(
                TerrainType.DeepWater
            );


            MarkBoardDirty();
        }
    }


    private void DrawDeploymentZoneTools()
    {
        EditorGUILayout.LabelField(
            "Deployment Zone Authoring",
            EditorStyles.boldLabel
        );


        if (!_Board.HasDeploymentZoneData)
        {
            EditorGUILayout.HelpBox(
                "Deployment zone data has not been initialized. Initialize it once before painting.",
                MessageType.Warning
            );
        }


        if (GUILayout.Button(
                "Initialize Deployment Zone Data"))
        {
            Undo.RecordObject(
                _Board,
                "Initialize Deployment Zone Data"
            );


            _Board.InitializeDeploymentZoneData();


            MarkBoardDirty();
        }


        _SelectedDeploymentZone =
            (DeploymentZone)EditorGUILayout.EnumPopup(
                "Selected Zone",
                _SelectedDeploymentZone
            );


        _ShowDeploymentZoneOverlay =
            EditorGUILayout.Toggle(
                "Show Deployment Zone Overlay",
                _ShowDeploymentZoneOverlay
            );


        bool IsPaintingDeploymentZone =
            _PaintingMode ==
            PaintingMode.DeploymentZone;


        string PaintButtonText =
            IsPaintingDeploymentZone
                ? "Stop Deployment Zone Painting"
                : "Start Deployment Zone Painting";


        if (GUILayout.Button(
                PaintButtonText))
        {
            _PaintingMode =
                IsPaintingDeploymentZone
                    ? PaintingMode.None
                    : PaintingMode.DeploymentZone;


            _PickingTilemapOrigin =
                false;


            SceneView.RepaintAll();
        }


        if (GUILayout.Button(
                "Clear All Deployment Zones"))
        {
            Undo.RecordObject(
                _Board,
                "Clear Deployment Zones"
            );


            _Board.FillDeploymentZones(
                DeploymentZone.None
            );


            MarkBoardDirty();
        }
    }


    private void OnSceneGUI(
        SceneView SceneView
    )
    {
        if (_Board == null ||
            _Board.TerrainTilemap == null)
        {
            return;
        }


        DrawBoardOutline();


        if (_ShowTerrainOverlay &&
            _Board.HasTerrainData)
        {
            DrawTerrainOverlay();
        }


        if (_ShowDeploymentZoneOverlay &&
            _Board.HasDeploymentZoneData)
        {
            DrawDeploymentZoneOverlay();
        }


        Event CurrentEvent =
            Event.current;


        if (_PickingTilemapOrigin)
        {
            HandleOriginPicking(
                CurrentEvent
            );

            return;
        }


        if (_PaintingMode ==
            PaintingMode.None)
        {
            return;
        }


        HandlePainting(
            CurrentEvent
        );
    }


    private void HandleOriginPicking(
        Event CurrentEvent
    )
    {
        HandleUtility.AddDefaultControl(
            GUIUtility.GetControlID(
                FocusType.Passive
            )
        );


        if (!TryGetMouseWorldPosition(
                CurrentEvent.mousePosition,
                out Vector3 WorldPosition))
        {
            return;
        }


        Vector3Int TilemapCell =
            _Board.TerrainTilemap.WorldToCell(
                WorldPosition
            );


        DrawCellOutline(
            TilemapCell,
            HoverColor
        );


        if (CurrentEvent.type !=
                EventType.MouseDown ||
            CurrentEvent.button != 0)
        {
            return;
        }


        Undo.RecordObject(
            _Board,
            "Set Board Tilemap Origin"
        );


        SerializedProperty OriginProperty =
            serializedObject.FindProperty(
                "_TilemapOrigin"
            );


        OriginProperty.vector2IntValue =
            new Vector2Int(
                TilemapCell.x,
                TilemapCell.y
            );


        serializedObject.ApplyModifiedProperties();


        _PickingTilemapOrigin =
            false;


        MarkBoardDirty();


        CurrentEvent.Use();
    }


    private void HandlePainting(
        Event CurrentEvent
    )
    {
        HandleUtility.AddDefaultControl(
            GUIUtility.GetControlID(
                FocusType.Passive
            )
        );


        if (!TryGetMouseWorldPosition(
                CurrentEvent.mousePosition,
                out Vector3 WorldPosition))
        {
            return;
        }


        Vector2Int BoardCoordinate =
            _Board.WorldToBoard(
                WorldPosition
            );


        if (!_Board.IsWithinBoard(
                BoardCoordinate))
        {
            return;
        }


        Vector3Int TilemapCell =
            _Board.BoardToTilemapCell(
                BoardCoordinate
            );


        DrawCellOutline(
            TilemapCell,
            HoverColor
        );


        bool ShouldPaint =
            CurrentEvent.button == 0 &&
            (
                CurrentEvent.type ==
                    EventType.MouseDown ||
                CurrentEvent.type ==
                    EventType.MouseDrag
            );


        if (!ShouldPaint)
        {
            return;
        }


        Undo.RecordObject(
            _Board,
            "Paint Map Cell"
        );


        switch (_PaintingMode)
        {
            case PaintingMode.Terrain:

                _Board.SetTerrainType(
                    BoardCoordinate,
                    _SelectedTerrain
                );

                break;


            case PaintingMode.DeploymentZone:

                _Board.SetDeploymentZone(
                    BoardCoordinate,
                    _SelectedDeploymentZone
                );

                break;
        }


        MarkBoardDirty();


        CurrentEvent.Use();
    }


    private void DrawTerrainOverlay()
    {
        for (
            int Y = 0;
            Y < _Board.BoardSize.y;
            Y++
        )
        {
            for (
                int X = 0;
                X < _Board.BoardSize.x;
                X++
            )
            {
                Vector2Int Coordinate =
                    new Vector2Int(
                        X,
                        Y
                    );


                if (!_Board.TryGetTerrainType(
                        Coordinate,
                        out TerrainType Terrain))
                {
                    continue;
                }


                if (Terrain ==
                        TerrainType.DeepWater &&
                    !_ShowDeepWater)
                {
                    continue;
                }


                DrawCellFill(
                    Coordinate,
                    GetTerrainColor(
                        Terrain
                    )
                );
            }
        }
    }


    private void DrawDeploymentZoneOverlay()
    {
        for (
            int Y = 0;
            Y < _Board.BoardSize.y;
            Y++
        )
        {
            for (
                int X = 0;
                X < _Board.BoardSize.x;
                X++
            )
            {
                Vector2Int Coordinate =
                    new Vector2Int(
                        X,
                        Y
                    );


                if (!_Board.TryGetDeploymentZone(
                        Coordinate,
                        out DeploymentZone Zone))
                {
                    continue;
                }


                if (Zone ==
                    DeploymentZone.None)
                {
                    continue;
                }


                DrawCellFill(
                    Coordinate,
                    GetDeploymentZoneColor(
                        Zone
                    )
                );
            }
        }
    }


    private void DrawBoardOutline()
    {
        Vector3Int BottomLeftCell =
            _Board.BoardToTilemapCell(
                Vector2Int.zero
            );


        Vector3Int TopRightCell =
            _Board.BoardToTilemapCell(
                _Board.BoardSize
            );


        Vector3 BottomLeft =
            _Board.TerrainTilemap.CellToWorld(
                BottomLeftCell
            );


        Vector3 TopRight =
            _Board.TerrainTilemap.CellToWorld(
                TopRightCell
            );


        Vector3 BottomRight =
            new Vector3(
                TopRight.x,
                BottomLeft.y,
                BottomLeft.z
            );


        Vector3 TopLeft =
            new Vector3(
                BottomLeft.x,
                TopRight.y,
                BottomLeft.z
            );


        Handles.color =
            BoardOutlineColor;


        Handles.DrawLine(
            BottomLeft,
            BottomRight
        );

        Handles.DrawLine(
            BottomRight,
            TopRight
        );

        Handles.DrawLine(
            TopRight,
            TopLeft
        );

        Handles.DrawLine(
            TopLeft,
            BottomLeft
        );
    }


    private void DrawCellFill(
        Vector2Int BoardCoordinate,
        Color Color
    )
    {
        Vector3Int TilemapCell =
            _Board.BoardToTilemapCell(
                BoardCoordinate
            );


        Vector3 BottomLeft =
            _Board.TerrainTilemap.CellToWorld(
                TilemapCell
            );


        Vector3 TopRight =
            _Board.TerrainTilemap.CellToWorld(
                TilemapCell +
                new Vector3Int(
                    1,
                    1,
                    0
                )
            );


        Vector3[] Vertices =
        {
            BottomLeft,

            new Vector3(
                TopRight.x,
                BottomLeft.y,
                BottomLeft.z
            ),

            TopRight,

            new Vector3(
                BottomLeft.x,
                TopRight.y,
                BottomLeft.z
            )
        };


        Handles.DrawSolidRectangleWithOutline(
            Vertices,
            Color,
            Color.clear
        );
    }


    private void DrawCellOutline(
        Vector3Int TilemapCell,
        Color Color
    )
    {
        Vector3 BottomLeft =
            _Board.TerrainTilemap.CellToWorld(
                TilemapCell
            );


        Vector3 TopRight =
            _Board.TerrainTilemap.CellToWorld(
                TilemapCell +
                new Vector3Int(
                    1,
                    1,
                    0
                )
            );


        Vector3[] Vertices =
        {
            BottomLeft,

            new Vector3(
                TopRight.x,
                BottomLeft.y,
                BottomLeft.z
            ),

            TopRight,

            new Vector3(
                BottomLeft.x,
                TopRight.y,
                BottomLeft.z
            )
        };


        Handles.DrawSolidRectangleWithOutline(
            Vertices,
            Color.clear,
            Color
        );
    }


    private bool TryGetMouseWorldPosition(
        Vector2 MousePosition,
        out Vector3 WorldPosition
    )
    {
        Ray MouseRay =
            HandleUtility.GUIPointToWorldRay(
                MousePosition
            );


        float PlaneZ =
            _Board.TerrainTilemap.transform.position.z;


        if (Mathf.Approximately(
                MouseRay.direction.z,
                0f))
        {
            WorldPosition =
                Vector3.zero;

            return false;
        }


        float Distance =
            (
                PlaneZ -
                MouseRay.origin.z
            ) /
            MouseRay.direction.z;


        if (Distance < 0f)
        {
            WorldPosition =
                Vector3.zero;

            return false;
        }


        WorldPosition =
            MouseRay.GetPoint(
                Distance
            );


        return true;
    }


    private Color GetTerrainColor(
        TerrainType Terrain
    )
    {
        switch (Terrain)
        {
            case TerrainType.DeepWater:

                return DeepWaterColor;


            case TerrainType.ShallowWater:

                return ShallowWaterColor;


            case TerrainType.Land:

                return LandColor;


            case TerrainType.Barrier:

                return BarrierColor;


            default:

                return Color.clear;
        }
    }


    private Color GetDeploymentZoneColor(
        DeploymentZone Zone
    )
    {
        switch (Zone)
        {
            case DeploymentZone.ZoneA:

                return ZoneAColor;


            case DeploymentZone.ZoneB:

                return ZoneBColor;


            default:

                return Color.clear;
        }
    }


    private void MarkBoardDirty()
    {
        EditorUtility.SetDirty(
            _Board
        );


        PrefabUtility.RecordPrefabInstancePropertyModifications(
            _Board
        );


        SceneView.RepaintAll();
    }
}