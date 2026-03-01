using UnityEngine;

public class FoodContainer : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // 1. Kutuya herhangi bir þey deðdiðinde konsola yazdýr
        Debug.Log("Kutuya bir þey deðdi: " + other.gameObject.name);

        // Bazen collider alt objede olur, bu yüzden InParent ile arýyoruz
        LootableItem item = other.GetComponentInParent<LootableItem>();

        if (item != null)
        {
            // 2. Eþyayý bulduysa özelliklerini yazdýr
            Debug.Log($"Eþya bulundu -> Türü: {item.itemType} | Tutuluyor mu (isHeld): {item.isHeld}");

            if (item.itemType == LootableItem.ItemType.Food && !item.isHeld)
            {
                GameManager.Instance.foodStock += item.value;
                Debug.Log($"<color=green>Depoya +{item.value} Yemek Eklendi! Toplam Stok: {GameManager.Instance.foodStock}</color>");

                Destroy(item.gameObject);
            }
            else
            {
                Debug.LogWarning("Eþya depoya alýnmadý! Sebebi: Ya türü Food deðil ya da hala oyuncunun elinde sayýlýyor.");
            }
        }
    }
}