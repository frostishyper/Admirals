using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines A Reusable Manufacturer Classification
[CreateAssetMenu(
    fileName = "Manufacturer_New",
    menuName = "Admirals/Manufacturer"
)]
public class Manufacturers : ScriptableObject
{
    // Display Name Of The Manufacturer
    [SerializeField] private string _ManufacturerName;

    // Unique ID Used To Identify The Manufacturer
    [SerializeField] private string _ManufacturerID;

    // Logo Used To Represent The Manufacturer
    [SerializeField] private Sprite _ManufacturerLogo;


    public string ManufacturerName
    {
        get { return _ManufacturerName; }
    }

    public string ManufacturerID
    {
        get { return _ManufacturerID; }
    }

    public Sprite ManufacturerLogo
    {
        get { return _ManufacturerLogo; }
    }
}