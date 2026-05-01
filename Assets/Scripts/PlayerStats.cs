using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("Zehir Sensörü UI ☢️")]
    public Image toxicityBar;
    public TMP_Text toxicityText;

    [Header("Temel İstatistikler")]
    public float health = 100f;
    public float hunger = 100f;
    public float stamina = 100f;
    public float maxStamina = 100f;

    [Header("Oksijen Sistemi 🤿")]
    public float oxygen = 100f;
    public float maxOxygen = 100f;
    public bool isOutside = false;
    public bool isToxicZone = false;

    [Header("Hareket Ayarları")]
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    private bool isRunning = false;

    [Header("UI Barları (Filled olmalı)")]
    public Image healthBar;
    public Image hungerBar;
    public Image staminaBar;
    public Image oxygenBar;

    [Header("Hasar Efektleri (Ekran Kanı)")]
    public Image bloodOverlay; // Inspector'dan buraya UI Image (Kan/Kararma) sürükleyeceksin
    public float bloodFadeSpeed = 1.5f; // Kanın ekrandan ne kadar sürede silineceği
    private float currentBloodAlpha = 0f;

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
        HandleOxygen();

        if (Input.GetKeyDown(KeyCode.G)) TryEatFromStock();

        // --- KAN EFEKTİNİ YAVAŞÇA SİLME MANTIĞI ---
        if (bloodOverlay != null && currentBloodAlpha > 0)
        {
            currentBloodAlpha -= bloodFadeSpeed * Time.deltaTime;
            Color c = bloodOverlay.color;
            c.a = Mathf.Clamp01(currentBloodAlpha); // Alpha değerini 0 ile 1 arasında tutar
            bloodOverlay.color = c;
        }
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

    void HandleStatsUI()
    {
        if (healthBar) healthBar.fillAmount = health / 100f;
        if (hungerBar) hungerBar.fillAmount = hunger / 100f;
        if (staminaBar) staminaBar.fillAmount = stamina / maxStamina;
        if (oxygenBar) oxygenBar.fillAmount = oxygen / maxOxygen;

        float currentToxicity = isOutside ? GetOutdoorToxicity() : (VentilationSystem.Instance != null ? VentilationSystem.Instance.currentGasLevel : 0f);
        string prefixText = isOutside ? "Dışarıdaki Zehir: %" : "Sığınak İçi Zehir: %";

        if (toxicityBar) toxicityBar.fillAmount = currentToxicity / 100f;

        if (toxicityText)
        {
            toxicityText.text = prefixText + Mathf.RoundToInt(currentToxicity);
            toxicityText.color = currentToxicity >= 50f ? Color.red : Color.white;
        }
    }

    void HandleOxygen()
    {
        float currentToxicity = isOutside ? GetOutdoorToxicity() : (VentilationSystem.Instance != null ? VentilationSystem.Instance.currentGasLevel : 0f);

        if (currentToxicity > 50f)
        {
            float excessToxicity = currentToxicity - 50f;
            float depletionRate = 2f + (excessToxicity * 0.1f);
            oxygen -= depletionRate * Time.deltaTime;

            if (oxygen <= 0)
            {
                oxygen = 0;
                TakeDamage(5f * Time.deltaTime);
            }
        }
    }

    public float GetOutdoorToxicity()
    {
        int day = GameManager.Instance != null ? GameManager.Instance.currentDay : 1;

        if (isToxicZone) return Mathf.Clamp(60f + (day * 2f), 60f, 100f);
        else return Mathf.Clamp(10f + (day * 1f), 10f, 45f);
    }

    // --- YENİLENEN HASAR ALMA VE TEPKİ SİSTEMİ ---
    public void TakeDamage(float amount)
    {
        health -= amount;

        // 1. KAN/KARARMA EFEKTİNİ FULLE (Update içinde yavaşça silinecek)
        if (bloodOverlay != null)
        {
            currentBloodAlpha = 0.8f; // 1 tam mat, 0.8 hafif saydam başlar
            Color c = bloodOverlay.color;
            c.a = currentBloodAlpha;
            bloodOverlay.color = c;
        }

        // 2. CS2 TAVANA BAKMA (AIM PUNCH)
        PlayerMovement pm = GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.ApplyDamageFlinch();
        }

        // 3. KAMERA SARSINTISI (Sert ve kısa bir kemik kırılma hissi)
        CameraShake camShake = GetComponentInChildren<CameraShake>();
        if (camShake != null)
        {
            StartCoroutine(camShake.Shake(0.2f, 0.6f));
        }

        // 4. ÖLÜM KONTROLÜ
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
        oxygen = maxOxygen;
    }

    public void EatPhysicalFood(float restoreAmount)
    {
        bool hasChef = GameManager.Instance.residents.Exists(x => x.assignedProfile != null && x.assignedProfile.occupationRole == OccupationType.Chef);
        float finalRestore = hasChef ? restoreAmount * 2f : restoreAmount;

        hunger = Mathf.Min(100f, hunger + finalRestore);
        health = Mathf.Min(100f, health + 5f);

        string chefBonus = hasChef ? " (Aşçı Bonusu!)" : "";
        Debug.Log($"<color=green>Yemek yendi: +{finalRestore}{chefBonus}</color>");
    }
}