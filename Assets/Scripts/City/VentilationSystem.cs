using UnityEngine;

public class VentilationSystem : MonoBehaviour
{
    public static VentilationSystem Instance;

    [Header("Gaz Seviyeleri")]
    public float currentGasLevel = 0f;
    public float gasFillRate = 3f;          // Gazın dolma hızı (Saniyede)
    public float gasClearRate = 15f;        // Fanın temizleme hızı (Saniyede)

    [Header("Sistem Durumu")]
    public bool isFanRunning = false;
    public bool isBroken = false;

    [Header("Bağlantılar")]
    public DoorController mainDoor;

    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }

    void Update()
    {
        if (GameManager.Instance == null || PlayerStats.Instance == null) return;

        // DIŞARIDAKİ ZEHİR SEVİYESİNİ AL (PlayerStats'tan)
        float maxAllowedGas = PlayerStats.Instance.GetOutdoorToxicity();

        // 1. KAPI AÇIK VE GECE İSE: Gaz yavaş yavaş içeri sızar 
        if (GameManager.Instance.currentPhase == DayPhase.Nighttime)
        {
            if (mainDoor != null && mainDoor.isOpen)
            {
                currentGasLevel += gasFillRate * Time.deltaTime;

                // FİZİK KURALI: İçerideki gaz, dışarıdaki gazı geçemez!
                if (currentGasLevel > maxAllowedGas)
                {
                    currentGasLevel = maxAllowedGas;
                }
            }
        }

        // 2. FAN ÇALIŞIYORSA: Gazı temizle
        if (isFanRunning)
        {
            if (mainDoor != null && !mainDoor.isOpen)
            {
                currentGasLevel -= gasClearRate * Time.deltaTime;

                if (currentGasLevel <= 0)
                {
                    currentGasLevel = 0;
                    isFanRunning = false;
                    Debug.Log("<color=green>BİLGİ: Sığınak havası tamamen temizlendi.</color>");
                }
            }
            else
            {
                // Kapı açılırsa fanlar durur
                isFanRunning = false;
                Debug.LogWarning("Kapı açıldığı için fanlar güvenlik sebebiyle durduruldu!");
            }
        }
    }

    // OYUNCU ŞALTERE BASTIĞINDA ÇALIŞACAK KOD
    public void ToggleVentilation()
    {
        Debug.Log("Şaltere basıldı! Sistem kontrol ediliyor...");

        if (isBroken)
        {
            Debug.Log("<color=red>HATA: Şalter arızalı!</color>");
            return;
        }

        if (mainDoor != null && mainDoor.isOpen)
        {
            Debug.Log("<color=orange>HATA: Kapı açıkken fanlar işe yaramaz, önce kapıyı kapat!</color>");
            return;
        }

        if (currentGasLevel <= 0)
        {
            Debug.Log("<color=yellow>BİLGİ: İçeride zehirli gaz yok, fanları çalıştırmaya gerek yok.</color>");
            return;
        }

        // Her şey uygunsa fanları çalıştır
        if (!isFanRunning)
        {
            isFanRunning = true;
            Debug.Log("<color=cyan>BAŞARILI: Fanlar çalıştırıldı, gaz tahliye ediliyor...</color>");
        }
    }

    public void NightlyCheck(bool hasEngineer)
    {
        if (isBroken) { if (hasEngineer && Random.Range(0, 100) < 30) isBroken = false; }
        else { if (Random.Range(0, 100) < 5) { isBroken = true; isFanRunning = false; } }
    }
}