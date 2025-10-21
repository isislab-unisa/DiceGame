//-----------------------------------------------------------------------
// <copyright file="LocalPlayerController.cs" company="Google">
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

using System.Linq;
using System.Net.NetworkInformation;

namespace Google.XR.ARCoreExtensions.Samples.CloudAnchors
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.XR.ARFoundation;

    /// <summary>
    /// Local player controller. Handles the spawning of the networked Game Objects.
    /// </summary>
#pragma warning disable 618
    public class LocalPlayerController : NetworkBehaviour
#pragma warning restore 618
    {

        public static LocalPlayerController localPlayer;

        /// <summary>
        /// The Unity OnStartLocalPlayer() method.
        /// </summary>
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            localPlayer = this;
            /*transform.position = Camera.main.transform.position;
            transform.rotation = Camera.main.transform.rotation;
            transform.SetParent(Camera.main.transform);*/
            // A Name is provided to the Game Object so it can be found by other Scripts, since this
            // is instantiated as a prefab in the scene.
            gameObject.name = "LocalPlayer";
        }

        /// <summary>
        /// Will spawn the origin anchor and host the Cloud Anchor. Must be called by the host.
        /// </summary>
        /// <param name="referencePoint">The AR Reference Point to be hosted.</param>
        public void SpawnAnchor(ARReferencePoint referencePoint)
        {
            // Instantiate Anchor model at the hit pose.
            var anchorObject = Instantiate(CloudAnchorsController.instance.anchorPrefab, Vector3.zero, Quaternion.identity);

            // Anchor must be hosted in the device.
            anchorObject.GetComponent<AnchorController>().HostReferencePoint(referencePoint);

            // Host can spawn directly without using a Command because the server is running in this
            // instance.
#pragma warning disable 618
            NetworkServer.Spawn(anchorObject);
#pragma warning restore 618
        }

#if UNITY_EDITOR
        public void SpawnAnchor(Vector3 position)
        {
            var anchorObject = Instantiate(CloudAnchorsController.instance.anchorPrefab, position, Quaternion.identity);
            NetworkServer.Spawn(anchorObject);
            /*var table = Instantiate(TablePrefab, position, Quaternion.identity);
            NetworkServer.Spawn(table);*/
        }
#endif

        [Command]
        public void CmdAnchorResolved()
        {
            CloudAnchorsController.instance.OnClientResolvedAnchor();
        }

        [Command]
        public void CmdSpawnTurricola()
        {
            Debug.Log("Spawning dice");
            var spawnPos = CloudAnchorsController.instance.Anchor.transform.GetChild(0);
            var turricola = Instantiate(LaunchDice.instance.turricolaPrefab, spawnPos.position, Quaternion.identity);
            NetworkServer.Spawn(turricola);
            LaunchDice.instance.Turricola = turricola.GetComponent<TurricolaController>();
            //LaunchDice.instance.Turricola.SpawnDices();
            //LaunchDice.instance.Turricola = turricolaController;
        }

        [Command]
        public void CmdSpawnDice(GameObject dice)
        {
            NetworkServer.Spawn(dice);
        }

        [Command]
        public void CmdShakeDices()
        {
            LaunchDice.instance.ShakeDices();
            LaunchDice.instance.SetCurrentPlayer(netId);
        }

        [Command]
        public void CmdLaunchDices()
        {
            LaunchDice.instance.LaunchDices();
        }

        [Command]
        public void CmdDestroyTurricolaAndDices()
        {
            LaunchDice.instance.DestroyTurricolaAndDices();
        }

        [ClientRpc]
        public void RpcActivateDices()
        {
            LaunchDice.instance.ActivateDices();
        }

        [Command]
        public void CmdDicesActivated()
        {
            LaunchDice.instance.PlayerActivatedDices();
        }

        [ClientRpc]
        public void RpcDiceValue(int[] diceValues)
        {
            LaunchDice.instance.DiceValueReceived(diceValues);
        }

        [ClientRpc]
        public void RpcWinners(NetworkInstanceId[] winners, int value)
        {
            var isWinner = false;
            NetworkInstanceId[] realWinners = winners
                .Where(w => winners.Count(wi => wi == w) > Mathf.FloorToInt(winners.Length / 2)).ToArray();
            foreach(var winner in realWinners)
                if (winner.Equals(localPlayer.netId))
                {
                    isWinner = true;
                    break;
                }

            LaunchDice.instance.FoundWinner(isWinner, value, realWinners.Length > 1);
        }
        
        [ClientRpc]
        public void RpcRoundWinners(NetworkInstanceId[] winners, int roundNo)
        {
            var isWinner = false;
            foreach(var winner in winners)
                if (winner.Equals(localPlayer.netId))
                {
                    isWinner = true;
                    break;
                }

            LaunchDice.instance.FoundRoundWinner(isWinner, roundNo, winners.Length > 1);
        }

    }
}
