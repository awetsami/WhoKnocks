using UnityEngine;

[System.Serializable]
public class CityData
{
    public string cityName;       // Þehir Adý
    [TextArea]
    public string geoDescription; // "Kuzeydeki daðlýk alan"

    [HideInInspector] // Inspector'da görünmesine gerek yok, kod yönetecek
    public int gasDay;

    public bool IsInfected(int currentDay)
    {
        return currentDay >= gasDay;
    }
}