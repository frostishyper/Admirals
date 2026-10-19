using System;
using System.Collections.Generic;
using UnityEngine;


// Embedded Serializable Data - Not A ScriptableObject
// Represents One Eventual Unit Instance In A Fleet.
[Serializable]
public class FleetEntry
{
    [SerializeField]
    private string _ID;

    [ScriptableObjectPicker]
    [SerializeField]
    private Definition_Unit _Unit;


    public string ID
    {
        get
        {
            return _ID;
        }
    }


    public Definition_Unit Unit
    {
        get
        {
            return _Unit;
        }
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                _ID))
        {
            ValidationMessage =
                "Fleet Entry ID is empty.";

            return false;
        }


        if (_Unit == null)
        {
            ValidationMessage =
                $"Fleet Entry '{_ID}' has no Unit.";

            return false;
        }


        ValidationMessage =
            string.Empty;

        return true;
    }
}


// ScriptableObject Asset - Not Attached To A GameObject
// Defines One Reusable Authored Fleet.
[CreateAssetMenu(
    fileName = "Fleet_New",
    menuName = "Admirals/Fleet"
)]
public class Definition_Fleet :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string _Name;

    [SerializeField]
    private string _ID;


    [Header("Fleet")]
    [SerializeField]
    private FleetEntry[] _Entries;


    public string Name
    {
        get
        {
            return _Name;
        }
    }


    public string ID
    {
        get
        {
            return _ID;
        }
    }


    public FleetEntry[] Entries
    {
        get
        {
            return _Entries;
        }
    }


    public int EntryCount
    {
        get
        {
            if (_Entries == null)
            {
                return 0;
            }


            return _Entries.Length;
        }
    }


    // Finds one exact Fleet Entry by its stable authored ID.
    public bool TryGetEntry(
        string EntryID,
        out FleetEntry FoundEntry
    )
    {
        FoundEntry =
            null;


        if (string.IsNullOrWhiteSpace(
                EntryID) ||
            _Entries == null)
        {
            return false;
        }


        for (
            int i = 0;
            i < _Entries.Length;
            i++
        )
        {
            FleetEntry Entry =
                _Entries[i];


            if (Entry == null)
            {
                continue;
            }


            if (Entry.ID ==
                EntryID)
            {
                FoundEntry =
                    Entry;

                return true;
            }
        }


        return false;
    }


    public bool ContainsEntry(
        string EntryID
    )
    {
        return
            TryGetEntry(
                EntryID,
                out _
            );
    }


    public bool Validate(
        out string ValidationMessage
    )
    {
        if (string.IsNullOrWhiteSpace(
                _Name))
        {
            ValidationMessage =
                "Fleet Name is empty.";

            return false;
        }


        if (string.IsNullOrWhiteSpace(
                _ID))
        {
            ValidationMessage =
                "Fleet ID is empty.";

            return false;
        }


        if (_Entries == null ||
            _Entries.Length == 0)
        {
            ValidationMessage =
                "Fleet has no Entries.";

            return false;
        }


        HashSet<string> UsedEntryIDs =
            new HashSet<string>();


        for (
            int i = 0;
            i < _Entries.Length;
            i++
        )
        {
            FleetEntry Entry =
                _Entries[i];


            if (Entry == null)
            {
                ValidationMessage =
                    $"Fleet Entry {i} is empty.";

                return false;
            }


            if (!Entry.Validate(
                    out ValidationMessage))
            {
                return false;
            }


            if (!UsedEntryIDs.Add(
                    Entry.ID))
            {
                ValidationMessage =
                    $"Fleet Entry ID '{Entry.ID}' is used more than once.";

                return false;
            }
        }


        ValidationMessage =
            string.Empty;

        return true;
    }


    private void OnValidate()
    {
        if (!Validate(
                out string ValidationMessage))
        {
            Debug.LogError(
                $"Invalid Fleet '{name}': {ValidationMessage}",
                this
            );
        }
    }
}