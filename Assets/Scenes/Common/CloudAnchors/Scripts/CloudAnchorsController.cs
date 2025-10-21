//-----------------------------------------------------------------------
// <copyright file="CloudAnchorsExampleController.cs" company="Google">
//
// Copyright 2019 Google LLC. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace Google.XR.ARCoreExtensions.Samples.CloudAnchors
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.Networking;
    using UnityEngine.SceneManagement;
    using UnityEngine.XR.ARFoundation;

    /// <summary>
    /// Controller for the Cloud Anchors Example. Handles the ARCore lifecycle.
    /// See details in
    /// <a href="https://developers.google.com/ar/develop/unity-arf/cloud-anchors/overview">
    /// Share AR experiences with Cloud Anchors</a>
    /// </summary>
    public class CloudAnchorsController : MonoBehaviour
    {
        public static CloudAnchorsController instance;

        [Header("AR Foundation")]

        /// <summary>
        /// The active AR Session Origin used in the example.
        /// </summary>
        public ARSessionOrigin SessionOrigin;

        /// <summary>
        /// The AR Session used in the example.
        /// </summary>
        public GameObject SessionCore;

        /// <summary>
        /// The AR Extentions used in the example.
        /// </summary>
        public GameObject ARExtentions;

        public GameObject anchorPrefab;

        /// <summary>
        /// The active AR Reference Point Manager used in the example.
        /// </summary>
        public ARReferencePointManager ReferencePointManager;

        /// <summary>
        /// The active AR Raycast Manager used in the example.
        /// </summary>
        public ARRaycastManager RaycastManager;

        [Header("UI")]

        /// <summary>
        /// The network manager UI Controller.
        /// </summary>
        public NetworkManagerUIController NetworkUIController;

        [Header("Session Manager")]
        public SessionManagerDiceGameMP sessionManager;

        /// <summary>
        /// The time between room starts up and ARCore session starts resolving.
        /// </summary>
        private const float k_ResolvingPrepareTime = 3.0f;

        /// <summary>
        /// Record the time since the room started. If it passed the resolving prepare time,
        /// applications in resolving mode start resolving the anchor.
        /// </summary>
        private float m_TimeSinceStart = 0.0f;

        /// <summary>
        /// Indicates whether passes the resolving prepare time.
        /// </summary>
        private bool m_PassedResolvingPreparedTime = false;

        /// <summary>
        /// Indicates whether the Anchor was already instantiated.
        /// </summary>
        private bool m_AnchorAlreadyInstantiated = false;

        /// <summary>
        /// Indicates whether the Cloud Anchor finished hosting.
        /// </summary>
        private bool m_AnchorFinishedHosting = false;

        /// <summary>
        /// True if the app is in the process of quitting due to an ARCore connection error,
        /// otherwise false.
        /// </summary>
        private bool m_IsQuitting = false;

        /// <summary>
        /// The world origin transform for this session.
        /// </summary>
        private Transform m_WorldOrigin = null;

        private int numClientReady = 0;

        public GameObject Anchor { get; set; }

        /// <summary>
        /// The current cloud anchor mode.
        /// </summary>
        private ApplicationMode m_CurrentMode = ApplicationMode.Ready;


        /// <summary>
        /// The Network Manager.
        /// </summary>
#pragma warning disable 618
        private CloudAnchorsNetworkManager m_NetworkManager;
#pragma warning restore 618

        /// <summary>
        /// Enumerates modes the example application can be in.
        /// </summary>
        public enum ApplicationMode
        {
            /// <summary>
            /// Enume mode that indicate the example application is ready to host or resolve.
            /// </summary>
            Ready,

            /// <summary>
            /// Enume mode that indicate the example application is hosting cloud anchors.
            /// </summary>
            Hosting,

            /// <summary>
            /// Enume mode that indicate the example application is resolving cloud anchors.
            /// </summary>
            Resolving,
        }

        public enum AnchorPlacementMode
        {
            CloudAnchors,
            Marker
        }

        public AnchorPlacementMode anchorPlacementMode = AnchorPlacementMode.CloudAnchors;

        /// <summary>
        /// Gets a value indicating whether the Origin of the new World Coordinate System,
        /// i.e. the Cloud Anchor was placed.
        /// </summary>
        public bool IsOriginPlaced
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the world origin which will be used as the transform parent for network
        /// spawned objects.
        /// </summary>
        public Transform WorldOrigin
        {
            get
            {
                return m_WorldOrigin;
            }

            set
            {
                IsOriginPlaced = true;
                m_WorldOrigin = value;

                Pose sessionPose = _ToWorldOriginPose(new Pose(SessionOrigin.transform.position,
                    SessionOrigin.transform.rotation));
                SessionOrigin.transform.SetPositionAndRotation(
                    sessionPose.position, sessionPose.rotation);
            }
        }

        private void Awake()
        {
            instance = this;
        }

        /// <summary>
        /// The Unity Start() method.
        /// </summary>
        public void Start()
        {
#pragma warning disable 618
            m_NetworkManager = NetworkUIController.GetComponent<CloudAnchorsNetworkManager>();
#pragma warning restore 618
            m_NetworkManager.OnClientConnected += _OnConnectedToServer;
            m_NetworkManager.OnClientDisconnected += _OnDisconnectedFromServer;

            // A Name is provided to the Game Object so it can be found by other Scripts
            // instantiated as prefabs in the scene.
            gameObject.name = "CloudAnchorsExampleController";
            _ResetStatus();
        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
#if !UNITY_EDITOR
            _UpdateApplicationLifecycle();

            if (sessionManager.GetState() != SessionManagerDiceGameMP.SessionState.Playing)
                return;

            // If we are neither in hosting nor resolving mode then the update is complete.
            if (m_CurrentMode != ApplicationMode.Hosting &&
                m_CurrentMode != ApplicationMode.Resolving)
            {
                return;
            }

            if (anchorPlacementMode != AnchorPlacementMode.CloudAnchors)
                return;

            // Give AR session some time to prepare for resolving and update the UI message
            // once the preparation time passed.
            if (m_CurrentMode == ApplicationMode.Resolving && !m_PassedResolvingPreparedTime)
            {
                m_TimeSinceStart += Time.deltaTime;

                if (m_TimeSinceStart > k_ResolvingPrepareTime)
                {
                    m_PassedResolvingPreparedTime = true;
                    NetworkUIController.ShowDebugMessage(
                        "Waiting for Cloud Anchor to be hosted...");
                }
            }

            // If the origin anchor has not been placed yet, then update in resolving mode is
            // complete.
            if (m_CurrentMode == ApplicationMode.Resolving && !IsOriginPlaced)
            {
                return;
            }

            // If the player has not touched the screen then the update is complete.
            Touch touch;
            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }

            // Ignore the touch if it's pointing on UI objects.
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                return;
            }

            List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
            RaycastManager.Raycast(Input.GetTouch(0).position, hitResults);

            // If there was an anchor placed, then instantiate the corresponding object.
            if (hitResults.Count > 0)
            {
                if (!IsOriginPlaced && m_CurrentMode == ApplicationMode.Hosting)
                {
                    ARReferencePoint referencePoint =
                        ReferencePointManager.AddReferencePoint(hitResults[0].pose);
                    WorldOrigin = referencePoint.transform;
                    _InstantiateAnchor(referencePoint);
                    OnAnchorInstantiated(true);
                }
            }
