using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;


// Unity Editor Script - Not Attached To A GameObject
// Prevents Tile Palette painting outside an authored Admirals Map's bounds.
[CustomGridBrush(
    false,
    false,
    false,
    "Admirals Bounded Map Brush"
)]
public class MapBoundedGridBrush :
    GridBrush
{
    public override void Paint(
        GridLayout GridLayout,
        GameObject BrushTarget,
        Vector3Int Position
    )
    {
        if (!TryGetMap(
                BrushTarget,
                out MapAuthoring Map))
        {
            base.Paint(
                GridLayout,
                BrushTarget,
                Position
            );

            return;
        }


        if (!IsBrushStampInsideMap(
                Map,
                Position))
        {
            return;
        }


        base.Paint(
            GridLayout,
            BrushTarget,
            Position
        );
    }


    public override void Erase(
        GridLayout GridLayout,
        GameObject BrushTarget,
        Vector3Int Position
    )
    {
        if (!TryGetMap(
                BrushTarget,
                out MapAuthoring Map))
        {
            base.Erase(
                GridLayout,
                BrushTarget,
                Position
            );

            return;
        }


        if (!IsBrushStampInsideMap(
                Map,
                Position))
        {
            return;
        }


        base.Erase(
            GridLayout,
            BrushTarget,
            Position
        );
    }


    public override void BoxFill(
        GridLayout GridLayout,
        GameObject BrushTarget,
        BoundsInt Position
    )
    {
        if (!TryGetMap(
                BrushTarget,
                out MapAuthoring Map))
        {
            base.BoxFill(
                GridLayout,
                BrushTarget,
                Position
            );

            return;
        }


        if (!IsBoundsInsideMap(
                Map,
                Position))
        {
            return;
        }


        base.BoxFill(
            GridLayout,
            BrushTarget,
            Position
        );
    }


    public override void BoxErase(
        GridLayout GridLayout,
        GameObject BrushTarget,
        BoundsInt Position
    )
    {
        if (!TryGetMap(
                BrushTarget,
                out MapAuthoring Map))
        {
            base.BoxErase(
                GridLayout,
                BrushTarget,
                Position
            );

            return;
        }


        if (!IsBoundsInsideMap(
                Map,
                Position))
        {
            return;
        }


        base.BoxErase(
            GridLayout,
            BrushTarget,
            Position
        );
    }


    // Visual flood-fill is deliberately disabled on authored Maps.
    // Gameplay-data flood-fill remains available through the Map Maker.
    public override void FloodFill(
        GridLayout GridLayout,
        GameObject BrushTarget,
        Vector3Int Position
    )
    {
        if (TryGetMap(
                BrushTarget,
                out _))
        {
            return;
        }


        base.FloodFill(
            GridLayout,
            BrushTarget,
            Position
        );
    }


    private bool TryGetMap(
        GameObject BrushTarget,
        out MapAuthoring Map
    )
    {
        Map =
            null;


        if (BrushTarget == null)
        {
            return false;
        }


        Map =
            BrushTarget.GetComponentInParent<MapAuthoring>();


        return
            Map != null;
    }


    // Checks the complete currently selected Palette stamp,
    // not just the cell under the mouse.
    private bool IsBrushStampInsideMap(
        MapAuthoring Map,
        Vector3Int Position
    )
    {
        Vector3Int Minimum =
            Position -
            pivot;


        for (
            int Y = 0;
            Y < size.y;
            Y++
        )
        {
            for (
                int X = 0;
                X < size.x;
                X++
            )
            {
                Vector2Int Coordinate =
                    new Vector2Int(
                        Minimum.x + X,
                        Minimum.y + Y
                    );


                if (!Map.IsInsideBounds(
                        Coordinate))
                {
                    return false;
                }
            }
        }


        return true;
    }


    private bool IsBoundsInsideMap(
        MapAuthoring Map,
        BoundsInt Bounds
    )
    {
        for (
            int Y = Bounds.yMin;
            Y < Bounds.yMax;
            Y++
        )
        {
            for (
                int X = Bounds.xMin;
                X < Bounds.xMax;
                X++
            )
            {
                if (!Map.IsInsideBounds(
                        new Vector2Int(
                            X,
                            Y
                        )))
                {
                    return false;
                }
            }
        }


        return true;
    }
}


// Unity Editor Script - Not Attached To A GameObject
[CustomEditor(typeof(MapBoundedGridBrush))]
public class MapBoundedGridBrushEditor :
    GridBrushEditor
{
}