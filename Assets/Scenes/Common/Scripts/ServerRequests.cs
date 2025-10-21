using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ServerRequests : MonoBehaviour
{
    //private const string URL = "http://ec2-34-218-246-178.us-west-2.compute.amazonaws.com";
    private static string URL = "http://192.168.1.17:1337";
    static ServerRequests instance;

    private const string gameStateSelector = "gamestate";

    private void Awake()
    {
        instance = this;
        Debug.Log("server requests awaken");
    }

    public static void SetServerURL(string newURL)
    {
        URL = newURL;
    }

    public struct KeyValue
    {
        public string key;
        public string value;
    }

    [System.Serializable]
    public struct GameState
    {
        public string locationid;
        public string gameState;
    }

    #region user

    #region User registration
    public static void UserRegistration(string username, string facebookID = null)
    {
        instance.StartCoroutine(UserRegistrationCoroutine(username, facebookID));
    }

    static IEnumerator UserRegistrationCoroutine(string username, string facebookID = null)
    {
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        if (facebookID != null)
            form.AddField("facebookid", facebookID);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/register", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully

            //get user id
            string id = request.GetResponseHeader("id");

            Debug.Log("registration completed, id: " + id);

            //id must be saved for future requests
        }

    }
    #endregion


    #region Facebook association
    public static void FacebookAssociation(string userID, string facebookID)
    {
        instance.StartCoroutine(FacebookAssociationCoroutine(userID, facebookID));
    }

    static IEnumerator FacebookAssociationCoroutine(string userID, string facebookID)
    {
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("userid", userID);
        form.AddField("facebookid", facebookID);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/associatefacebook", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully
            Debug.Log(request.downloadHandler.text);
        }

    }

    #endregion


    #region Start game session
    public static void StartGameSession(string userID, string locationID)
    {
        instance.StartCoroutine(StartGameSessionCoroutine(userID, locationID));
    }

    static IEnumerator StartGameSessionCoroutine(string userID, string locationID)
    {
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("userid", userID);
        form.AddField("locationid", locationID);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/startsession", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully
            //get session id
            string sessionID = request.GetResponseHeader("sessionid");
            var gameStates = JsonHelper.GetJsonArray<GameState>(request.downloadHandler.text);
            var locationGameStatePairs = new Dictionary<string, List<KeyValue>>();
            if (gameStates != null)
                foreach (var gameState in gameStates)
                    locationGameStatePairs.Add(gameState.locationid, BuildGameState(gameState.gameState));

            Debug.Log("session created with id: " + sessionID);
            instance.GetComponent<ISessionManager>().SessionStarted(true, locationGameStatePairs);
            //session id must be saved for future requests
        }

    }

    #endregion


    #region Start game session token
    public static void StartGameSessionToken(string userID, string locationID, string token)
    {
        instance.StartCoroutine(StartGameSessionTokenCoroutine(userID, locationID, token));
    }

    static IEnumerator StartGameSessionTokenCoroutine(string userID, string locationID, string token)
    {
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("userid", userID);
        form.AddField("locationid", locationID);
        form.AddField("token", token);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/startsessiontoken", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
            instance.GetComponent<ISessionManager>().SessionStarted(false);
        }
        else
        {
            //response ended succesfully
            //get session id
            string sessionID = request.GetResponseHeader("sessionid");
            var gameStates = JsonHelper.GetJsonArray<GameState>(request.downloadHandler.text);
            var locationGameStatePairs = new Dictionary<string, List<KeyValue>>();
            if (gameStates != null)
                foreach (var gameState in gameStates)
                    locationGameStatePairs.Add(gameState.locationid, BuildGameState(gameState.gameState));

            Debug.Log("session created with id: " + sessionID);
            instance.GetComponent<ISessionManager>().SessionStarted(true, locationGameStatePairs);
            //session id must be saved for future requests
        }

    }

    #endregion


    #region Restart game session
    public static void RestartGameSession(string userID, string locationID)
    {
        instance.StartCoroutine(RestartGameSessionCoroutine(userID, locationID));
    }

    static IEnumerator RestartGameSessionCoroutine(string userID, string locationID)
    {
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("userid", userID);
        form.AddField("locationid", locationID);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/restartexistingsession", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
            if (request.responseCode == 422)
                instance.GetComponent<ISessionManager>().NoExistingSession();
        }
        else
        {
            //response ended succesfully
            var gameState = request.GetResponseHeader("gamestate");
            instance.GetComponent<ISessionManager>().RestartSession(BuildGameState(gameState));
        }

    }

    #endregion


    #region Confirm session
    public static void ConfirmSession(string userID, string locationID)
    {
        instance.StartCoroutine(ConfirmSessionCoroutine(userID, locationID));
    }

    static IEnumerator ConfirmSessionCoroutine(string userID, string locationID)
    {
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("userid", userID);
        form.AddField("locationid", locationID);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/confirmsession", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully
            instance.GetComponent<ISessionManager>().SessionConfirmed();
            Debug.Log(request.downloadHandler.text);
        }

    }

    #endregion


    #region Get new session
    public static void GetNewSession(string locationID)
    {
        instance.StartCoroutine(GetNewSessionCoroutine(locationID));
    }

    static IEnumerator GetNewSessionCoroutine(string locationID)
    {
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("locationid", locationID);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/getnewsession", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully
            var sessionid = request.GetResponseHeader("sessionid");
            var userid = request.GetResponseHeader("userid");

            Debug.LogFormat("Found session with ID {0} for user {1}", sessionid, userid);
        }

    }

    #endregion


    #region End game session
    public static void EndGameSession(string userID, string locationID, List<KeyValue> gameState)
    {
        var gameStateString = FormatGameState(gameState);
        if (gameStateString != null)
            instance.StartCoroutine(EndGameSessionCoroutine(userID, locationID, gameStateString));
        else
            Debug.Log("game state needed");
    }


    static IEnumerator EndGameSessionCoroutine(string userID, string locationID, string gameState)
    {
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("userid", userID);
        form.AddField("locationid", locationID);
        form.AddField("gamestate", gameState);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/endsession", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully
            Debug.Log(request.downloadHandler.text);
            instance.GetComponent<ISessionManager>().SessionEnded();
        }

    }

    #endregion

    #endregion

    #region admin

    #region Get user
    public static void GetUserByID(string userid)
    {
        instance.StartCoroutine(GetUserByIDCoroutine(userid));
    }

    static IEnumerator GetUserByIDCoroutine(string userid)
    {

        //creating request
        StringBuilder requestURL = new StringBuilder(URL);
        requestURL.Append("/getuser");
        requestURL.AppendFormat("?userid={0}", userid);
        UnityWebRequest request = UnityWebRequest.Get(requestURL.ToString());

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully

            Debug.Log(request.downloadHandler.text);
        }

    }
    #endregion

    #region List Users
    public static void ListUsers()
    {
        instance.StartCoroutine(ListUsersCoroutine());
    }

    static IEnumerator ListUsersCoroutine()
    {

        //creating request
        StringBuilder requestURL = new StringBuilder(URL);
        requestURL.Append("/listusers");
        UnityWebRequest request = UnityWebRequest.Get(requestURL.ToString());

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully

            Debug.Log(request.downloadHandler.text);
        }

    }
    #endregion

    #region Update User
    public static void UpdateUser(string userID, string username = null, string facebookID = null)
    {
        instance.StartCoroutine(UpdateUserCoroutine(userID, username, facebookID));
    }

    static IEnumerator UpdateUserCoroutine(string userID, string username = null, string facebookID = null)
    {
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("userid", userID);
        if (username != null)
            form.AddField("username", username);
        if (facebookID != null)
            form.AddField("facebookid", facebookID);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/updateuser", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully
            Debug.Log(request.downloadHandler.text);
        }

    }

    #endregion

    #region Add user
    public static void AddUser(string username, string facebookID = null)
    {
        instance.StartCoroutine(AddUserCoroutine(username, facebookID));
    }

    static IEnumerator AddUserCoroutine(string username, string facebookID = null)
    {
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        if (facebookID != null)
            form.AddField("facebookid", facebookID);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/adduser", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully

            //get user id
            string id = request.GetResponseHeader("id");

            Debug.Log("user added, id: " + id);
        }

    }
    #endregion

    #region remove user
    public static void RemoveUser(string userid)
    {
        instance.StartCoroutine(RemoveUserCoroutine(userid));
    }

    static IEnumerator RemoveUserCoroutine(string userid)
    {

        //creating request
        WWWForm form = new WWWForm();
        form.AddField("userid", userid);
        UnityWebRequest request = UnityWebRequest.Post(URL + "/removeuser", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully

            Debug.Log(request.downloadHandler.text);
        }

    }
    #endregion


    #region Get Session
    public static void GetSessionByID(string sessionid)
    {
        instance.StartCoroutine(GetSessionByIDCoroutine(sessionid));
    }

    static IEnumerator GetSessionByIDCoroutine(string sessionid)
    {

        //creating request
        StringBuilder requestURL = new StringBuilder(URL);
        requestURL.Append("/getsession");
        requestURL.AppendFormat("?sessionid={0}", sessionid);
        UnityWebRequest request = UnityWebRequest.Get(requestURL.ToString());

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully

            Debug.Log(request.downloadHandler.text);
        }

    }
    #endregion

    #region List Sessions
    public static void ListSessions()
    {
        instance.StartCoroutine(ListSessionsCoroutine());
    }

    static IEnumerator ListSessionsCoroutine()
    {

        //creating request
        StringBuilder requestURL = new StringBuilder(URL);
        requestURL.Append("/listsessions");
        UnityWebRequest request = UnityWebRequest.Get(requestURL.ToString());

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully

            Debug.Log(request.downloadHandler.text);
        }

    }
    #endregion

    #region Update Session
    public static void UpdateSession(string sessionid, string userid = null, string locationid = null, string state = null, List<KeyValue> gameState = null)
    {
        instance.StartCoroutine(UpdateSessionCoroutine(sessionid, userid, locationid, state, FormatGameState(gameState)));
    }

    static IEnumerator UpdateSessionCoroutine(string sessionid, string userid = null, string locationid = null, string state = null, string gameState = null)
    {
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("sessionid", sessionid);
        if (userid != null)
            form.AddField("userid", userid);
        if (locationid != null)
            form.AddField("locationid", locationid);
        if (state != null)
            form.AddField("state", state);
        if (gameState != null)
            form.AddField("gamestate", gameState);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/updatesession", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully
            Debug.Log(request.downloadHandler.text);
        }

    }

    #endregion

    #region Add Session
    public static void AddSession(string userid, string locationid, string state, List<KeyValue> gameState = null)
    {
        instance.StartCoroutine(AddSessionCoroutine(userid, locationid, state, FormatGameState(gameState)));
    }

    static IEnumerator AddSessionCoroutine(string userid, string locationid, string state, string gameState = null)
    {
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("userid", userid);
        form.AddField("locationid", locationid);
        form.AddField("state", state);
        if (gameState != null)
            form.AddField("gamestate", gameState);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/addsession", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully
            Debug.Log(request.downloadHandler.text);
        }

    }

    #endregion

    #region remove Session
    public static void RemoveSession(string sessionid)
    {
        instance.StartCoroutine(RemoveSessionCoroutine(sessionid));
    }

    static IEnumerator RemoveSessionCoroutine(string sessionid)
    {

        //creating request
        WWWForm form = new WWWForm();
        form.AddField("sessionid", sessionid);
        UnityWebRequest request = UnityWebRequest.Post(URL + "/removesession", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully

            Debug.Log(request.downloadHandler.text);
        }

    }
    #endregion


    #endregion


    static string FormatGameState(List<KeyValue> gameState)
    {
        if (gameState == null)
            return null;

        if (gameState.Count <= 0)
            return string.Empty;

        StringBuilder formattedString = new StringBuilder();
        foreach (var pair in gameState)
        {
            formattedString.AppendFormat("{0}:{1};", pair.key, pair.value);
        }

        return formattedString.ToString();
    }

    static List<KeyValue> BuildGameState(string gameState)
    {
        if (gameState == null)
            return null;

        var items = gameState.Split(';');
        var result = new List<KeyValue>();
        foreach(var item in items)
        {
            if (!item.Trim().Equals(string.Empty))
            {
                var separatorIndex = item.IndexOf(':');
                var key = item.Substring(0, separatorIndex);
                var value = item.Substring(separatorIndex + 1, item.Length - separatorIndex - 1);
                result.Add(new KeyValue { key = key, value = value });
            }
        }

        return result;
    }
}
