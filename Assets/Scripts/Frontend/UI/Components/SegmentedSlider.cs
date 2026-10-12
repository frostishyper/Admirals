using System;
using UnityEngine;
using UnityEngine.UI;

// Attach To Segmented_Slider GameObject
public class SegmentedSlider : MonoBehaviour
{
    // Get Slider Object
    [SerializeField] private Button[] _SegmentBTNs;

    // Index For Segments
    [SerializeField] private Image[] _SegmentSprites;

    // Active & Inactive Sprite
    [SerializeField] private Sprite _ActiveSegment;
    [SerializeField] private Sprite _InactiveSegment;

    // Allow Others To Read Value Of Slider (What are you set to?)
    public int CurrentValue { get; private set; }

    // Subscribable Event For When Slider Value Changes
    public event Action<int> SliderUpdated;

    // IlLUSTRATION:
    // SelectedIndex = 4 (you clicked the 5th Segment)
    // [ON][ON][ON][ON][ON][OFF][OFF][OFF][OFF][OFF] 
    // CurrentValue = 50

    private void Awake()
    {
        // Loop Through Each Segment
        for (int SegmentIndex = 0; SegmentIndex < _SegmentBTNs.Length; SegmentIndex++)
        {
            int ClickedSegment = SegmentIndex;
            
            // Access Array Of Segments Here (using lambda) and add a listener to each button to call SelectSegment with the index of the clicked segment
            _SegmentBTNs[ClickedSegment].onClick.AddListener( () => SelectSegment(ClickedSegment) );
        }
    }
    
    // Intialize Slider Value From Outside
    public void IntializeSliderValue(int IntitialValue)
    {
        IntitialValue = Mathf.Clamp(IntitialValue, 10, 100);
        int SelectedSegment = (IntitialValue / 10) - 1;
        CurrentValue = IntitialValue;
        // Update Slider Visuals
        for (int SegmentIndex = 0; SegmentIndex < _SegmentSprites.Length; SegmentIndex++)
        {
            bool IsActiveSegment = SegmentIndex <= SelectedSegment;
            _SegmentSprites[SegmentIndex].sprite = IsActiveSegment ? _ActiveSegment : _InactiveSegment;
        }
    }

    //(100 max value / 10 segments = 10 value per segment)
    // User Interaction
    private void SelectSegment(int SelectedSegment)
    {
        // Convert Selected Segment To Value
        CurrentValue = (SelectedSegment + 1) * 10;
        // Intialize Slider Visuals & Value
        IntializeSliderValue(CurrentValue);
        // Invoker (Only works if something is subscribed to the event)
        SliderUpdated?.Invoke(CurrentValue);
    }
}
