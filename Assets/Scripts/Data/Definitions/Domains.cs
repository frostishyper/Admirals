using UnityEngine;

// ScriptableObject Asset - Not Attached To A GameObject
// Defines A Universal Gameplay Domain And Its Display Badge
[CreateAssetMenu(
    fileName = "Domain_New",
    menuName = "Admirals/Domain"
)]
public class Domains : ScriptableObject
{
    // Display Name Of The Domain
    [SerializeField] private string _DomainName;

    // Unique ID Used To Identify The Domain
    [SerializeField] private string _DomainID;

    // Badge / Icon Used To Represent The Domain
    [SerializeField] private Sprite _DomainSprite;


    public string DomainName
    {
        get { return _DomainName; }
    }

    public string DomainID
    {
        get { return _DomainID; }
    }

    public Sprite DomainSprite
    {
        get { return _DomainSprite; }
    }
}