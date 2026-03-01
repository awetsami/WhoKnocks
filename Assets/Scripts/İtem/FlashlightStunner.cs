using UnityEngine;

public class FlashlightStunner : MonoBehaviour
{
    [Header("Fener Ayarları")]
    public Light myFlashlight;
    public float stunRange = 15f;
    public float stunRadius = 2f;
    public float stunDuration = 3f;

    void Update()
    {
        if (myFlashlight != null && myFlashlight.enabled)
        {
            RaycastHit hit;
            // DİKKAT: En sondaki "~0" komutu "Bütün Layer'ları Tara (Ignore Raycast dahil)" demektir!
            // Bu sayede görünmez/hayalet layer'daki düşmanı da dondurur.
            if (Physics.SphereCast(transform.position, stunRadius, transform.forward, out hit, stunRange, ~0))
            {
                EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
                if (enemy != null)
                {
                    enemy.StunEnemy(stunDuration);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward * stunRange, stunRadius);
    }
}