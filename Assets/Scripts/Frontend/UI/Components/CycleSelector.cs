using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach To Selector_Cycle Root GameObject
public class CycleSelector : MonoBehaviour
{
    // Previous & Next Buttons
    [SerializeField] private Button _PreviousBTN;
    [SerializeField] private Button _NextBTN;

    // Current Displayed Value
    [SerializeField] private TMP_Text _ValueText;


    // Notify Outside Systems When Either Arrow Is Clicked
    public event Action PreviousClicked;
    public event Action NextClicked;


    private void Awake()
    {
        // Listen To Arrow Buttons
        _PreviousBTN.onClick.AddListener(Previous);
        _NextBTN.onClick.AddListener(Next);
    }


    // Update Displayed Value From Outside
    public void SetValue(string NewValue)
    {
        _ValueText.text = NewValue;
    }


    // Previous Arrow Clicked
    private void Previous()
    {
        PreviousClicked?.Invoke();
    }


    // Next Arrow Clicked
    private void Next()
    {
        NextClicked?.Invoke();
    }
}