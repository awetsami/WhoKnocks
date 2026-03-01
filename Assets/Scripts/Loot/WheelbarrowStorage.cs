using UnityEngine;
using System.Collections.Generic;

public class WheelbarrowStorage : MonoBehaviour
{
    [Header("Depo Ayarları")]
    public List<LootableItem> storedItems = new List<LootableItem>();
    public Transform itemContainer;
    public int maxCapacity = 10;

    private WheelbarrowController controller;

    void Start()
    {
        // Arabanın ana kontrolcüsünü bul
        controller = GetComponentInParent<WheelbarrowController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        LootableItem item = other.GetComponentInParent<LootableItem>();

        if (item != null && !item.isHeld && !storedItems.Contains(item))
        {
            if (storedItems.Count >= maxCapacity) return;
            StoreItem(item);
        }
    }

    void StoreItem(LootableItem item)
    {
        storedItems.Add(item);
        item.transform.SetParent(itemContainer);

        Rigidbody itemRb = item.GetComponent<Rigidbody>();
        if (itemRb != null)
        {
            // SENİN MANTIĞIN: Eğer araba şu an oyuncunun elindeyse eşyayı hemen dondur
            if (controller != null && controller.isHeld)
            {
                itemRb.isKinematic = true;
            }
            else
            {
                // Araba park halindeyse fizik motoru çalışsın, kendi kendine düşüp yerleşsin
                itemRb.isKinematic = false;
            }
        }

        if (controller != null) controller.AddWeight(2.5f);

        Debug.Log($"<color=green>{item.gameObject.name} arabaya atıldı!</color>");
    }

    public void RemoveItemFromStorage(LootableItem item)
    {
        if (storedItems.Contains(item))
        {
            storedItems.Remove(item);
            item.transform.SetParent(null);

            if (controller != null) controller.AddWeight(-2.5f);
        }
    }
}