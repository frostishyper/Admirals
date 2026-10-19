using System.Collections.Generic;

// Runtime Data Class - Not Attached To A GameObject
// Holds The Live Match State Belonging To One Side
public class MatchSideState
{
    private readonly List<UnitRuntimeState> _Units =
        new();


    public MatchSide Side
    {
        get;
        private set;
    }


    public IReadOnlyList<UnitRuntimeState> Units
    {
        get
        {
            return _Units;
        }
    }


    public int UnitCount
    {
        get
        {
            return _Units.Count;
        }
    }


    public int UndeployedUnitCount
    {
        get
        {
            int Count = 0;


            for (
                int i = 0;
                i < _Units.Count;
                i++
            )
            {
                if (!_Units[i].IsDeployed &&
                    !_Units[i].IsDestroyed)
                {
                    Count++;
                }
            }


            return Count;
        }
    }


    public MatchSideState(
        MatchSide Side
    )
    {
        this.Side =
            Side;
    }


    internal void AddUnit(
        UnitRuntimeState Unit
    )
    {
        if (Unit == null)
        {
            return;
        }


        _Units.Add(
            Unit
        );
    }
}