using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("Zehir Sensörü UI ☢️")]
    public Image toxicityBar;      // İstersen bar olarak göster
    public TMP_Text toxicityText;  // İstersen "Ortam Zehri: %45" diye yazıyla göster

    [Header("Temel İstatistikler")]
    public float health = 100f;
    public float hunger = 100f;
    public float stamina = 100f;
    public float maxStamina = 100f;

    [Header("Oksijen Sistemi 🤿")]
    public float oxygen = 100f;
    public float maxOxygen = 100f;
    public bool isOutside = false;    // Sığınak dışında mı?
    public bool isToxicZone = false;  // Şehir zehirli gaza maruz kalmış mı?

    [Header("Hareket Ayarları")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    private bool isRunning = false;

    [Header("UI Barları (Filled olmalı)")]
    public Image healthBar;
    public Image hungerBar;
    public Image staminaBar;
    public Image oxygenBar; // YENİ: Oksijen Barı

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        HandleStatsUI();
        HandleHungerAndHealth();
        HandleMovementAndStamina();
        HandleOxygen(); // YENİ: Oksijen Tüketimi

        if (Input.GetKeyDown(KeyCode.G)) TryEatFromStock();
    }

   

    void HandleHungerAndHealth()
    {
        if (hunger > 0) hunger -= 0.5f * Time.deltaTime;
        else { hunger = 0; TakeDamage(2f * Time.deltaTime); }
    }

    void HandleMovementAndStamina()
    {
        bool isMoving = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
        if (Input.GetKey(KeyCode.LeftShift) && stamina > 5f && isMoving)
        {
            isRunning = true;
            stamina -= 15f * Time.deltaTime;
        }
        else
        {
            isRunning = false;
            if (stamina < maxStamina) stamina += 10f * Time.deltaTime;
        }
    }

    // --- OKSİJEN MANTIĞI ---
    void HandleStatsUI()
    {
        if (healthBar) healthBar.fillAmount = health / 100f;
        if (hungerBar) hungerBar.fillAmount = hunger / 100f;
        if (staminaBar) staminaBar.fillAmount = stamina / maxStamina;
        if (oxygenBar) oxygenBar.fillAmount = oxygen / maxOxygen;

        // O an soluduğumuz havanın zehir seviyesi
        float currentToxicity = isOutside ? GetOutdoorToxicity() : (VentilationSystem.Instance != null ? VentilationSystem.Instance.currentGasLevel : 0f);
        string prefixText = isOutside ? "Dışarıdaki Zehir: %" : "Sığınak İçi Zehir: %";

        if (toxicityBar) toxicityBar.fillAmount = currentToxicity / 100f;

        if (toxicityText)
        {
            toxicityText.text = prefixText + Mathf.RoundToInt(currentToxicity);
            toxicityText.color = currentToxicity >= 50f ? Color.red : Color.white;
        }
    }

    // --- KADEMELİ OKSİJEN TÜKETİMİ ---
    void HandleOxygen()
    {
        float currentToxicity = isOutside ? GetOutdoorToxicity() : (VentilationSystem.Instance != null ? VentilationSystem.Instance.currentGasLevel : 0f);

        // ZEHİR %50'Yİ GEÇTİYSE OKSİJEN AZALMAYA BAŞLAR
        if (currentToxicity > 50f)
        {
            // %50'yi ne kadar geçtiyse (örn: %80 ise 30 birim geçmiştir) o kadar hızlı azalır
            float excessToxicity = currentToxicity - 50f;

            // Temel azalma hızı (2) + Zehir fazlası çarpanı (Örn: %100 zehirde saniyede 7 oksijen gider!)
            float depletionRate = 2f + (excessToxicity * 0.1f);

            oxygen -= depletionRate * Time.deltaTime;

            if (oxygen <= 0)
            {
                oxygen = 0;
                TakeDamage(5f * Time.deltaTime); // Boğulma hasarı
            }
        }
    }

    // --- DIŞ DÜNYA ZEHİR HESAPLAYICI (GÜNE VE ŞEHRE GÖRE) ---
    public float GetOutdoorToxicity()
    {
        int day = GameManager.Instance != null ? GameManager.Instance.currentDay : 1;

        if (isToxicZone)
        {
            // BOMBALANMIŞ ŞEHİR: %60'tan başlar, her gün %2 artar. (Maks %100)
            return Mathf.Clamp(60f + (day * 2f), 60f, 100f);
        }
        else
        {
            // GÜVENLİ ŞEHİR: %10'dan başlar, her gün %1 artar. (Maks %45 - Asla %50'yi geçmez, oksijen bitirmez)
            return Mathf.Clamp(10f + (day * 1f), 10f, 45f);
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0) { health = 0; GameManager.Instance.EndGame(false); }
    }

    public void TryEatFromStock()
    {
        if (GameManager.Instance.foodStock > 0 && hunger < 95f)
        {
            GameManager.Instance.foodStock--;
            hunger = Mathf.Min(100f, hunger + 25f);
        }
    }

    public void OnNightPass(bool stayedAwake)
    {
        hunger -= 25f;
        if (hunger < 0) hunger = 0;

        if (stayedAwake) maxStamina = Mathf.Max(30f, maxStamina - 15f);
        else maxStamina = Mathf.Min(100f, maxStamina + 20f);

        stamina = maxStamina;

        // YENİ: Şarj İstasyonu Mantığı! Sadece sabaha geçişte tam dolar.
        oxygen = maxOxygen;
    }
    // YENİ: Oyuncu elindeki FİZİKSEL YEMEĞİ yediğinde dışarıdan çağırılacak fonksiyon
    public void EatPhysicalFood(float restoreAmount)
    {
        // Sığınakta Aşçı var mı kontrol et
        bool hasChef = GameManager.Instance.residents.Exists(x => x.assignedProfile != null && x.assignedProfile.occupationRole == OccupationType.Chef);

        // Eğer aşçı varsa yemeğin etkisi 2 katına çıkar!
        float finalRestore = hasChef ? restoreAmount * 2f : restoreAmount;

        hunger = Mathf.Min(100f, hunger + finalRestore);
        health = Mathf.Min(100f, health + 5f);

        string chefBonus = hasChef ? " (Aşçı Bonusu!)" : "";
        Debug.Log($"<color=green>Yemek yendi: +{finalRestore}{chefBonus}</color>");
    }
}