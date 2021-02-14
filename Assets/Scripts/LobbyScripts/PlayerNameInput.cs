using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerNameInput : MonoBehaviour
{
    [Header("UI")] // property attribute, adds a header in the Unity Inspector above its fields
    [SerializeField] private TMP_InputField nameInputField = null;
    [SerializeField] private Button continueButton = null;

    public static string DisplayName { get; private set; } // can only be set internally, but can be accessed by other classes
    private const string PlayerPrefsNameKey = "PlayerName";

    // Start is called before the first frame update
    void Start()
    {
        continueButton.interactable = false;
        SetUpInputField();
    }

    private void SetUpInputField()
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsNameKey)) { return; }
        string defaultName = PlayerPrefs.GetString(PlayerPrefsNameKey); // if you've played before, load up your name
        nameInputField.text = defaultName;
        SetPlayerName(defaultName);
    }

// should probably be renamed as it seems to be checking disabled/enabled for the continuebutton
    public void SetPlayerName(string name)
    {
        continueButton.interactable = !string.IsNullOrEmpty(name);
    }

    public void SavePlayerName()
    {
        DisplayName = nameInputField.text;
        PlayerPrefs.SetString(PlayerPrefsNameKey, DisplayName);
    }
}
