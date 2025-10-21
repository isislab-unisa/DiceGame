
namespace Google.XR.ARCoreExtensions.Samples.CloudAnchors
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.Networking.Match;
    using UnityEngine.Networking.Types;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// Controller managing UI for joining and creating rooms.
    /// </summary>
#pragma warning disable 618
    [RequireComponent(typeof(CloudAnchorsNetworkManager))]
#pragma warning restore 618
    public class NetworkManagerUIController : MonoBehaviour
    {
        /// <summary>
        /// The Cloud Anchors Example Controller.
        /// </summary>
        public CloudAnchorsController CloudAnchorsController;

        /// <summary>
        /// The number of matches that will be shown.
        /// </summary>
        private const int k_MatchPageSize = 5;

        /// <summary>
        /// The Network Manager.
        /// </summary>
#pragma warning disable 618
        private CloudAnchorsNetworkManager m_Manager;
#pragma warning restore 618

        /// <summary>
        /// The current room number.
        /// </summary>
        private string m_CurrentRoomNumber;

        /// <summary>
        /// The Join Room buttons.
        /// </summary>
        private List<GameObject> m_JoinRoomButtonsPool = new List<GameObject>();

        /// <summary>
        /// The Unity Awake() method.
        /// </summary>
        public void Awake()
        {
#pragma warning disable 618
            m_Manager = GetComponent<CloudAnchorsNetworkManager>();
#pragma warning restore 618
            m_Manager.StartMatchMaker();
#if UNITY_EDITOR
            GetMatchList();
#endif
        }

        public void StartGame()
        {
            GetMatchList();
        }

        private void GetMatchList()
        {
            m_Manager.matchMaker.ListMatches(
                startPageNumber: 0,
                resultPageSize: k_MatchPageSize,
                matchNameFilter: string.Empty,
                filterOutPrivateMatchesFromResults: false,
                eloScoreTarget: 0,
                requestDomain: 0,
                callback: _OnMatchList);
        }

        /// <summary>
        /// Handles the user intent to create a new room.
        /// </summary>
        public void CreateMatch()
        {
            m_Manager.matchMaker.CreateMatch(
                m_Manager.matchName, m_Manager.matchSize, true, string.Empty, string.Empty,
                string.Empty, 0, 0, _OnMatchCreate);
        }

        /// <summary>
        /// Handles the user intent to refresh the room list.
        /// </summary>
        public void OnRefhreshRoomListClicked()
        {
            m_Manager.matchMaker.ListMatches(
                startPageNumber: 0,
                resultPageSize: k_MatchPageSize,
                matchNameFilter: string.Empty,
                filterOutPrivateMatchesFromResults: false,
                eloScoreTarget: 0,
                requestDomain: 0,
                callback: _OnMatchList);
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was instantiated and the host request was
        /// made.
        /// </summary>
        /// <param name="isHost">Indicates whether this player is the host.</param>
        public void OnAnchorInstantiated(bool isHost)
        {
            if (isHost)
            {
                MessageHandler.instance.ShowMessage("Hosting Cloud Anchor...");
            }
            else
            {
                MessageHandler.instance.ShowMessage("Cloud Anchor added to session! Attempting to resolve anchor...");
            }
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was hosted.
        /// </summary>
        /// <param name="success">If set to <c>true</c> indicates the Cloud Anchor was hosted
        /// successfully.</param>
        /// <param name="response">The response string received.</param>
        public void OnAnchorHosted(bool success, string response)
        {
            if (success)
            {
                MessageHandler.instance.ShowMessage("Cloud Anchor successfully hosted!\nWaiting for other player");
            }
            else
            {
                MessageHandler.instance.ShowMessage("Cloud Anchor could not be hosted. " + response);
            }
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was resolved.
        /// </summary>
        /// <param name="success">If set to <c>true</c> indicates the Cloud Anchor was resolved
        /// successfully.</param>
        /// <param name="response">The response string received.</param>
        public void OnAnchorResolved(bool success, string response)
        {
            if (success)
            {
                MessageHandler.instance.ShowMessage("Cloud Anchor successfully resolved!");
            }
            else
            {
                MessageHandler.instance.ShowMessage("Cloud Anchor could not be resolved. Will attempt again. " + response);
            }
        }

        /// <summary>
        /// Use the snackbar to display the error message.
        /// </summary>
        /// <param name="debugMessage">The debug message to be displayed on the snackbar.</param>
        public void ShowDebugMessage(string debugMessage)
        {
            MessageHandler.instance.ShowMessage(debugMessage);
        }

        /// <summary>
        /// Handles the user intent to join the room associated with the button clicked.
        /// </summary>
        /// <param name="match">The information about the match that the user intents to
        /// join.</param>
#pragma warning disable 618
        private void JoinRoom(MatchInfoSnapshot match)
#pragma warning restore 618
        {
            m_Manager.matchName = match.name;
            m_Manager.matchMaker.JoinMatch(match.networkId, string.Empty, string.Empty,
                                         string.Empty, 0, 0, _OnMatchJoined);
        }

        /// <summary>
        /// Callback that happens when a <see cref="NetworkMatch.ListMatches"/> request has been
        /// processed on the server.
        /// </summary>
        /// <param name="success">Indicates if the request succeeded.</param>
        /// <param name="extendedInfo">A text description for the error if success is false.</param>
        /// <param name="matches">A list of matches corresponding to the filters set in the initial
        /// list request.</param>
#pragma warning disable 618
        private void _OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
#pragma warning restore 618
        {
            if (!success)
            {
                GetMatchList();
                return;
            }

            m_Manager.OnMatchList(success, extendedInfo, matches);
            bool joinedRoom = false;
            if (m_Manager.matches != null && m_Manager.matches.Count > 0)
            {
#pragma warning disable 618
                foreach (var match in m_Manager.matches)
#pragma warning restore 618
                {
                    if (match.currentSize < match.maxSize)
                    {
                        JoinRoom(match);
                        joinedRoom = true;
                        break;

                    }
                }
            }

            if (!joinedRoom)
            {
                CreateMatch();
            }
        }

        /// <summary>
        /// Callback that happens when a <see cref="NetworkMatch.CreateMatch"/> request has been
        /// processed on the server.
        /// </summary>
        /// <param name="success">Indicates if the request succeeded.</param>
        /// <param name="extendedInfo">A text description for the error if success is false.</param>
        /// <param name="matchInfo">The information about the newly created match.</param>
#pragma warning disable 618
        private void _OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
#pragma warning restore 618
        {
            if (!success)
            {
                MessageHandler.instance.ShowMessage("Could not create match: " + extendedInfo);
                GetMatchList();
                return;
            }

            m_Manager.OnMatchCreate(success, extendedInfo, matchInfo);
            m_CurrentRoomNumber = _GetRoomNumberFromNetworkId(matchInfo.networkId);
            MessageHandler.instance.ShowMessage("Connecting to server...");
            CloudAnchorsController.OnEnterHostingMode();

        }

        /// <summary>
        /// Callback that happens when a <see cref="NetworkMatch.JoinMatch"/> request has been
        /// processed on the server.
        /// </summary>
        /// <param name="success">Indicates if the request succeeded.</param>
        /// <param name="extendedInfo">A text description for the error if success is false.</param>
        /// <param name="matchInfo">The info for the newly joined match.</param>
#pragma warning disable 618
        private void _OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo)
#pragma warning restore 618
        {
            if (!success)
            {
                MessageHandler.instance.ShowMessage("Could not join to match: " + extendedInfo);
                GetMatchList();
                return;
            }

            m_Manager.OnMatchJoined(success, extendedInfo, matchInfo);
            m_CurrentRoomNumber = _GetRoomNumberFromNetworkId(matchInfo.networkId);
            MessageHandler.instance.ShowMessage("Connecting to server...");
            CloudAnchorsController.OnEnterResolvingMode();
        }

        /// <summary>
        /// Callback that happens when a <see cref="NetworkMatch.DropConnection"/> request has been
        /// processed on the server.
        /// </summary>
        /// <param name="success">Indicates if the request succeeded.</param>
        /// <param name="extendedInfo">A text description for the error if success is false.
        /// </param>
        private void _OnMatchDropped(bool success, string extendedInfo)
        {
            if (!success)
            {
                MessageHandler.instance.ShowMessage("Could not drop the match: " + extendedInfo);
                return;
            }

            m_Manager.OnDropConnection(success, extendedInfo);
#pragma warning disable 618
            NetworkManager.Shutdown();
#pragma warning restore 618
            SceneManager.LoadScene("CloudAnchors");
        }

        private string _GetRoomNumberFromNetworkId(NetworkID networkID)
        {
            return (System.Convert.ToInt64(networkID.ToString()) % 10000).ToString();
        }
    }
}
