using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


// Controls the runtime gameplay Camera.
//
// The Camera itself renders full-screen.
// HUD elements are rendered over it by Canvas_Game.
//
// PlayAreaUI defines the unobstructed screen region in which
// Map edges must remain reachable. Camera bounds are calculated
// against that safe region rather than against the full screen.
[RequireComponent(
    typeof(Camera)
)]
public sealed class GameplayCamera :
    MonoBehaviour
{
    [Header("Game")]
    [SerializeField]
    private GameMaster _GameMaster;


    [Header("HUD")]
    [SerializeField]
    private RectTransform _PlayAreaUI;

    [SerializeField]
    private Canvas _HUDCanvas;


    [Header("Movement")]
    [Min(0f)]
    [SerializeField]
    private float _PanSpeedPerZoomUnit =
        2.5f;


    [Header("Zoom")]
    [Min(0.01f)]
    [SerializeField]
    private float _MinimumOrthographicSize =
        3f;

    [Min(0.01f)]
    [SerializeField]
    private float _MaximumOrthographicSize =
        12f;

    [Min(0f)]
    [SerializeField]
    private float _ZoomSensitivity =
        1.5f;

    [Min(0.001f)]
    [SerializeField]
    private float _ZoomSmoothTime =
        0.10f;


    [Header("Map Bounds")]
    [Min(0f)]
    [SerializeField]
    private float _MapPadding =
        3f;


    private Camera _Camera;


    private float _TargetOrthographicSize;

    private float _ZoomVelocity;


    private float _MapMinimumX;

    private float _MapMaximumX;

    private float _MapMinimumY;

    private float _MapMaximumY;


    private bool _Initialized;

    private GameParticipantAPI.ParticipantAccess _PendingFocusAccess;


    private readonly Vector3[] _PlayAreaCorners =
        new Vector3[4];


    private void Awake()
    {
        _Camera =
            GetComponent<Camera>();


        if (!_Camera.orthographic)
        {
            Debug.LogError(
                "GameplayCamera requires an Orthographic Camera.",
                this
            );


            enabled =
                false;

            return;
        }


        // Gameplay Camera always renders the complete display.
        // HUD is overlaid separately by Canvas_Game.
        _Camera.rect =
            new Rect(
                0f,
                0f,
                1f,
                1f
            );


        _TargetOrthographicSize =
            Mathf.Clamp(
                _Camera.orthographicSize,
                _MinimumOrthographicSize,
                _MaximumOrthographicSize
            );


        _Camera.orthographicSize =
            _TargetOrthographicSize;
    }


    private void Start()
    {
        TryInitialize();
    }


    private void Update()
    {
        if (!_Initialized)
        {
            TryInitialize();

            return;
        }


        HandleMovement();

        HandleZoom();
    }


    private void LateUpdate()
    {
        if (!_Initialized)
        {
            return;
        }


        if (_PendingFocusAccess != null)
        {
            // ParticipantChanged refreshes the HUD before this point. Resolve
            // its new play area before centering, including bottom-bar changes.
            Canvas.ForceUpdateCanvases();
            FocusParticipant(_PendingFocusAccess);
            _PendingFocusAccess = null;
        }

        // Clamp after movement, zoom and UI layout have had
        // an opportunity to update for the current frame.
        ClampCameraToMap();
    }

    internal void RequestPassAndPlayFocus(
        GameParticipantAPI.ParticipantAccess Access)
    {
        if (_GameMaster?.MatchConfig?.IsPassAndPlay == true)
            _PendingFocusAccess = Access;
    }


    private void FocusParticipant(GameParticipantAPI.ParticipantAccess Access)
    {
        if (!Access.TryGetSnapshot(out ParticipantGameSnapshot Snapshot, out _) ||
            Snapshot == null)
            return;

        ParticipantUnitSnapshot FocusUnit = null;
        GameBattle.EngagementView Engagement = Snapshot.Battle?.Engagement;
        string EngagementUnitID = Engagement?.IsActive == true
            ? (Engagement.IsAttacker
                ? Engagement.AttackerRuntimeUnitID : Engagement.DefenderRuntimeUnitID)
            : null;

        // Only use the receiving side's filtered data. Destroyed and embarked
        // records remain in the roster but are not camera targets.
        foreach (ParticipantUnitSnapshot Unit in Snapshot.OwnUnits)
        {
            if (Unit == null || !Unit.IsOnMap ||
                Unit.LifeState != RuntimeUnitLifeState.Active)
                continue;
            if (FocusUnit == null)
                FocusUnit = Unit;
            if (Unit.RuntimeUnitID == EngagementUnitID)
            {
                FocusUnit = Unit;
                break;
            }
        }

        if (FocusUnit != null)
        {
            Vector2Int[] Footprint = FocusUnit.OccupiedCoordinates;
            CenterCoordinatesInsidePlayArea(Footprint.Length > 0
                ? Footprint : new[] { FocusUnit.MovementPivotCoordinate });
            return;
        }

        if (Snapshot.CurrentPhase == GameplayPhase.Deployment &&
            Access.TryGetDeploymentView(out GameDeployment.ParticipantView Deployment))
        {
            foreach (GameDeployment.PlacementView Placement in Deployment.Placements)
            {
                if (!Placement.IsPlaced)
                    continue;
                CenterCoordinatesInsidePlayArea(new[] { Placement.MovementPivotCoordinate });
                return;
            }
        }

        if (Access.TryGetDeploymentZoneCoordinates(out Vector2Int[] Zone) &&
            Zone.Length > 0)
        {
            CenterCoordinatesInsidePlayArea(Zone);
            return;
        }

        CenterMapInsidePlayArea();
    }


    private void CenterCoordinatesInsidePlayArea(Vector2Int[] Coordinates)
    {
        Grid GridReference = _GameMaster.MapAuthoring.GridReference;
        Vector3 Center = Vector3.zero;
        foreach (Vector2Int Coordinate in Coordinates)
            Center += GridReference.GetCellCenterWorld(
                new Vector3Int(Coordinate.x, Coordinate.y, 0));
        Center /= Coordinates.Length;

        // Keep the player's current zoom, but stop an old zoom interpolation
        // from shifting the newly centered view after the handoff.
        _TargetOrthographicSize = _Camera.orthographicSize;
        _ZoomVelocity = 0f;
        GetSafeViewOffsets(out float Left, out float Right,
            out float Bottom, out float Top);
        transform.position = new Vector3(
            Center.x - (Left + Right) * 0.5f,
            Center.y - (Bottom + Top) * 0.5f,
            transform.position.z);
    }


    private bool TryInitialize()
    {
        if (_Initialized)
        {
            return true;
        }


        if (_GameMaster == null ||
            !_GameMaster.InitializationComplete)
        {
            return false;
        }


        if (_GameMaster.GameMap == null ||
            _GameMaster.MapAuthoring == null ||
            _GameMaster.MapAuthoring.GridReference == null)
        {
            return false;
        }


        CalculateMapBounds();


        _TargetOrthographicSize =
            Mathf.Clamp(
                _Camera.orthographicSize,
                _MinimumOrthographicSize,
                _MaximumOrthographicSize
            );


        _Camera.orthographicSize =
            _TargetOrthographicSize;


        CenterMapInsidePlayArea();


        _Initialized =
            true;


        return true;
    }


    private void CalculateMapBounds()
    {
        Grid GridReference =
            _GameMaster.MapAuthoring.GridReference;


        Vector2Int MapSize =
            _GameMaster.GameMap.MapSize;


        Vector3 BottomLeft =
            GridReference.CellToWorld(
                new Vector3Int(
                    0,
                    0,
                    0
                )
            );


        Vector3 BottomRight =
            GridReference.CellToWorld(
                new Vector3Int(
                    MapSize.x,
                    0,
                    0
                )
            );


        Vector3 TopLeft =
            GridReference.CellToWorld(
                new Vector3Int(
                    0,
                    MapSize.y,
                    0
                )
            );


        Vector3 TopRight =
            GridReference.CellToWorld(
                new Vector3Int(
                    MapSize.x,
                    MapSize.y,
                    0
                )
            );


        float MinimumX =
            Mathf.Min(
                BottomLeft.x,
                BottomRight.x,
                TopLeft.x,
                TopRight.x
            );


        float MaximumX =
            Mathf.Max(
                BottomLeft.x,
                BottomRight.x,
                TopLeft.x,
                TopRight.x
            );


        float MinimumY =
            Mathf.Min(
                BottomLeft.y,
                BottomRight.y,
                TopLeft.y,
                TopRight.y
            );


        float MaximumY =
            Mathf.Max(
                BottomLeft.y,
                BottomRight.y,
                TopLeft.y,
                TopRight.y
            );


        _MapMinimumX =
            MinimumX -
            _MapPadding;

        _MapMaximumX =
            MaximumX +
            _MapPadding;

        _MapMinimumY =
            MinimumY -
            _MapPadding;

        _MapMaximumY =
            MaximumY +
            _MapPadding;
    }


    private void CenterMapInsidePlayArea()
    {
        GetSafeViewOffsets(
            out float SafeLeft,
            out float SafeRight,
            out float SafeBottom,
            out float SafeTop
        );


        float MapCenterX =
            (
                _MapMinimumX +
                _MapMaximumX
            ) *
            0.5f;

        float MapCenterY =
            (
                _MapMinimumY +
                _MapMaximumY
            ) *
            0.5f;

        float SafeCenterOffsetX =
            (
                SafeLeft +
                SafeRight
            ) *
            0.5f;

        float SafeCenterOffsetY =
            (
                SafeBottom +
                SafeTop
            ) *
            0.5f;


        transform.position =
            new Vector3(
                MapCenterX -
                    SafeCenterOffsetX,
                MapCenterY -
                    SafeCenterOffsetY,
                transform.position.z
            );


        ClampCameraToMap();
    }


    private void HandleMovement()
    {
        Keyboard Keyboard =
            UnityEngine.InputSystem.Keyboard.current;


        if (Keyboard == null)
        {
            return;
        }


        Vector2 Direction =
            Vector2.zero;


        if (Keyboard.wKey.isPressed)
        {
            Direction.y +=
                1f;
        }


        if (Keyboard.sKey.isPressed)
        {
            Direction.y -=
                1f;
        }


        if (Keyboard.dKey.isPressed)
        {
            Direction.x +=
                1f;
        }


        if (Keyboard.aKey.isPressed)
        {
            Direction.x -=
                1f;
        }


        if (Direction.sqrMagnitude <=
            0f)
        {
            return;
        }


        Direction.Normalize();


        float PanSpeed =
            _PanSpeedPerZoomUnit *
            _Camera.orthographicSize;


        Vector3 Movement =
            new Vector3(
                Direction.x,
                Direction.y,
                0f
            ) *
            PanSpeed *
            Time.unscaledDeltaTime;


        transform.position +=
            Movement;
    }


    private void HandleZoom()
    {
        Mouse Mouse =
            UnityEngine.InputSystem.Mouse.current;


        // HUD interaction owns the mouse wheel while the pointer is over
        // raycastable UI. The existing zoom interpolation still runs so a
        // previous map zoom can finish smoothly.
        bool PointerOverUI =
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject();


        if (Mouse != null &&
            !PointerOverUI)
        {
            float Scroll =
                Mouse.scroll.ReadValue().y;


            if (!Mathf.Approximately(
                    Scroll,
                    0f))
            {
                float ScrollDirection =
                    Mathf.Sign(
                        Scroll
                    );


                _TargetOrthographicSize =
                    Mathf.Clamp(
                        _TargetOrthographicSize -
                            ScrollDirection *
                            _ZoomSensitivity,
                        _MinimumOrthographicSize,
                        _MaximumOrthographicSize
                    );
            }
        }


        _Camera.orthographicSize =
            Mathf.SmoothDamp(
                _Camera.orthographicSize,
                _TargetOrthographicSize,
                ref _ZoomVelocity,
                _ZoomSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );
    }


    private void ClampCameraToMap()
    {
        GetSafeViewOffsets(
            out float SafeLeft,
            out float SafeRight,
            out float SafeBottom,
            out float SafeTop
        );


        Vector3 Position =
            transform.position;


        Position.x =
            ClampAxisToSafeArea(
                Position.x,
                _MapMinimumX,
                _MapMaximumX,
                SafeLeft,
                SafeRight
            );

        Position.y =
            ClampAxisToSafeArea(
                Position.y,
                _MapMinimumY,
                _MapMaximumY,
                SafeBottom,
                SafeTop
            );


        transform.position =
            new Vector3(
                Position.x,
                Position.y,
                transform.position.z
            );
    }


    private void GetSafeViewOffsets(
        out float SafeLeft,
        out float SafeRight,
        out float SafeBottom,
        out float SafeTop
    )
    {
        float HalfHeight =
            _Camera.orthographicSize;

        float HalfWidth =
            HalfHeight *
            _Camera.aspect;


        if (_PlayAreaUI == null ||
            Screen.width <= 0 ||
            Screen.height <= 0)
        {
            SafeLeft =
                -HalfWidth;

            SafeRight =
                HalfWidth;

            SafeBottom =
                -HalfHeight;

            SafeTop =
                HalfHeight;

            return;
        }


        _PlayAreaUI.GetWorldCorners(
            _PlayAreaCorners
        );


        Camera UICamera =
            GetUICamera();


        Vector2 BottomLeft =
            RectTransformUtility.WorldToScreenPoint(
                UICamera,
                _PlayAreaCorners[0]
            );

        Vector2 TopRight =
            RectTransformUtility.WorldToScreenPoint(
                UICamera,
                _PlayAreaCorners[2]
            );


        float NormalizedLeft =
            Mathf.Clamp01(
                BottomLeft.x /
                    Screen.width
            );

        float NormalizedRight =
            Mathf.Clamp01(
                TopRight.x /
                    Screen.width
            );

        float NormalizedBottom =
            Mathf.Clamp01(
                BottomLeft.y /
                    Screen.height
            );

        float NormalizedTop =
            Mathf.Clamp01(
                TopRight.y /
                    Screen.height
            );


        SafeLeft =
            Mathf.Lerp(
                -HalfWidth,
                HalfWidth,
                NormalizedLeft
            );

        SafeRight =
            Mathf.Lerp(
                -HalfWidth,
                HalfWidth,
                NormalizedRight
            );

        SafeBottom =
            Mathf.Lerp(
                -HalfHeight,
                HalfHeight,
                NormalizedBottom
            );

        SafeTop =
            Mathf.Lerp(
                -HalfHeight,
                HalfHeight,
                NormalizedTop
            );
    }


    private Camera GetUICamera()
    {
        if (_HUDCanvas == null ||
            _HUDCanvas.renderMode ==
                RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }


        return
            _HUDCanvas.worldCamera;
    }


    private float ClampAxisToSafeArea(
        float CurrentPosition,
        float MapMinimum,
        float MapMaximum,
        float SafeMinimumOffset,
        float SafeMaximumOffset
    )
    {
        float MapSpan =
            MapMaximum -
            MapMinimum;

        float SafeSpan =
            SafeMaximumOffset -
            SafeMinimumOffset;


        if (MapSpan <=
            SafeSpan)
        {
            float MapCenter =
                (
                    MapMinimum +
                    MapMaximum
                ) *
                0.5f;

            float SafeCenterOffset =
                (
                    SafeMinimumOffset +
                    SafeMaximumOffset
                ) *
                0.5f;


            return
                MapCenter -
                SafeCenterOffset;
        }


        float MinimumCameraPosition =
            MapMinimum -
            SafeMinimumOffset;

        float MaximumCameraPosition =
            MapMaximum -
            SafeMaximumOffset;


        return
            Mathf.Clamp(
                CurrentPosition,
                MinimumCameraPosition,
                MaximumCameraPosition
            );
    }
}
