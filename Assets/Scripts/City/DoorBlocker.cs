using UnityEngine;

public class DoorBlocker : MonoBehaviour
{
    private Collider wallCollider;

    void Start() { wallCollider = GetComponent<Collider>(); }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // Gündüz katı duvar, gece içinden geçilebilir
        wallCollider.isTrigger = (GameManager.Instance.currentPhase == DayPhase.Nighttime);

    }
}