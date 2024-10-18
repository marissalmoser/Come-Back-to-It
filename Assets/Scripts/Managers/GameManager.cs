// +-----------------------------------------------------------------------------------+
// @author - Ryan Herwig
// @Contributers - 
// @Last Modified - October 16th 2024
// @Description - The engine of the game which controls and initializes everything else
// +-----------------------------------------------------------------------------------+

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    //Makes Class a Singleton Class.
    #region Singleton
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType(typeof(GameManager)) as GameManager;
            return instance;
        }
        set
        {
            instance = value;
        }
    }
    #endregion
    #region Variables
    [SerializeField] private List<Card> _dealtCards;
    [SerializeField] private List<Card> _playedCards;
    [SerializeField] bool _doDebugMode;
    [SerializeField] private int _deathTimerLength;

    public Image darken;
    public Image deckShownDarken;

    private DeckManager<Card> _deckManagerCard;
    private DeckManager<int> _deckManagerInt;
    private CardManager _cardManager;
    private UIManager _uiManager;
    private CollectibleStats _collectibleStats;
    private ArrowsManager _arrowsManager;

    public STATE gameState;
    private bool _gameWon;

    private LevelDeck _levelDeck;
    public List<Card> _deck;
    private List<Card> _demoDeck;

    private List<int> _collectedSwitchIDs;
    private List<Collectable> collectablesCollected = new List<Collectable>();
    private (Card, int) _lastCardPlayed;
    public Card confirmationCard;
    [NonSerialized] public bool isSwitching, isClearing, isStalling, isUsingWild, currentlyOnWild;
    [NonSerialized] public bool isTurning;

    [NonSerialized] public List<Card> _startingDeck;

    public bool isTurningleft {get; private set; }
    private bool _getOriginalDeck;
    public bool hasSwitched { get; private set; }
    #endregion
    #region Actions
    public static Action<List<Card>> PlayActionOrder;
    public static Action<List<Card>> PlayDemoActionOrder;
    public static Action DeathAction;
    public static Action TrapAction;
    #endregion

    [NonSerialized] public bool lowerDarkenIndex;
    private void Start()
    {
        //Carefully change order if needed. Some managers must be initialzed before others
        _deckManagerCard = DeckManager<Card>.Instance;
        _deckManagerInt = DeckManager<int>.Instance;
        _uiManager = UIManager.Instance;
        _cardManager = CardManager.Instance;
        _arrowsManager = ArrowsManager.Instance;
        _levelDeck = FindObjectOfType<LevelDeck>();
        lowerDarkenIndex = false;

        isSwitching = false;
        isTurning = false;
        isClearing = false;
        isStalling = false;
        isUsingWild = false;
        currentlyOnWild = false;
        hasSwitched = false;
        _getOriginalDeck = true;

        ChangeGameState(STATE.LoadGame);
    }

    public void OnEnable()
    {
        DeathAction += DeathMethod;
    }

    public void OnDisable()
    {
        DeathAction -= DeathMethod;
    }

    /// <summary>
    /// Method that is listening to the DeathAction being invoked.
    /// </summary>
    private void DeathMethod()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Controls the current game state.
    /// 
    /// Always use gameState = STATE.[State] for every branch or game will break
    /// </summary>
    public void ChangeGameState(STATE state)
    {
        //Checks for what state to change to
        switch (state)
        {
            case STATE.LoadGame:
                //Sets up and initializes necessary functions to play
                gameState = STATE.LoadGame; //call when level loads
                StartGame();
                break;
            case STATE.Menu:
                //Menu methods called
                gameState = STATE.Menu;
                break;
            case STATE.StartLevel:
                gameState = STATE.StartLevel;
                SetUpLevel();
                break;
            case STATE.ChooseCards:
                //Choose Cards methods called
                gameState = STATE.ChooseCards;
                DealCards();
                break;
            case STATE.ConfirmCards:
                gameState = STATE.ConfirmCards;
                PlayDemo();
                break;
            case STATE.PlayingActionOrder:
                gameState = STATE.PlayingActionOrder;
                break;
            case STATE.ChooseTurn:
                // Waiting STATE. Game locks in this state until user input
                gameState = STATE.ChooseTurn;
                break;
            case STATE.Death:
                gameState = STATE.Death;
                Failure();
                break;
            case STATE.OutOfCards:
                gameState = STATE.OutOfCards;
                OutOfCards();
                break;
            case STATE.End:
                gameState = STATE.End;
                //Add method here if needed
                _gameWon = true;
                Invoke("LoadLevelSelect", 1);
                break;
            default:
                //Error check
                print("ERROR: FAILED TO SWITCH GAME STATE.");
                break;
        }
    }

    #region State Machine Methods
    /// <summary>
    /// Initializes everything needed for the game
    /// Called at the start of the game
    /// </summary>
    private void StartGame()
    {
        //Initialzes other managers. Order may matter
        _uiManager.Init();
        _cardManager.Init();
        _levelDeck.Init();

        //Initializes lists.
        _deck = new();
        _demoDeck = new();
        _startingDeck = new();
        _dealtCards = new();
        _playedCards = new();

        _collectedSwitchIDs = new();

        ChangeGameState(STATE.StartLevel);
    }

    /// <summary>
    /// Sets up the current level
    /// Called when level is selected
    /// </summary>
    private void SetUpLevel()
    {
        //Gets the deck
        _deck = _levelDeck.deck;
        darken.enabled = false;

        //Keeps permenent record of the original deck
        foreach (var card in _deck)
        {
            _startingDeck.Add(card);
        }

        //Disables Darken Effects
        darken.enabled = false;
        deckShownDarken.enabled = false;

        //Add whatever additional set up here (after clicking on a level from the level to the point the player can start choosing cards)
        ChangeGameState(STATE.ChooseCards);
    }

    /// <summary>
    /// Gets how many dealt cards there are and draws up to max hand size (4)
    /// Called at the beginning of every turn
    /// </summary>
    private void DealCards()
    {
        //Draws until the player has 4 cards or until the deck runs out
        while (_dealtCards.Count < 4 && _deck.Count > 0)
        {
            if (_lastCardPlayed.Item1 != null)
            {
                //Replaces the last played card with a new card in the same index
                //Keeps the other dealt cards in the same location
                _dealtCards.Insert(_lastCardPlayed.Item2, _deck[0]);
            }
            else
            {
                //Adds the top card from the deck onto the dealtCards
                _dealtCards.Add(_deck[0]);
            }
            _deck = _deckManagerCard.RemoveFirst(_deck); //Removes the now dealt card from the deck
        }

        //If out of cards, go to corresponding game state AND IF NOT ALREADY WON
        if (_dealtCards.Count == 0 && _deck.Count == 0 && !_gameWon)
        {
            ChangeGameState(STATE.OutOfCards);
        }
        else if (!_gameWon)
        {
            _uiManager.UpdateTextBox("DRAG A CARD TO PLAY.");
            _uiManager.UpdateDealtCards(_dealtCards); //Updates Cards
        }
    }

    /// <summary>
    /// Runs the current action order
    /// Called after a card has been played
    /// </summary>
    public void RunPlaySequence()
    {
        _uiManager.UpdateConfirmCard();
        _uiManager.cancelButton.GetComponent<ConfirmationControls>().SetIsActive(true);
        //If Clear Card was Played
        if (confirmationCard != null && confirmationCard.name == Card.CardName.Clear) //Error check and checks if last card played was a Clear
        {
            if (_playedCards.Count > 0)
            {
                darken.enabled = true;
                isClearing = true;
                _uiManager.UpdateTextBox("SELECT A CARD TO CLEAR.");
            }
            else
            {
                confirmationCard = null;
                _uiManager.DestroyConfirmCard();
            }    
        }

        //POYO
        //If Switch Card was played
        if (confirmationCard != null && confirmationCard.name == Card.CardName.Switch) //Error check and checks if last card played was a Switch
        {
            if (_playedCards.Count > 1)
            {
                darken.enabled = true;
                isSwitching = true;
                _uiManager.UpdateTextBox("SELECT TWO CARDS TO SWAP.");
            }
            else
            {
                confirmationCard = null;
                _uiManager.DestroyConfirmCard();

            }
        }

        //If Turn Card was played
        if (confirmationCard != null && confirmationCard.name == Card.CardName.Turn) //Error check and checks if last card played was a Switch
        {
            darken.enabled = true;
            darken.transform.SetSiblingIndex(darken.transform.GetSiblingIndex() + 1);
            lowerDarkenIndex = true;
            ChangeGameState(STATE.ChooseTurn); //Waits for User Input to Switch two cards
            _uiManager.UpdateTextBox("CHOOSE TO TURN LEFT OR RIGHT.");
            _uiManager.CreateTurnCards();
            isTurning = true;
        }
        //If Stall Card was played
        if (confirmationCard != null && confirmationCard.name == Card.CardName.Stall) //Error check and checks if last card played was a Stall
        {
            isStalling = true;
        }

        //If Wild Card was played
        if (confirmationCard != null && confirmationCard.name == Card.CardName.Wild) //Error check and checks if last card played was a Stall
        {
            isUsingWild = true;
            currentlyOnWild = true;
            _arrowsManager.ResetIndex();
        }

        //Updates the arrows
        _uiManager.UpdateArrows();

        //If demo is good to go
        if (!isClearing && !isSwitching && !isTurning && !currentlyOnWild)
        {
            _uiManager.confirmButton.GetComponent<ConfirmationControls>().SetIsActive(true);
            ChangeGameState(STATE.ConfirmCards);
        }
    }

    /// <summary>
    /// Plays a forecast on where the player will move
    /// </summary>
    /// <param name="editedList">The tempList that was edited. If it was not edited, give the original List</param>
    /// <param name="originalList">The original List</param>
    private void PlayDemo()
    {
        _uiManager.UpdateTextBox("Confirm / Cancel");

        //Copies played cards to the demo deck
        //Restores deck when clearing to avoid losing cards
        if (_getOriginalDeck || isClearing)
        {
            _demoDeck = new();
            int playedCardsCount = _playedCards.Count;
            for (int i = 0; i < playedCardsCount; i++)
            {
                _demoDeck.Add(_playedCards[i]);
            }

            //Adds the confirmation card to be played in demo
            if (!isClearing && !isSwitching && !isStalling && !currentlyOnWild)
                _demoDeck.Add(confirmationCard);

            _getOriginalDeck = false;
        }

        //If the confirmed card was a clear card
        if (isClearing)
        {
            //sound effect call
            SfxManager.Instance.PlaySFX(6189);

            List<Image> instantiatedImages = _uiManager.GetInstantiatedPlayedCardImages(); //Gets the instantiated played cards images

            int instantiatedImagesCount = instantiatedImages.Count;

            int numOfClearedCards = 0;
            for (int i = 0; i < instantiatedImagesCount; i++)
            {
                //Loops over clear list
                for (int j = 0; j < _cardManager.numOfCardsToClear; j++)
                {
                    //Compares instantiated images' unique ID to the target ID
                    if (_cardManager.clearCards[j] != null && instantiatedImages[i].GetComponentInChildren<CardDisplay>().ID 
                        == _cardManager.clearCards[j].GetComponentInChildren<CardDisplay>().ID)
                    {
                        _demoDeck = _deckManagerCard.RemoveAt(_demoDeck, i - numOfClearedCards); //When IDs match, remove the card from the list
                        numOfClearedCards++; //Since a card is removed, list must be indexed to compensate for lost card
                    }
                }
            }
        }

        //If the confirmed card was a switch card
        if (isSwitching)
        {
            List<Image> instantiatedImages = _uiManager.GetInstantiatedPlayedCardImages(); //Gets the instantiated played cards images

            int instantiatedImagesCount = instantiatedImages.Count;

            //Initializes variables
            int target1Index = -1;
            int target2Index = -1;
            (Image, Image) switchCards = _cardManager.switchCards;

            //Finds the first target ID
            for (int i = 0; i < instantiatedImagesCount; i++)
            {
                if (instantiatedImages[i].GetComponentInChildren<CardDisplay>().ID == switchCards.Item1.GetComponentInChildren<CardDisplay>().ID) //Compares instantiated images' unique ID to the target ID
                {
                    target1Index = i;

                    break;
                }
            }

            //Finds the second target ID
            for (int i = 0; i < instantiatedImagesCount; i++)
            {
                if (instantiatedImages[i].GetComponentInChildren<CardDisplay>().ID == switchCards.Item2.GetComponentInChildren<CardDisplay>().ID) //Compares instantiated images' unique ID to the target ID
                {
                    target2Index = i;

                    break;
                }
            }
            if (target1Index != -1 && target2Index != -1) //Error check. Does not continue if both cards are not found
            {
                _demoDeck = _deckManagerCard.Swap(_demoDeck, target1Index, target2Index); //Swaps the two cards
            }
            else
            {
                print("FAILED TO LOCATE CARD IDS");
            }
            _uiManager.UpdatePlayedCards(_demoDeck);

            int demoCount = _demoDeck.Count;
            hasSwitched = false;
            for (int i = 0; i < demoCount; i++)
            {
                if (_demoDeck[i].name != _playedCards[i].name)
                {
                    hasSwitched = true;
                    break;
                }
            }
        }

        PlayDemoActionOrder?.Invoke(_demoDeck);
    }

    /// <summary>
    /// Stops the demo from being played
    /// </summary>
    public void StopDemo()
    {
        PlayDemoActionOrder?.Invoke(new List<Card>());
        _getOriginalDeck = true;
    }

    /// <summary>
    /// Toggles traps on and off
    /// </summary>
    public void ToggleTraps()
    {
        TrapAction?.Invoke();
    }

    /// <summary>
    /// When the player fails, this method is called
    /// </summary>
    private void Failure()
    {
        DeathAction?.Invoke();
        StartCoroutine(DeathTimer());
    }

    /// <summary>
    /// When the player runs out of cards, this method is called
    /// </summary>
    private void OutOfCards()
    {
        ChangeGameState(STATE.Death);
        print("Skill Issue."); //TODO - Replace this with actual functionality
    }

    #endregion

    /// <summary>
    /// Removes a card from the dealt deck, and adds it to the played deck
    /// </summary>
    /// <param name="targetID">The ID of the card to play</param>
    public void PlayCard(int targetID)
    {
        //Due to the Cards being Scriptable Objects, I couldn't find a good way to give them unique IDs
        //So I gave the images unique IDs and used those. Weird way, but it works

        List<Image> instantiatedImages = _uiManager.GetInstantiatedDealtCardImages(); //Gets instantiated dealt card images

        int instantiatedImagesCount = instantiatedImages.Count;
        for (int i = 0; i < instantiatedImagesCount; i++)
        {
            if (instantiatedImages[i].GetComponentInChildren<CardDisplay>().ID == targetID) //Compares instantiated images' unique ID to the target ID
            {
                Card playedCard = _dealtCards[i]; //Gets the same ID card
                _lastCardPlayed = (playedCard, i); //Stores card and the index
                confirmationCard = _lastCardPlayed.Item1;
                break;
            }
        }
        RunPlaySequence();
    }

    /// <summary>
    /// If the confirm button is pressed, 
    /// </summary>
    public void ConfirmCards()
    {
        //NOTE:
        //If Clear and Switch do not want to play animation, check if _isClearing and _isSwitching is false
        //If they are both false, do _uiManager.MoveCardToActionOrder() AND do the following at end of method
        // _uiManager.UpdatePlayedCards();
        // _uiManager.UpdateDealtCards();
        // PlaySequence();

        _getOriginalDeck = true;
        ChangeGameState(STATE.PlayingActionOrder);
        _cardManager.lastConfirmationCard = null;

        //Disables Arrows
        isUsingWild = false;
        currentlyOnWild = false;
        _uiManager.UpdateArrows();

        //Removes all highlight from cards
        List<Image> tempPlayedCards = _uiManager.GetInstantiatedPlayedCardImages();

        int tempCount = tempPlayedCards.Count;
        for (int i = 0; i < tempCount; i++)
        {
            tempPlayedCards[i].gameObject.transform.Find("Clear").GetComponent<Image>().enabled = false;
            tempPlayedCards[i].gameObject.transform.Find("Swap").GetComponent<Image>().enabled = false;
        }

        _uiManager.MoveCardToActionOrder();
        _uiManager.DisableTextBox();
        //If the confirmation card is a clear or switch, do not add it into play order
        if (confirmationCard.name != Card.CardName.Clear && confirmationCard.name != Card.CardName.Switch 
            && confirmationCard.name != Card.CardName.Stall && confirmationCard.name != Card.CardName.Wild)
            _playedCards.Add(confirmationCard);

        _dealtCards.Remove(_lastCardPlayed.Item1);

        //If the confirmed card was a clear card
        if (isClearing)
        {
            isClearing = false;

            //sound effect call
            SfxManager.Instance.PlaySFX(6189);

            List<Image> instantiatedImages = _uiManager.GetInstantiatedPlayedCardImages(); //Gets the instantiated played cards images

            //   int instantiatedImagesCount = instantiatedImages.Count;
            //   for (int i = 0; i < instantiatedImagesCount; i++)
            //   {
            //       //Loops over clear list
            //       for (int j = 0; j < _cardManager.numOfCardsToClear; j++)
            //       {
            //           if (instantiatedImages[i].GetComponentInChildren<CardDisplay>().ID == _cardManager.clearCards[j].GetComponentInChildren<CardDisplay>().ID) //Compares instantiated images' unique ID to the target ID
            //           {
            //              _playedCards = _deckManagerCard.RemoveAt(_playedCards, i); //When IDs match, remove the card from the list
            //           }
            //       }
            //   }
            int demoCount = _demoDeck.Count;
            _playedCards = new();
            for (int i = 0; i < demoCount; i++)
            {
                _playedCards.Add(_demoDeck[i]);
            }
            _cardManager.clearCards = new Image[_cardManager.numOfCardsToClear];
        }

        //If the confirmed card was a switch card
        if (isSwitching)
        {
            isSwitching = false;

            int demoCount = _demoDeck.Count;
            _playedCards = new();
            for (int i = 0; i < demoCount; i++)
            {
                _playedCards.Add(_demoDeck[i]);
            }
        }
        isStalling = false;
    }

    /// <summary>
    /// 
    /// </summary>
    public void CancelCard()
    {
        //Stops Demo
        StopDemo();

        //Resets CardManager
        _cardManager.lastConfirmationCard = null;
        _cardManager.clearCards = new Image[_cardManager.numOfCardsToClear];
        _cardManager.switchCards.Item1 = null;
        _cardManager.switchCards.Item2 = null;
        _uiManager.DestroyTurnCards(); //Destroys turn cards

        //Removes all highlight from the images
        List<Image> tempPlayedCards = _uiManager.GetInstantiatedPlayedCardImages();
        int tempCount = tempPlayedCards.Count;
        for (int i = 0; i < tempCount; i++)
        {
            tempPlayedCards[i].gameObject.transform.Find("Clear").GetComponent<Image>().enabled = false;
            tempPlayedCards[i].gameObject.transform.Find("Swap").GetComponent<Image>().enabled = false;
        }

        //Resets to prior state
        if (lowerDarkenIndex)
        {
            darken.transform.SetSiblingIndex(darken.transform.GetSiblingIndex() - 1);
            lowerDarkenIndex = false;
        }
        darken.enabled = false;
        isClearing = false;
        isSwitching = false;
        isStalling = false;
        isUsingWild = false;
        currentlyOnWild = false;
        _getOriginalDeck = true;

        _cardManager.RemoveAllHighlight(_uiManager.GetInstantiatedPlayedCardImages()); //Removes the highlight
        _collectedSwitchIDs = new(); //Clears the list
        _uiManager.UpdatePlayedCards(_playedCards);
        _uiManager.UpdateArrows();
        ChangeGameState(STATE.ChooseCards);
    }

    /// <summary>
    /// Forces UI played cards display to display _playedCards
    /// This method is to be used if player replaces the Switch or Clear Card with another card
    /// </summary>
    public void ResetPlayedDisplay()
    {
        _uiManager.UpdatePlayedCards(_playedCards);
    }

    /// <summary>
    /// Checks the sequence and calls the animation based off the card
    /// Called after the player chooses a card and when the player returns to the idle animation
    /// </summary>
    public void PlaySequence()
    {
        if (lowerDarkenIndex)
        {
            darken.transform.SetSiblingIndex(darken.transform.GetSiblingIndex() - 1);
            lowerDarkenIndex = false;
        }
        darken.enabled = false;
        //Invokes Action that Eli's script is listening to
        PlayActionOrder?.Invoke(_playedCards);

        if (_doDebugMode)
            ChangeGameState(STATE.ChooseCards);
    }

    /// <summary>
    /// Adds a card at the bottom of the action order
    /// </summary>
    /// <param name="card">The Card to add</param>
    public void AddTurnCard(Card card, bool isTurningLeft)
    {
        confirmationCard = card;
        isTurning = false;
        isTurningleft = isTurningLeft;

        if (lowerDarkenIndex)
        {
            darken.transform.SetSiblingIndex(darken.transform.GetSiblingIndex() - 1);
            lowerDarkenIndex = false;
        }
        darken.enabled = false;

        RunPlaySequence();
    }

    #region Action Order Card Effects
    /// <summary>
    /// Functionality of the Clear Card
    /// </summary>
    public void ClearAction()
    {
        _uiManager.UpdateConfirmCard();
        ChangeGameState(STATE.ConfirmCards);
    }

    /// <summary>
    /// Functionality of the Switch Card
    /// </summary>
    /// <param name="firstTargetID">The first card ID to switch</param>
    /// <param name="secondTargetID">The second card ID to switch</param>
    public void SwitchAction()
    {
        (Image, Image) cards = _cardManager.switchCards;

        if (cards.Item1 != null && cards.Item2 != null)
        {
            _uiManager.UpdateConfirmCard();
            ChangeGameState(STATE.ConfirmCards);
        }

    }

    public void WildAction(Card card)
    {
        //Resets state to before card was viewed
        _getOriginalDeck = true;
        _cardManager.clearCards = new Image[_cardManager.numOfCardsToClear];
        _cardManager.switchCards.Item1 = null;
        _cardManager.switchCards.Item2 = null;
        StopDemo();

        //Removes Card Clear and Swap Highlights
        List<Image> tempPlayedCards = _uiManager.GetInstantiatedPlayedCardImages();
        int tempCount = tempPlayedCards.Count;
        for (int i = 0; i < tempCount; i++)
        {
            tempPlayedCards[i].gameObject.transform.Find("Clear").GetComponent<Image>().enabled = false;
            tempPlayedCards[i].gameObject.transform.Find("Swap").GetComponent<Image>().enabled = false;
        }

        //Sets variables based on the switched card
        switch (card.name)
        {
            case Card.CardName.Move:
            case Card.CardName.Jump:
            case Card.CardName.TurnRight:
            case Card.CardName.TurnLeft:
                isClearing = false;
                isSwitching = false;
                isStalling = false;
                isUsingWild = true;
                currentlyOnWild = false;
                _getOriginalDeck = true;

                //Deactivates Darken Effect
                if (lowerDarkenIndex)
                {
                    darken.transform.SetSiblingIndex(darken.transform.GetSiblingIndex() - 1);
                    lowerDarkenIndex = false;
                }
                darken.enabled = false;
                break;
            case Card.CardName.Clear:
                isClearing = true;
                isSwitching = false;
                isStalling = false;
                isUsingWild = false;
                currentlyOnWild = true;
                _getOriginalDeck = true;

                //Activates Darken Effect
                if (_playedCards.Count > 0)
                {
                    darken.enabled = true;
                    _uiManager.UpdateTextBox("SELECT A CARD TO CLEAR.");
                }
                else //Disables Darken Effect - Shouldn't ever need this - Failsafe
                    darken.enabled = false;

                //Deactivates Confirm Button
                _uiManager.confirmButton.GetComponent<ConfirmationControls>().SetIsActive(false);
                break;
            case Card.CardName.Switch:
                isClearing = false;
                isSwitching = true;
                isStalling = false;
                isUsingWild = false;
                currentlyOnWild = true;
                _getOriginalDeck = true;

                //Activates Darken Effect
                if (_playedCards.Count > 1)
                {
                    darken.enabled = true;
                    _uiManager.UpdateTextBox("SELECT TWO CARDS TO SWAP.");
                }
                else //Disables Darken Effect
                    darken.enabled = false;

                //Deactivates Confirm Button
                _uiManager.confirmButton.GetComponent<ConfirmationControls>().SetIsActive(false);
                break;
            case Card.CardName.Stall:
                isClearing = false;
                isSwitching = false;
                isStalling = true;
                isUsingWild = true;
                currentlyOnWild = false;
                _getOriginalDeck = true;

                //Deactivates Darken Effect
                if (lowerDarkenIndex)
                {
                    darken.transform.SetSiblingIndex(darken.transform.GetSiblingIndex() - 1);
                    lowerDarkenIndex = false;
                }
                darken.enabled = false;
                break;
        }
    }
    #endregion

    /// <summary>
    /// Starts a new turn
    /// </summary>
    public void NewTurn()
    {
        if (_playedCards.Count == 5)
            DeathMethod();
        ChangeGameState(STATE.ChooseCards);
    }

    /// <summary>
    /// Timer to reset the game state
    /// </summary>
    IEnumerator DeathTimer()
    {
        yield return new WaitForSeconds(_deathTimerLength);
        ChangeGameState(STATE.StartLevel);
        yield return null;
    }

    private void LoadLevelSelect()
    {
        SceneManager.LoadScene(2);
    }

    #region Getters
    public int GetCollectableCount() { return collectablesCollected.Count; }

    /// <summary>
    /// Gets the current played cards
    /// </summary>
    /// <returns>A List<Card> Cards that have been played</returns>
    public List<Card> GetStartingCards() { return _startingDeck; }

    /// <summary>
    /// Gets the current dealt cards
    /// </summary>
    /// <returns>A List<Card> Cards that have been dealt</returns>
    public List<Card> GetDealtCards() { return _dealtCards; }


    /// <summary>
    /// Gets the current played cards
    /// </summary>
    /// <returns>A List<Card> Cards that have been played</returns>
    public List<Card> GetPlayedCards() { return _playedCards; }

    /// <summary>
    /// Gets the last played card
    /// </summary>
    /// <returns>A Card - the last card played, waiting to be confirmed</returns>
    public Card GetLastPlayedCard() { return confirmationCard; }

    /// <summary>
    /// Gets the current collected switch IDs
    /// </summary>
    /// <returns>List<int> list of IDs from played cards being selected to be swapped</returns>
    public List<int> GetCollectedSwitchIDs() { return _collectedSwitchIDs; }
    #endregion

    public enum STATE
    {
        LoadGame,
        Menu,
        StartLevel,
        ChooseCards,
        ConfirmCards,
        PlayingActionOrder,
        Trap,
        ChooseTurn,
        Death,
        OutOfCards,
        End
    }
}