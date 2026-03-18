using UnityEngine;
using System.Collections.Generic;

public class PantryShelf : MonoBehaviour
{
    public static PantryShelf Instance;

    [Header("Grid Ayarları")]
    public List<Transform> allSlots = new List<Transform>(); // Editor'den sürükle
    private Dictionary<Transform, GameObject> slotData = new Dictionary<Transform, GameObject>();
    


    void Awake()
    {
        Instance = this;
        foreach (var slot in allSlots) slotData[slot] = null;
    }

    // YENİ OYUN: Yemekleri slotlara pıt pıt yerleştirir
    public void SpawnInitialFood(int count)
    {
        GameObject foodPrefab = null;

        foreach (GameObject prefab in GameManager.Instance.allItemPrefabs)
        {
            LootableItem lootScript = prefab.GetComponent<LootableItem>();

            // Hem scriptin olduğundan hem de veri atanmış olduğundan emin oluyoruz
            if (lootScript != null && lootScript.itemData != null)
            {
                if (lootScript.itemData.category == ItemCategory.Food)
                {
                    foodPrefab = prefab;
                    break;
                }
            }
        }

        if (foodPrefab == null)
        {
            Debug.LogError("HATA: allItemPrefabs listesinde 'Category: Food' olan bir prefab bulunamadı!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (i >= allSlots.Count) break;
            GameObject can = Instantiate(foodPrefab, allSlots[i].position, allSlots[i].rotation);
            SnapToSlot(can, allSlots[i]);
        }
        UpdateGlobalStock();
    }

    // En yakın boş slotu bulur (Performans için sadece mesafe bakar)
    public Transform GetClosestEmptySlot(Vector3 position)
    {
        Transform bestSlot = null;
        float closestDist = 0.5f; // 50cm'den uzaksa yapışmasın

        foreach (var slot in allSlots)
        {
            if (slotData[slot] == null)
            {
                float dist = Vector3.Distance(position, slot.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestSlot = slot;
                }
            }
        }
        return bestSlot;
    }

    public void SnapToSlot(GameObject can, Transform slot)
    {
        slotData[slot] = can;

        // Fiziği tamamen kapat (Düşmemesi için)
        Rigidbody rb = can.GetComponent<Rigidbody>();
        if (rb) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; }

        // ÇÖZÜM: Oyuncunun tekrar eline alabilmesi için Collider'ı zorla geri açıyoruz!
        Collider[] cols = can.GetComponentsInChildren<Collider>();
        foreach (Collider c in cols) c.enabled = true;

        can.transform.position = slot.position;
        can.transform.rotation = slot.rotation;

        UpdateGlobalStock();
    }

    public void RemoveFromSlot(GameObject can)
    {
        Transform key = null;
        foreach (var pair in slotData)
        {
            if (pair.Value == can) { key = pair.Key; break; }
        }

        if (key != null)
        {
            slotData[key] = null;
            UpdateGlobalStock();
        }
    }

    public void UpdateGlobalStock()
    {
        int count = 0;
        foreach (var pair in slotData) if (pair.Value != null) count++;

        GameManager.Instance.foodStock = count;
        GameManager.Instance.UpdateStatsUI();
    }

    public void ConsumePhysicalFood(int amount)
    {
        int destroyed = 0;
        foreach (var slot in allSlots)
        {
            if (slotData[slot] != null && destroyed < amount)
            {
                Destroy(slotData[slot]);
                slotData[slot] = null;
                destroyed++;
            }
        }
        UpdateGlobalStock();
    }
}