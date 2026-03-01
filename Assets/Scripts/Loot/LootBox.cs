using UnityEngine;

public class LootBox : MonoBehaviour
{
    [Header("Sandık Ayarları")]
    public GameObject[] possibleLootPrefabs; // İçinden çıkabilecek eşyaların prefabları
    public int minLoot = 1;
    public int maxLoot = 3;

    public bool isOpened = false;

    public void OpenBox()
    {
        if (isOpened) return;
        isOpened = true;

        int lootCount = Random.Range(minLoot, maxLoot + 1);

        for (int i = 0; i < lootCount; i++)
        {
            // Rastgele bir prefab seç
            GameObject prefabToSpawn = possibleLootPrefabs[Random.Range(0, possibleLootPrefabs.Length)];

            // Sandığın biraz üstünde oluştur
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            GameObject lootObj = Instantiate(prefabToSpawn, spawnPos, Random.rotation);

            // İçinden fırlama efekti ver
            Rigidbody rb = lootObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 jumpForce = Vector3.up * 4f + Random.insideUnitSphere * 1.5f;
                rb.AddForce(jumpForce, ForceMode.Impulse);
            }
        }

        // İstersen burada sandığın kapağını açan bir animasyon veya "Açıldı" meteryali tetikleyebilirsin
        Debug.Log("Sandık açıldı!");
    }
}