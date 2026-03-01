using UnityEngine;

public enum ItemCategory { Survival, Valuable, Story }

[CreateAssetMenu(fileName = "New Item", menuName = "Oyun/Yeni Eşya Yarat")]
public class ItemData : ScriptableObject
{
    [Header("Eşya Bilgileri")]
    public string itemName = "Yeni Eşya";
    [TextArea] public string description;
    public ItemCategory category;

    [Header("Ekonomi")]
    public int taxValue = 10; // Devlete vergi olarak verirsek ne kadar borç düşer?

    [Header("Görsel")]
    public Sprite icon; // Envanterdeki resmi
    public GameObject dropPrefab; // Yere attığımızda çıkacak 3D modeli
}