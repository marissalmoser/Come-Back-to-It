/******************************************************************
*    Author: Sky Turner
*    Contributors: 
*    Date Created: 9/11/24
*    Description: This script handles the pause menu
*******************************************************************/

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private GameObject _pauseMenu;
    [SerializeField] private GameObject _collectibleUI;
    private bool _isPaused = false;

    private void Start()
    {
        // Subscribe to input events
        _playerInput.currentActionMap["Pause"].performed += ctx =>
            TogglePause();
        StartCoroutine(DisableCollectibleOnStart());
    }
    
    /// <summary>
    /// Handles toggling the pause menu
    /// </summary>
    public void TogglePause()
    {
        _isPaused = !_isPaused;

        if (_isPaused && _pauseMenu != null)
        {
            _pauseMenu.SetActive(true);
            _collectibleUI.SetActive(true);
            Time.timeScale = 0f;
        }
        else if(!_isPaused && _pauseMenu != null)
        {
            _pauseMenu.SetActive(false);
            _collectibleUI.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// Restarts the level
    /// </summary>
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1;
    }

    public void SetSfxVolume(float volume)
    {
        SfxManager.Instance.SetSfxMixerVolume(volume);
    }

    public void SetMusicVolume(float volume)
    {
        MusicManager.Instance.SetMusicMixerVolume(volume);
    }

    private IEnumerator DisableCollectibleOnStart()
    {
        yield return new WaitForSeconds(0.01f);

        _collectibleUI.SetActive(false);
    }
}
