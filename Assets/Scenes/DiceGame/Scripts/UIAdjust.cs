using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DiceElement
{
    public RectTransform Parent;
    public RectTransform Image;
    public RectTransform Text;
}

public class UIAdjust : MonoBehaviour
{
    public CanvasScaler UiCanvasScaler;
    public RectTransform LaunchResult;
    public RectTransform CenterPanel;
    public RectTransform SidePanel;
    public int SidePanelWidth;
    public int CenterPanelHeight;
    public DiceElement[] DiceElements;
    private int lastWidth;
    private int lastHeight;

    float GetDiceElementScale()
    {
        return Mathf.Clamp01(lastHeight / (SidePanelWidth * 0.5f * DiceElements.Length));
    }

    void Update()
    {
        if (lastWidth != Screen.width || lastHeight != Screen.height)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            float diceScale = GetDiceElementScale();
            UiCanvasScaler.referenceResolution = new Vector2(lastWidth, lastHeight);
            LaunchResult.sizeDelta = new Vector2(lastWidth, lastHeight);
            CenterPanel.sizeDelta = new Vector2(lastWidth - SidePanelWidth * diceScale, CenterPanelHeight);
            SidePanel.sizeDelta = new Vector2(SidePanelWidth * diceScale, lastHeight);
            foreach (DiceElement curElem in DiceElements)
            {
                curElem.Parent.sizeDelta = new Vector2(SidePanelWidth * diceScale, SidePanelWidth * 0.5f * diceScale);
                curElem.Image.sizeDelta = new Vector2(SidePanelWidth * 0.5f * diceScale, SidePanelWidth * 0.5f * diceScale);
                curElem.Text.sizeDelta = new Vector2(SidePanelWidth * 0.5f * diceScale, SidePanelWidth * 0.5f * diceScale);
            }
        }
    }
}

