using UnityEngine;

[CreateAssetMenu(menuName = "Sýðýnak/Diyalog/Konu Baþlýðý")]
public class DialogueTopic : ScriptableObject
{
    public string topicID; // Hafýza için ID (Örn: "Ahmet_Kapi_Tanisma")
    public DialogueNode startNode; // Ýlk cümle
    public bool onlyOnce = true;   // Bir kere konuþulunca listeden silinsin mi?
}