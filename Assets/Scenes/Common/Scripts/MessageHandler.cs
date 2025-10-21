using UnityEngine;
using UnityEngine.UI;

public class MessageHandler : MonoBehaviour
{
    public static MessageHandler instance;

    private void Awake()
    {
        instance = this;
    }

    public void ShowMessage(string message)
    {
        CancelInvoke();
        GetComponentInChildren<Text>().text = message;
        gameObject.SetActive(true);
    }

    public void ShowMessageWithTimeout(string message, float time)
    {
        CancelInvoke();
        GetComponentInChildren<Text>().text = message;
        gameObject.SetActive(true);
        Invoke("DisableMessage", time);
    }

    void DisableMessage()
    {
        gameObject.SetActive(false);
    }
}
