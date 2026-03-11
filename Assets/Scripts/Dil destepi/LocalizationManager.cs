using System.Collections.Generic;
using UnityEngine;

public enum Language { Turkish, English }

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;
    public Language currentLanguage = Language.Turkish;

    // Metin veritabanı (ID, Metin)
    private Dictionary<string, string> trText = new Dictionary<string, string>();
    private Dictionary<string, string> enText = new Dictionary<string, string>();

    void Awake()
    {
        Instance = this;
        SetupLines();
    }

    void SetupLines()
    {
        // ÖRNEK VERİLER
        trText.Add("welcome_msg", "Sığınağa hoş geldin... Dışarıda kaos hakim.");
        enText.Add("welcome_msg", "Welcome to the shelter... Chaos reigns outside.");

        trText.Add("npc_hungry", "Sami, karnımız çok aç, yemek stoğuna baktın mı?");
        enText.Add("npc_hungry", "Sami, we are very hungry, did you check the food stock?");
    }

    public string GetTranslation(string key)
    {
        if (currentLanguage == Language.Turkish)
            return trText.ContainsKey(key) ? trText[key] : "KEY NOT FOUND";
        else
            return enText.ContainsKey(key) ? enText[key] : "KEY NOT FOUND";
    }
}