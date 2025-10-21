using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DiceLegendItem
{
    public int Value;
    public Text Text;
    public RawImage Image;
    public Texture2D Texture;
}

public class ShowLaunchResult : MonoBehaviour
{
    public GameObject diceDisplayPrefab;
    public Text finalValueText;
    public GameObject dicesParent;
    public DiceLegendItem[] diceItems;
    public Color ColorOn;
    public Color ColorOff;

    public void ShowResult(bool isLocal, int [] diceValues)
    {
        MessageHandler.instance.gameObject.SetActive(false);
        int total = 0;
        foreach(var value in diceValues)
        {
            total += value;
            var diceDisplay = Instantiate(diceDisplayPrefab, dicesParent.transform);
            DiceLegendItem item = diceItems.First(i => i.Value == value);
            diceDisplay.GetComponent<RawImage>().texture = item.Texture;
        }

        foreach (DiceLegendItem curItem in diceItems)
        {
            if (diceValues.Contains(curItem.Value))
            {
                curItem.Text.color = ColorOn;
                curItem.Image.color = ColorOn;
            }
            else
            {   
                curItem.Text.color = ColorOff;
                curItem.Image.color = ColorOff;
            }
        }

        string message = isLocal ? "Il tuo punteggio: <color=#005500>" : "Il suo punteggio: <color=#005500>";

        finalValueText.text = message + total + "</color>";
        gameObject.SetActive(true);
        Invoke("DisableMessage", 8f);
    }

    public void ShowRoundResult(string message)
    {
        CancelInvoke();
        MessageHandler.instance.gameObject.SetActive(false);
        dicesParent.gameObject.SetActive(false);
        finalValueText.text = message;
        gameObject.SetActive(true);
        Invoke("DisableMessage", 5f);
    }

    public void ShowFinalResult(string message)
    {
        CancelInvoke();
        MessageHandler.instance.gameObject.SetActive(false);
        dicesParent.gameObject.SetActive(false);
        finalValueText.text = message;
        gameObject.SetActive(true);

    }

    private void DisableMessage()
    {
        gameObject.SetActive(false);
        for (int i = 0; i < dicesParent.transform.childCount; ++i)
        {
            Destroy(dicesParent.transform.GetChild(i).gameObject);
        }
    }
}