#endif
        }

        /// <summary>
        /// Indicates whether the resolving prepare time has passed so the AnchorController
        /// can start to resolve the anchor.
        /// </summary>
        /// <returns><c>true</c>, if resolving prepare time passed, otherwise returns <c>false</c>.
        /// </returns>
        public bool IsResolvingPrepareTimePassed()
        {
            return m_CurrentMode != ApplicationMode.Ready &&
                m_TimeSinceStart > k_ResolvingPrepareTime;
        }

        /// <summary>
        /// Callback called when the resolving timeout is passed.
        /// </summary>
        public void OnResolvingTimeoutPassed()
        {
            if (m_CurrentMode == ApplicationMode.Ready)
            {
                Debug.LogWarning("OnResolvingTimeoutPassed shouldn't be called" +
                    "when the application is in ready mode.");
                return;
            }

            NetworkUIController.ShowDebugMessage("Still resolving the anchor." +
                "Please make sure you're looking at where the Cloud Anchor was hosted." +
                "Or, try to re-join the room.");
        }

        /// <summary>
        /// Handles user intent to enter a mode where they can place an anchor to host or to exit
        /// this mode if already in it.
        /// </summary>
        public void OnEnterHostingMode()
        {
            if (m_CurrentMode == ApplicationMode.Hosting)
            {
                m_CurrentMode = ApplicationMode.Ready;
                _ResetStatus();
                Debug.Log("Reset ApplicationMode from Hosting to Ready.");
            }
#if !UNITY_EDITOR
            if(anchorPlacementMode ==AnchorPlacementMode.CloudAnchors)
                SessionOrigin.GetComponent<ARPlaneManager>().enabled = true;
            else if(anchorPlacementMode == AnchorPlacementMode.Marker)
                SessionOrigin.GetComponent<TrackedImageInfoManager>().enabled = true;

            sessionManager.ConfirmSession();
#else
            //LocalPlayerController.localPlayer.SpawnAnchor(Vector3.zero);
            StartCoroutine(SpawnAnchor());


#endif
            m_CurrentMode = ApplicationMode.Hosting;            
            LaunchDice.instance.InitializeHost();
        }

