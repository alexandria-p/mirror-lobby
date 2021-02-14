using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.ComponentModel; 
using TMPro;
using System.Linq;

public class AlertManager: MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject disconnectMenuPanel = null;
    public TMP_Text ErrorMessageGameobject = null;

     public void Start() {      
                DontDestroyOnLoad(gameObject);          
        }

    private void ShowMenu() {
        disconnectMenuPanel.SetActive(true);
    }
// call when you click resume
    public void HideMenu() {  
        disconnectMenuPanel.SetActive(false);
    }

    public void ShowDisconnectMessage(int disconnectType) {
        DisconnectType disconnectEnum = (DisconnectType)disconnectType;
        var fieldInfo = disconnectEnum.GetType().GetField(disconnectEnum.ToString());
        var descriptionAttributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false); 
        string errorDescription = descriptionAttributes.Length > 0 ? descriptionAttributes[0].Description : "An error has occurred.";
        Debug.LogWarning(errorDescription);
        ErrorMessageGameobject.text = errorDescription;
        ShowMenu();
    }

}
