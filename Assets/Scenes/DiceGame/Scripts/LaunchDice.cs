using Google.XR.ARCoreExtensions.Samples.CloudAnchors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LaunchDice : MonoBehaviour
{
    private string debugString = "0";
        
    public int numberOfTurns;
    public GameObject turricolaPrefab;
    public GameObject dicePrefab;
    public Button button;
    public GameObject sessionOrigin;

    public ShowLaunchResult resultDisplay;

    public float shakeSpeed, shakeAmount;

    public TurricolaController Turricola { get; set; }

    public static LaunchDice instance;

    struct PlayerValue
    {
        public NetworkInstanceId player;
        public int value;
        public int turn;
    }

    private List<PlayerValue> playersValue;
    private NetworkInstanceId currentPlayer;

    private int localDiceValue = 0, numPlayersActiveDices = 0;
    private List<int> diceValues;

    public enum State
    {
        Idle,
        SearchingDices,
        PreparingLaunch,
        ShakingDices,
        LaunchingDices,
        DiceLaunched
    }

    public State localState;

    private State hostState;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        localState = State.Idle;        
    }

    public void InitializeHost()
    {
        playersValue = new List<PlayerValue>();
        diceValues = new List<int>();
        hostState = State.Idle;
    }


    // Update is called once per frame
    void Update()
    {
        
        if (localState == State.SearchingDices) {
#if !UNITY_EDITOR
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began){
                var raycast = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
                int layerMask = LayerMask.GetMask("Turricola");
                if (Physics.Raycast(raycast, out RaycastHit hit, layerMask)) {
                    LocalPlayerController.localPlayer.CmdDestroyTurricolaAndDices();
                    PlayerReadyForLaunch();
                }
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("Pressed: SearchingDices");
                var raycast = Camera.main.ScreenPointToRay(Input.mousePosition);
                int layerMask = LayerMask.GetMask("Turricola");
                if (Physics.Raycast(raycast, out RaycastHit hit, layerMask))
                {
                    LocalPlayerController.localPlayer.CmdDestroyTurricolaAndDices();
                    PlayerReadyForLaunch();
                }
            }
#endif
        }

        else if (localState == State.PreparingLaunch)
        {
#if !UNITY_EDITOR
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                localState = State.ShakingDices;
                LocalPlayerController.localPlayer.CmdShakeDices();
            }
#else
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("Pressed: PreparingLaunch");
                localState = State.ShakingDices;
                LocalPlayerController.localPlayer.CmdShakeDices();
            }
#endif
        }

        else if (localState == State.ShakingDices)
        {
#if !UNITY_EDITOR
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended){
                localState = State.LaunchingDices;
                LocalPlayerController.localPlayer.CmdLaunchDices();
            }
#else
            if (Input.GetMouseButtonUp(0))
            {
                Debug.Log("Pressed: ShakingDices");
                localState = State.LaunchingDices;
                LocalPlayerController.localPlayer.CmdLaunchDices();
            }
