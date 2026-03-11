using UnityEngine;
using System.Collections.Generic;
public enum OccupationType { None, Engineer, Doctor, Chef, Thief, Police, Unemployed }

[CreateAssetMenu(fileName = "YeniKarakter", menuName = "Sýðýnak/NPC Profili")]
public class NpcProfile : ScriptableObject
{
    [Header("Mekanik Rol")]
    public OccupationType occupationRole;

    [Header("Sosyal Ýliþkiler")]
    public string familyID; // Ayný ID'ye sahip olanlar aile sayýlýr (Örn: "Yilmaz_Ailesi")
    public string relationType; // "Baba", "Kýz", "Eþ" gibi (Diyalogda kullanmak için)

    [Header("Kiþilik Özellikleri")]
    public bool isAggressive; // Kavgacý mý? (Gece kavga çýkarma ihtimali)
    public List<string> dislikedJobs; // Sevmediði meslekler (Örn: "Polis" sevmiyorsa kavga çýkar)

    [Header("Özel Aile Diyaloglarý")]
    public DialogueNode familyMemberFoundNode; // Ailesinden birini sýðýnakta bulunca gireceði diyalog

    [Header("Görünür Kimlik Bilgileri")]
    public string adSoyad;
    public string meslek;
    public int yas;
    public Sprite fotograf;

    [Header("NORMAL Diyalog Havuzu (Masum)")]
    public List<DialogueTopic> normalDoorTopics; // Kapýdayken (Doðrucu)
    public List<DialogueTopic> normalSeatTopics; // Ýçerideyken (Doðrucu)

    [Header("ANOMALÝ Diyalog Havuzu (Yalancý/Tuhaf)")]
    public List<DialogueTopic> anomalyDoorTopics; // Kapýdayken (Yalan/Sapýtma)
    public List<DialogueTopic> anomalySeatTopics; // Ýçerideyken (Yalan/Sapýtma)
}