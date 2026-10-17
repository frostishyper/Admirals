using System;
using UnityEngine;


// Shared cardinal footprint geometry.
//
// Authored Unit footprints use East as their canonical orientation.
// This class calculates requested footprint coordinates only.
// GameMap remains responsible for deciding whether those coordinates
// are physically legal.
public static class UnitFootprintGeometry
{
    // Returns the visual center of the complete footprint relative to its
    // MovementPivot. The result is expressed in board-cell coordinates so
    // presentation can place centered-pivot art without changing gameplay
    // coordinates or requiring per-facing sprite pivots.
    public static bool TryGetVisualCenterOffset(
        Definition_Unit Unit,
        UnitFacing Facing,
        out Vector2 VisualCenterOffset,
        out string ValidationMessage
    )
    {
        VisualCenterOffset =
            Vector2.zero;


        if (!TryValidateUnitGeometry(
                Unit,
                Facing,
                out Vector2Int FootprintSize,
                out Vector2Int LocalPivot,
                out ValidationMessage))
        {
            return false;
        }


        Vector2 EastCanonicalCenterOffset =
            new Vector2(
                ((FootprintSize.x - 1) * 0.5f) -
                    LocalPivot.x,
                ((FootprintSize.y - 1) * 0.5f) -
                    LocalPivot.y
            );


        if (!TryRotateEastCanonicalOffset(
                EastCanonicalCenterOffset,
                Facing,
                out VisualCenterOffset))
        {
            ValidationMessage =
                $"Facing '{Facing}' is invalid.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    public static bool TryBuildPlacement(
        Definition_Unit Unit,
        Vector2Int MovementPivotCoordinate,
        UnitFacing Facing,
        out RuntimeUnitTilePlacement[] Placements,
        out string ValidationMessage
    )
    {
        Placements =
            Array.Empty<RuntimeUnitTilePlacement>();


        if (!TryValidateUnitGeometry(
                Unit,
                Facing,
                out Vector2Int FootprintSize,
                out Vector2Int LocalPivot,
                out ValidationMessage))
        {
            return false;
        }


        Placements =
            new RuntimeUnitTilePlacement[
                FootprintSize.x *
                FootprintSize.y
            ];


        int PlacementIndex =
            0;


        for (int Y = 0;
            Y < FootprintSize.y;
            Y++)
        {
            for (int X = 0;
                X < FootprintSize.x;
                X++)
            {
                Vector2Int FootprintCoordinate =
                    new Vector2Int(
                        X,
                        Y
                    );

                Vector2Int LocalOffset =
                    FootprintCoordinate -
                    LocalPivot;


                if (!TryRotateEastCanonicalOffset(
                        LocalOffset,
                        Facing,
                        out Vector2Int RotatedOffset))
                {
                    Placements =
                        Array.Empty<RuntimeUnitTilePlacement>();

                    ValidationMessage =
                        $"Facing '{Facing}' is invalid.";

                    return false;
                }


                Placements[PlacementIndex] =
                    new RuntimeUnitTilePlacement(
                        MovementPivotCoordinate +
                            RotatedOffset,
                        FootprintCoordinate
                    );


                PlacementIndex++;
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    public static bool TryRotateEastCanonicalOffset(
        Vector2Int Offset,
        UnitFacing Facing,
        out Vector2Int RotatedOffset
    )
    {
        switch (Facing)
        {
            case UnitFacing.North:

                RotatedOffset =
                    new Vector2Int(
                        -Offset.y,
                        Offset.x
                    );

                return true;


            case UnitFacing.East:

                RotatedOffset =
                    Offset;

                return true;


            case UnitFacing.South:

                RotatedOffset =
                    new Vector2Int(
                        Offset.y,
                        -Offset.x
                    );

                return true;


            case UnitFacing.West:

                RotatedOffset =
                    new Vector2Int(
                        -Offset.x,
                        -Offset.y
                    );

                return true;


            default:

                RotatedOffset =
                    default;

                return false;
        }
    }


    public static bool TryRotateEastCanonicalOffset(
        Vector2 Offset,
        UnitFacing Facing,
        out Vector2 RotatedOffset
    )
    {
        switch (Facing)
        {
            case UnitFacing.North:

                RotatedOffset =
                    new Vector2(
                        -Offset.y,
                        Offset.x
                    );

                return true;


            case UnitFacing.East:

                RotatedOffset =
                    Offset;

                return true;


            case UnitFacing.South:

                RotatedOffset =
                    new Vector2(
                        Offset.y,
                        -Offset.x
                    );

                return true;


            case UnitFacing.West:

                RotatedOffset =
                    new Vector2(
                        -Offset.x,
                        -Offset.y
                    );

                return true;


            default:

                RotatedOffset =
                    default;

                return false;
        }
    }


    private static bool TryValidateUnitGeometry(
        Definition_Unit Unit,
        UnitFacing Facing,
        out Vector2Int FootprintSize,
        out Vector2Int LocalPivot,
        out string ValidationMessage
    )
    {
        FootprintSize =
            default;

        LocalPivot =
            default;


        if (Unit == null)
        {
            ValidationMessage =
                "Unit Definition is missing.";

            return false;
        }


        if (!IsValidFacing(
                Facing))
        {
            ValidationMessage =
                $"Facing '{Facing}' is invalid.";

            return false;
        }


        FootprintSize =
            Unit.FootprintSize;

        LocalPivot =
            Unit.MovementPivot;


        if (FootprintSize.x < 1 ||
            FootprintSize.y < 1 ||
            LocalPivot.x < 0 ||
            LocalPivot.y < 0 ||
            LocalPivot.x >= FootprintSize.x ||
            LocalPivot.y >= FootprintSize.y)
        {
            ValidationMessage =
                $"Unit '{Unit.Name}' has invalid Footprint or MovementPivot data.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    public static bool IsValidFacing(
        UnitFacing Facing
    )
    {
        return
            Facing == UnitFacing.North ||
            Facing == UnitFacing.East ||
            Facing == UnitFacing.South ||
            Facing == UnitFacing.West;
    }
}
