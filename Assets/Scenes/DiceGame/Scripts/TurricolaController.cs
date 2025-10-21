using Google.XR.ARCoreExtensions.Samples.CloudAnchors;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TurricolaController : MonoBehaviour
{
    public GameObject top;

    private List<DiceController> dices;
    public List<DiceController> Dices { get { if (dices == null) dices = new List<DiceController>(); return dices; } private set { dices = value; } }

    private DiceSpawner[] diceSpawners;
    private int nextSpawn = 0;

    // Start is called before the first frame update
    void Start()
    {
#if !UNITY_EDITOR
        CloudAnchorsController.instance.SessionOrigin.MakeContentAppearAt(transform, CloudAnchorsController.instance.Anchor.transform.GetChild(0).position);
#endif
        diceSpawners = transform.GetComponentsInChildren<DiceSpawner>();

        if (CloudAnchorsController.instance.IsHost())
            Debug.Log("host"); //SpawnDices();
        else
            LaunchDice.instance.Turricola = this;
    }

    public void SpawnDices()
    {
        Dices = new List<DiceController>();
        foreach (var spawn in diceSpawners)
            Dices.Add(spawn.SpawnDice());
    }

    public void AddDice(DiceController dice)
    {
        Dices.Add(dice);
        if (Dices.Count >= diceSpawners.Length)
            LocalPlayerController.localPlayer.CmdDicesActivated();
    }

    public void DisableDicesRigidBody()
    {
        foreach(var dice in Dices)
            dice.GetComponent<Rigidbody>().isKinematic = true;
    }

    public Vector3 GetNextSpawnPosition()
    {
        return diceSpawners[nextSpawn++].transform.position;
    }

    public int GetNumDices()
    {
        return Dices.Count;
    }
}