#if UNITY_EDITOR
        IEnumerator SpawnAnchor()
        {
            while (LocalPlayerController.localPlayer == null)
                yield return null;
            if(anchorPlacementMode == AnchorPlacementMode.CloudAnchors)
                LocalPlayerController.localPlayer.SpawnAnchor(Vector3.zero);
            else
                Instantiate(anchorPrefab, Vector3.zero, Quaternion.identity);
        }
#endif

        /// <summary>
        /// Handles a user intent to enter a mode where they can input an anchor to be resolved or
        /// exit this mode if already in it.
        /// </summary>
        public void OnEnterResolvingMode()
        {
            if (m_CurrentMode == ApplicationMode.Resolving)
            {
                m_CurrentMode = ApplicationMode.Ready;
                _ResetStatus();
                Debug.Log("Reset ApplicationMode from Resolving to Ready.");
            }

#if !UNITY_EDITOR
            if(anchorPlacementMode ==AnchorPlacementMode.CloudAnchors){
                var planeManager = SessionOrigin.GetComponent<ARPlaneManager>();
                planeManager.planePrefab = null;
                planeManager.enabled = true;
            }
            else if(anchorPlacementMode == AnchorPlacementMode.Marker)
                SessionOrigin.GetComponent<TrackedImageInfoManager>().enabled = true;

            sessionManager.ConfirmSession();
#else
            StartCoroutine(SpawnAnchor());
#endif


            m_CurrentMode = ApplicationMode.Resolving;            
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was instantiated and the host request was
        /// made.
        /// </summary>
        /// <param name="isHost">Indicates whether this player is the host.</param>
        public void OnAnchorInstantiated(bool isHost)
        {
            if (m_AnchorAlreadyInstantiated)
            {
                return;
            }

            m_AnchorAlreadyInstantiated = true;
            NetworkUIController.OnAnchorInstantiated(isHost);

#if !UNITY_EDITOR
            SessionOrigin.GetComponent<ARPlaneManager>().planePrefab = null;
#endif
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was hosted.
        /// </summary>
        /// <param name="success">If set to <c>true</c> indicates the Cloud Anchor was hosted
        /// successfully.</param>
        /// <param name="response">The response string received.</param>
        public void OnAnchorHosted(bool success, string response)
        {
            m_AnchorFinishedHosting = success;
            NetworkUIController.OnAnchorHosted(success, response);
            //SessionOrigin.GetComponent<ARPlaneManager>().planePrefab = null;
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was resolved.
        /// </summary>
        /// <param name="success">If set to <c>true</c> indicates the Cloud Anchor was resolved
        /// successfully.</param>
        /// <param name="response">The response string received.</param>
        public void OnAnchorResolved(bool success, string response)
        {
            NetworkUIController.OnAnchorResolved(success, response);
            if(success)
                LocalPlayerController.localPlayer.CmdAnchorResolved();
            //LaunchDice.instance.WaitForDice();
        }


        public void OnClientResolvedAnchor()
        {
            numClientReady++;
            Debug.Log(numClientReady);
            if(numClientReady == m_NetworkManager.matchSize)
            {
                LaunchDice.instance.PlayerReadyForLaunch();
            }
        }

        public bool IsHost()
        {
            return m_CurrentMode == ApplicationMode.Hosting;
        }

        /// <summary>
        /// Callback that happens when the client successfully connected to the server.
        /// </summary>
        private void _OnConnectedToServer()
        {
            if (m_CurrentMode == ApplicationMode.Hosting)
            {
                if(anchorPlacementMode == AnchorPlacementMode.CloudAnchors)
                    NetworkUIController.ShowDebugMessage(
                        "Find a plane, tap to create a Cloud Anchor.");
                else
                    NetworkUIController.ShowDebugMessage(
                            "Inquadra il marker Tavolo");
            }
            else if (m_CurrentMode == ApplicationMode.Resolving)
            {
                if (anchorPlacementMode == AnchorPlacementMode.CloudAnchors)
                    NetworkUIController.ShowDebugMessage(
                    "Look at the same scene as the hosting phone.");
                else
                    NetworkUIController.ShowDebugMessage(
                            "Inquadra il marker Tavolo");
            }
            else
            {
                _ReturnToLobbyWithReason(
                    "Connected to server with neither Hosting nor Resolving" +
                    "mode. Please start the app again.");
            }
        }

        /// <summary>
        /// Callback that happens when the client disconnected from the server.
        /// </summary>
        private void _OnDisconnectedFromServer()
        {
            _ReturnToLobbyWithReason("Network session disconnected! " +
                "Please start the app again and try another room.");
        }

        private Pose _ToWorldOriginPose(Pose pose)
        {
            if (!IsOriginPlaced)
            {
                return pose;
            }

            Matrix4x4 anchorTWorld = Matrix4x4.TRS(
                m_WorldOrigin.position, m_WorldOrigin.rotation, Vector3.one).inverse;
            Quaternion rotation = Quaternion.LookRotation(
                anchorTWorld.GetColumn(2),
                anchorTWorld.GetColumn(1));
            return new Pose(
                anchorTWorld.MultiplyPoint(pose.position),
                pose.rotation * rotation);
        }

        /// <summary>
        /// Instantiates the anchor object at the pose of the m_WorldOriginReferencePoint Anchor. 
        /// This will host the Cloud Anchor.
        /// </summary>
        /// <param name="referencePoint">The reference point holding the anchor.</param>
        private void _InstantiateAnchor(ARReferencePoint referencePoint)
        {
            // The anchor will be spawned by the host, so no networking Command is needed.
            LocalPlayerController.localPlayer.SpawnAnchor(referencePoint);
        }


        /// <summary>
        /// Resets the internal status.
        /// </summary>
        private void _ResetStatus()
        {
            // Reset internal status.
            m_CurrentMode = ApplicationMode.Ready;
            m_WorldOrigin = null;
            IsOriginPlaced = false;
        }

        /// <summary>
        /// Check and update the application lifecycle.
        /// </summary>
        private void _UpdateApplicationLifecycle()
        {
            // Exit the app when the 'back' button is pressed.
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            var sleepTimeout = SleepTimeout.NeverSleep;
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                sleepTimeout = SleepTimeout.SystemSetting;
            }

            Screen.sleepTimeout = sleepTimeout;

            if (m_IsQuitting)
            {
                return;
            }

            if (ARSession.state == ARSessionState.Unsupported)
            {
                _QuitWithReason("AR Experience is unsupported on this devices.");
            }
        }

        /// <summary>
        /// Quits the application after 5 seconds for the toast to appear.
        /// </summary>
        /// <param name="reason">The reason of quitting the application.</param>
        private void _QuitWithReason(string reason)
        {
            if (m_IsQuitting)
            {
                return;
            }

            NetworkUIController.ShowDebugMessage(reason);
            m_IsQuitting = true;
            Invoke("_DoQuit", 5.0f);
        }

        /// <summary>
        /// Returns to lobby after 3 seconds for the reason message to appear.
        /// </summary>
        /// <param name="reason">The reason of returning to lobby.</param>
        private void _ReturnToLobbyWithReason(string reason)
        {
            // No need to return if the application is currently quitting.
            if (m_IsQuitting)
            {
                return;
            }

            NetworkUIController.ShowDebugMessage(reason);
            Invoke("_DoReturnToLobby", 3.0f);
        }

        /// <summary>
        /// Actually quit the application.
        /// </summary>
        private void _DoQuit()
        {
            Application.Quit();
        }

        /// <summary>
        /// Actually return to lobby scene.
        /// </summary>
        private void _DoReturnToLobby()
        {
#pragma warning disable 618
            NetworkManager.Shutdown();
#pragma warning restore 618
            SceneManager.LoadScene("CloudAnchors");
        }

        public NetworkManager GetNetworkManager()
        {
            return m_NetworkManager;
        }
    }
}
