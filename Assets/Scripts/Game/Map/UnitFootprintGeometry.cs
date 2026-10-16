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


        Vector2Int FootprintSize =
            Unit.FootprintSize;

        Vector2Int LocalPivot =
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