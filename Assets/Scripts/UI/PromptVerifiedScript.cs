using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;  

// -- SCRIPT --
// Script for the verified input prompt prefab variant, which allows for user input with validation and feedback. -Frostishyper
// actaul validation logic is provided by the caller through a callback function, allowing for flexible use cases. -Frostishyper
public class PromptVerifiedScript : MonoBehaviour
{
    [SerializeField] private TMP_Text PromptText;
    [SerializeField] private TMP_Text StatusText;
    [SerializeField] private TMP_InputField TextInput;
    [SerializeField] private Button SubmitButton;

    // My first time using and studying C# Func<>, its basically a variable that contains a function instead of just a value, and it can be invoked like a normal function. -Frostishyper
    // Not hardcoded for anything specific, just a generic input prompt with input validation and feedback provided by the caller. -Frostishyper
    private Func<string, (bool isValid, string message)> OnValidateInput;

    // Sets up prompt for use
    // Expects a title for the prompt and a validation callback function that takes a string input and returns a tuple indicating validity and a message. -Frostishyper
    public void Initialize(string PromptTitle, Func<string, (bool isValid, string message)> ValidationCallBack)
    {
        PromptText.text = PromptTitle;
        StatusText.text = string.Empty;
        TextInput.text = string.Empty;

        SubmitButton.interactable = true;

        // set the internal func to the provided validation callback -Frostishyper
        OnValidateInput = ValidationCallBack;

        // Allow multiple submission attempts by clearing previous listeners and adding the new one for this instance. -Frostishyper
        // so you dont get stuck with the first validation result and can try again if the input is invalid. -Frostishyper
        SubmitButton.onClick.RemoveAllListeners();
        SubmitButton.onClick.AddListener(ProcessSubmit); // calls the internal method that invokes the provided validation callback and handles the result. -Frostishyper
    }

    // Processes the submission of the input, invoking the validation callback and providing feedback to the user. -Frostishyper
    private void ProcessSubmit() 
    {
        if (OnValidateInput == null) return;

        // make a copy of the input text and pass it to the validation callback, which returns a tuple indicating validity and a message. -Frostishyper
        var (isValid, message) = OnValidateInput(TextInput.text);

        if (isValid)
        {
            SubmitButton.interactable = false;
            StatusText.text = message;
            StatusText.color = Color.green;
            StartCoroutine(ClosePromptAfterDelay(2f));
        }
        else
        {
            StatusText.text = message; 
            StatusText.color = Color.red;
        }
    }
    
    // Coroutine so the prompt doesnt insta close and give the user a chance to read the feedback message, we wait a few seconds before closing it. -Frostishyper
    private IEnumerator ClosePromptAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
    
}
