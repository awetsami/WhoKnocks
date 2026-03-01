using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Cebimizdeki Eşyalar")]
    public List<ItemData> myItems = new List<ItemData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // Yerden eşya aldığımızda bu çalışacak
    public void AddItem(ItemData newItem)
    {
        myItems.Add(newItem);
        Debug.Log($"<color=green>Cebine eklendi: {newItem.itemName} (Değer: {newItem.taxValue}$)</color>");
    }

    // Vergiyi öderken veya eşyayı kullanırken bu çalışacak
    public void RemoveItem(ItemData itemToRemove)
    {
        if (myItems.Contains(itemToRemove))
        {
            myItems.Remove(itemToRemove);
        }
    }

    // Cebimizdeki tüm eşyaların toplam vergi değerini hesaplar
    public int GetTotalInventoryValue()
    {
        int total = 0;
        foreach (var item in myItems)
        {
            total += item.taxValue;
        }
        return total;
    }
}