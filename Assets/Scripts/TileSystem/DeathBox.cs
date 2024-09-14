using UnityEngine;

public class DeathBox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            print("DIED");
            GameManager.Instance.ChangeGameState(GameManager.STATE.Death);
        }
    }
}