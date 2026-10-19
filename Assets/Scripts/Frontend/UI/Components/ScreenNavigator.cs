using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class ScreenNavigator : MonoBehaviour
{
    // Collect All Screens
    [SerializeField] private List<UIScreen> Screens;
    // Starting Screen
    [SerializeField] private ScreenID StartScreen;

    // Store Screens In A Stack
    private Stack<UIScreen> ScreenStack = new Stack<UIScreen>();

    private void Awake()
    {
        foreach (UIScreen screen in Screens)
        {
            screen.Initialize(this);
            screen.HideScreen();
        }
    }

    private void Start()
    {
        // Pushes The Set Start Screen
        Push(StartScreen);
    }

    // Add New Screen On Top Of Current
    public void Push(ScreenID TargetID)
    {
        UIScreen Target = FindScreen(TargetID);
        if (Target == null) return;

        if (ScreenStack.Count > 0)
        {
            ScreenStack.Peek().HideScreen();
        }

        Target.ShowScreen();
        ScreenStack.Push(Target);
    }

    // Removes The Top Screen And Goes Back To The Previous One (BACK)
    public void Pop()
    {
        if (ScreenStack.Count > 1)
        {
            UIScreen TopScreen = ScreenStack.Pop();
            TopScreen.HideScreen();

            ScreenStack.Peek().ShowScreen();
        }
    }

    // Removes & Replace Current Screen (NAV)
    public void Swap(ScreenID TargetID)
    {
        UIScreen Target = FindScreen(TargetID);
        if (Target == null) return;

        if (ScreenStack.Count > 0)
        {
            UIScreen TopScreen = ScreenStack.Pop();
            TopScreen.HideScreen();
        }

        Target.ShowScreen();
        ScreenStack.Push(Target);
    }

    // ID search Logic
    private UIScreen FindScreen(ScreenID TargetID)
    {
        foreach (UIScreen screen in Screens)
        {
            if (screen.ID == TargetID)
            {
                return screen;
            }
        }
        return null;
    }
}