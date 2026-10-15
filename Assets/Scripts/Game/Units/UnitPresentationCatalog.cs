using System;
using UnityEngine;


[CreateAssetMenu(
    fileName = "UnitPresentationCatalog",
    menuName = "Admirals/Unit Presentation Catalog"
)]
public sealed class UnitPresentationCatalog :
    ScriptableObject
{
    [Serializable]
    private sealed class Entry
    {
        [SerializeField]
        private Definition_Unit _Unit;

        [SerializeField]
        private UnitView _Prefab;


        public Definition_Unit Unit =>
            _Unit;


        public UnitView Prefab =>
            _Prefab;
    }


    [SerializeField]
    private Entry[] _Entries =
        Array.Empty<Entry>();


    public bool TryGetPrefab(
        Definition_Unit Unit,
        out UnitView Prefab
    )
    {
        Prefab =
            null;


        if (Unit == null)
        {
            return false;
        }


        for (int Index = 0;
            Index < _Entries.Length;
            Index++)
        {
            Entry Current =
                _Entries[Index];


            if (Current == null ||
                Current.Unit != Unit)
            {
                continue;
            }


            Prefab =
                Current.Prefab;


            return
                Prefab != null;
        }


        return false;
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        if (_Entries == null)
        {
            _Entries =
                Array.Empty<Entry>();

            return;
        }


        for (int Index = 0;
            Index < _Entries.Length;
            Index++)
        {
            Entry Current =
                _Entries[Index];


            if (Current == null ||
                Current.Unit == null)
            {
                continue;
            }


            for (int OtherIndex =
                    Index + 1;
                OtherIndex < _Entries.Length;
                OtherIndex++)
            {
                Entry Other =
                    _Entries[OtherIndex];


                if (Other == null ||
                    Other.Unit != Current.Unit)
                {
                    continue;
                }


                Debug.LogWarning(
                    $"UnitPresentationCatalog contains duplicate entries for '{Current.Unit.name}'.",
                    this
                );
            }
        }
    }

#endif
}