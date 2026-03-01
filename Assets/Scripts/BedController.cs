using UnityEngine;

public class BedController : MonoBehaviour
{
    public void Sleep()
    {
        if (GameManager.Instance != null)
        {
            // Eski: ToggleSleepState
            // Yeni: TryToSleep (Soru sorar)
            GameManager.Instance.TryToSleep();
        }
    }
}