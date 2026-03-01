using UnityEngine;

public class Revolver : MonoBehaviour
{
    [Header("Silah Ayarlarý")]
    public float damage = 40f;      // Tek merminin hasarý
    public float range = 50f;       // Merminin menzili
    public Transform playerCamera;  // Oyuncunun kamerasý (Iþýn buradan çýkacak)
    public GameObject hitEffect;    // (Opsiyonel) Vurulan yerde çýkacak kývýlcým

    [Header("Mermi Yuvalarý (6 Slot)")]
    public bool[] chambers = new bool[6]; // True: Dolu, False: Boþ

    [Header("Durum")]
    public int currentChamber = 0; // Namludaki yuva

    public void Fire()
    {
        if (chambers[currentChamber])
        {
            Debug.Log("BANG! (Yuva " + currentChamber + ")");
            chambers[currentChamber] = false; // Mermi gitti

            // GERÇEK HASAR ÝÞLEMÝ (Iþýn Yollama)
            ShootRaycast();
        }
        else
        {
            Debug.Log("Klik... (Yuva " + currentChamber + " boþ)");
        }

        RotateCylinder();
    }

    void ShootRaycast()
    {
        // Kamera atanmadýysa ana kamerayý otomatik bul
        if (playerCamera == null) playerCamera = Camera.main.transform;

        RaycastHit hit;
        // Kameranýn tam ortasýndan ileriye ýþýn yolla
        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, range))
        {
            // Vurduðumuz þey Düþman mý?
            EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage); // 40 Hasar Ver!
            }

            // (Opsiyonel) Duvara çarparsa efekt çýksýn
            if (hitEffect != null)
            {
                Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
    }

    public void RotateCylinder()
    {
        currentChamber++;
        if (currentChamber >= 6) currentChamber = 0;
    }

    public void SpinCylinder()
    {
        currentChamber = Random.Range(0, 6);
        Debug.Log("Silindir döndü. Þanslý yuva: " + currentChamber);
    }

    public bool LoadBullet()
    {
        for (int i = 0; i < 6; i++)
        {
            if (chambers[i] == false)
            {
                chambers[i] = true;
                Debug.Log("Mermi yuvaya (Slot " + i + ") yüklendi. Klik sesi.");
                return true;
            }
        }
        Debug.Log("Silah tamamen dolu! Daha fazla almaz.");
        return false;
    }
}