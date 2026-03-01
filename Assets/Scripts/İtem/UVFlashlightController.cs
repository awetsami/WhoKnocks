using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI; // Yeni ekledik: UI Bar için

public class UVFlashlightController : MonoBehaviour
{
    public static UVFlashlightController Instance;

    [Header("Batarya Ayarlarý")]
    public int currentBatteries = 1;      // Cihazdaki YEDEK pillerin sayýsý
    public int maxBatteries = 3;          // Maksimum alabileceði pil
    public float maxChargePerBattery = 100f; // 1 pilin gücü (%100)
    public float currentCharge = 100f;       // Þu an çalýþan pilin gücü
    public float drainRate = 2f;             // Saniyede ne kadar pil gidecek? (100 / 2 = 50 saniye dayanýr)

    [Header("Bileþenler")]
    public Light uvSpotLight;
    public AudioSource audioSource;
    public AudioClip toggleSound;
    public AudioClip reloadSound;
    public AudioClip errorSound;
    public Transform cameraTransform;

    [Header("UI Baðlantýlarý")]
    public TMP_Text batteryCountText;
    public TMP_Text warningText;
    public Image chargeBar; // YENÝ: Ekranda pilin ne kadar kaldýðýný gösteren Bar

    private bool isOn = false;
    private IDCard lastLitCard;

    void Awake()
    {
        if (Instance == null) Instance = this;

        if (uvSpotLight) uvSpotLight.enabled = false;
        if (warningText) warningText.gameObject.SetActive(false);
        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        UpdateUI();
    }

    void Update()
    {
        // [F] Tuþu ile Iþýðý Aç/Kapa
        if (Input.GetKeyDown(KeyCode.F)) ToggleLight();

        if (isOn)
        {
            HandleRaycastLight(); // Belge arama iþlevi (Senin orijinal kodun)
            DrainBattery();       // YENÝ: Pili Harcama Ýþlevi
        }
        else if (lastLitCard != null)
        {
            lastLitCard.ToggleUV(false);
            lastLitCard = null;
        }
    }

    // YENÝ EKLENEN FONSÝYON: Pili Tüket
    void DrainBattery()
    {
        if (currentCharge > 0)
        {
            currentCharge -= drainRate * Time.deltaTime;
            UpdateUI();
        }
        else
        {
            // Þarj %0 olduysa
            currentCharge = 0;

            if (currentBatteries > 0)
            {
                // Yedek pil varsa otomatik tak!
                currentBatteries--;
                currentCharge = maxChargePerBattery;
                if (audioSource && reloadSound) audioSource.PlayOneShot(reloadSound);
                Debug.Log("<color=green>BÝLGÝ: Pil bitti, yedek pil otomatik takýldý.</color>");
            }
            else
            {
                // YEDEK PÝL DE YOKSA IÞIK KAPANIR!
                TurnOffForcefully();
            }
        }
    }

    void TurnOffForcefully()
    {
        isOn = false;
        if (uvSpotLight) uvSpotLight.enabled = false;
        if (audioSource && toggleSound) audioSource.PlayOneShot(toggleSound); // Ýstersen farklý bir kapanma sesi koyabilirsin
        StartCoroutine(ShowWarning("PÝL BÝTTÝ!"));
        UpdateUI();
    }

    public void ToggleLight()
    {
        if (isOn)
        {
            // Kapat
            isOn = false;
            if (uvSpotLight) uvSpotLight.enabled = false;
            if (audioSource && toggleSound) audioSource.PlayOneShot(toggleSound);
        }
        else
        {
            // Açmayý dene
            if (currentCharge > 0 || currentBatteries > 0)
            {
                // Pil %0 ise ama yedek varsa önce yedeði taksýn
                if (currentCharge <= 0 && currentBatteries > 0)
                {
                    currentBatteries--;
                    currentCharge = maxChargePerBattery;
                    if (audioSource && reloadSound) audioSource.PlayOneShot(reloadSound);
                }

                isOn = true;
                if (uvSpotLight) uvSpotLight.enabled = true;
                if (audioSource && toggleSound) audioSource.PlayOneShot(toggleSound);
            }
            else
            {
                // Pil yok
                if (audioSource && toggleSound) audioSource.PlayOneShot(toggleSound);
                StartCoroutine(ShowWarning("PÝL YOK!"));
            }
        }
    }

    // InteractionSystem içinden (Yerden alýnan pille) çaðrýlýr
    public bool TryReloadBattery()
    {
        // Piller tam doluysa alamazsýn
        if (currentBatteries >= maxBatteries)
        {
            if (audioSource && errorSound) audioSource.PlayOneShot(errorSound);
            StartCoroutine(ShowWarning("DEPO DOLU!"));
            return false;
        }

        // Eðer fener tamamen boþsa ve yedek de yoksa (Þarj %0 ise), ilk aldýðýmýz pil direkt fenerin þarjý olur
        if (currentCharge <= 0 && currentBatteries == 0)
        {
            currentCharge = maxChargePerBattery;
            if (audioSource && reloadSound) audioSource.PlayOneShot(reloadSound);
            Debug.Log("Pil doðrudan fenere takýldý.");
        }
        else
        {
            // Aksi halde yedek cebe (currentBatteries'e) eklenir
            currentBatteries++;
            if (audioSource && reloadSound) audioSource.PlayOneShot(reloadSound);
            Debug.Log("Pil cebe/cihaz deposuna yedeklendi.");
        }

        UpdateUI();
        return true;
    }

    void UpdateUI()
    {
        if (batteryCountText) batteryCountText.text = $"YEDEK PÝL: {currentBatteries}/{maxBatteries}";
        if (chargeBar) chargeBar.fillAmount = currentCharge / maxChargePerBattery;
    }

    IEnumerator ShowWarning(string message)
    {
        if (warningText)
        {
            warningText.text = message;
            warningText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2f);
            warningText.gameObject.SetActive(false);
        }
    }

    // --- SENÝN ESKÝ IÞIN KODUN (Hiç Dokunulmadý) ---
    void HandleRaycastLight()
    {
        RaycastHit hit;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, 5f))
        {
            IDCard card = hit.collider.GetComponentInParent<IDCard>();
            if (card != null)
            {
                if (lastLitCard != null && lastLitCard != card) lastLitCard.ToggleUV(false);
                card.ToggleUV(true);
                lastLitCard = card;
            }
            else
            {
                if (lastLitCard != null) { lastLitCard.ToggleUV(false); lastLitCard = null; }
            }
        }
        else
        {
            if (lastLitCard != null) { lastLitCard.ToggleUV(false); lastLitCard = null; }
        }
    }
}