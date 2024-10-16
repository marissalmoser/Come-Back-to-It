/******************************************************************
*    Author: Sky Turner
*    Contributors: 
*    Date Created: 9/2/24
*    Description: This script handles the camera movement
*******************************************************************/

using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private CinemachineVirtualCamera _virtualCamera;
    private Coroutine _cameraMovementCoroutine;
    [SerializeField] private float _cameraSpeedMult;

    private void OnEnable()
    {
        // Store the action map reference
        var actionMap = _playerInput.currentActionMap;

        // Subscribe to input events
        actionMap["PanCamera"].started += PanCamera;
        actionMap["PanCamera"].canceled += PanCameraCanceled;

        actionMap["DragCamera"].performed += DragCamera;
    }

    private void OnDisable()
    {
        var actionMap = _playerInput.currentActionMap;

        // Unsubscribe from input events
        actionMap["PanCamera"].started -= PanCamera;
        actionMap["PanCamera"].canceled -= PanCameraCanceled;

        actionMap["DragCamera"].performed -= DragCamera;
    }

    /// <summary>
    /// Initiates camera panning based on player input.
    /// </summary>
    private void PanCamera(InputAction.CallbackContext ctx)
    {
        // Start moving the camera with the current input
        float panInput = ctx.ReadValue<float>();

        // Stop any ongoing camera movement
        if (_cameraMovementCoroutine != null)
        {
            StopCoroutine(_cameraMovementCoroutine);
        }

        _cameraMovementCoroutine = StartCoroutine(MoveCamera(panInput));
    }

    /// <summary>
    /// Stops camera panning when input is canceled.
    /// </summary>
    private void PanCameraCanceled(InputAction.CallbackContext ctx)
    {
        // Stop camera movement when input is canceled
        if (_cameraMovementCoroutine != null)
        {
            StopCoroutine(_cameraMovementCoroutine);
            _cameraMovementCoroutine = null; // Clear reference
        }
    }

    /// <summary>
    /// Moves the camera along the Cinemachine Dolly path based on direction.
    /// </summary>
    /// <param name="direction">Direction input for camera movement.</param>
    private IEnumerator MoveCamera(float direction)
    {
        CinemachineTrackedDolly dolly = _virtualCamera.GetCinemachineComponent
            <CinemachineTrackedDolly>();
        float movementSpeed = direction * _cameraSpeedMult;

        while (Mathf.Abs(direction) > 0.01f)
        {
            dolly.m_PathPosition += movementSpeed * Time.deltaTime * -1;
            direction = _playerInput.currentActionMap["PanCamera"].ReadValue<float>();
            yield return null;
        }
        _cameraMovementCoroutine = null; // Clear reference when done
    }

    /// <summary>
    /// Initiates camera movement based on mouse input
    /// </summary>
    /// <param name="ctx"></param>
    private void DragCamera(InputAction.CallbackContext ctx)
    {
        Vector2 mouseDelta = ctx.ReadValue<Vector2>();
        MoveCameraWithMouse(mouseDelta.x);
    }

    /// <summary>
    /// Moves the camera along the cinemachine dolly
    /// </summary>
    /// <param name="deltaX"></param>
    private void MoveCameraWithMouse(float deltaX)
    {
        if (_virtualCamera == null) return;

        CinemachineTrackedDolly dolly = _virtualCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        dolly.m_PathPosition += deltaX * _cameraSpeedMult * Time.deltaTime * -1;
    }
}
