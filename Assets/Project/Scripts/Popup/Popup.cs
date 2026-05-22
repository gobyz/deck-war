using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PopupType
{
    Undefined,
    Error,
    ServerFull,
    Timeout
}

public class Popup : MonoBehaviour
{
    [SerializeField] private PopupType popupType;

    public PopupType PopupType => popupType;

    [SerializeField] private bool canBeClosed;

    [SerializeField] private Button okButton;

    private void Start()
    {
        if (!canBeClosed)
        {
            okButton.gameObject.SetActive(false);
        }
        else
        {
            okButton.gameObject.SetActive(true);
            okButton.onClick.AddListener(Hide);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (canBeClosed)
        {
             okButton.onClick.RemoveListener(Hide); 
        }     
    }
}
