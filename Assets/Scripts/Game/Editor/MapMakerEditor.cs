using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;


[InitializeOnLoad]
public static class MapMakerSceneOverlay
{
    public static bool ShowTerrainOverlay =
        true;

    public static bool ShowDeploymentOverlay =
        true;

    public static bool ShowGuides =
        true;


    static MapMakerSceneOverlay()
    {
        SceneView.duringSceneGui -=
            DrawSceneOverlay;

        SceneView.duringSceneGui +=
            DrawSceneOverlay;
    }


    private static void DrawSceneOverlay(
        SceneView SceneView
    )
    {
        MapAuthoring Map =
            GetSelectedMap();


        if (Map == null ||
            Map.GridReference == null)
        {
            return;
        }


        CompareFunction PreviousZTest =
            Handles.zTest;


        Handles.zTest =
            CompareFunction.Always;


        if (ShowTerrainOverlay ||
            ShowDeploymentOverlay)
        {
            DrawTileOverlays(
                Map
            );
        }


        if (ShowGuides)
        {
            DrawMapGuides(
                Map
            );
        }


        Handles.zTest =
            PreviousZTest;
    }


    public static MapAuthoring GetSelectedMap()
    {
        GameObject SelectedObject =
            Selection.activeGameObject;


        if (SelectedObject == null)
        {
            return null;
        }


        return
            SelectedObject.GetComponentInParent<MapAuthoring>();
    }


    private static void DrawTileOverlays(
        MapAuthoring Map
    )
    {
        Vector2Int Size =
            Map.MapSize;


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
                Vector2Int Coordinate =
                    new Vector2Int(
                        X,
                        Y
                    );


                if (!Map.TryGetTile(
                        Coordinate,
                        out MapTileAuthoringData Tile))
                {
                    continue;
                }


                Vector3[] Corners =
                    GetCellCorners(
                        Map,
                        Coordinate
                    );


                if (ShowTerrainOverlay)
                {
                    Color TerrainColor =
                        GetTerrainColor(
                            Tile.Terrain
                        );


                    if (TerrainColor.a >
                        0f)
                    {
                        Handles.DrawSolidRectangleWithOutline(
                            Corners,
                            TerrainColor,
                            new Color(
                                0f,
                                0f,
                                0f,
                                0.10f
                            )
                        );
                    }
                    else
                    {
                        Handles.DrawSolidRectangleWithOutline(
                            Corners,
                            Color.clear,
                            new Color(
                                1f,
                                1f,
                                1f,
                                0.08f
                            )
                        );
                    }
                }


                if (ShowDeploymentOverlay)
                {
                    MapTileCondition Deployment =
                        Tile.GetDeploymentCondition();


                    Color DeploymentColor =
                        GetDeploymentColor(
                            Deployment
                        );


                    if (DeploymentColor.a >
                        0f)
                    {
                        Handles.DrawSolidRectangleWithOutline(
                            Corners,
                            DeploymentColor,
                            Color.clear
                        );
                    }
                }
            }
        }
    }


    private static void DrawMapGuides(
        MapAuthoring Map
    )
    {
        Vector2Int Size =
            Map.MapSize;


        Vector3 BottomLeft =
            GetGridPoint(
                Map,
                0f,
                0f
            );

        Vector3 BottomRight =
            GetGridPoint(
                Map,
                Size.x,
                0f
            );

        Vector3 TopLeft =
            GetGridPoint(
                Map,
                0f,
                Size.y
            );

        Vector3 TopRight =
            GetGridPoint(
                Map,
                Size.x,
                Size.y
            );


        Handles.color =
            Color.white;


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


        float MeridianX =
            Size.x *
            0.5f;

        float EquatorY =
            Size.y *
            0.5f;


        Handles.color =
            new Color(
                1f,
                1f,
                1f,
                0.70f
            );


        Handles.DrawLine(
            GetGridPoint(
                Map,
                MeridianX,
                0f
            ),
            GetGridPoint(
                Map,
                MeridianX,
                Size.y
            )
        );


        Handles.DrawLine(
            GetGridPoint(
                Map,
                0f,
                EquatorY
            ),
            GetGridPoint(
                Map,
                Size.x,
                EquatorY
            )
        );


        Handles.color =
            Color.white;
    }


    public static Vector3[] GetCellCorners(
        MapAuthoring Map,
        Vector2Int Coordinate
    )
    {
        return
            new Vector3[]
            {
                GetGridPoint(
                    Map,
                    Coordinate.x,
                    Coordinate.y
                ),

                GetGridPoint(
                    Map,
                    Coordinate.x + 1f,
                    Coordinate.y
                ),

                GetGridPoint(
                    Map,
                    Coordinate.x + 1f,
                    Coordinate.y + 1f
                ),

                GetGridPoint(
                    Map,
                    Coordinate.x,
                    Coordinate.y + 1f
                )
            };
    }


    public static Vector3 GetGridPoint(
        MapAuthoring Map,
        float X,
        float Y
    )
    {
        Vector3 LocalPosition =
            Map.GridReference.CellToLocalInterpolated(
                new Vector3(
                    X,
                    Y,
                    0f
                )
            );


        return
            Map.GridReference.transform.TransformPoint(
                LocalPosition
            );
    }


    private static Color GetTerrainColor(
        MapTerrain Terrain
    )
    {
        switch (Terrain)
        {
            case MapTerrain.Deep_Water:

                return
                    new Color(
                        0.05f,
                        0.20f,
                        0.70f,
                        0.25f
                    );


            case MapTerrain.Shallow_Water:

                return
                    new Color(
                        0.20f,
                        0.65f,
                        1f,
                        0.25f
                    );


            case MapTerrain.Land:

                return
                    new Color(
                        0.10f,
                        0.75f,
                        0.20f,
                        0.25f
                    );


            case MapTerrain.Barrier:

                return
                    new Color(
                        0.90f,
                        0.10f,
                        0.10f,
                        0.32f
                    );


            default:

                return
                    Color.clear;
        }
    }


    private static Color GetDeploymentColor(
        MapTileCondition Condition
    )
    {
        switch (Condition)
        {
            case MapTileCondition.Deployment_Player_A:

                return
                    new Color(
                        1f,
                        0.90f,
                        0.10f,
                        0.35f
                    );


            case MapTileCondition.Deployment_Player_B:

                return
                    new Color(
                        1f,
                        0.40f,
                        0.05f,
                        0.35f
                    );


            default:

                return
                    Color.clear;
        }
    }
}


