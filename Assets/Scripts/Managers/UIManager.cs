// +-------------------------------------------------------+
// @author - Ryan Herwig
// @Contributers - 
// @Last modified - October 16th 2024
// @Description - Manages the UI for the game
// +-------------------------------------------------------+

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    #region Singleton
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType(typeof(UIManager)) as UIManager;
            return instance;
        }
        set
        {
            instance = value;
        }
    }
    #endregion

    [Header("Cards")]
    [SerializeField] private Image _deckImage;
    [SerializeField] private Image _deckShownImage, _dealtCardImage, _playedCardImage, _confirmCardImage, _turnLeftImage, _turnRightImage;
    [SerializeField] private int _widthPadding, _heightPadding;
    [SerializeField] private int _dealtCardWidthSpacing, _playedCardWidthSpacing, _cardHeightSpacing;
    [SerializeField] private bool doVerticalFormat;
    [SerializeField] private int numOfUniqueCards = 7;

    [Header("Card Slots")]
    [SerializeField] private Image cardSlot1;
    [SerializeField] private Image cardSlot2, cardSlot3, cardSlot4, cardSlot5;

    [Header("Index Buttons")]
    [SerializeField] private Button _leftButton;
    [SerializeField] private Button _rightButton;

    [Header("Tooltip")]
    [SerializeField] private Sprite _movedTooltip;
    [SerializeField] private Sprite _jumpTooltip, _turnTooltip, _switchTooltip, _clearTooltip;
    [SerializeField] private Image _upperTextBox;

    [Header("Folders")]
    [SerializeField] private Transform _dealtCardsFolder;
    [SerializeField] private Transform _playedCardsFolder;

    [Header("Canvas")]
    [SerializeField] private GameObject _canvas;
    [SerializeField] private TextMeshProUGUI _collectablesCount;
    [SerializeField] private TextMeshProUGUI _deckCount;
    private Vector2 _deckCountPos;
    public Button confirmButton, cancelButton;

    [Header("Deck Scriptable Objects")]
    [SerializeField] private Card _deckCardSingle;
    [SerializeField] private Card _deckCard;

    [Header("Card Scriptable Objects")]
    [SerializeField] private Card _moveCard;
    [SerializeField] private Card _jumpCard;
    [SerializeField] private Card _turnCard;
    [SerializeField] private Card _turnLeftCard;
    [SerializeField] private Card _turnRightCard;
    [SerializeField] private Card _switchCard;
    [SerializeField] private Card _clearCard;
    [SerializeField] private Card _stallCard;
    [SerializeField] private Card _wildCard;

    private GameManager _gameManager;
    private ArrowsManager _arrowsManager;
    private CardManager _cardManager;

    private Image _deck;
    private List<Image> _shownDeck;
    private List<Image> _dealtCardImages;
    private List<Image> _playedCardImages;

    private float _screenWidth, _screenHeight;

    private float cardWidth, cardHeight;

    private Vector2 _nextPlayCardPosition;
    /// <summary>
    /// Initializes variables for UIManager. Called by GameManager
    /// </summary>
    public void Init()
    {
        _gameManager = GameManager.Instance;
        _arrowsManager = ArrowsManager.Instance;
        _cardManager = CardManager.Instance;

        _screenWidth = _canvas.GetComponent<RectTransform>().rect.width;
        _screenHeight = _canvas.GetComponent<RectTransform>().rect.height;

        _shownDeck = new();
        _dealtCardImages = new();
        _playedCardImages = new();

        cardWidth = _dealtCardImage.rectTransform.rect.width;
        cardHeight = _dealtCardImage.rectTransform.rect.height;

        //Disables buttons on start 
        confirmButton.GetComponent<ConfirmationControls>().SetIsActive(false);
        cancelButton.GetComponent<ConfirmationControls>().SetIsActive(false);

        _leftButton.gameObject.SetActive(false);
        _rightButton.gameObject.SetActive(false);

        _upperTextBox.enabled = false;
        _upperTextBox.GetComponentInChildren<TextMeshProUGUI>().enabled = false;

        _nextPlayCardPosition = new Vector2(-_widthPadding, _screenHeight - cardHeight / 2 - _heightPadding);

        _deckCountPos = _deckCount.GetComponent<RectTransform>().anchoredPosition;

        //Sets Card Slot Positions
        cardSlot1.rectTransform.anchoredPosition = new Vector2(-_widthPadding, _screenHeight - cardHeight / 2 - _heightPadding);
        cardSlot2.rectTransform.anchoredPosition = new Vector2(-_widthPadding, _screenHeight - cardHeight / 2 - _cardHeightSpacing * 1 - _heightPadding);
        cardSlot3.rectTransform.anchoredPosition = new Vector2(-_widthPadding, _screenHeight - cardHeight / 2 - _cardHeightSpacing * 2 - _heightPadding);
        cardSlot4.rectTransform.anchoredPosition = new Vector2(-_widthPadding, _screenHeight - cardHeight / 2 - _cardHeightSpacing * 3 - _heightPadding);
        cardSlot5.rectTransform.anchoredPosition = new Vector2(-_widthPadding, _screenHeight - cardHeight / 2 - _cardHeightSpacing * 4 - _heightPadding);
    }

    /// <summary>
    /// Updates the dealt cards in the UI
    /// </summary>
    public void UpdateDealtCards(List<Card> dealtCards)
    {
        //Destroys all previous instances of dealt card images
        for (int i = 0; i < _dealtCardImages.Count; i++)
        {
            if (_dealtCardImages[i] != null)
                Destroy(_dealtCardImages[i].gameObject);
        }
        if (_deck != null)
            Destroy(_deck.gameObject); //Destroys deck image

        //Resets list
        _dealtCardImages = new();

        int numOfDealtCards = dealtCards.Count;

        //Instantiates Deck Back
        _deck = Instantiate(_deckImage, Vector2.zero, Quaternion.identity);
        _deck.transform.SetParent(_canvas.transform, false); //Sets canvas as its parent
        _deck.rectTransform.anchoredPosition = new Vector3(_widthPadding, cardHeight + 20, 0); //Sets position

        CardDisplay deckCard = _deck.GetComponentInChildren<CardDisplay>(); //Gets data from image

        if (_gameManager._deck.Count > 1)
        {
            deckCard.card = _deckCard;
            _deckCount.enabled = true;
        }
        else if (_gameManager._deck.Count <= 1)
        {
            deckCard.card = _deckCardSingle;
            _deckCount.enabled = true;
            _deckCount.GetComponent<RectTransform>().anchoredPosition = _deckCountPos;
            _deckCount.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, 40);
        }
        
        _deckCount.transform.SetAsLastSibling();

        _deckCount.text = _gameManager._deck.Count.ToString();

        //Instantiates and sets up Cards
        for (int i = 0; i < numOfDealtCards; i++)
        {
            Image newImage = Instantiate(_dealtCardImage, Vector3.zero, Quaternion.identity); //Instantiates new card
            newImage.transform.SetParent(_canvas.transform, false); //Sets canvas as its parent
            newImage.rectTransform.anchoredPosition = new Vector3( (cardWidth + _dealtCardWidthSpacing ) * i + _widthPadding, _heightPadding, 0); //Sets position
            newImage.GetComponentInChildren<CardDisplay>().ID = i; //Sets ID
            newImage.enabled = false; //Sets highlight to off

            //Makes tooltip invisible
            newImage.gameObject.transform.Find("Tooltip").GetComponent<Image>().enabled = false;
            newImage.GetComponentInChildren<TextMeshProUGUI>().enabled = false;

            //If it is the first tooltip, off center it to keep it on screen
            if (i == 0)
                newImage.gameObject.transform.Find("Tooltip").gameObject.transform.position = 
                    new Vector2(newImage.gameObject.transform.Find("Tooltip").gameObject.transform.position.x + 25, 
                    newImage.gameObject.transform.Find("Tooltip").gameObject.transform.position.y);

            //Adds card into folder
            newImage.gameObject.transform.SetParent(_dealtCardsFolder);

            _dealtCardImages.Add(newImage); //Adds instantiated image to list

            CardDisplay card = newImage.GetComponentInChildren<CardDisplay>(); //Gets data from image

            //TODO - Put in function when you have time - UpdateCard(Card.name)
            //Finds the name and sets the image to the found data
            switch (dealtCards[i].name)
            {
                case Card.CardName.Move:
                    card.card = _moveCard;
                    newImage.GetComponentInChildren<TextMeshProUGUI>().text = "MOVE FORWARD ONE TILE.";
                    break;
                case Card.CardName.Jump:
                    card.card = _jumpCard;
                    newImage.GetComponentInChildren<TextMeshProUGUI>().text = "MOVE FORWARD ONE TILE.\nCAN JUMP TO HIGHER GROUND.";
                    break;
                case Card.CardName.Turn:
                    card.card = _turnCard;
                    newImage.GetComponentInChildren<TextMeshProUGUI>().text = "TURNS LEFT OR RIGHT.";
                    break;
                case Card.CardName.TurnLeft: //Error Case. Should not be used, but it can be used if needed
                    card.card = _turnLeftCard;
                    break;
                case Card.CardName.TurnRight: //Error Case. Should not be used, but it can be used if needed
                    card.card = _turnRightCard;
                    break;
                case Card.CardName.Clear:
                    card.card = _clearCard;
                    newImage.GetComponentInChildren<TextMeshProUGUI>().text = "REMOVES ONE CARD FROM ACTION ORDER.";
                    break;
                case Card.CardName.Switch:
                    card.card = _switchCard;
                    newImage.GetComponentInChildren<TextMeshProUGUI>().text = "SWAP TWO CARDS IN ACTION ORDER.";
                    break;
                case Card.CardName.Stall:
                    card.card = _stallCard;
                    newImage.GetComponentInChildren<TextMeshProUGUI>().text = "REPEAT ACTION ORDER WITHOUT ADDING ANY CARD.";
                    break;
                case Card.CardName.Wild:
                    card.card = _wildCard;
                    newImage.GetComponentInChildren<TextMeshProUGUI>().text = "CHOOSE ANY CARD TO PUT INTO THE ACTION ORDER.";
                    break;
                default:
                    print("ERROR: COULD NOT UPDATE CARD IN UI");
                    break;
            }
        }


    }

    /// <summary>
    /// Updates the played cards in the UI
    /// </summary>
    public void UpdatePlayedCards(List<Card> playedCards)
    {
        //Destroys all previous instantiations of played cards
        for (int i = 0; i < _playedCardImages.Count; i++)
        {
            if (_playedCardImages[i] != null)
                Destroy(_playedCardImages[i].gameObject);
        }
        //Resets list
        _playedCardImages = new();

        //Instantiates card images
        int numOfPlayedCards = playedCards.Count;
        for (int i = 0; i < numOfPlayedCards; i++)
        {
            Image newImage = Instantiate(_playedCardImage, Vector3.zero, Quaternion.identity); //Instantiates image
            newImage.transform.SetParent(_canvas.transform, false); //Sets canvas as the parent

            if (doVerticalFormat)
                newImage.rectTransform.anchoredPosition = new Vector2(-_widthPadding, _screenHeight - cardHeight / 2 -_cardHeightSpacing * i - _heightPadding); //Sets position - Vertical Format
            else
                newImage.rectTransform.anchoredPosition = new Vector2((-_screenWidth / 2 + cardWidth / 2) - (_playedCardWidthSpacing * numOfPlayedCards / 2) 
                    + (_playedCardWidthSpacing * i + _widthPadding), -_heightPadding); //Sets position - Horizontal Format
            
            if (i == numOfPlayedCards - 1)
            {
                //Gets next card position
                _nextPlayCardPosition = new Vector2(-_widthPadding, _screenHeight - cardHeight / 2 - _cardHeightSpacing * (i + 1) - _heightPadding);
            }

            newImage.GetComponentInChildren<CardDisplay>().ID = i; //Sets ID
            newImage.enabled = false; //Turns off highlight

            //Makes tooltip invisible
            newImage.gameObject.transform.Find("Tooltip").GetComponent<Image>().enabled = false;
            newImage.GetComponentInChildren<TextMeshProUGUI>().enabled = false;

            //Makes clear and switch hover invisible
            newImage.gameObject.transform.Find("Clear").GetComponent<Image>().enabled = false;
            newImage.gameObject.transform.Find("Swap").GetComponent<Image>().enabled = false;

            //If it is the first tooltip, off center it to keep it on screen
            if (i == 0)
                newImage.gameObject.transform.Find("Tooltip").gameObject.transform.position =
                    new Vector2(newImage.gameObject.transform.Find("Tooltip").gameObject.transform.position.x,
                    newImage.gameObject.transform.Find("Tooltip").gameObject.transform.position.y - 20);

            //Adds card to folder
            newImage.gameObject.transform.SetParent(_playedCardsFolder);

            _playedCardImages.Add(newImage); //Adds image to list

            CardDisplay card = newImage.GetComponentInChildren<CardDisplay>(); //Grabs data from image
                                                                               //Uses grabbed data to compare with possible types and convert image to found typ
            switch (playedCards[i].name)
            {
                case Card.CardName.Move:
                    card.card = _moveCard;
                    newImage.GetComponentInChildren<TextMeshProUGUI>().text = "MOVE FORWARD ONE TILE.";
                    //newImage.gameObject.transform.Find("Tooltip").GetComponent<Image>().sprite = _movedTooltip;
                    break;
                case Card.CardName.Jump:
                    card.card = _jumpCard;
                    newImage.GetComponentInChildren<TextMeshProUGUI>().text = "MOVE FORWARD ONE TILE.\nCAN JUMP TO HIGHER GROUND.";
                    //newImage.gameObject.transform.Find("Tooltip").GetComponent<Image>().sprite = _jumpTooltip;
                    break;
                case Card.CardName.Turn: //Error Case. Should not be used, but it can be used if needed
                    card.card = _turnCard;
                    newImage.GetComponentInChildren<TextMeshProUGUI>().text = "TURNS LEFT OR RIGHT.";
                    //newImage.gameObject.transform.Find("Tooltip").GetComponent<Image>().sprite = _turnTooltip;
                    break;
                case Card.CardName.TurnLeft:
                    card.card = _turnLeftCard;
                    newImage.GetComponentInChildren<TextMeshProUGUI>().text = "TURNS LEFT";
                    //newImage.gameObject.transform.Find("Tooltip").GetComponent<Image>().sprite = _turnTooltip;
                    break;
                case Card.CardName.TurnRight:
                    card.card = _turnRightCard;
                    newImage.GetComponentInChildren<TextMeshProUGUI>().text = "TURNS LEFT RIGHT.";
                    //newImage.gameObject.transform.Find("Tooltip").GetComponent<Image>().sprite = _turnTooltip;
                    break;
                case Card.CardName.Clear: //Error Case. Should not be used, but it can be used if needed
                    card.card = _clearCard;
                    break;
                case Card.CardName.Switch: //Error Case. Should not be used, but it can be used if needed
                    card.card = _switchCard;
                    break;
                case Card.CardName.Stall:
                    card.card = _stallCard;
                    break;
                case Card.CardName.Wild:
                    card.card = _wildCard;
                    break;
                default:
                    print("ERROR: COULD NOT UPDATE CARD IN UI");
                    break;
            }
        }
    }

    //Initializes helper variable
    Image _confirmationImage;
    CardDisplay _confirmationDisplay;
    Card _confirmCard;
    /// <summary>
    /// Creates a new instance of a card when a card is placed into the play area
    /// </summary>
    public void UpdateConfirmCard()
    {
        _arrowsManager.ChangeMaxIndex(numOfUniqueCards);
        //Makes sure a clear or switch card was not played when it wasn't supposed to be played
        if (_gameManager.confirmationCard != null)
        {
            _confirmCard = _gameManager.GetLastPlayedCard();

            //ERROR CHECK - They should already be deleted. If they haven't for whatever reason, delete them
            if (_confirmationImage != null)
                Destroy(_confirmationImage.gameObject);

            _confirmationImage = Instantiate(_confirmCardImage, Vector3.zero, Quaternion.identity); //Instantiates image
            _confirmationImage.transform.SetParent(_canvas.transform, false); //Sets canvas as the parent

            _confirmationImage.rectTransform.anchoredPosition = new Vector2(_screenWidth - 12 - cardWidth, 20);

            _confirmationImage.enabled = false; //Turns off highlight

            //Makes tooltip invisible
            _confirmationImage.gameObject.transform.Find("Tooltip").GetComponent<Image>().enabled = false;
            _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().enabled = false;

            _confirmationImage.gameObject.transform.Find("Tooltip").gameObject.transform.position =
                        new Vector2(_confirmationImage.gameObject.transform.Find("Tooltip").gameObject.transform.position.x - 25,
                        _confirmationImage.gameObject.transform.Find("Tooltip").gameObject.transform.position.y);

            _confirmationDisplay = _confirmationImage.GetComponentInChildren<CardDisplay>(); //Grabs data from image
            switch (_confirmCard.name)
            {
                case Card.CardName.Move:
                    _confirmationDisplay.card = _moveCard;
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "MOVE FORWARD ONE TILE.";
                    break;
                case Card.CardName.Jump:
                    _confirmationDisplay.card = _jumpCard;
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "MOVE FORWARD ONE TILE.\nCAN JUMP TO HIGHER GROUND.";
                    break;
                case Card.CardName.Turn: //Error Case. Should not be used, but it can be used if needed
                    _confirmationDisplay.card = _turnCard;
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "TURNS LEFT OR RIGHT.";
                    break;
                case Card.CardName.TurnLeft:
                    _confirmationDisplay.card = _turnLeftCard;
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "TURNS LEFT";
                    break;
                case Card.CardName.TurnRight:
                    _confirmationDisplay.card = _turnRightCard;
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "TURNS RIGHT.";
                    break;
                case Card.CardName.Clear:
                    _confirmationDisplay.card = _clearCard;
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "REMOVES ONE CARD FROM ACTION ORDER.";
                    break;
                case Card.CardName.Switch:
                    _confirmationDisplay.card = _switchCard;
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "SWAP TWO CARDS IN ACTION ORDER.";
                    break;
                case Card.CardName.Stall:
                    _confirmationDisplay.card = _stallCard;
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "REPEAT ACTION ORDER WITHOUT ADDING ANY CARD.";
                    break;
                case Card.CardName.Wild:
                    _confirmationDisplay.card = _wildCard;
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "CHOOSE ANY CARD TO PUT INTO THE ACTION ORDER.";
                    break;
                default:
                    print("ERROR: COULD NOT UPDATE CARD IN UI");
                    break;
            }
        }
    }

    /// <summary>
    /// Changes the confirm card to a different card determined by the index
    /// Main functionaility is to choose which card to play when Wild or Turn card is being played
    /// </summary>
    /// <param name="index">The card to switch the confirm card to</param>
    public void SetConfirmCard(int index)
    {
        //Makes sure a clear or switch card was not played when it wasn't supposed to be played
        if (_gameManager.confirmationCard != null)
        {
            Card card = _gameManager.GetLastPlayedCard();
            //ERROR CHECK - They should already be deleted. If they haven't for whatever reason, delete them
            if (_confirmationImage != null)
                Destroy(_confirmationImage.gameObject);

            _confirmationImage = Instantiate(_confirmCardImage, Vector3.zero, Quaternion.identity); //Instantiates image
            _confirmationImage.transform.SetParent(_canvas.transform, false); //Sets canvas as the parent

            _confirmationImage.rectTransform.anchoredPosition = new Vector2(_screenWidth - 12 - cardWidth, 20);

            _confirmationImage.enabled = false; //Turns off highlight

            //Makes tooltip invisible
            _confirmationImage.gameObject.transform.Find("Tooltip").GetComponent<Image>().enabled = false;
            _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().enabled = false;

            _confirmationImage.gameObject.transform.Find("Tooltip").gameObject.transform.position =
                        new Vector2(_confirmationImage.gameObject.transform.Find("Tooltip").gameObject.transform.position.x - 25,
                        _confirmationImage.gameObject.transform.Find("Tooltip").gameObject.transform.position.y);

            _confirmationDisplay = _confirmationImage.GetComponentInChildren<CardDisplay>(); //Grabs data from image
            switch (index)
            {
                case 0:
                    //Sets the confirm card
                    _confirmationDisplay.card = _moveCard;
                    _gameManager.confirmationCard = _moveCard;
                    _confirmCard = _moveCard;

                    //Sets gamestate to accomodate the new card
                    _gameManager.WildAction(_confirmCard);
                    _gameManager.RunPlaySequence();

                    //Updates UI
                    UpdatePlayedCards(_gameManager.GetPlayedCards());
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "MOVE FORWARD ONE TILE.";
                    break;
                case 1:
                    //Sets the confirm card
                    _confirmationDisplay.card = _jumpCard;
                    _gameManager.confirmationCard = _jumpCard;
                    _confirmCard = _jumpCard;

                    //Sets gamestate to accomodate the new card
                    _gameManager.WildAction(_confirmCard);
                    _gameManager.RunPlaySequence();

                    //Updates UI
                    UpdatePlayedCards(_gameManager.GetPlayedCards());
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "MOVE FORWARD ONE TILE.\nCAN JUMP TO HIGHER GROUND.";
                    break;
                case 2:
                    //Sets the confirm card
                    _confirmationDisplay.card = _turnLeftCard;
                    _gameManager.confirmationCard = _turnLeftCard;
                    _confirmCard = _turnLeftCard;

                    //Sets the gamestate to accomodate the new card
                    _gameManager.WildAction(_confirmCard);
                    _gameManager.RunPlaySequence();

                    //Updates UI
                    UpdatePlayedCards(_gameManager.GetPlayedCards());
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "TURNS LEFT";
                    break;
                case 3:
                    //Sets the confirm card
                    _confirmationDisplay.card = _turnRightCard;
                    _gameManager.confirmationCard = _turnRightCard;
                    _confirmCard = _turnRightCard;
                    //Sets gamestate to accomodate the new card
                    _gameManager.WildAction(_confirmCard);
                    _gameManager.RunPlaySequence();

                    //Updates UI
                    UpdatePlayedCards(_gameManager.GetPlayedCards());
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "TURNS RIGHT.";
                    break;
                case 4:
                    //Sets the confirm card
                    _confirmationDisplay.card = _clearCard;
                    _gameManager.confirmationCard = _clearCard;
                    _confirmCard = _clearCard;

                    //Sets gamestate to accomodate the new card
                    _gameManager.WildAction(_confirmCard);

                    //Updates UI
                    UpdatePlayedCards(_gameManager.GetPlayedCards());
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "REMOVES ONE CARD FROM ACTION ORDER.";
                    break;
                case 5:
                    //Sets the confirm card
                    _confirmationDisplay.card = _switchCard;
                    _gameManager.confirmationCard = _switchCard;
                    _confirmCard = _switchCard;

                    ////Sets gamestate to accomodate the new card
                    _gameManager.WildAction(_confirmCard);

                    //Updates UI
                    UpdatePlayedCards(_gameManager.GetPlayedCards());
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "SWAP TWO CARDS IN ACTION ORDER.";
                    break;
                case 6:
                    //Sets the confirm card
                    _confirmationDisplay.card = _stallCard;
                    _gameManager.confirmationCard = _stallCard;
                    _confirmCard = _stallCard;

                    //Sets gamestate to accomodate the new card
                    _gameManager.WildAction(_confirmCard);
                    _gameManager.RunPlaySequence();

                    //Updates UI
                    UpdatePlayedCards(_gameManager.GetPlayedCards());
                    _confirmationImage.GetComponentInChildren<TextMeshProUGUI>().text = "REPEAT ACTION ORDER WITHOUT ADDING ANY CARD.";
                    break;
                default:
                    print("ERROR: COULD NOT UPDATE CARD IN UI");
                    break;
            }
        }
    }

    /// <summary>
    /// Destroys the confirm card
    /// </summary>
    public void DestroyConfirmCard()
    {
        //Disables buttons
        confirmButton.GetComponent<ConfirmationControls>().SetIsActive(false);
        cancelButton.GetComponent<ConfirmationControls>().SetIsActive(false);

        if (_confirmationImage != null)
            Destroy(_confirmationImage.gameObject);
    }

    /// <summary>
    /// Displays or hides the arrows to change the confirm card
    /// </summary>
    public void UpdateArrows()
    {
        //If the gamestate is on wild, show the arrows
        if (_gameManager.isUsingWild)
        {
            _leftButton.gameObject.SetActive(true);
            _rightButton.gameObject.SetActive(true);
        }
        else
        {
            _leftButton.gameObject.SetActive(false);
            _rightButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Updates how many collectables the player has attained
    /// </summary>
    public void UpdateCollectables()
    {
        int numOfCollectables = _gameManager.GetCollectableCount();
        _collectablesCount.text = numOfCollectables.ToString();
    }

    //Initialzes helper variable
    private Image _leftImage, _rightImage;
    /// <summary>
    /// Creates interactable Turn Cards to select which direction the player will turn
    /// </summary>
    public void CreateTurnCards()
    {
        _arrowsManager.ChangeMaxIndex(2);
        //ERROR CHECK - They should already be deleted. If they haven't for whatever reason, delete them

        if (_leftImage != null)
            Destroy(_leftImage.gameObject);
        if (_rightImage != null)
            Destroy(_rightImage.gameObject);

        _leftImage = Instantiate(_turnLeftImage, Vector3.zero, Quaternion.identity); //Instantiates new card
        _leftImage.transform.SetParent(_canvas.transform, false); //Sets canvas as its parent
        _leftImage.rectTransform.anchoredPosition = new Vector2(_screenWidth - cardWidth * 4 - _dealtCardWidthSpacing, 0); //Sets position
        _rightImage = Instantiate(_turnRightImage, Vector3.zero, Quaternion.identity); //Instantiates new card
        _rightImage.transform.SetParent(_canvas.transform, false); //Sets canvas as its parent
        _rightImage.rectTransform.anchoredPosition = new Vector2(_screenWidth - cardWidth * 3, 0); //Sets position

        CardDisplay leftCard = _leftImage.GetComponent<CardDisplay>(); //Grabs data from image

        //Disables tooltips

        _leftImage.gameObject.transform.Find("Tooltip").GetComponent<Image>().enabled = false;
        _leftImage.GetComponentInChildren<TextMeshProUGUI>().enabled = false;

        _rightImage.gameObject.transform.Find("Tooltip").GetComponent<Image>().enabled = false;
        _rightImage.GetComponentInChildren<TextMeshProUGUI>().enabled = false;

        //Uses grabbed data to compare with possible types and convert image to found type
        leftCard.card = _turnLeftCard;

        CardDisplay rightCard = _rightImage.GetComponent<CardDisplay>(); //Grabs data from image

        //Uses grabbed data to compare with possible types and convert image to found type
        rightCard.card = _turnRightCard;
    }

    /// <summary>
    /// Destroys the turn cards after one is selected
    /// </summary>
    /// <param name="wasTurnLeftChosen">If the left turn card was selected or not</param>
    public void DestroyTurnCards(bool wasTurnLeftChosen)
    {
        //Destroys game objects
        if (_leftImage != null)
            Destroy(_leftImage.gameObject);
        if (_rightImage != null)
            Destroy(_rightImage.gameObject);

        //Player is turning left
        if (wasTurnLeftChosen)
        {
            _gameManager.AddTurnCard(_turnLeftCard, true);
        }
        //Player is turning right
        else
        {
            _gameManager.AddTurnCard(_turnRightCard, false);
        }
    }

    public void DestroyTurnCards()
    {
        //Destroys game objects
        if (_leftImage != null)
            Destroy(_leftImage.gameObject);
        if (_rightImage != null)
            Destroy(_rightImage.gameObject);
    }

    public void ShowDeck(bool isShowingDeck)
    {
        //TODO - Switch Order. Figure out how many instances are in the deck, THEN instantiate the images.
        //Set numOfCardsToShowInDeck to equal the number of cards with 0 instances
        //If number of instances == 0, decrease i
        //Make new indexer for switch statement
        if (isShowingDeck)
        {

            //ERROR CHECK - This should already be done
            foreach (Image image in _shownDeck)
            {
                if (image != null)
                    Destroy(image.gameObject);
            }
            _gameManager.deckShownDarken.enabled = true;

            //Gets all dealt cards
            List<Card> startingCards = _gameManager.GetStartingCards();

            int numOfCardsToShow = 0;
            int[] instances = new int[numOfUniqueCards];

            //Gets the number of cards in the deck, for each card
            for (int j = 0; j < startingCards.Count; j++)
            {
                switch (startingCards[j].name)
                {
                    case Card.CardName.Move:
                        instances[0]++;
                        break;
                    case Card.CardName.Turn:
                        instances[1]++;
                        break;
                    case Card.CardName.Jump:
                        instances[2]++;
                        break;
                    case Card.CardName.Stall:
                        instances[3]++;
                        break;
                    case Card.CardName.Clear:
                        instances[4]++;
                        break;
                    case Card.CardName.Switch:
                        instances[5]++;
                        break;
                    case Card.CardName.Wild:
                        instances[6]++;
                        break;
                }
            }

            //Gets how many cards to show
            for (int i = 0; i < numOfUniqueCards; i++)
            {
                if (instances[i] > 0)
                    numOfCardsToShow++;
            }

            //Shows Cards
            int indexPosition = 0;
            for (int i = 0; i < numOfUniqueCards; i++)
            {
                print(numOfCardsToShow);
                if (instances[i] > 0)
                {
                    Image newImage = Instantiate(_deckShownImage, Vector3.zero, Quaternion.identity); //Instantiates new card
                    newImage.transform.SetParent(_canvas.transform, false); //Sets canvas as its parent

                    //Centers Image Horizontally
                    if (numOfCardsToShow % 2 == 0) // Number of Cards to Show is Even
                    {
                        float x = _screenWidth / 2;

                        if (indexPosition < numOfCardsToShow / 2)
                        {
                            newImage.rectTransform.anchoredPosition = new Vector3((cardWidth + _dealtCardWidthSpacing + 50) *
                                -(numOfCardsToShow / 2 - indexPosition) + x, 650, 0); //Sets position
                            
                        }
                        else
                        {
                            newImage.rectTransform.anchoredPosition = new Vector3((cardWidth + _dealtCardWidthSpacing + 50) *
                                ((indexPosition - numOfCardsToShow / 2)) + x, 650, 0); //Sets position
                        }
                    }
                    else // Number of Cards To Show is Odd
                    {
                        float x = _screenWidth / 2 - cardWidth / 2;

                        if (i < numOfCardsToShow / 2)
                        {
                            newImage.rectTransform.anchoredPosition = new Vector3((cardWidth + _dealtCardWidthSpacing + 50) *
                                -(numOfCardsToShow / 2 - indexPosition) + x, 650, 0); //Sets position
                        }
                        else if (indexPosition > numOfCardsToShow / 2)
                        {
                            newImage.rectTransform.anchoredPosition = new Vector3((cardWidth + _dealtCardWidthSpacing + 50) *
                                ((indexPosition - numOfCardsToShow / 2)) + x, 650, 0); //Sets position
                        }
                        else
                        {
                            newImage.rectTransform.anchoredPosition = new Vector3((cardWidth + _dealtCardWidthSpacing + 50) * 0 +
                                x, 650, 0); //Sets position
                        }
                    }

                    _shownDeck.Add(newImage); //Adds instantiated image to list

                    newImage.GetComponentInChildren<TextMeshProUGUI>().text = instances[i].ToString();

                    //Shows Card Sprite
                    CardDisplay card = newImage.GetComponentInChildren<CardDisplay>(); //Gets data from image
                    switch (i)
                    {
                        case 0: //Move
                            card.card = _moveCard;
                            break;
                        case 1: //Turn
                            card.card = _turnCard;
                            break;
                        case 2: //Jump
                            card.card = _jumpCard;
                            break;
                        case 3: //Replay
                            card.card = _stallCard;
                            break;
                        case 4: //Clear
                            card.card = _clearCard;
                            break;
                        case 5: //Switch
                            card.card = _switchCard;
                            break;
                        case 6: //Wild
                            card.card = _wildCard;
                            break;
                        default:
                            print("ERROR: COULD NOT UPDATE CARD IN UI");
                            break;
                    }
                }
                else
                {
                    //There are no instances of this card. Do not show it and reduce position index
                    indexPosition--;
                }
                indexPosition++;
            }

            /**
            for (int i = 0; i < numOfCardsToShowInDeck; i++)
            {
                Image newImage = Instantiate(_deckShownImage, Vector3.zero, Quaternion.identity); //Instantiates new card
                newImage.transform.SetParent(_canvas.transform, false); //Sets canvas as its parent

                //Centers Image Horizontally
                if (numOfCardsToShowInDeck % 2 == 0) // Number of Unique Cards is Even
                {
                    float x = _screenWidth / 2;

                    if (i < numOfCardsToShowInDeck / 2)
                    {
                        newImage.rectTransform.anchoredPosition = new Vector3((cardWidth + _dealtCardWidthSpacing + 50) *
                            -(numOfCardsToShowInDeck / 2 - i) + x, 650, 0); //Sets position
                    }
                    else
                    {
                        newImage.rectTransform.anchoredPosition = new Vector3((cardWidth + _dealtCardWidthSpacing + 50) *
                            ((i - numOfCardsToShowInDeck / 2)) + x, 650, 0); //Sets position
                    }
                }
                else // Number of Unqiue Cards is Odd
                {
                    float x = _screenWidth / 2 - cardWidth / 2;

                    if (i < numOfCardsToShowInDeck / 2)
                    {
                        newImage.rectTransform.anchoredPosition = new Vector3((cardWidth + _dealtCardWidthSpacing + 50) * 
                            -(numOfCardsToShowInDeck / 2 - i) + x, 650, 0); //Sets position
                    }
                    else if (i > numOfCardsToShowInDeck / 2)
                    {
                        newImage.rectTransform.anchoredPosition = new Vector3((cardWidth + _dealtCardWidthSpacing + 50) * 
                            ((i - numOfCardsToShowInDeck / 2)) + x, 650, 0); //Sets position
                    }
                    else
                    {
                        newImage.rectTransform.anchoredPosition = new Vector3((cardWidth + _dealtCardWidthSpacing + 50) * 0 +
                            x, 650, 0); //Sets position
                    }
                }

                _shownDeck.Add(newImage); //Adds instantiated image to list

                CardDisplay card = newImage.GetComponentInChildren<CardDisplay>(); //Gets data from image

                int numOfInstances = 0;
                //Finds the name and sets the image to the found data

                switch (i)
                {
                    case 0: //Move
                        card.card = _moveCard;

                        foreach (Card cardObject in startingCards)
                        {
                            if (cardObject.name == Card.CardName.Move)
                                numOfInstances++;
                        }
                        newImage.GetComponentInChildren<TextMeshProUGUI>().text = numOfInstances.ToString();
                        break;
                    case 1: //Turn
                        card.card = _turnCard;

                        foreach (Card cardObject in startingCards)
                        {
                            if (cardObject.name == Card.CardName.Turn)
                                numOfInstances++;
                        }
                        newImage.GetComponentInChildren<TextMeshProUGUI>().text = numOfInstances.ToString();
                        break;
                    case 2: //Jump
                        card.card = _jumpCard;

                        foreach (Card cardObject in startingCards)
                        {
                            if (cardObject.name == Card.CardName.Jump)
                                numOfInstances++;
                        }
                        newImage.GetComponentInChildren<TextMeshProUGUI>().text = numOfInstances.ToString();
                        break;
                    case 3: //Replay
                        card.card = _stallCard;

                        foreach (Card cardObject in startingCards)
                        {
                            if (cardObject.name == Card.CardName.Stall)
                                numOfInstances++;
                        }
                        newImage.GetComponentInChildren<TextMeshProUGUI>().text = numOfInstances.ToString();
                        break;
                    case 4: //Clear
                        card.card = _clearCard;

                        foreach (Card cardObject in startingCards)
                        {
                            if (cardObject.name == Card.CardName.Clear)
                                numOfInstances++;
                        }
                        newImage.GetComponentInChildren<TextMeshProUGUI>().text = numOfInstances.ToString();
                        break;
                    case 5: //Switch
                        card.card = _switchCard;

                        foreach (Card cardObject in startingCards)
                        {
                            if (cardObject.name == Card.CardName.Switch)
                                numOfInstances++;
                        }
                        newImage.GetComponentInChildren<TextMeshProUGUI>().text = numOfInstances.ToString();
                        break;
                    case 6: //Wild
                        card.card = _wildCard;

                        foreach (Card cardObject in startingCards)
                        {
                            if (cardObject.name == Card.CardName.Wild)
                                numOfInstances++;
                        }
                        newImage.GetComponentInChildren<TextMeshProUGUI>().text = numOfInstances.ToString();
                        break;
                    default:
                        print("ERROR: COULD NOT UPDATE CARD IN UI");
                        break;
                }
            }
        */
        }
        else
        {
            _gameManager.deckShownDarken.enabled = false;
            foreach (Image image in _shownDeck)
            {
                if (image != null)
                    Destroy(image.gameObject);
            }
        }
    }

    /// <summary>
    /// When confirm is clicked, moves the card from the play area to the action order
    /// </summary>
    public void MoveCardToActionOrder()
    {
        StartCoroutine(MoveCard(_confirmationImage, _nextPlayCardPosition.y));
    }

    /// <summary>
    /// Updates the text box text
    /// </summary>
    public void UpdateTextBox(string text)
    {
        _upperTextBox.enabled = true;
        _upperTextBox.GetComponentInChildren<TextMeshProUGUI>().enabled = true;
        _upperTextBox.GetComponentInChildren<TextMeshProUGUI>().text = text;
    }

    /// <summary>
    /// Disables the text box and its text
    /// </summary>
    public void DisableTextBox()
    {
        _upperTextBox.enabled = false;
        _upperTextBox.GetComponentInChildren<TextMeshProUGUI>().enabled = false;
        _upperTextBox.GetComponentInChildren<TextMeshProUGUI>().text = "null";
    }

    /// <summary>
    /// Returns the instantiated dealt card images
    /// </summary>
    /// <returns>List<Image> a list of images containing the instantiated dealt cards</returns>
    public List<Image> GetInstantiatedDealtCardImages() { return _dealtCardImages; }

    public List<Image> GetInstantiatedPlayedCardImages() {  return _playedCardImages; }

    public IEnumerator MoveCard(Image image, float targetYPosition)
    {
        while (image.rectTransform.anchoredPosition.y != targetYPosition)
        {
            //Moves card
            image.rectTransform.anchoredPosition = Vector2.MoveTowards(image.rectTransform.anchoredPosition, new Vector2(_screenWidth - cardWidth / 2 - _widthPadding, targetYPosition), 12f);

            //Shrinks x value
            if(image.gameObject.transform.GetChild(0).GetComponent<Image>().rectTransform.sizeDelta.x > _playedCardImage.rectTransform.sizeDelta.x)
            {
                image.gameObject.transform.GetChild(0).GetComponent<Image>().rectTransform.sizeDelta -= new Vector2(2f, 0);
                image.rectTransform.position += new Vector3(0.25f, 0);
                if (image.gameObject.transform.GetChild(0).GetComponent<Image>().rectTransform.sizeDelta.x < _playedCardImage.rectTransform.sizeDelta.x)
                    image.gameObject.transform.GetChild(0).GetComponent<Image>().rectTransform.sizeDelta = new Vector2(_playedCardImage.rectTransform.sizeDelta.x, 
                        image.gameObject.transform.GetChild(0).GetComponent<Image>().rectTransform.sizeDelta.y);
            }

            //Shrinks Y value
            if (image.gameObject.transform.GetChild(0).GetComponent<Image>().rectTransform.sizeDelta.y > _playedCardImage.rectTransform.sizeDelta.y)
            {
                image.gameObject.transform.GetChild(0).GetComponent<Image>().rectTransform.sizeDelta -= new Vector2(0, 2f);
                image.rectTransform.position += new Vector3(0, 0.25f);
                if (image.gameObject.transform.GetChild(0).GetComponent<Image>().rectTransform.sizeDelta.y < _playedCardImage.rectTransform.sizeDelta.y)
                    image.gameObject.transform.GetChild(0).GetComponent<Image>().rectTransform.sizeDelta = new Vector2(image.gameObject.transform.GetChild(0).GetComponent<Image>().rectTransform.sizeDelta.x, 
                        _playedCardImage.rectTransform.sizeDelta.y);
            }

            yield return new WaitForEndOfFrame();
        }
        UpdatePlayedCards(_gameManager.GetPlayedCards());
        _gameManager.PlaySequence();
        DestroyConfirmCard();
    }

}