#endif
        }

    }

    public void PlayerReadyForLaunch()
    {
        localState = State.PreparingLaunch;        
        button.GetComponentInChildren<Text>().text = "Pronto a lanciare";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickReady);
        button.gameObject.SetActive(true);
        MessageHandler.instance.ShowMessageWithTimeout("Premi il pulsante quando sei pronto al lancio", 5f);
    }

    private void OnClickReady()
    {
        button.gameObject.SetActive(false);
        localState = State.PreparingLaunch;
        LocalPlayerController.localPlayer.CmdSpawnTurricola();
        //Turricola.SpawnDices();
        MessageHandler.instance.ShowMessageWithTimeout("Tocca lo schermo per agitare i dadi", 5f);
    }

    public void ShakeDices()
    {
        hostState = State.ShakingDices;        
        StartCoroutine(ShakeAndLaunch());
    }

    public void ActivateDices()
    {
        foreach (var dice in Turricola.Dices)
            dice.Activate();
        LocalPlayerController.localPlayer.CmdDicesActivated();
    }

    public void PlayerActivatedDices()
    {
        numPlayersActiveDices++;
        Debug.Log("dices activated: " + numPlayersActiveDices);
        if(numPlayersActiveDices >= CloudAnchorsController.instance.GetNetworkManager().numPlayers)
        {
            hostState = State.LaunchingDices;
            numPlayersActiveDices = 0;
        }
    }

    public void LaunchDices()
    {
        hostState = State.PreparingLaunch;
    }

    IEnumerator ShakeAndLaunch()
    {
        var timer = 0f;
        var turricolaTarget = CloudAnchorsController.instance.Anchor.transform.GetChild(1).position;
        while (timer <= 1)
        {
            yield return null;
            timer += Time.deltaTime;
            Turricola.transform.position = Vector3.Lerp(Turricola.transform.position, turricolaTarget, Mathf.Clamp01(timer));
        }


        var offset = Vector3.zero;
        while (hostState == State.ShakingDices)
        {
            var newOffset = Vector3.one * Mathf.Sin(Time.time * shakeSpeed);
            Turricola.transform.Translate((newOffset - offset) * shakeAmount);

            offset = newOffset;
            yield return null;
        }

        numPlayersActiveDices = 1;
        LocalPlayerController.localPlayer.RpcActivateDices();
        // yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => hostState == State.LaunchingDices);
        timer = 0f;
        var targetDirection = CloudAnchorsController.instance.Anchor.transform.GetChild(2).position - Turricola.transform.position;
        var target = Quaternion.LookRotation(Turricola.transform.forward, targetDirection);
        bool spawned = false;

        while (timer <= 1)
        {
            yield return null;
            timer += Time.deltaTime;
            Turricola.transform.rotation = Quaternion.Slerp(Turricola.transform.rotation, target, Mathf.Clamp01(timer));
            if (!spawned && timer >= 0.12f)
            {
                spawned = true;
                Turricola.SpawnDices();
            }
        }
        
        yield return new WaitForSeconds(0.1f);

        foreach (var dice in Turricola.Dices)
            dice.DiceLaunched();

        yield return new WaitForSeconds(2f);
        NetworkServer.Destroy(Turricola.gameObject);
    }

    public void SetCurrentPlayer(NetworkInstanceId player)
    {
        currentPlayer = player;
    }

    public void DiceValueReceived(int[] values)
    {
        Turricola.DisableDicesRigidBody();
        resultDisplay.ShowResult(localState == State.LaunchingDices, values);
        if (localState == State.LaunchingDices)
        {
            int total = 0;
            foreach (var value in values)
                total += value;
            localDiceValue = total;
            localState = State.DiceLaunched;
        }
        else
        {
            //if (localState != State.DiceLaunched)
                WaitForDice();
        }
    }

    public void WaitForDice()
    {
        localState = State.SearchingDices;
    }

    public void DiceValue(int newDiceValue)
    {
        debugString = "1";
        diceValues.Add(newDiceValue);
        if (diceValues.Count >= Turricola.GetNumDices())
        {
            int totalValue = 0;
            foreach (var value in diceValues)
                totalValue += value;
            playersValue.Add(new PlayerValue { player = currentPlayer, value = totalValue, turn = Mathf.FloorToInt(playersValue.Count / 2) });
            LocalPlayerController.localPlayer.RpcDiceValue(diceValues.ToArray());
            diceValues.Clear();
            if (playersValue.Count % CloudAnchorsController.instance.GetNetworkManager().numPlayers == 0)
                StartCoroutine(EndRound());

        }
    }

    private IEnumerator EndRound()
    {
        debugString = "2";
        yield return new WaitForSeconds(3f);
        debugString = "3";
        int maxValue = -1;
        var winners = new List<NetworkInstanceId>();

        for (int curValue = playersValue.Count - 1;
            curValue >= playersValue.Count - CloudAnchorsController.instance.GetNetworkManager().numPlayers;
            --curValue)
        {
            if (playersValue[curValue].value >= maxValue)
            {
                if (playersValue[curValue].value > maxValue)
                {
                    maxValue = playersValue[curValue].value;
                    winners.Clear();
                }
                winners.Add(playersValue[curValue].player);
            }
        }
        debugString = "4";
        int roundNo =
            Mathf.FloorToInt(playersValue.Count / CloudAnchorsController.instance.GetNetworkManager().numPlayers);
        LocalPlayerController.localPlayer.RpcRoundWinners(winners.ToArray(), roundNo);
        if (playersValue.Count >= CloudAnchorsController.instance.GetNetworkManager().numPlayers * numberOfTurns)
        {
            debugString = "5";
            StartCoroutine(FindWinner());
        }

    }

    private IEnumerator FindWinner()
    {
        yield return new WaitForSeconds(4f);
        var maxValue = -1;
        var winners = new List<NetworkInstanceId>();
        for (int curTurn = 0; curTurn < numberOfTurns; ++curTurn)
        {
            maxValue = -1;
            List<PlayerValue> curTurnValues = playersValue.Where(v => v.turn == curTurn).ToList();
            foreach (var playerValue in curTurnValues)
            {
                if (playerValue.value >= maxValue)
                {
                    if (playerValue.value > maxValue)
                    {
                        maxValue = playerValue.value;
                        winners.Clear();
                    }

                    winners.Add(playerValue.player);
                }
            }
        }

        LocalPlayerController.localPlayer.RpcWinners(winners.ToArray(), maxValue);
    }

    public void DestroyTurricolaAndDices()
    {
        foreach (var dice in Turricola.Dices)
            NetworkServer.Destroy(dice.gameObject);
    }

    public void FoundWinner(bool winner, int winnerValue, bool draw)
    {
        var result = "lose";
        string message;
        if (winner) {
            if (draw)
            {
                message = "Pareggio!";
                result = "draw";
            }
            else
            {
                message = "Hai vinto!";
                result = "win";
            }
        }
        else
            message = "Hai perso!";

        resultDisplay.ShowFinalResult(message);

#if !UNITY_EDITOR
        button.GetComponentInChildren<Text>().text = "Termina Sessione";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => { CloudAnchorsController.instance.sessionManager.EndSession(result, localDiceValue); button.gameObject.SetActive(false); });
        button.gameObject.SetActive(true);
#endif
    }
    
    public void FoundRoundWinner(bool winner, int roundNo, bool draw)
    {
        var result = "lose";
        string message;
        if (winner) {
            if (draw)
            {
                message = "Pareggio!";
                result = "draw";
            }
            else
            {
                message = "Hai vinto la manche " + roundNo;
                result = "win";
            }
        }
        else
            message = "Hai perso la manche " + roundNo;

        resultDisplay.ShowRoundResult(message);

    }

    public string GetDiceValue()
    {
        return localDiceValue + "";
    }

    /*private void OnGUI()
    {
        GUI.Label(new Rect(10f, 10f, 200f,40f),  "Local state: " + localState);
        GUI.Label(new Rect(10f, 40f, 200f,40f),  "Host state: " + hostState);
        GUI.Label(new Rect(10f, 70f, 200f,40f),  debugString);
    }*/
}
