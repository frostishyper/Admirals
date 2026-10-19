using UnityEngine;
using UnityEngine.InputSystem;

// Attach To Main Camera GameObject
// Controls RTS-Style Camera Movement Across The Loaded Map
[RequireComponent(typeof(Camera))]
public class MapCameraController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _PanSpeed = 12f;

    [Header("Zoom")]
    [SerializeField] private int _MinimumVisibleRows = 8;
    [SerializeField] private int _StartingVisibleRows = 14;
    [SerializeField] private float _ZoomRowsPerScroll = 2f;


    private Camera _Camera;

    private MapBoard _ActiveBoard;
    private Bounds _MapBounds;

    private float _CellWorldHeight;

    private Vector2 _LastMousePosition;


    private void Awake()
    {
        _Camera =
            GetComponent<Camera>();

        _Camera.orthographic = true;
    }


    public void Initialize(MapBoard Board)
    {
        if (Board == null)
        {
            Debug.LogError(
                "MapCameraController cannot initialize with a null MapBoard."
            );

            return;
        }


        _ActiveBoard = Board;

        _MapBounds =
            Board.GetWorldBounds();


        CalculateCellWorldHeight();


        _MinimumVisibleRows =
            Mathf.Max(
                1,
                _MinimumVisibleRows
            );


        _StartingVisibleRows =
            Mathf.Max(
                _MinimumVisibleRows,
                _StartingVisibleRows
            );


        _Camera.orthographicSize =
            GetOrthographicSizeForVisibleRows(
                _StartingVisibleRows
            );


        float MaximumZoom =
            GetMaximumZoom();


        _Camera.orthographicSize =
            Mathf.Clamp(
                _Camera.orthographicSize,
                GetMinimumZoom(),
                MaximumZoom
            );


        Vector3 CurrentPosition =
            transform.position;


        transform.position =
            new Vector3(
                _MapBounds.center.x,
                _MapBounds.center.y,
                CurrentPosition.z
            );


        ClampCameraToMap();


        Debug.Log(
            $"Camera Initialized At {_MapBounds.center}"
        );
    }


    private void Update()
    {
        if (_ActiveBoard == null)
        {
            return;
        }


        HandleKeyboardPanning();

        HandleMouseDragging();

        HandleZoom();

        ClampCameraToMap();
    }


    private void HandleKeyboardPanning()
    {
        Keyboard Keyboard =
            UnityEngine.InputSystem.Keyboard.current;


        if (Keyboard == null)
        {
            return;
        }


        Vector2 Direction =
            Vector2.zero;


        if (Keyboard.wKey.isPressed ||
            Keyboard.upArrowKey.isPressed)
        {
            Direction.y += 1f;
        }


        if (Keyboard.sKey.isPressed ||
            Keyboard.downArrowKey.isPressed)
        {
            Direction.y -= 1f;
        }


        if (Keyboard.aKey.isPressed ||
            Keyboard.leftArrowKey.isPressed)
        {
            Direction.x -= 1f;
        }


        if (Keyboard.dKey.isPressed ||
            Keyboard.rightArrowKey.isPressed)
        {
            Direction.x += 1f;
        }


        if (Direction.sqrMagnitude > 1f)
        {
            Direction.Normalize();
        }


        float ZoomScale =
            _Camera.orthographicSize /
            GetMinimumZoom();


        Vector3 Movement =
            new Vector3(
                Direction.x,
                Direction.y,
                0f
            ) *
            _PanSpeed *
            ZoomScale *
            Time.unscaledDeltaTime;


        transform.position +=
            Movement;
    }


    private void HandleMouseDragging()
    {
        Mouse Mouse =
            UnityEngine.InputSystem.Mouse.current;


        if (Mouse == null)
        {
            return;
        }


        if (Mouse.middleButton.wasPressedThisFrame)
        {
            _LastMousePosition =
                Mouse.position.ReadValue();
        }


        if (!Mouse.middleButton.isPressed)
        {
            return;
        }


        Vector2 CurrentMousePosition =
            Mouse.position.ReadValue();


        Vector2 MouseDelta =
            CurrentMousePosition -
            _LastMousePosition;


        float WorldUnitsPerPixel =
            (_Camera.orthographicSize * 2f) /
            Screen.height;


        transform.position -=
            new Vector3(
                MouseDelta.x,
                MouseDelta.y,
                0f
            ) *
            WorldUnitsPerPixel;


        _LastMousePosition =
            CurrentMousePosition;
    }


    private void HandleZoom()
    {
        Mouse Mouse =
            UnityEngine.InputSystem.Mouse.current;


        if (Mouse == null)
        {
            return;
        }


        float Scroll =
            Mouse.scroll.ReadValue().y;


        if (Mathf.Approximately(
                Scroll,
                0f))
        {
            return;
        }


        float ZoomDirection =
            Mathf.Sign(Scroll);


        float ZoomChange =
            GetOrthographicSizeForVisibleRows(
                _ZoomRowsPerScroll
            );


        _Camera.orthographicSize =
            Mathf.Clamp(
                _Camera.orthographicSize -
                ZoomDirection * ZoomChange,
                GetMinimumZoom(),
                GetMaximumZoom()
            );
    }


    private void CalculateCellWorldHeight()
    {
        Vector3 Bottom =
            _ActiveBoard.TerrainTilemap.CellToWorld(
                Vector3Int.zero
            );


        Vector3 Top =
            _ActiveBoard.TerrainTilemap.CellToWorld(
                Vector3Int.up
            );


        _CellWorldHeight =
            Mathf.Abs(
                Top.y - Bottom.y
            );


        if (_CellWorldHeight <= 0f)
        {
            _CellWorldHeight = 1f;
        }
    }


    private float GetOrthographicSizeForVisibleRows(
        float VisibleRows
    )
    {
        return
            VisibleRows *
            _CellWorldHeight *
            0.5f;
    }


    private float GetMinimumZoom()
    {
        return
            GetOrthographicSizeForVisibleRows(
                _MinimumVisibleRows
            );
    }


    private float GetMaximumZoom()
    {
        float VerticalRequirement =
            _MapBounds.extents.y;


        float HorizontalRequirement =
            _MapBounds.extents.x /
            _Camera.aspect;


        return Mathf.Max(
            GetMinimumZoom(),
            Mathf.Max(
                VerticalRequirement,
                HorizontalRequirement
            )
        );
    }


    private void ClampCameraToMap()
    {
        float HalfHeight =
            _Camera.orthographicSize;


        float HalfWidth =
            HalfHeight *
            _Camera.aspect;


        Vector3 Position =
            transform.position;


        if (HalfWidth >= _MapBounds.extents.x)
        {
            Position.x =
                _MapBounds.center.x;
        }
        else
        {
            Position.x =
                Mathf.Clamp(
                    Position.x,
                    _MapBounds.min.x + HalfWidth,
                    _MapBounds.max.x - HalfWidth
                );
        }


        if (HalfHeight >= _MapBounds.extents.y)
        {
            Position.y =
                _MapBounds.center.y;
        }
        else
        {
            Position.y =
                Mathf.Clamp(
                    Position.y,
                    _MapBounds.min.y + HalfHeight,
                    _MapBounds.max.y - HalfHeight
                );
        }


        transform.position =
            Position;
    }
}