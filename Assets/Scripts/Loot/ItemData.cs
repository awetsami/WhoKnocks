using UnityEngine;

public enum ItemCategory
{
    Food,        // Konserve, su vb. (Rafa dizilecek)
    Battery,     // Fener ve enerji için (Rafa dizilebilir veya masada durur)
    Medical,     // İlk yardım kiti, bandaj, hap
    Valuable,    // Mücevher, saat, para (Hırsızın öncelikli hedefi)
    Scrap,       // Hurda, değersiz eşya (Tamirat veya ticaret için)
    Tool,        // Çekiç, anahtar (Kullan-at veya kalıcı aletler)
    Story        // Görev eşyaları, belgeler
}
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