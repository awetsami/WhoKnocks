using UnityEngine;
using TMPro;
using UnityEngine.UI; // Pil barı (Image) kullanmak için ekledik

public class ObjectInspector : MonoBehaviour
{
    public static ObjectInspector Instance;

    [Header("UV Pil Sistemi 🔋")]
    public int batteryCount = 1;             // Ceptedki yedek pil sayısı
    public int maxBatteries = 3;             // Taşınabilecek maksimum yedek pil
    public float maxCharge = 100f;           // 1 pilin gücü (%100)
    public float currentCharge = 100f;       // Cihazın içindeki pilin şu anki şarjı
    public float drainRate = 5f;             // Saniyede harcanan şarj (100 / 5 = 20 saniye dayanır)

    [Header("UI Bağlantıları")]
    public TMP_Text batteryText;
    public Image chargeBar;                  // Ekranda pil durumunu gösteren UI Bar (Yeni)

    [Header("Ayarlar")]
    public Transform inspectionPoint;
    public float rotateSpeed = 5f;
    public float smoothTime = 20f;

    [Header("Bileşenler")]
    public Light uvLightSource;
    public AudioClip uvSwitchSound;
    public AudioClip uvFailSound;

    private bool isUVOn = false;
    private GameObject currentObject;
    private Rigidbody currentRb;
    private bool isInspecting = false;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null) Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (uvLightSource) uvLightSource.enabled = false;
        UpdateBatteryUI();
    }

    void Update()
    {
        if (!isInspecting || currentObject == null)
        {
            if (isUVOn) TurnOffUV();
            return;
        }

        // [F] TUŞU - IŞIK AÇ/KAPA
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleUV();
        }

        // SAĞ TIK VEYA ESC - EŞYAYI BIRAK
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            DropObject();
        }

        // IŞIK AÇIKSA PİLİ HARCA
        if (isUVOn)
        {
            DrainBattery();
        }
    }

    void LateUpdate()
    {
        if (!isInspecting || currentObject == null) return;

        // 1. POZİSYON TAKİBİ (Pürüzsüz Lerp)
        currentObject.transform.position = Vector3.Lerp(
            currentObject.transform.position,
            inspectionPoint.position,
            Time.deltaTime * smoothTime
        );

        // 2. DÖNDÜRME (Mouse ile)
        if (Input.GetMouseButton(0))
        {
            float rotX = Input.GetAxis("Mouse X") * rotateSpeed;
            float rotY = Input.GetAxis("Mouse Y") * rotateSpeed;

            currentObject.transform.Rotate(Vector3.up, -rotX, Space.World);
            currentObject.transform.Rotate(Vector3.right, rotY, Space.World);
        }
        else
        {
            currentObject.transform.rotation = Quaternion.Lerp(
                currentObject.transform.rotation,
                inspectionPoint.rotation,
                Time.deltaTime * 5f
            );
        }
    }

    public void Inspect(GameObject obj)
    {
        if (isInspecting) return;
        currentObject = obj;

        currentRb = obj.GetComponent<Rigidbody>();
        if (currentRb)
        {
            currentRb.isKinematic = true;
            currentRb.useGravity = false;
        }

        isInspecting = true;
    }

    public void DropObject()
    {
        if (currentObject == null) return;

        if (currentRb)
        {
            currentRb.isKinematic = false;
            currentRb.useGravity = true;
        }

        IDCard card = currentObject.GetComponent<IDCard>();
        if (card != null) card.ToggleUV(false);

        currentObject = null;
        isInspecting = false;
    }

    // --- YENİ EKLENEN: ZAMANLA PİL BİTME MANTIĞI ---
    void DrainBattery()
    {
        if (currentCharge > 0)
        {
            currentCharge -= drainRate * Time.deltaTime;
            UpdateBatteryUI();
        }
        else
        {
            currentCharge = 0;

            // Pil bitti, yedek var mı?
            if (batteryCount > 0)
            {
                batteryCount--;
                currentCharge = maxCharge;
                // İstersen buraya bir "reload/pil takma" sesi ekleyebilirsin
                Debug.Log("<color=green>BİLGİ: Pil bitti, cihaza yedek pil takıldı.</color>");
            }
            else
            {
                // Yedek de yok, ışığı zorla kapat
                TurnOffUV();
                if (audioSource && uvFailSound) audioSource.PlayOneShot(uvFailSound);
                Debug.Log("<color=red>TEHLİKE: Fenerin pili tamamen bitti!</color>");
            }
            UpdateBatteryUI();
        }
    }

    // --- GÜNCELLENEN: IŞIK AÇMA/KAPAMA ---
    void ToggleUV()
    {
        if (isUVOn)
        {
            TurnOffUV();
            return;
        }

        // Açmayı deniyoruz
        if (currentCharge > 0 || batteryCount > 0)
        {
            // Şarj 0 ise ama yedek varsa, önce yedeği takıp öyle açalım
            if (currentCharge <= 0 && batteryCount > 0)
            {
                batteryCount--;
                currentCharge = maxCharge;
            }

            isUVOn = true;
            if (uvLightSource) uvLightSource.enabled = true;
            if (audioSource && uvSwitchSound) audioSource.PlayOneShot(uvSwitchSound);
            if (currentObject != null) currentObject.GetComponent<IDCard>()?.ToggleUV(true);

            UpdateBatteryUI();
        }
        else
        {
            // Hiç pil yoksa sadece hata sesi çal
            if (audioSource && uvFailSound) audioSource.PlayOneShot(uvFailSound);
        }
    }

    void TurnOffUV()
    {
        isUVOn = false;
        if (uvLightSource) uvLightSource.enabled = false;
        if (currentObject != null) currentObject.GetComponent<IDCard>()?.ToggleUV(false);
    }

    // --- GÜNCELLENEN: YERDEN PİL ALMA ---
    public void AddBattery(int amount)
    {
        if (batteryCount >= maxBatteries)
        {
            Debug.Log("Pil taşıma kapasitesi dolu!");
            return;
        }

        // Eğer fener tamamen ölü durumdaysa, ilk alınan pil direkt şarj olur
        if (currentCharge <= 0 && batteryCount == 0)
        {
            currentCharge = maxCharge;
            Debug.Log("Alınan pil doğrudan fenere takıldı.");
        }
        else
        {
            batteryCount += amount;
        }

        UpdateBatteryUI();
    }

    void UpdateBatteryUI()
    {
        if (batteryText) batteryText.text = "YEDEK PİL: " + batteryCount + "/" + maxBatteries;
        if (chargeBar) chargeBar.fillAmount = currentCharge / maxCharge;
    }

    public GameObject GetCurrentObject() { return currentObject; }
}