[CustomEditor(typeof(MapAuthoring))]
public class MapMakerEditor :
    Editor
{
    private enum AuthoringLayer
    {
        Terrain = 0,
        Deployment = 1
    }


    private enum PaintMode
    {
        Brush = 0,
        Bucket = 1
    }


    private MapAuthoring _Map;

    private Vector2Int _RequestedSize;

    private AuthoringLayer _AuthoringLayer =
        AuthoringLayer.Terrain;

    private PaintMode _PaintMode =
        PaintMode.Brush;

    private MapTerrain _TerrainBrush =
        MapTerrain.Deep_Water;

    private MapTileCondition _DeploymentBrush =
        MapTileCondition.Deployment_Player_A;


    private bool _ScenePaintingEnabled =
        true;


    private string _ValidationMessage =
        string.Empty;

    private MessageType _ValidationMessageType =
        MessageType.None;


    private void OnEnable()
    {
        _Map =
            target as MapAuthoring;


        if (_Map != null)
        {
            _RequestedSize =
                _Map.MapSize;
        }
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();


        EditorGUILayout.LabelField(
            "Map Prefab References",
            EditorStyles.boldLabel
        );


        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "_MapSize",
            "_Tiles"
        );


        serializedObject.ApplyModifiedProperties();


        EditorGUILayout.Space(10);

        DrawMapSize();

        EditorGUILayout.Space(10);

        DrawMapOperations();

        EditorGUILayout.Space(10);

        DrawAuthoringControls();

        EditorGUILayout.Space(10);

        DrawOverlayControls();

        EditorGUILayout.Space(10);

        DrawValidation();


        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }


    private void DrawMapSize()
    {
        EditorGUILayout.LabelField(
            "Map Bounds",
            EditorStyles.boldLabel
        );


        EditorGUILayout.LabelField(
            "Current Size",
            $"{_Map.MapSize.x} × {_Map.MapSize.y}"
        );


        _RequestedSize =
            EditorGUILayout.Vector2IntField(
                "Requested Size",
                _RequestedSize
            );


        EditorGUILayout.HelpBox(
            "Expansion adds empty cells around the existing Map while preserving and shifting authored data and visuals together. " +
            "Shrinking is allowed only through empty outer rows or columns.",
            MessageType.Info
        );


        if (GUILayout.Button(
                "Resize Map Bounds"))
        {
            RecordResizeUndo();


            if (_Map.ResizeMap(
                    _RequestedSize,
                    out string ValidationMessage))
            {
                EditorUtility.SetDirty(
                    _Map
                );


                MarkVisualLayersDirty();


                _ValidationMessage =
                    $"Map bounds are now {_Map.MapSize.x} × {_Map.MapSize.y}.";

                _ValidationMessageType =
                    MessageType.Info;
            }
            else
            {
                _ValidationMessage =
                    ValidationMessage;

                _ValidationMessageType =
                    MessageType.Error;
            }


            SceneView.RepaintAll();
        }
    }


    private void RecordResizeUndo()
    {
        List<UnityEngine.Object> Objects =
            new List<UnityEngine.Object>();


        Objects.Add(
            _Map
        );


        AddUndoObject(
            Objects,
            _Map.TerrainArt
        );

        AddUndoObject(
            Objects,
            _Map.BelowUnits
        );

        AddUndoObject(
            Objects,
            _Map.TileMarkers
        );

        AddUndoObject(
            Objects,
            _Map.Fog
        );

        AddUndoObject(
            Objects,
            _Map.VFX
        );


        Undo.RecordObjects(
            Objects.ToArray(),
            "Resize Admirals Map"
        );
    }


    private void AddUndoObject(
        List<UnityEngine.Object> Objects,
        UnityEngine.Object Object
    )
    {
        if (Object != null)
        {
            Objects.Add(
                Object
            );
        }
    }


    private void MarkVisualLayersDirty()
    {
        MarkDirty(
            _Map.TerrainArt
        );

        MarkDirty(
            _Map.BelowUnits
        );

        MarkDirty(
            _Map.TileMarkers
        );

        MarkDirty(
            _Map.Fog
        );

        MarkDirty(
            _Map.VFX
        );
    }


    private void MarkDirty(
        UnityEngine.Object Object
    )
    {
        if (Object != null)
        {
            EditorUtility.SetDirty(
                Object
            );
        }
    }


    private void DrawMapOperations()
    {
        EditorGUILayout.LabelField(
            "Map Operations",
            EditorStyles.boldLabel
        );


        if (GUILayout.Button(
                "Prime Map Terrain"))
        {
            bool Confirmed =
                EditorUtility.DisplayDialog(
                    "Prime Map Terrain",
                    "This will set the outer Map edge to Barrier and every interior Tile to Deep Water.\n\n" +
                    "Visual Art and Tile Conditions will not be changed.",
                    "Prime Terrain",
                    "Cancel"
                );


            if (Confirmed)
            {
                Undo.RecordObject(
                    _Map,
                    "Prime Admirals Map"
                );


                _Map.PrimeMap();


                EditorUtility.SetDirty(
                    _Map
                );


                SceneView.RepaintAll();
            }
        }
    }


    private void DrawAuthoringControls()
    {
        EditorGUILayout.LabelField(
            "Data Painting",
            EditorStyles.boldLabel
        );


        _ScenePaintingEnabled =
            EditorGUILayout.Toggle(
                "Scene Painting Enabled",
                _ScenePaintingEnabled
            );


        _AuthoringLayer =
            (AuthoringLayer)
            EditorGUILayout.EnumPopup(
                "Authoring Layer",
                _AuthoringLayer
            );


        _PaintMode =
            (PaintMode)
            EditorGUILayout.EnumPopup(
                "Paint Mode",
                _PaintMode
            );


        switch (_AuthoringLayer)
        {
            case AuthoringLayer.Terrain:

                _TerrainBrush =
                    (MapTerrain)
                    EditorGUILayout.EnumPopup(
                        "Terrain",
                        _TerrainBrush
                    );

                break;


            case AuthoringLayer.Deployment:

                _DeploymentBrush =
                    (MapTileCondition)
                    EditorGUILayout.EnumPopup(
                        "Deployment Zone",
                        _DeploymentBrush
                    );


                if (_DeploymentBrush !=
                        MapTileCondition.Deployment_Player_A &&
                    _DeploymentBrush !=
                        MapTileCondition.Deployment_Player_B)
                {
                    _DeploymentBrush =
                        MapTileCondition.Deployment_Player_A;
                }

                break;
        }


        EditorGUILayout.HelpBox(
            "Left Mouse paints. Right Mouse erases. " +
            "Brush paints individual Tiles. Bucket flood-fills the contiguous matching region.",
            MessageType.Info
        );
    }


    private void DrawOverlayControls()
    {
        EditorGUILayout.LabelField(
            "Scene View",
            EditorStyles.boldLabel
        );


        MapMakerSceneOverlay.ShowGuides =
            EditorGUILayout.Toggle(
                "Show Bounds / Center Guides",
                MapMakerSceneOverlay.ShowGuides
            );


        MapMakerSceneOverlay.ShowTerrainOverlay =
            EditorGUILayout.Toggle(
                "Show Terrain Data",
                MapMakerSceneOverlay.ShowTerrainOverlay
            );


        MapMakerSceneOverlay.ShowDeploymentOverlay =
            EditorGUILayout.Toggle(
                "Show Deployment Zones",
                MapMakerSceneOverlay.ShowDeploymentOverlay
            );
    }


    private void DrawValidation()
    {
        EditorGUILayout.LabelField(
            "Validation",
            EditorStyles.boldLabel
        );


        if (GUILayout.Button(
                "Validate Authored Map"))
        {
            if (_Map.ValidateMap(
                    out string ValidationMessage))
            {
                _ValidationMessageType =
                    MessageType.Info;
            }
            else
            {
                _ValidationMessageType =
                    MessageType.Error;
            }


            _ValidationMessage =
                ValidationMessage;
        }


        if (!string.IsNullOrWhiteSpace(
                _ValidationMessage))
        {
            EditorGUILayout.HelpBox(
                _ValidationMessage,
                _ValidationMessageType
            );
        }
    }


    private void OnSceneGUI()
    {
        if (_Map == null ||
            _Map.GridReference == null)
        {
            return;
        }


        DrawHoveredTile();


        if (_ScenePaintingEnabled)
        {
            HandleScenePainting();
        }
    }


    private void DrawHoveredTile()
    {
        if (!TryGetMouseCoordinate(
                out Vector2Int Coordinate))
        {
            return;
        }


        if (!_Map.IsInsideBounds(
                Coordinate))
        {
            return;
        }


        CompareFunction PreviousZTest =
            Handles.zTest;


        Handles.zTest =
            CompareFunction.Always;


        Handles.DrawSolidRectangleWithOutline(
            MapMakerSceneOverlay.GetCellCorners(
                _Map,
                Coordinate
            ),
            Color.clear,
            new Color(
                1f,
                1f,
                1f,
                0.9f
            )
        );


        Handles.zTest =
            PreviousZTest;
    }


    private void HandleScenePainting()
    {
        Event CurrentEvent =
            Event.current;


        if (CurrentEvent.alt)
        {
            return;
        }


        int ControlID =
            GUIUtility.GetControlID(
                FocusType.Passive
            );


        if (CurrentEvent.type ==
            EventType.Layout)
        {
            HandleUtility.AddDefaultControl(
                ControlID
            );
        }


        bool IsMouseDown =
            CurrentEvent.type ==
            EventType.MouseDown;

        bool IsMouseDrag =
            CurrentEvent.type ==
            EventType.MouseDrag;


        if (!IsMouseDown &&
            !IsMouseDrag)
        {
            return;
        }


        if (CurrentEvent.button != 0 &&
            CurrentEvent.button != 1)
        {
            return;
        }


        if (_PaintMode ==
                PaintMode.Bucket &&
            IsMouseDrag)
        {
            return;
        }


        if (!TryGetMouseCoordinate(
                out Vector2Int Coordinate))
        {
            return;
        }


        if (!_Map.IsInsideBounds(
                Coordinate))
        {
            return;
        }


        bool Erase =
            CurrentEvent.button ==
            1;


        Undo.RecordObject(
            _Map,
            "Paint Admirals Map Data"
        );


        switch (_PaintMode)
        {
            case PaintMode.Brush:

                PaintSingleTile(
                    Coordinate,
                    Erase
                );

                break;


            case PaintMode.Bucket:

                BucketFill(
                    Coordinate,
                    Erase
                );

                break;
        }


        EditorUtility.SetDirty(
            _Map
        );


        SceneView.RepaintAll();


        CurrentEvent.Use();
    }


    private void PaintSingleTile(
        Vector2Int Coordinate,
        bool Erase
    )
    {
        switch (_AuthoringLayer)
        {
            case AuthoringLayer.Terrain:

                _Map.SetTerrain(
                    Coordinate,
                    Erase
                        ? MapTerrain.None
                        : _TerrainBrush
                );

                break;


            case AuthoringLayer.Deployment:

                if (Erase)
                {
                    _Map.ClearDeploymentCondition(
                        Coordinate
                    );
                }
                else
                {
                    _Map.SetDeploymentCondition(
                        Coordinate,
                        _DeploymentBrush
                    );
                }

                break;
        }
    }


    private void BucketFill(
        Vector2Int StartCoordinate,
        bool Erase
    )
    {
        switch (_AuthoringLayer)
        {
            case AuthoringLayer.Terrain:

                BucketFillTerrain(
                    StartCoordinate,
                    Erase
                );

                break;


            case AuthoringLayer.Deployment:

                BucketFillDeployment(
                    StartCoordinate,
                    Erase
                );

                break;
        }
    }


    private void BucketFillTerrain(
        Vector2Int StartCoordinate,
        bool Erase
    )
    {
        if (!_Map.TryGetTile(
                StartCoordinate,
                out MapTileAuthoringData StartTile))
        {
            return;
        }


        MapTerrain OriginalTerrain =
            StartTile.Terrain;

        MapTerrain ReplacementTerrain =
            Erase
                ? MapTerrain.None
                : _TerrainBrush;


        if (OriginalTerrain ==
            ReplacementTerrain)
        {
            return;
        }


        Queue<Vector2Int> Open =
            new Queue<Vector2Int>();

        HashSet<Vector2Int> Visited =
            new HashSet<Vector2Int>();


        Open.Enqueue(
            StartCoordinate
        );


        while (Open.Count >
            0)
        {
            Vector2Int Coordinate =
                Open.Dequeue();


            if (!Visited.Add(
                    Coordinate))
            {
                continue;
            }


            if (!_Map.IsInsideBounds(
                    Coordinate))
            {
                continue;
            }


            if (!_Map.TryGetTile(
                    Coordinate,
                    out MapTileAuthoringData Tile))
            {
                continue;
            }


            if (Tile.Terrain !=
                OriginalTerrain)
            {
                continue;
            }


            _Map.SetTerrain(
                Coordinate,
                ReplacementTerrain
            );


            EnqueueCardinalNeighbors(
                Open,
                Coordinate
            );
        }
    }


    private void BucketFillDeployment(
        Vector2Int StartCoordinate,
        bool Erase
    )
    {
        if (!_Map.TryGetTile(
                StartCoordinate,
                out MapTileAuthoringData StartTile))
        {
            return;
        }


        MapTileCondition OriginalCondition =
            StartTile.GetDeploymentCondition();

        MapTileCondition ReplacementCondition =
            Erase
                ? MapTileCondition.None
                : _DeploymentBrush;


        if (OriginalCondition ==
            ReplacementCondition)
        {
            return;
        }


        Queue<Vector2Int> Open =
            new Queue<Vector2Int>();

        HashSet<Vector2Int> Visited =
            new HashSet<Vector2Int>();


        Open.Enqueue(
            StartCoordinate
        );


        while (Open.Count >
            0)
        {
            Vector2Int Coordinate =
                Open.Dequeue();


            if (!Visited.Add(
                    Coordinate))
            {
                continue;
            }


            if (!_Map.IsInsideBounds(
                    Coordinate))
            {
                continue;
            }


            if (!_Map.TryGetTile(
                    Coordinate,
                    out MapTileAuthoringData Tile))
            {
                continue;
            }


            if (Tile.Terrain ==
                MapTerrain.Barrier)
            {
                continue;
            }


            if (Tile.GetDeploymentCondition() !=
                OriginalCondition)
            {
                continue;
            }


            if (ReplacementCondition ==
                MapTileCondition.None)
            {
                _Map.ClearDeploymentCondition(
                    Coordinate
                );
            }
            else
            {
                _Map.SetDeploymentCondition(
                    Coordinate,
                    ReplacementCondition
                );
            }


            EnqueueCardinalNeighbors(
                Open,
                Coordinate
            );
        }
    }


    private void EnqueueCardinalNeighbors(
        Queue<Vector2Int> Open,
        Vector2Int Coordinate
    )
    {
        Open.Enqueue(
            Coordinate +
            Vector2Int.right
        );

        Open.Enqueue(
            Coordinate +
            Vector2Int.left
        );

        Open.Enqueue(
            Coordinate +
            Vector2Int.up
        );

        Open.Enqueue(
            Coordinate +
            Vector2Int.down
        );
    }


    private bool TryGetMouseCoordinate(
        out Vector2Int Coordinate
    )
    {
        Coordinate =
            default;


        Ray MouseRay =
            HandleUtility.GUIPointToWorldRay(
                Event.current.mousePosition
            );


        Plane GridPlane =
            new Plane(
                _Map.GridReference.transform.forward,
                _Map.GridReference.transform.position
            );


        if (!GridPlane.Raycast(
                MouseRay,
                out float Distance))
        {
            return false;
        }


        Vector3 WorldPosition =
            MouseRay.GetPoint(
                Distance
            );


        Vector3Int Cell =
            _Map.GridReference.WorldToCell(
                WorldPosition
            );


        Coordinate =
            new Vector2Int(
                Cell.x,
                Cell.y
            );


        return true;
    }


    [MenuItem(
        "GameObject/Admirals/Game Map",
        false,
        10
    )]
    private static void CreateGameMap(
        MenuCommand MenuCommand
    )
    {
        GameObject MapRoot =
            new GameObject(
                "GameMap"
            );


        Undo.RegisterCreatedObjectUndo(
            MapRoot,
            "Create Admirals Game Map"
        );


        GameObject GridObject =
            new GameObject(
                "Grid"
            );


        Undo.RegisterCreatedObjectUndo(
            GridObject,
            "Create Admirals Game Map"
        );


        GridObject.transform.SetParent(
            MapRoot.transform,
            false
        );


        Grid Grid =
            GridObject.AddComponent<Grid>();


        Grid.cellSize =
            Vector3.one;


        Tilemap TerrainArt =
            CreateTilemapLayer(
                GridObject.transform,
                "Terrain_Art",
                0
            );


        Tilemap BelowUnits =
            CreateTilemapLayer(
                GridObject.transform,
                "Below_Units",
                10
            );


        GameObject UnitsObject =
            new GameObject(
                "Units"
            );


        Undo.RegisterCreatedObjectUndo(
            UnitsObject,
            "Create Admirals Game Map"
        );


        UnitsObject.transform.SetParent(
            GridObject.transform,
            false
        );


        SortingGroup UnitsSortingGroup =
            UnitsObject.AddComponent<SortingGroup>();


        UnitsSortingGroup.sortingLayerName =
            "Default";

        UnitsSortingGroup.sortingOrder =
            50;


        Tilemap TileMarkers =
            CreateTilemapLayer(
                GridObject.transform,
                "Tile_Markers",
                60
            );


        Tilemap Fog =
            CreateTilemapLayer(
                GridObject.transform,
                "Fog",
                40
            );


        Tilemap VFX =
            CreateTilemapLayer(
                GridObject.transform,
                "VFX",
                70
            );


        MapAuthoring Map =
            MapRoot.AddComponent<MapAuthoring>();


        SerializedObject MapSerialized =
            new SerializedObject(
                Map
            );


        MapSerialized
            .FindProperty(
                "_Grid"
            )
            .objectReferenceValue =
            Grid;


        MapSerialized
            .FindProperty(
                "_TerrainArt"
            )
            .objectReferenceValue =
            TerrainArt;


        MapSerialized
            .FindProperty(
                "_BelowUnits"
            )
            .objectReferenceValue =
            BelowUnits;


        MapSerialized
            .FindProperty(
                "_Units"
            )
            .objectReferenceValue =
            UnitsObject.transform;


        MapSerialized
            .FindProperty(
                "_TileMarkers"
            )
            .objectReferenceValue =
            TileMarkers;


        MapSerialized
            .FindProperty(
                "_Fog"
            )
            .objectReferenceValue =
            Fog;


        MapSerialized
            .FindProperty(
                "_VFX"
            )
            .objectReferenceValue =
            VFX;


        MapSerialized.ApplyModifiedPropertiesWithoutUndo();


        Map.ResizeMap(
            Vector2Int.one,
            out _
        );


        GameObjectUtility.SetParentAndAlign(
            MapRoot,
            MenuCommand.context as GameObject
        );


        Selection.activeGameObject =
            MapRoot;
    }


    private static Tilemap CreateTilemapLayer(
        Transform Parent,
        string Name,
        int SortingOrder
    )
    {
        GameObject LayerObject =
            new GameObject(
                Name
            );


        Undo.RegisterCreatedObjectUndo(
            LayerObject,
            "Create Admirals Game Map"
        );


        LayerObject.transform.SetParent(
            Parent,
            false
        );


        Tilemap Tilemap =
            LayerObject.AddComponent<Tilemap>();


        TilemapRenderer Renderer =
            LayerObject.AddComponent<TilemapRenderer>();


        Renderer.sortingLayerName =
            "Default";

        Renderer.sortingOrder =
            SortingOrder;


        return
            Tilemap;
    }
}
