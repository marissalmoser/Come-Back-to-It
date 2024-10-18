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
*******************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachineBrain : MonoBehaviour
{
    [SerializeField] private State _currentState;
    private Coroutine _currentStateCoroutine;
    private Card _currentAction;
    private List<Card> _actions = new List<Card>();
    private List<Card> _actionCopies = new List<Card>();
    private Tile _targetTile;
    private GameObject _player, _ghostPlayer;
    private PlayerController _currentPlayerController, _playerControllerOriginal, _playerControllerGhost;
    private int _distance;
    private bool _firedTraps = false;
    private bool _isGhost = false;
    
    public void Start()
    {
        PlayerController.ReachedDestination += HandleReachedDestination;
        PlayerController.AddCard += HandleCardAdd;
        PlayerController.SpikeCollision += HandleSpikeInterruption;
        PlayerController.WallInterruptAnimation += HandleWallInterruption;
        GameManager.PlayActionOrder += HandleIncomingActions;
        GameManager.PlayDemoActionOrder += HandleIncomingGhostActions;
        TileManager.Instance.LoadTileList();
        TileManager.Instance.LoadObstacleList();
        ConfirmationControls.CancelCard += ResetGhost;
        FindPlayer();
        _player.transform.position = _playerControllerOriginal.GetCurrentTile().GetPlayerSnapPosition();
        ResetGhost(); //Reused this method turn off ghost and shadow on level start
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
        TrapPlayState,
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
                if (_currentStateCoroutine != null)
                {
                    StopCoroutine(_currentStateCoroutine);
                }
                print("Waiting for actions");
                GameManager.Instance.NewTurn();
                _currentState = State.WaitingForActions;
                _currentStateCoroutine = StartCoroutine(WaitingForActions());
                break;

            case State.FindTileUponAction:
                if (_currentStateCoroutine != null)
                {
                    StopCoroutine(_currentStateCoroutine);
                }
                _currentState = State.FindTileUponAction;
                print("Finding target tile");
                _currentStateCoroutine = StartCoroutine(FindTileUponAction());
                break;

            case State.PlayResult:
                if (_currentStateCoroutine != null)
                {
                    StopCoroutine(_currentStateCoroutine);
                }
                _currentState = State.PlayResult;
                print("Playing results");
                _currentStateCoroutine = StartCoroutine(PlayResult());
                break;

            case State.PrepareNextAction:
                if (_currentStateCoroutine != null)
                {
                    StopCoroutine(_currentStateCoroutine);
                }
                _currentState = State.PrepareNextAction;
                print("Preparing next action");
                _currentStateCoroutine = StartCoroutine(PrepareNextAction());
                break;
            case State.TrapPlayState:
                if (_currentStateCoroutine != null)
                {
                    StopCoroutine(_currentStateCoroutine);
                }
                print("Turning traps on and off");
                _currentState = State.TrapPlayState;
                _currentStateCoroutine = StartCoroutine(TrapFiring());
                break;
        }
    }
    public void FindPlayer()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _currentPlayerController= _playerControllerOriginal = _player.GetComponent<PlayerController>();
            _ghostPlayer = GameObject.FindGameObjectWithTag("PlayerGhost");
            _playerControllerGhost = _ghostPlayer.GetComponent<PlayerController>();

            if (_currentPlayerController == null)
            {
                print("NULL");
            }
            if (_player == null)
            {
                Debug.Log("No gameobject in scene tagged with player");
            }
        }
    }
    public void SetGhostState(bool isGhost)
    {
        if(!isGhost)
        {
            _isGhost = false;
            _currentPlayerController = _playerControllerOriginal;
        }
        else
        {
            _isGhost = true;
            _currentPlayerController = _playerControllerGhost;
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
    public PlayerController GetOriginalPlayerController()
    {
        return _playerControllerOriginal;
    }

    public void HandleCardAdd(Card card)
    {
        AddCardToList(card);
    }
    public void HandleIncomingActions(List<Card> cardList)
    {
        ResetGhost();

        SetGhostState(false); //change the selected player script back to player from ghost
        _ghostPlayer.transform.position = Vector3.zero; //make sure the ghost goes back to the parent

        _firedTraps = false;

        StartCardActions(cardList);
        FSM(State.PrepareNextAction);
    }
    private void ResetGhost()
    {
        gameObject.transform.GetChild(0).gameObject.SetActive(true);  //turn on main player
        gameObject.transform.GetChild(1).gameObject.SetActive(false); //turn off shadow
        _ghostPlayer.SetActive(false); //turn off the ghost gameobject
        _currentPlayerController.StopAllCoroutines(); //stop whatever ghost is doing
    }
    public void HandleIncomingGhostActions(List<Card> cardList)
    {
        SfxManager.Instance.SetPlayerSfxVolume(true); //set player sfx to ghost volume

        gameObject.transform.GetChild(0).gameObject.SetActive(false);
        gameObject.transform.GetChild(1).gameObject.SetActive(true);  //turn on shadow
        _ghostPlayer.SetActive(true);
        _currentPlayerController.StopAllCoroutines();

        _firedTraps = true; //so that traps dont fire at the end of this state
        SetGhostState(true);
        _actionCopies.Clear();
        _actionCopies.AddRange(cardList); //for ghost repeating

        StartCardActions(cardList);
        FSM(State.PrepareNextAction);
    }
    public void HandleWallInterruption()
    {
        if (_currentPlayerController.GetCurrentMovementCoroutine() != null)
        {
            _currentPlayerController.StopCoroutine(_currentPlayerController.GetCurrentMovementCoroutine());
            _currentPlayerController.SetPreviousTile(_currentPlayerController.GetTileWithPlayerRaycast());
            _currentPlayerController.StartFallCoroutine(_currentPlayerController.transform.position, _currentPlayerController.GetPreviousTile().GetPlayerSnapPosition());
        }
        else
        {
            Debug.LogError("Current coroutine was null after hitting wall");
        }
        //FSM(State.PrepareNextAction);
    }
    public void HandleSpikeInterruption()
    {
        if (_currentPlayerController.GetCurrentMovementCoroutine() != null)
        {
            _currentPlayerController.StopCoroutine(_currentPlayerController.GetCurrentMovementCoroutine());
        }
        FSM(State.WaitingForActions);
    }
    public void HandleReachedDestination()
    {
        if(_currentPlayerController.GetCurrentMovementCoroutine() != null)
        {
            _currentPlayerController.StopCoroutine(_currentPlayerController.GetCurrentMovementCoroutine());
        }
        _currentPlayerController.SetPreviousTile(_currentPlayerController.GetTileWithPlayerRaycast());

        FSM(State.PrepareNextAction);
    }
    /// <summary>
    /// The method used to start a new action order from outside scripts
    /// </summary>
    /// <param name="incomingActions"></param>
    public void StartCardActions(List<Card> incomingActions)
    {
        ActionOrderDisplay.ActionOrderComplete?.Invoke();
        _currentPlayerController.SetCurrentTile(_playerControllerOriginal.GetCurrentTile());
        _currentPlayerController.SetFacingDirection(_playerControllerOriginal.GetCurrentFacingDirection());
        _currentPlayerController.transform.position = _currentPlayerController.GetCurrentTile().GetPlayerSnapPosition();
        _actions.Clear();
        if (incomingActions != null)
        {
            _actions.AddRange(incomingActions);
        }
    }

    /// <summary>
    /// Adds a card to the 0th index
    /// </summary>
    /// <param name="card"></param>
    public void AddCardToList(Card card)
    {
        if (card != null)
        {
            _actions.Insert(0, card);
        }
        else
        {
            //Debug.LogError("Card is null");
        }
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
            else if (!_firedTraps)
            {
                FSM(State.TrapPlayState);
            }
            else if (_currentAction == null && _isGhost)
            {
                yield return new WaitForSeconds(.75f);
                StartCardActions(_actionCopies); //restart the preview until the true cards come in
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
            var currentTile = _currentPlayerController.GetCurrentTile();

            _distance = _currentAction.GetDistance(); //main focus of this state

            int facingDirection = _currentPlayerController.GetCurrentFacingDirection();
            if (currentTile.GetObstacleClass() != null && currentTile.GetObstacleClass().IsActive()) //If youre standing on an obstacle that sends you in a direction
            {
               
                facingDirection = currentTile.GetObstacleClass().GetDirection();
                if(facingDirection == 4) //IF the tile doesnt have a facing direction, use the player's current FD
                {
                    facingDirection = _currentPlayerController.GetCurrentFacingDirection();
                }
                if (_currentAction.name == Card.CardName.TurnLeft || _currentAction.name == Card.CardName.TurnRight)
                {
                    _currentPlayerController.SetFacingDirection(facingDirection); //turn the player to face where they are going
                }               
            }

            _targetTile = TileManager.Instance.GetTileAtLocation(currentTile, facingDirection, _distance);

            FSM(State.PlayResult);
            yield return null;
        }
    }

    private IEnumerator PlayResult()
    {
        if (_currentState == State.PlayResult)
        {
            if(_currentAction.GetIsObstacle())
            {
                _currentPlayerController.GetCurrentTile().GetObstacleClass().PerformObstacleAnim();
            }
            else
            {
                ActionOrderDisplay.NewActionPlayed?.Invoke();
            }
            switch (_currentAction.name)
            {
                case Card.CardName.TurnLeft:
                    SfxManager.Instance.PlaySFX(2469);
                    _currentPlayerController.StartTurnCoroutine(true);
                    break;
                case Card.CardName.TurnRight:
                    SfxManager.Instance.PlaySFX(2469);
                    _currentPlayerController.StartTurnCoroutine(false);
                    break;
                case Card.CardName.Jump:
                    if (_distance > 1) //this is a spring tile
                    {
                        SfxManager.Instance.PlaySFX(1917);
                        _currentPlayerController.StartJumpCoroutine(_currentPlayerController.GetCurrentTile().GetPlayerSnapPosition(), _targetTile.GetPlayerSnapPosition());
                    }
                    else // this is a normal jump
                    {
                        SfxManager.Instance.PlaySFX(3740);
                        //determine result by getting difference of elevation betwen current tile and tile right in front of player
                        _distance += (_currentPlayerController.GetCurrentTile().GetElevation() - 
                            (TileManager.Instance.GetTileAtLocation(_currentPlayerController.GetCurrentTile(), _currentPlayerController.GetCurrentFacingDirection(), 1).GetElevation()));
                        if (_distance < 0) //block is too high
                        {
                            Vector3 newVector = (_targetTile.GetPlayerSnapPosition());
                            _currentPlayerController.StartJumpCoroutine(_currentPlayerController.GetCurrentTile().GetPlayerSnapPosition(), new Vector3(newVector.x, newVector.y - 1, newVector.z));
                        }
                        else //block isnt too tall; need to add distance if 0 to actually jump onto the next block
                        {
                            if (_distance == 0)
                            {
                                _distance++;
                            }
                            _currentPlayerController.StartJumpCoroutine(_currentPlayerController.GetCurrentTile().GetPlayerSnapPosition(), 
                                TileManager.Instance.GetTileAtLocation(_currentPlayerController.GetCurrentTile(), _currentPlayerController.GetCurrentFacingDirection(), _distance).GetPlayerSnapPosition());
                        }
                    }
                    break;
                case Card.CardName.Move:
                    SfxManager.Instance.PlaySFX(9754);
                    Vector3 newV = new Vector3(_targetTile.GetPlayerSnapPosition().x, _currentPlayerController.transform.position.y, _targetTile.GetPlayerSnapPosition().z);
                    _currentPlayerController.StartMoveCoroutine(_currentPlayerController.GetCurrentTile().GetPlayerSnapPosition(), newV);
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

    private IEnumerator TrapFiring()
    {
        while (_currentState == State.TrapPlayState)
        {
            yield return new WaitForSeconds(.75f);
            GameManager.TrapAction?.Invoke();
            _firedTraps = true;
            var tile = _currentPlayerController.GetTileWithPlayerRaycast();
            if (tile != null && tile.GetObstacleClass() != null && tile.GetObstacleClass().IsActive())
            {
                var temp = _currentPlayerController.GetTileWithPlayerRaycast().GetObstacleClass().GetCard();
                //get card and check if its not a turn tables
                if (temp != null && (temp.name != Card.CardName.TurnLeft && temp.name != Card.CardName.TurnRight))
                {
                    AddCardToList(_currentPlayerController.GetTileWithPlayerRaycast().GetObstacleClass().GetCard());
                }
            }
            FSM(State.PrepareNextAction);
        }
    }

    private void OnDisable()
    {
        PlayerController.ReachedDestination -= HandleReachedDestination;
        PlayerController.AddCard -= HandleCardAdd;
        PlayerController.SpikeCollision -= HandleSpikeInterruption;
        PlayerController.WallInterruptAnimation -= HandleWallInterruption;
        GameManager.PlayActionOrder -= HandleIncomingActions;
        GameManager.PlayDemoActionOrder -= HandleIncomingGhostActions;
        ConfirmationControls.CancelCard -= ResetGhost;
    }
}
