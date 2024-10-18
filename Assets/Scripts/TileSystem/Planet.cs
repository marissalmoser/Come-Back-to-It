using UnityEngine;
using UnityEngine.SceneManagement;

public class Planet : Collectable
{
    GameManager gameManager;
    private CollectibleManager _collectibleManager;
    private UIManager _uiManager;


    private void Start()
    {
        gameManager = GameManager.Instance;
        _collectibleManager = CollectibleManager.Instance;
        _uiManager = UIManager.Instance;
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            //sound effect caller
            SfxManager.Instance.PlaySFX(8346);

            _collectibleManager.CollectCollectible();
            _uiManager.UpdateLevelCollectibles();
            Destroy(gameObject);
        }
    }
}
