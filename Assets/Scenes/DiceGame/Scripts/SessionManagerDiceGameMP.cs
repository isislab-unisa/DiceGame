using Google.XR.ARCoreExtensions.Samples.CloudAnchors;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SessionManagerDiceGameMP : MonoBehaviour, ISessionManager
{
    public string userid, locationid;
    public Button button;
    public MessageHandler message;
    public CloudAnchorsController controller;

    public enum SessionState
    {
        Idle,
        Waiting,
        Playing,
        Ended
    }

    private SessionState state;

    string currentLocation;

    private void Awake()
    {
        state = SessionState.Idle;
#if UNITY_EDITOR
        SessionConfirmed();
#else
        Invoke("StartSuccessfulSessionDebug", 5);
#endif
    }

    void StartSuccessfulSessionDebug()
    {
        SessionStarted(true);
    }

    /*private void OnEnable()
    {
        Debug.Log("[SessionaManager] sending message to rn");
        UnityMessageManager.Instance.OnMessage += OnRNMessage;
        UnityMessageManager.Instance.SendMessageToRN(new UnityMessage() {name = "getInfo"});
    }

    private void OnDestroy()
    {
        UnityMessageManager.Instance.OnMessage -= OnRNMessage;
    }

    void OnRNMessage(string message)
    {
        Debug.Log(message);
        var values = message.Split('&');
        userid = values[0];
        ServerRequests.SetServerURL(values[1]);

    }*/

    
    public SessionState GetState()
    {
        return state;
    }


    public void StartSession(string qrCodeValue)
    {
        var token = GetValuesFromQRString(qrCodeValue);
        if (token == null)
        {
            message.ShowMessage("Codice QR non valido, riprova");
            return;
        }
        Debug.Log(currentLocation);
        ServerRequests.StartGameSessionToken(userid, currentLocation, token);
    }

    string GetValuesFromQRString(string qrCodeValue)
    {
        var values = qrCodeValue.Split('&');
        if (values.Length > 2)
        {
            Debug.Log("string format is invalid");
            return null;
        }
        if (values.Length > 1)
        {
            currentLocation = values[0];
            return values[1];
        }

        currentLocation = locationid;
        return values[0];

    }

    public void SessionStarted(bool success, Dictionary<string, List<ServerRequests.KeyValue>> gameStatePairs = null)
    {
        if (!success)
        {
            message.ShowMessage("La sessione non è partita, riprova");
#if !UNITY_EDITOR
            GetComponent<QRReader>().StartScanning();
#endif
            return;
        }

        state = SessionState.Waiting;
#if !UNITY_EDITOR
        GetComponent<QRReader>().enabled = false;
#endif

        Debug.Log("session started");
        controller.NetworkUIController.StartGame();
        /*if (currentLocation == locationid)
        {
            //ServerRequests.ConfirmSession(userid, locationid);
            GetComponent<TrackedImageInfoManager>().enabled = true;
            message.ShowMessageWithTimeout("Scan the marker to find the dice", 5f);
        }
        else
        {
            Debug.Log("returning to react");
            UnityMessageManager.Instance.SendMessageToRN(new UnityMessage() {name = currentLocation});
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }*/
        
    }

    public void ConfirmSession()
    {
        ServerRequests.ConfirmSession(userid, currentLocation);
    }

    public void SessionConfirmed()
    {
        Debug.Log("session confirmed");
        state = SessionState.Playing;
    }

    public void RestartSession(List<ServerRequests.KeyValue> gameState)
    {
        SessionConfirmed();
    }

    public void NoExistingSession()
    {
        Debug.Log("no existing session found");
        ServerRequests.StartGameSession(userid, locationid);
    }

    public void EndSession()
    {
        Debug.Log("end session");
        //GetComponent<ARPlaneManager>().enabled = false;
        List<ServerRequests.KeyValue> gameState = new List<ServerRequests.KeyValue>();
        gameState.Add(new ServerRequests.KeyValue { key = "dice_value", value = LaunchDice.instance.GetDiceValue() });

        ServerRequests.EndGameSession(userid, currentLocation, gameState);
    }

    public void EndSession(string result, int diceValue)
    {
        Debug.Log("end session");
        //GetComponent<ARPlaneManager>().enabled = false;
        List<ServerRequests.KeyValue> gameState = new List<ServerRequests.KeyValue>();
        gameState.Add(new ServerRequests.KeyValue { key = "result", value = result });
        gameState.Add(new ServerRequests.KeyValue { key = "dice_value", value = diceValue.ToString() });

        ServerRequests.EndGameSession(userid, currentLocation, gameState);
    }

    public void SessionEnded()
    {
        message.ShowMessageWithTimeout("Sessione terminata", 5f);
        state = SessionState.Ended;
        /*UnityMessageManager.Instance.SendMessageToRN(new UnityMessage() { name = "ARSessionEnded" });
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);*/
    }
}
