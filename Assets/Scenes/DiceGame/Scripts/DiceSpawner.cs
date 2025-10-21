using Google.XR.ARCoreExtensions.Samples.CloudAnchors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DiceSpawner : MonoBehaviour
{
    public DiceController SpawnDice()
    {
        var dice = Instantiate(LaunchDice.instance.dicePrefab, transform.position, Random.rotation);
        LocalPlayerController.localPlayer.CmdSpawnDice(dice);
        //NetworkServer.Spawn(dice);
        dice.GetComponent<Rigidbody>().isKinematic = false;
        return dice.GetComponent<DiceController>();
    }
}
