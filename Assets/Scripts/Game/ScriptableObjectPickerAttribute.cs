using UnityEngine;


// Supplies optional filtering information to the custom ScriptableObject picker.
public sealed class ScriptableObjectPickerAttribute :
    PropertyAttribute
{
    public string FilterMemberName
    {
        get;
    }


    public object FilterValue
    {
        get;
    }


    public bool HasFilter
    {
        get
        {
            return
                !string.IsNullOrWhiteSpace(
                    FilterMemberName);
        }
    }


    public ScriptableObjectPickerAttribute()
    {
    }


    public ScriptableObjectPickerAttribute(
        string FilterMemberName,
        object FilterValue
    )
    {
        this.FilterMemberName =
            FilterMemberName;

        this.FilterValue =
            FilterValue;
    }
}