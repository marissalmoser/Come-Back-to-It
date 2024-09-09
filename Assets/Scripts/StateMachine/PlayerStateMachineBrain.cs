/******************************************************************
*    Author: Elijah Vroman
*    Contributors: 
*    Date Created: 9/02/24
*    Description: After many, many, iterations, this is the state 
*    machine i decided upon. The process is as follows:
*    1. GameManager or Obstacle tiles determine player movement is 
*       needed, if player played a card or stepped on a move tile
*    2. Using the HandleIncomingActions method, this script collects 
*       a list of actions in StartCardActions
*    3. Enters FSM at Prepare next action, which gets the first card
*       in the list
*    4. Goes to FindTileUponAction, which sets targetTile and distance
*    5. Plays a coroutine from PlayerController in PlayResult state
*    6. Gets another card in PrepareNextAction state and repeats, or
*       goes to waiting for actions state.
*       
*       IMPORTANT: AS OF 9/9/24, THERE IS NO HANDLER FOR HITTING 
*       OBSTACLES OR FALLING INTO HOLES/OFF MAP. WIP. 
*******************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachineBrain : MonoBehaviour
{

    [SerializeField] private State _currentState;
    private Coroutine _currentCoroutine;
    private Card _currentAction;
    private List<Card> _actions = new List<Card>();
    private Tile _targetTile;
    private GameObject _player;
    private PlayerController _pC;
    private int _distance;

    public void Start()
    {
        PlayerController.ReachedDestination += HandleReachedDestination;
        GameManager.PlayActionOrder += HandleIncomingActions;
        TileManager.Instance.LoadTileList();
        FindPlayer();
        //TODO: Have something else load the tileList()
    }

    /// <summary>
    /// Good old enum finite state machine. 
    /// </summary>
    public enum State
    {
        WaitingForActions,
        FindTileUponAction,
        PlayResult,
        PrepareNextAction,
    }

    /// <summary>
    /// This is private because you should not directly influence the FSM
    /// Additionally, you cant have enums as params in animation events,
    /// and that would be the only other reason to be public
    /// </summary>
    /// <param name="stateTo"></param>
    private void FSM(State stateTo)
    {
        switch (stateTo)
        {
            case State.WaitingForActions:
                if (_currentCoroutine != null)
                {
                    StopCoroutine(_currentCoroutine);
                }
                print("Waiting for actions");
                _currentState = State.WaitingForActions;
                _currentCoroutine = StartCoroutine(WaitingForActions());
                break;

            case State.FindTileUponAction:
                if (_currentCoroutine != null)
                {
                    StopCoroutine(_currentCoroutine);
                }
                _currentState = State.FindTileUponAction;
                print("Finding target tile");
                _currentCoroutine = StartCoroutine(FindTileUponAction());
                break;

            case State.PlayResult:
                if (_currentCoroutine != null)
                {
                    StopCoroutine(_currentCoroutine);
                }
                _currentState = State.PlayResult;
                print("Playing results");
                _currentCoroutine = StartCoroutine(PlayResult());
                break;

            case State.PrepareNextAction:
                if (_currentCoroutine != null)
                {
                    StopCoroutine(_currentCoroutine);
                }
                _currentState = State.PrepareNextAction;
                print("Preparing next action");
                _currentCoroutine = StartCoroutine(PrepareNextAction());
                break;
        }
    }

    public void FindPlayer()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _pC = GetPlayerController();
            if (_pC == null)
            {
                print("NULL");
            }
            if (_player == null)
            {
                Debug.Log("No gameobject in scene tagged with player");
            }
        }
    }
    public Card GetNextAction()
    {
        if (_actions.Count >= 1)
        {
            Card actionToGive = _actions[0];
            _actions.RemoveAt(0);
            return actionToGive;
        }
        return null;
    }
    public State GetState()
    {
        return _currentState;
    }
    public PlayerController GetPlayerController()
    {
        if (_pC != null)
        {
            return _pC;
        }
        return GetPlayer().GetComponent<PlayerController>();
    }
    public GameObject GetPlayer()
    {
        if (_player != null)
        {
            return _player;
        }
        {
            FindPlayer();
            return _player;
        }
    }

    public void HandleIncomingActions(List<Card> cardList)
    {
        StartCardActions(cardList);
    }
    public void HandleObstacleInterruption()
    {
        _pC.StopFallCoroutine(); //We dont need to stop all these coroutines, but Unity doesnt care
        _pC.StopJumpCoroutine(); // and I couldnt figure out how to stop a specific coroutine from
        _pC.StopMoveCoroutine(); // another script without making methods

        //TODO : play an interruption animation
        Debug.Log("HIT AN OBSTACLE");

        _player.transform.position = _pC.GetCurrentTile().GetPlayerSnapPosition();

        FSM(State.PrepareNextAction);
    }
    public void HandleReachedDestination()
    {
        _pC.StopFallCoroutine(); //We dont need to stop all these coroutines, but Unity doesnt care
        _pC.StopJumpCoroutine(); // and I couldnt figure out how to stop a specific coroutine from
        _pC.StopMoveCoroutine(); // another script without making methods. Just to make sure the player is 
        // done moving so there is no jittery behavior

        FSM(State.PrepareNextAction);
    }
    public void SetCardList(List<Card> incomingActions)
    {
        _actions.Clear();
        _actions.AddRange(incomingActions);
    }

    /// <summary>
    /// Adds a card to the 0th index
    /// </summary>
    /// <param name="card"></param>
    public void AddCardToList(Card card)
    {
        if(card != null)
        {
            _actions.Insert(0, card);
        }
        else
        {
            Debug.LogError("Card is null");
        }
    }

    /// <summary>
    /// The method used to start a new action order from outside scripts
    /// </summary>
    /// <param name="incomingActions"></param>
    public void StartCardActions(List<Card> incomingActions)
    {
        SetCardList(incomingActions);
        FSM(State.PrepareNextAction);
    }

    private IEnumerator PrepareNextAction()
    {
        while (_currentState == State.PrepareNextAction)
        {
            _currentAction = GetNextAction();
            if (_currentAction != null)
            {
                FSM(State.FindTileUponAction);
            }
            else
            {
                FSM(State.WaitingForActions);
            }
            yield return null;
        }
    }

    private IEnumerator FindTileUponAction()
    {
        while (_currentState == State.FindTileUponAction)
        {
            var currentTile = _pC.GetCurrentTile();

            _distance = _currentAction.GetDistance(); //main focus of this state
            _targetTile = TileManager.Instance.GetTileAtLocation(currentTile, _pC.GetCurrentFacingDirection(), _distance);

            FSM(State.PlayResult);
            yield return null;
        }
    }

    private IEnumerator PlayResult()
    {
        if (_currentState == State.PlayResult)
        {
            switch (_currentAction.name)
            {
                case Card.CardName.TurnLeft:
                    _pC.TurnPlayer(true);
                    PlayerController.ReachedDestination?.Invoke();
                    //TODO: listen for wait for turn player animation event 
                    break;
                case Card.CardName.TurnRight:
                    _pC.TurnPlayer(false);
                    PlayerController.ReachedDestination?.Invoke();
                    //TODO: animation here
                    break;
                case Card.CardName.Jump:
                    if (_distance > 2) //this is a spring tile
                    {
                        _distance -= 1;
                        //uhhhhhh im counting on spring distance being three, because \/`8 = 2.8... almost 3 tiles. Code wise, i need it to be two
                        // (two up, two across) to work properly
                        int[] possibleNumbers = { 0, 2, 6, 8 };
                        int randomIndex = Random.Range(0, possibleNumbers.Length);

                        _targetTile = TileManager.Instance.GetTileAtLocation //TODO: must change from random to targetdirection or direction of targettile
                    (_pC.GetCurrentTile(), possibleNumbers[randomIndex], _distance);

                        _pC.StartJumpCoroutine(_pC.GetCurrentTile().GetPlayerSnapPosition(), _targetTile.GetPlayerSnapPosition());
                    }

                    else // this is a normal jump
                    {
                        //determine result by getting difference of elevation
                        _distance += (_pC.GetCurrentTile().GetElevation() - _targetTile.GetElevation());

                        if (_distance < 0) //block is too high
                        {
                            Vector3 newVector = (_targetTile.GetPlayerSnapPosition());
                            _pC.StartJumpCoroutine(_pC.GetCurrentTile().GetPlayerSnapPosition(), new Vector3(newVector.x, newVector.y - 1, newVector.z));
                        }
                        else
                        {
                            _pC.StartJumpCoroutine(_pC.GetCurrentTile().GetPlayerSnapPosition(), _targetTile.GetPlayerSnapPosition());
                        }
                    }
                    break;
                case Card.CardName.Move:
                    _pC.StartMoveCoroutine(_pC.GetCurrentTile().GetPlayerSnapPosition(), _targetTile.GetPlayerSnapPosition());
                    break;
            }
            yield return null;
        }
    }

    private IEnumerator WaitingForActions()
    {
        while (_currentState == State.WaitingForActions)
        {
            yield return null;
        }
    }
}
