using UnityEngine;

// Attach To Controller_Map GameObject
// Loads Maps And Builds Their Live Runtime Board State
public class MapController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform _MapRoot;

    [SerializeField] private MapCameraController _MapCameraController;


    private GameObject _ActiveMapObject;

    private MapBoard _ActiveBoard;

    private MapRuntimeState _RuntimeState;


    public MapBoard ActiveBoard
    {
        get
        {
            return _ActiveBoard;
        }
    }


    public MapRuntimeState RuntimeState
    {
        get
        {
            return _RuntimeState;
        }
    }


    private void Awake()
    {
        if (_MapCameraController == null &&
            Camera.main != null)
        {
            _MapCameraController =
                Camera.main.GetComponent<MapCameraController>();
        }
    }


    public void LoadMap(
        MapInfo Map
    )
    {
        if (Map == null)
        {
            Debug.LogError(
                "MapController cannot load a null MapInfo."
            );

            return;
        }


        if (Map.MapReference == null)
        {
            Debug.LogError(
                $"MapInfo '{Map.name}' has no MapReference assigned."
            );

            return;
        }


        if (_ActiveMapObject != null)
        {
            Destroy(
                _ActiveMapObject
            );
        }


        _ActiveMapObject =
            Instantiate(
                Map.MapReference,
                _MapRoot,
                false
            );


        _ActiveBoard =
            _ActiveMapObject.GetComponent<MapBoard>();


        if (_ActiveBoard == null)
        {
            Debug.LogError(
                $"Loaded map '{Map.MapName}' does not contain a MapBoard component."
            );


            Destroy(
                _ActiveMapObject
            );


            _ActiveMapObject =
                null;


            return;
        }


        BuildRuntimeState();


        if (_MapCameraController != null)
        {
            _MapCameraController.Initialize(
                _ActiveBoard
            );
        }
        else
        {
            Debug.LogError(
                "MapController could not find a MapCameraController on the Main Camera."
            );
        }


        Debug.Log(
            $"Loaded Map: {Map.MapName}"
        );
    }


    private void BuildRuntimeState()
    {
        _RuntimeState =
            new MapRuntimeState();


        int DeepWaterCount = 0;
        int ShallowWaterCount = 0;
        int LandCount = 0;
        int BarrierCount = 0;

        int ZoneACount = 0;
        int ZoneBCount = 0;


        for (
            int Y = 0;
            Y < _ActiveBoard.BoardSize.y;
            Y++
        )
        {
            for (
                int X = 0;
                X < _ActiveBoard.BoardSize.x;
                X++
            )
            {
                Vector2Int Coordinate =
                    new Vector2Int(
                        X,
                        Y
                    );


                if (!_ActiveBoard.TryGetTerrainType(
                        Coordinate,
                        out TerrainType Terrain))
                {
                    Debug.LogError(
                        $"Map terrain data missing at board coordinate {Coordinate}."
                    );

                    continue;
                }


                _ActiveBoard.TryGetDeploymentZone(
                    Coordinate,
                    out DeploymentZone Zone
                );


                _RuntimeState.AddCell(
                    Coordinate,
                    Terrain,
                    Zone
                );


                switch (Terrain)
                {
                    case TerrainType.DeepWater:

                        DeepWaterCount++;

                        break;


                    case TerrainType.ShallowWater:

                        ShallowWaterCount++;

                        break;


                    case TerrainType.Land:

                        LandCount++;

                        break;


                    case TerrainType.Barrier:

                        BarrierCount++;

                        break;
                }


                switch (Zone)
                {
                    case DeploymentZone.ZoneA:

                        ZoneACount++;

                        break;


                    case DeploymentZone.ZoneB:

                        ZoneBCount++;

                        break;
                }
            }
        }


        Debug.Log(
            $"Runtime Board Created: {_RuntimeState.CellCount} Cells\n" +
            $"Deep Water: {DeepWaterCount}\n" +
            $"Shallow Water: {ShallowWaterCount}\n" +
            $"Land: {LandCount}\n" +
            $"Barrier: {BarrierCount}\n" +
            $"Deployment Zone A: {ZoneACount}\n" +
            $"Deployment Zone B: {ZoneBCount}"
        );
    }
}