using UnityEngine;

[CreateAssetMenu(menuName = "Sýðýnak/Diyalog/Yeni Düðüm")]
public class DialogueNode : ScriptableObject
{
    [TextArea(3, 10)]
    public string npcText; // NPC ne diyor?

    [Header("Seçenek A")]
    public string textA;   // 1. Cevap metni
    public DialogueNode nextNodeA; // 1. Cevap nereye gider?

    [Header("Seçenek B")]
    public string textB;   // 2. Cevap metni
    public DialogueNode nextNodeB; // 2. Cevap nereye gider?

    [Header("Olay Tetikleyici")]
    // GIVE_ID, ENTER, LEAVE gibi komutlar buraya yazýlýr
    public string actionKey;

    // Ýþaretlenirse konuþma biter
    public bool isEndNode;
}