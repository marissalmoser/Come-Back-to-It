// +--------------------------------------------------------------+
// @author - Ryan Herwig
// @Contributers - 
// @Last modified - September 9 2024
// @Description - Displays the card onto an instantiated image.
//                Also holds helper methods for an Event Trigger
// +--------------------------------------------------------------+

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    public Card card;
    [SerializeField] private Image _sprite;
    public int ID;
    public bool IsMouseInCard { get;  private set; }
    public bool IsMouseDown { get; private set; }
    public bool IsClearing { get; private set; }
    public bool IsSwapping { get; private set; }

    private GameManager _gameManager;

    private Animator _anim;
    private bool _isSelected;
    private bool _isDragging;

    void Start()
    {
        _gameManager = GameManager.Instance;
        _anim = GetComponentInParent<Animator>();
        _sprite.sprite = card.cardSprite;

        IsMouseInCard = false;
        IsMouseDown = false;
        IsSwapping = false;
    }
    /// <summary>
    /// Updates the specified card's image
    /// </summary>
    /// <param name="card">The card to be updared</param>
    public void UpdateCard(Card card)
    {
        this.card = card;

        _gameManager = GameManager.Instance;
    }

    #region Deck Methods
    public void MousePressedDeck()
    {
        CardManager.Instance.MousePressedDeck();
    }

    public void MouseReleasedDeck()
    {
        CardManager.Instance.MouseReleasedDeck();
    }
    #endregion

    #region Dealt Card Methods

    /// <summary>
    /// Helper method for Event Trigger Pointer Down for DealtCards
    /// </summary>
    /// <param name="Card">Image object for the card</param>
    public void MouseEnterDealtCard(Image tooltip)
    {
        IsMouseInCard = true;
        CardManager.Instance.DealtMouseEnterCard(tooltip);

        //plays pop up animation if card is playable
        if (_anim != null && CardIsPlayable())
        {
            _anim.SetBool("Hover", true);
        }
    }

    /// <summary>
    /// Helper method for Event Trigger Pointer Down for DealtCards
    /// </summary>
    /// <param name="Card">Image object for the card</param>
    public void MouseExitDealtCard(Image tooltip)
    {
        IsMouseInCard = false;
        CardManager.Instance.DealtMouseExitCard(tooltip);

        //plays pop up animation if card is playable
        if (_anim != null && CardIsPlayable())
        {
            _anim.SetBool("Hover", false);
        }
    }
    /// <summary>
    /// Helper method for Event Trigger Pointer Down for DealtCards
    /// </summary>
    /// <param name="Card">Image object for the card</param>
    public void MousePressedDealtCard(Image Card)
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            IsMouseDown = true;
            CardManager.Instance.DealtMousePressedCard(Card);

            //double click to play functionality if the card is playable.
            if(CardIsPlayable())
            {
                SelectCard(Card);
            }
        }
    }

    /// <summary>
    /// Called when the player clicks on a dealt card that can be played. If it is selected,
    /// playes tha card, it it is not already selected, selects it.
    /// </summary>
    /// <param name="Card"></param>
    private void SelectCard(Image Card)
    {
        if (_isSelected)
        {
            //play card and sound
            CardManager.Instance.PlayCard(Card, ID);
            SfxManager.Instance.PlaySFX(4295);

            //TODO: move card to play area

            return;
        }

        //if card is not active yet, deselect any other active dealt cards
        //and make this one active and play anim and sound.
        if(CardIsPlayable())
        {
            foreach (Image dealtCard in UIManager.Instance.GetInstantiatedDealtCardImages())
            {
                dealtCard.GetComponentInChildren<CardDisplay>().SetIsSelected(false);
            }
            if(!_isDragging)
                _anim.SetBool("Select", true);
            _isSelected = true;
            SfxManager.Instance.PlaySFX(1092);
        }
    }

    /// <summary>
    /// Sets the dealt card display and animation for the double click to play functionality.
    /// </summary>
    /// <param name="input"></param>
    public void SetIsSelected(bool input)
    {
        if(_isSelected && !input)
        {
            _anim.SetBool("Select", false);
        }
        if(!_isSelected && input)
        {
            _anim.SetBool("Select", true);
        }
        _isSelected = input;
    }

    /// <summary>
    /// Retrns a bool that checks if the card is playable or not. Used to see if the
    /// animations should play or not.
    /// </summary>
    /// <returns></returns>
    private bool CardIsPlayable()
    {
        if (card.name == Card.CardName.Clear && UIManager.Instance.GetInstantiatedPlayedCardImages().Count < 1)
            return false;
        if (card.name == Card.CardName.Switch && UIManager.Instance.GetInstantiatedPlayedCardImages().Count < 2)
            return false;

        return true;
    }

    /// <summary>
    /// Helper method for Event Trigger Pointer Up for Dealt Cards
    /// </summary>
    /// <param name="Card">Image object for the card</param>
    public void MouseReleasedDealtCard(Image Card)
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            IsMouseDown = false;
            CardManager.Instance.DealtMouseReleasedCard(Card, ID);

            //disable animator to allow drag
            if(_anim != null)
            {
                _anim.enabled = true;
            }
        }
    }

    /// <summary>
    /// Helper method for Event Trigger Drag for Dealt Cards
    /// </summary>
    /// <param name="Card">Image object for the card</param>
    public void OnDragDealtCard(Image Card)
    {
        if (Mouse.current.leftButton.isPressed && CardIsPlayable())
        {
            CardManager.Instance.DealtOnDragCard(Card);

            //disable animator to allow drag
            if (_anim != null)
            {
                _anim.enabled = false;
            }
        }
    }

    /// <summary>
    /// Helper method for choosing to turn left
    /// </summary>
    public void TurnLeftChosen()
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            CardManager.Instance.PlayedTurnChooseLeft();
        }
    }

    /// <summary>
    /// Helper method for choosing to turn right
    /// </summary>
    public void TurnRightChosen()
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            CardManager.Instance.PlayedTurnChooseRight();
        }
    }

    /// <summary>
    /// Helper method for mouse entering turn card
    /// </summary>
    public void MouseEnterTurnCard(Image tooltip)
    {
        CardManager.Instance.MouseEnterTurnCard(tooltip);
    }

    /// <summary>
    /// Helper method for mouse leaving turn card
    /// </summary>
    public void MouseExitTurnCard(Image tooltip)
    {
        CardManager.Instance.MouseExitTurnCard(tooltip);
    }
    #endregion

    #region Played Card Methods

    /// <summary>
    /// Helper method for Event Trigger Mouse Down for Played Cards
    /// </summary>
    /// <param name="Card">Image object for the card</param>
    public void MousePressedPlayedCard(Image Card)
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            IsMouseDown = true;
            CardManager.Instance.PlayedMousePressedCard(Card);
        }
    }

    /// <summary>
    /// Helper method for Event Trigger Mouse Up for Played Cards
    /// </summary>
    /// <param name="Card">Image object for the card</param>
    public void MouseReleasedPlayedCard(Image Card)
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            IsMouseDown = false;

            if (_gameManager.isSwitching)
                IsSwapping = !IsSwapping;
            if (_gameManager.isClearing)
                IsClearing = !IsClearing;
        }

        CardManager.Instance.PlayedMouseReleasedCard(Card);
    }

    /// <summary>
    /// Helper method for Event Trigger Pointer Enter for Played Cards
    /// </summary>
    /// <param name="card">Image object for the card</param>
    public void OnMouseEnterPlayedCard(Image card)
    {
        IsMouseInCard = true;
        CardManager.Instance.PlayedMouseEnterCard(card);
    }

    /// <summary>
    /// Helper method for Event Trigger Pointer Exit for Played Cards
    /// </summary>
    /// <param name="card">Image object for the card</param>
    public void OnMouseExitPlayedCard(Image card)
    {
        IsMouseInCard = false;
        CardManager.Instance.PlayedMouseExitCard(card);
    }
    #endregion
}