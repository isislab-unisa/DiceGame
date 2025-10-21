//-----------------------------------------------------------------------
// <copyright file="AnchorController.cs" company="Google">
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
    using Google.XR.ARCoreExtensions;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.XR.ARFoundation;

    /// <summary>
    /// A Controller for the Anchor object that handles hosting and resolving the
    /// <see cref="ARCloudReferencePoint"/>.
    /// </summary>
#pragma warning disable 618
    public class AnchorController : NetworkBehaviour
#pragma warning restore 618
    {
        public GameObject tableMesh;

        /// <summary>
        /// The customized timeout duration for resolving request to prevent retrying to resolve
        /// indefinitely.
        /// </summary>
        private const float k_ResolvingTimeout = 10.0f;

        /// <summary>
        /// The Cloud Reference ID for the hosted anchor's <see cref="ARCloudReferencePoint"/>.
        /// This variable will be synchronized over all clients.
        /// </summary>
#pragma warning disable 618
        [SyncVar(hook = "_OnChangeId")]
#pragma warning restore 618
        private string m_CloudReferenceId = string.Empty;

        /// <summary>
        /// Indicates whether this script is running in the Host.
        /// </summary>
        private bool m_IsHost = false;

        /// <summary>
        /// Indicates whether an attempt to resolve the Cloud Reference Point should be made.
        /// </summary>
        private bool m_ShouldResolve = false;

        /// <summary>
        /// Indicates whether to chekc Cloud Reference Point state and update the anchor.
        /// </summary>
        private bool m_ShouldUpdatePoint = false;

        /// <summary>
        /// Record the time since resolving started. If the timeout has passed, display
        /// additional instructions.
        /// </summary>
        private float m_TimeSinceStartResolving = 0.0f;

        /// <summary>
        /// Indicates whether passes the resolving timeout duration or the anchor has been
        /// successfully resolved.
        /// </summary>
        private bool m_PassedResolvingTimeout = false;


        /// <summary>
        /// The Cloud Reference Point created locally which is used to moniter whether the
        /// hosting or resolving process finished.
        /// </summary>
        private ARCloudReferencePoint m_CloudReferencePoint;

        /// <summary>
        /// The Cloud Anchors example controller.
        /// </summary>
        private CloudAnchorsController m_CloudAnchorsController;

        /// <summary>
        /// The AR Reference Point Manager in the scene, used to host or resolve a Cloud Reference
        /// Point.
        /// </summary>
        private ARReferencePointManager m_ReferencePointManager;

        /// <summary>
        /// The Unity Awake() method.
        /// </summary>
        public void Awake()
        {
#if UNITY_EDITOR
            tableMesh.SetActive(true);
#endif
            Debug.Log("Anchor awake");
            m_CloudAnchorsController = CloudAnchorsController.instance;
            m_ReferencePointManager = m_CloudAnchorsController.ReferencePointManager;
        }

        /// <summary>
        /// The Unity OnStartClient() method.
        /// </summary>
        public override void OnStartClient()
        {
            if (m_CloudReferenceId != string.Empty)
            {
                m_ShouldResolve = true;
            }
            CloudAnchorsController.instance.Anchor = gameObject;
#if UNITY_EDITOR
            CmdSetCloudReferenceId("ua-e74042d2d5a32df2cdeada6b0f77826d");
#endif
        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
#if !UNITY_EDITOR
            if (m_IsHost)
            {
                if (m_ShouldUpdatePoint)
                {
                    _UpdateHostedCloudReferencePoint();
                }
            }
            else
            {
                if (m_ShouldResolve)
                {
                    if (!m_CloudAnchorsController.IsResolvingPrepareTimePassed())
                    {
                        return;
                    }

                    if (!m_PassedResolvingTimeout)
                    {
                        m_TimeSinceStartResolving += Time.deltaTime;

                        if (m_TimeSinceStartResolving > k_ResolvingTimeout)
                        {
                            m_PassedResolvingTimeout = true;
                            m_CloudAnchorsController.OnResolvingTimeoutPassed();
                        }
                    }

                    if (!string.IsNullOrEmpty(m_CloudReferenceId) && m_CloudReferencePoint == null)
                    {
                        _ResolveReferencePointId(m_CloudReferenceId);
                    }
                }

                if (m_ShouldUpdatePoint)
                {
                    _UpdateResolvedCloudReferencePoint();
                }
            }
#endif
        }

        /// <summary>
        /// Command run on the server to set the Cloud Reference Id.
        /// </summary>
        /// <param name="cloudReferenceId">The new Cloud Reference Id.</param>
#pragma warning disable 618
        [Command]
#pragma warning restore 618
        public void CmdSetCloudReferenceId(string cloudReferenceId)
        {
            Debug.Log("Update cloud reference id with: " + cloudReferenceId);
            m_CloudReferenceId = cloudReferenceId;
        }

        /// <summary>
        /// Hosts the user placed cloud anchor and associates the resulting Id with this object.
        /// </summary>
        /// <param name="referencePoint">The last placed anchor.</param>
        public void HostReferencePoint(ARReferencePoint referencePoint)
        {
            m_IsHost = true;
            m_ShouldResolve = false;
            transform.SetParent(referencePoint.transform);
            
            //GetComponent<TableManager>().ActivateCollider();
            tableMesh.SetActive(true);

            m_CloudReferencePoint =
                ARReferencePointManagerExtensions.AddCloudReferencePoint(
                    m_ReferencePointManager, referencePoint);
            if (m_CloudReferencePoint == null)
            {
                Debug.LogError("Failed to add Cloud Reference Point.");
                m_CloudAnchorsController.OnAnchorHosted(
                    false, "Failed to add Cloud Reference Point.");
                m_ShouldUpdatePoint = false;
            }
            else
            {
                m_ShouldUpdatePoint = true;
            }
        }

        /// <summary>
        /// Resolves the cloud reference Id and instantiate a Cloud Reference Point on it.
        /// </summary>
        /// <param name="cloudReferenceId">The cloud reference Id to be resolved.</param>
        private void _ResolveReferencePointId(string cloudReferenceId)
        {
            m_CloudAnchorsController.OnAnchorInstantiated(false);
            m_CloudReferencePoint =
                ARReferencePointManagerExtensions.ResolveCloudReferenceId(
                    m_ReferencePointManager, cloudReferenceId);
            if (m_CloudReferencePoint == null)
            {
                Debug.LogErrorFormat("Client could not resolve Cloud Reference Point {0}.",
                    cloudReferenceId);
                m_CloudAnchorsController.OnAnchorResolved(
                    false, "Client could not resolve Cloud Reference Point.");
                m_ShouldResolve = true;
                m_ShouldUpdatePoint = false;
            }
            else
            {
                m_ShouldResolve = false;
                m_ShouldUpdatePoint = true;
            }
        }

        /// <summary>
        /// Update the anchor if hosting Cloud Reference Point is success.
        /// </summary>
        private void _UpdateHostedCloudReferencePoint()
        {
            if (m_CloudReferencePoint == null)
            {
                Debug.LogError("No Cloud Reference Point.");
                return;
            }

            CloudReferenceState cloudReferenceState =
                m_CloudReferencePoint.cloudReferenceState;
            if (cloudReferenceState == CloudReferenceState.Success)
            {
                CmdSetCloudReferenceId(m_CloudReferencePoint.cloudReferenceId);
                m_CloudAnchorsController.OnAnchorHosted(
                    true, "Successfully hosted Cloud Reference Point.");
                m_ShouldUpdatePoint = false;
            }
            else if (cloudReferenceState != CloudReferenceState.TaskInProgress)
            {
                m_CloudAnchorsController.OnAnchorHosted(false,
                    "Fail to host Cloud Reference Point with state: " + cloudReferenceState);
                m_ShouldUpdatePoint = false;
            }
        }

        /// <summary>
        /// Update the anchor if resolving Cloud Reference Point is success.
        /// </summary>
        private void _UpdateResolvedCloudReferencePoint()
        {
            if (m_CloudReferencePoint == null)
            {
                Debug.LogError("No Cloud Reference Point.");
                return;
            }

            CloudReferenceState cloudReferenceState =
                m_CloudReferencePoint.cloudReferenceState;
            if (cloudReferenceState == CloudReferenceState.Success)
            {
                transform.SetParent(m_CloudReferencePoint.transform, false);
                tableMesh.SetActive(true);
                m_CloudAnchorsController.OnAnchorResolved(
                    true,
                    "Successfully resolved Cloud Reference Point.");
                m_CloudAnchorsController.WorldOrigin = transform;

                // Mark resolving timeout passed so it won't fire OnResolvingTimeoutPassed event.
                m_PassedResolvingTimeout = true;
                m_ShouldUpdatePoint = false;
            }
            else if (cloudReferenceState != CloudReferenceState.TaskInProgress)
            {
                m_CloudAnchorsController.OnAnchorResolved(
                    false,
                    "Fail to resolve Cloud Reference Point with state: " + cloudReferenceState);
                m_ShouldUpdatePoint = false;
            }
        }

        /// <summary>
        /// Callback invoked once the Cloud Reference Id changes.
        /// </summary>
        /// <param name="newId">New Cloud Reference Id.</param>
        private void _OnChangeId(string newId)
        {
#if !UNITY_EDITOR
            if (!m_IsHost && newId != string.Empty)
            {
                m_CloudReferenceId = newId;
                m_ShouldResolve = true;
                m_CloudReferencePoint = null;
            }
#endif
        }
    }
}
