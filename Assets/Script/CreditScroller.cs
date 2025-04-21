using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditScroller : MonoBehaviour
{
    [Header("UI References")]
    public GameObject  creditPanel;
    public RectTransform creditPanelTransform;
    public float scrollSpeed = 50f;
    public float delayBeforeScroll = 2f;


    private float startY;
    private bool startScrolling = false;
    private bool creditFinished = false;

    void Start()
    {
        creditPanel.SetActive(false);
        startY = creditPanelTransform.anchoredPosition.y;
    }

    public void ShowCredit(){
        creditPanelTransform.anchoredPosition = new Vector2(creditPanelTransform.anchoredPosition.x, startY);
        creditPanel.SetActive(true);
        creditFinished = false;
        Invoke("StartScroll", delayBeforeScroll);
    }

    void StartScroll()
    {
        startScrolling = true;
    }

    void Update()
    {
        if (startScrolling && !creditFinished)
        {
            creditPanelTransform.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

            if (creditPanelTransform.anchoredPosition.y >= creditPanelTransform.sizeDelta.y)
            {
                HideCredit();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
        {
            HideCredit();
        }
    }

    void HideCredit()
    {
        creditPanel.SetActive(false); // Menyembunyikan panel credit
        startScrolling = false;
        creditFinished = true;
    }
}
