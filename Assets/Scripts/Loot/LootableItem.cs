using UnityEngine;

public class LootableItem : MonoBehaviour
{
    [Header("Eþya Verisi")]
    public ItemData itemData;
    public enum ItemType { Food, Battery, Ammo }
    public ItemType itemType = ItemType.Food;

    public int value = 1; // Kaç tane yemek/pil vereceði
    public bool isHeld = false; // Biri bunu elinde tutuyor mu?

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Eþyayý eline aldýðýnda fizikleri kapatýr
    public void PickUp(Transform handTransform)
    {
        isHeld = true;
        if (rb != null) rb.isKinematic = true; // Yerçekimini kapat

        transform.SetParent(handTransform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    // Eþyayý yere attýðýnda fizikleri geri açar
    public void Drop()
    {
        isHeld = false;
        transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(transform.forward * 2f, ForceMode.Impulse); // Hafifçe ileri fýrlat
        }
    }
}