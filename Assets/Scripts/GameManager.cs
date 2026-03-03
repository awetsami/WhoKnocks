using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public enum DayPhase { Daytime, Nighttime }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Dünya Durumu")]
    public bool isUnderSiege = false; // Gerilim dolu Kuþatma Modu

    [Header("Ekonomi ve Vergi Sistemi")]
    public int currentMoney = 0; // Oyuncunun karaborsadan kazandýðý ve bilgisayarda harcayabileceði kredi
    public int taxPaidSoFar = 0; // Doðrudan Devletin Vergi Kutusuna atýlan toplam deðer

    [Header("Oyun Ýçi Uyarý Sistemi")]
    public TMP_Text warningText;

    [Header("Game Over Ekraný")]
    public GameObject gameOverPanel;

    public TMP_Text survivedDaysText;
    [Header("Dýþ Dünya Ayarlarý")]
    public GameObject invisibleWall; // Sýðýnak kapýsýndaki blok

    [Header("Dünya Haritasý")]
    public List<CityData> cities;
    private Dictionary<int, string> dailyHeadlines = new Dictionary<int, string>();

    [Header("Zaman & Faz")]
    public int currentDay = 1;
    public int maxDays = 15;
    public DayPhase currentPhase = DayPhase.Daytime;

    [Header("Sýðýnak Durumu")]
    public int foodStock = 25;
    public List<VisitorController> residents = new List<VisitorController>();

    [Header("Kemal (Vali) Ayarlarý")]
    public NpcProfile kemalProfile;
    public GameObject kemalPrefab;
    public int taxAmount = 5;
    public int taxInterval = 2;
    private bool kemalSpawnedToday = false;

    [Header("Loot & Eþya")]
    public GameObject batteryPrefab;
    public Transform lootTablePos;

    [Header("UI Baðlantýlarý")]
    public CanvasGroup transitionPanel;
    public TMP_Text transitionText;
    public TMP_Text newspaperText;
    public TMP_Text transitionLogText;
    public GameObject confirmationPanel;
    public TMP_Text confirmationInfoText;
    public Button btnYes, btnNo;
    public TMP_Text dayText, statsText;

    [Header("Karakter Havuzu")]
    public List<NpcProfile> allCharacters;
    private List<NpcProfile> remainingCharacters;
    public GameObject visitorPrefab;
    public Transform spawnPoint, hatchLedge;
    public DoorController mainDoor;
    public List<Transform> pathToDoor, pathToSeat, pathToExit;

    private int visitorsToday = 0;
    private int visitorsSpawned = 0;
    private GameObject currentVisitor;
    private float knockTimer;

    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }
    public void TriggerAnomalyInvasion()
    {
        isUnderSiege = true;
        ShowWarning("<color=red>SÝLAH SESLERÝ ANOMALÝLERÝ ÇEKTÝ! SIÐINAK KUÞATMA ALTINDA!</color>");

        // Gelecekte buraya: Anomali spawn kodunu (Spawner.StartSiege()) ekleyeceðiz.
    }
    void Start()
    {
        remainingCharacters = new List<NpcProfile>(allCharacters);
        SimulateWar();

        // YENÝ: Ana menüden "Devam Et" sinyali geldiyse verileri yükle!
        if (PlayerPrefs.GetInt("LoadRequested", 0) == 1)
        {
            LoadGameData();
            PlayerPrefs.SetInt("LoadRequested", 0); // Sinyali sýfýrla ki öldüðünde bug olmasýn
        }

        if (transitionPanel) { transitionPanel.alpha = 1; transitionPanel.gameObject.SetActive(true); }
        remainingCharacters = new List<NpcProfile>(allCharacters);
        SimulateWar();

        if (transitionPanel) { transitionPanel.alpha = 1; transitionPanel.gameObject.SetActive(true); }
        if (confirmationPanel) confirmationPanel.SetActive(false);
        if (btnYes) btnYes.onClick.AddListener(ConfirmSleep);
        if (btnNo) btnNo.onClick.AddListener(CancelSleep);

        UpdateStatsUI();
        StartCoroutine(DayStartSequence(true));
    }

    // --- VERGÝ VE CEZA SÝSTEMÝ ---
    public void PayTax()
    {
        if (foodStock >= taxAmount)
        {
            foodStock -= taxAmount;
            taxAmount += 3; // Vergi her seferinde artar
            if (currentVisitor != null) currentVisitor.GetComponent<VisitorController>().LeaveShelter();
        }
        else PunishForTax();
        UpdateStatsUI();
    }

    public void PunishForTax()
    {
        if (residents.Count == 0) { EndGame(false); return; } // Kimse yoksa vali seni öldürür

        int takenCount = Mathf.Min(2, residents.Count);
        for (int i = 0; i < takenCount; i++)
        {
            int randomIndex = Random.Range(0, residents.Count);
            VisitorController victim = residents[randomIndex];
            residents.RemoveAt(randomIndex);
            Destroy(victim.gameObject);
        }
        if (currentVisitor != null) currentVisitor.GetComponent<VisitorController>().LeaveShelter();
        UpdateStatsUI();
    }

    public void UpdateSecurityWall()
    {
        if (invisibleWall != null)
        {
            // Gündüzse duvar var, geceye geçince duvar kalkar
            invisibleWall.SetActive(currentPhase == DayPhase.Daytime);
        }
    }

    // --- SPAWN SÝSTEMÝ (KEMAL VE NORMAL NPC) ---
    void SpawnVisitor()
    {
        if (currentDay % taxInterval == 0 && !kemalSpawnedToday)
        {
            SpawnKemal();
            return;
        }

        if (remainingCharacters.Count == 0) return;

        int randomIndex = Random.Range(0, remainingCharacters.Count);
        NpcProfile selectedProfile = remainingCharacters[randomIndex];
        remainingCharacters.RemoveAt(randomIndex);

        bool isAnomaly = Random.Range(0, 100) < 30;
        CityData realCity = isAnomaly ? GetFallenCity() : GetSafeCity();
        CityData claimedCity = GetSafeCity();
        if (realCity == null) realCity = claimedCity;

        currentVisitor = Instantiate(visitorPrefab, spawnPoint.position, spawnPoint.rotation);
        visitorsSpawned++;

        VisitorController visitor = currentVisitor.GetComponent<VisitorController>();
        if (visitor != null)
        {
            visitor.idDropPoint = hatchLedge;
            visitor.SetupVisitor(selectedProfile, isAnomaly, realCity, claimedCity, pathToSeat, pathToExit);
            visitor.SetPath(pathToDoor);
        }
        mainDoor.Knock();
    }

    void SpawnKemal()
    {
        kemalSpawnedToday = true;
        currentVisitor = Instantiate(kemalPrefab != null ? kemalPrefab : visitorPrefab, spawnPoint.position, spawnPoint.rotation);
        visitorsSpawned++;

        VisitorController vc = currentVisitor.GetComponent<VisitorController>();
        if (vc != null)
        {
            vc.idDropPoint = hatchLedge;
            CityData novaCenter = cities.Find(x => x.cityName.Contains("Nova"));
            vc.SetupVisitor(kemalProfile, false, novaCenter, novaCenter, pathToSeat, pathToExit);
            vc.SetPath(pathToDoor);
        }
        mainDoor.Knock();
    }
    public void ShowWarning(string message)
    {
        if (warningText != null)
        {
            StopCoroutine("WarningRoutine");
            StartCoroutine(WarningRoutine(message));
        }
        else
        {
            // Eðer Unity'den text objesini baðlamayý unutursan hata vermesin, konsola yazsýn:
            Debug.Log(message);
        }
    }

    private IEnumerator WarningRoutine(string msg)
    {
        warningText.text = msg;
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(3f);
        warningText.gameObject.SetActive(false);
    }
    // --- GÜN DÖNGÜSÜ ---
    IEnumerator DayStartSequence(bool isFirstDay)
    {
        kemalSpawnedToday = false;
        transitionPanel.gameObject.SetActive(true);
        transitionPanel.alpha = 1;
        if (transitionLogText) transitionLogText.text = "";
        yield return new WaitForSeconds(0.5f);

        int batteriesFound = 0;
        int consumedLastNight = 0;
        bool hasEngineer = false;
        string report = "";

        if (!isFirstDay)
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnNightPass(false);

            int lootChance = 10;
            foreach (var r in residents)
            {
                if (r == null || r.assignedProfile == null) continue;
                string job = r.assignedProfile.meslek.ToLower();
                if (job.Contains("mühendis") || job.Contains("tamirci")) { lootChance += 100; hasEngineer = true; }
                else if (job.Contains("asker") || job.Contains("polis")) lootChance += 40;
            }
            if (Random.Range(0, 100) < lootChance)
            {
                batteriesFound = 1;
                if (hasEngineer && Random.Range(0, 100) < 50) batteriesFound = 2;
            }
            for (int i = 0; i < batteriesFound; i++) Instantiate(batteryPrefab, lootTablePos.position + Random.insideUnitSphere * 0.1f, lootTablePos.rotation);

            // GECE HAVALANDIRMA BOZULDU MU VEYA TAMÝR EDÝLDÝ MÝ KONTROLÜ
           

            consumedLastNight = CalculateConsumption();
            foodStock -= consumedLastNight;
            if (foodStock < 0) foodStock = 0;

            // YENÝ: HandleNightEvents'a mühendis bilgisini gönderiyoruz
            string nightEvent = HandleNightEvents(hasEngineer);

            report = (consumedLastNight > 0 ? $"<color=red>-{consumedLastNight} Yemek</color>\n" : "Yemek tüketilmedi.\n") +
                     (batteriesFound > 0 ? $"<color=green>+{batteriesFound} Pil {(hasEngineer ? "(Mühendis)" : "")}</color>\n" : "Pil bulunamadý.\n") + nightEvent;
        }

        if (transitionText) { transitionText.text = "GÜN " + currentDay; transitionText.gameObject.SetActive(true); yield return new WaitForSeconds(2f); transitionText.gameObject.SetActive(false); }
        if (newspaperText) { newspaperText.text = dailyHeadlines.ContainsKey(currentDay) ? dailyHeadlines[currentDay] : "Cephede durum sakin."; newspaperText.gameObject.SetActive(true); yield return new WaitForSeconds(3f); newspaperText.gameObject.SetActive(false); }

        float f = 0; while (f < 1.5f) { f += Time.deltaTime; transitionPanel.alpha = 1 - (f / 1.5f); yield return null; }
        transitionPanel.gameObject.SetActive(false);
        if (!isFirstDay && transitionLogText) { transitionLogText.text = report; StartCoroutine(ClearLogText()); }

        UpdateStatsUI();
        if (foodStock <= 0) EndGame(false); else StartDayLogic();
    }

    // --- GECE OLAYLARI ---
    // --- GECE OLAYLARI (Artýk hasEngineer parametresi alýyor) ---
    string HandleNightEvents(bool hasEngineer)
    {
        if (Random.Range(0, 100) > 30) return "";
        List<string> evts = new List<string>();
        if (residents.Count >= 3) evts.Add("KAVGA");
        evts.Add("ARIZA");
        if (residents.Exists(x => x.isAnomalyActive)) evts.Add("SES");

        string sel = evts[Random.Range(0, evts.Count)];
        switch (sel)
        {
            case "KAVGA":
                foodStock--;
                return "<color=red>UYARI: Gece kavga çýktý (-1 Yemek).</color>\n";

            case "ARIZA":
                if (hasEngineer)
                {
                    // Ýçeride mühendis varsa arýza anýnda çözülür
                    if (VentilationSystem.Instance != null) VentilationSystem.Instance.isBroken = false;
                    return "<color=green>BÝLGÝ: Havalandýrma arýzasý mühendis tarafýndan giderildi.</color>\n";
                }
                else
                {
                    // Mühendis yoksa GERÇEKTEN þalteri bozuyoruz!
                    if (VentilationSystem.Instance != null)
                    {
                        VentilationSystem.Instance.isBroken = true;
                        VentilationSystem.Instance.isFanRunning = false;
                    }
                    return "<color=orange>UYARI: Havalandýrma bozuldu, mühendise ihtiyaç var!</color>\n";
                }

            case "SES":
                return "<color=purple>KORKU: Duvarlardan týrmalama sesleri geldi...</color>\n";

            default: return "";
        }
    }

    public void OnDoorOpened()
    {
        Debug.Log("Kapý açýldý, sistem hazýr.");
    }

    // --- YENÝ SÝNEMATÝK GECE GEÇÝÞÝ ---
    IEnumerator NightTransitionRoutine()
    {
        // 1. Ekran Yavaþça Kararýr (GameManager'ýn kendi panelini kullanýyoruz)
        transitionPanel.gameObject.SetActive(true);
        float f = 0;
        while (f < 1.5f) { f += Time.deltaTime; transitionPanel.alpha = (f / 1.5f); yield return null; }

        // 2. Zifiri Karanlýk - Oyun Gece Moduna Geçer
        currentPhase = DayPhase.Nighttime;
        dayText.text = "GECE " + currentDay;
        UpdateSecurityWall();

        // Kutularý doður
        LootBoxSpawner spawner = Object.FindAnyObjectByType<LootBoxSpawner>();
        if (spawner != null) spawner.SpawnNightLootBoxes();

        Debug.Log("<color=red>Karanlýk çöktü... Kutular yerleþti...</color>");

        // 3 saniye karanlýkta bekle (Oyuncu bu sýrada kör, harika bir gerilim aný)
        yield return new WaitForSeconds(3f);

        // 3. Ekran Aydýnlanýr ve Gece Baþlar
        f = 0;
        while (f < 1.5f) { f += Time.deltaTime; transitionPanel.alpha = 1 - (f / 1.5f); yield return null; }
        transitionPanel.gameObject.SetActive(false);
    }

    // --- YARDIMCI FONKSÝYONLAR ---
    int CalculateConsumption() { residents.RemoveAll(r => r == null); int t = 0; foreach (var r in residents) { int c = 1; if (r.assignedProfile != null && r.assignedProfile.meslek.ToLower().Contains("doktor")) c = 0; switch (r.currentType) { case AnomalyType.Type3: t += 5; break; case AnomalyType.Type2: t += 3; break; default: t += c; break; } } return t; }
    void SimulateWar() { dailyHeadlines.Clear(); var shuffled = cities.OrderBy(a => System.Guid.NewGuid()).ToList(); int d = 1; foreach (var c in shuffled) { d += Random.Range(1, 4); c.gasDay = d; string m = $"SON DAKÝKA: {c.cityName} düþtü!"; if (dailyHeadlines.ContainsKey(d)) dailyHeadlines[d] += "\n" + m; else dailyHeadlines.Add(d, m); } }
    CityData GetSafeCity() { return cities.FindAll(c => !c.IsInfected(currentDay)).OrderBy(x => Random.value).FirstOrDefault() ?? cities[0]; }
    CityData GetFallenCity() { return cities.FindAll(c => c.IsInfected(currentDay)).OrderBy(x => Random.value).FirstOrDefault(); }
    public void ProcessDecision(VisitorController v, bool a) { if (currentVisitor == v.gameObject) currentVisitor = null; UpdateStatsUI(); }
    public void RegisterResident(VisitorController p) { if (!residents.Contains(p)) residents.Add(p); }
    public void UnregisterResident(VisitorController p) { residents.Remove(p); }
    void UpdateStatsUI() { if (statsText) statsText.text = "YEMEK: " + foodStock; }
    void Update() { if (currentPhase == DayPhase.Daytime && currentVisitor == null && visitorsSpawned < visitorsToday) { knockTimer -= Time.deltaTime; if (knockTimer <= 0) SpawnVisitor(); } }
    public void TryToSleep() { confirmationPanel.SetActive(true); confirmationInfoText.text = currentPhase == DayPhase.Daytime ? "Geceye geçilsin mi?" : "Sabah olsun mu?"; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

    // UYUMA VE GEÇÝÞÝ BAÞLATAN YER
    // UYUMA VE GEÇÝÞÝ BAÞLATAN YER
    public void ConfirmSleep()
    {
        confirmationPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // YENÝ: Uyumaya giderken kapýda bekleyen biri varsa sinirlenip gider (Sahnede kalmaz)
        if (currentVisitor != null)
        {
            Destroy(currentVisitor);
            currentVisitor = null;
            Debug.Log("<color=yellow>Kapýdaki ziyaretçi sen uyuduðun için gitti.</color>");
        }


        if (currentPhase == DayPhase.Daytime)
        {
            // Gündüzken uyursan gece geçiþi coroutine'i baþlar
            StartCoroutine(NightTransitionRoutine());
        }

        else
        {
            // Geceyken uyursan sabah olur
            currentDay++;
            currentPhase = DayPhase.Daytime;
            UpdateSecurityWall();

            // YENÝ: Yeni güne sað salim geçtik, OYUNU KAYDET!
            SaveGameData();

            StartCoroutine(DayStartSequence(false));
        }

    }

    public void CancelSleep() { confirmationPanel.SetActive(false); Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    void StartDayLogic() { visitorsSpawned = 0; visitorsToday = Random.Range(1, 4); dayText.text = "GÜN " + currentDay; knockTimer = Random.Range(3f, 6f); }
    public void EndGame(bool win)
    {
        // 1. Arayüzü (UI) ve Fareyi Aç
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (survivedDaysText != null)
            {
                survivedDaysText.text = win ?
                    $"TEBRÝKLER! {currentDay} GÜN DAYANDIN VE KURTULDUN." :
                    $"ÖLDÜN...\nSadece {currentDay} Gün Hayatta Kalabildin.";
            }
        }

        // 2. OYUNCUYU FELÇ ET (Dünya akmaya devam eder, oyuncu donar)
        if (PlayerStats.Instance != null)
        {
            // Yürüme ve etrafa bakma scriptini kapat
            PlayerMovement movement = PlayerStats.Instance.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;

            // Eþya alma ve kapý açma scriptini kapat
            InteractionSystem interaction = PlayerStats.Instance.GetComponent<InteractionSystem>();
            if (interaction != null) interaction.enabled = false;

            // (Opsiyonel) Eðer oyuncunun elinde araba varsa onu da zorla býraktýr
            WheelbarrowController heldCar = PlayerStats.Instance.GetComponentInChildren<WheelbarrowController>();
            if (heldCar != null && heldCar.isHeld)
            {
                heldCar.ToggleGrab(null);
            }
        }
    }

    // Ana Menüye Dön butonuna baðlanacak fonksiyon
    public void ReturnToMainMenu()
    {
        // Zamaný normal hýzýna alýp ana menü sahnesini yüklüyoruz
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    IEnumerator ClearLogText() { yield return new WaitForSeconds(6f); transitionLogText.text = ""; }
    // --- SAVE / LOAD SÝSTEMÝ (FÝZÝKSEL DÜNYA) ---
    public void SaveGameData()
    {
        PlayerPrefs.GetInt("SavedTax", taxPaidSoFar);
        PlayerPrefs.SetInt("SavedMoney", currentMoney);
        PlayerPrefs.SetInt("SavedDay", currentDay);
        PlayerPrefs.SetInt("SavedFood", foodStock);
        PlayerPrefs.SetInt("SavedTax", taxAmount);

        if (ObjectInspector.Instance != null)
        {
            PlayerPrefs.SetInt("SavedBatteries", ObjectInspector.Instance.batteryCount);
            PlayerPrefs.SetFloat("SavedCharge", ObjectInspector.Instance.currentCharge);
        }

        // 1. OYUNCUNUN KONUMUNU KAYDET
        if (PlayerStats.Instance != null)
        {
            PlayerPrefs.SetFloat("PlayerX", PlayerStats.Instance.transform.position.x);
            PlayerPrefs.SetFloat("PlayerY", PlayerStats.Instance.transform.position.y);
            PlayerPrefs.SetFloat("PlayerZ", PlayerStats.Instance.transform.position.z);
        }

        // 2. EL ARABASININ KONUMUNU KAYDET
        WheelbarrowController wheelbarrow = FindFirstObjectByType<WheelbarrowController>();
        if (wheelbarrow != null)
        {
            PlayerPrefs.SetFloat("CarX", wheelbarrow.transform.position.x);
            PlayerPrefs.SetFloat("CarY", wheelbarrow.transform.position.y);
            PlayerPrefs.SetFloat("CarZ", wheelbarrow.transform.position.z);

            // Arabanýn dönüþ açýsýný (Rotation) da kaydedelim ki devrilmiþse devrilmiþ kalsýn
            PlayerPrefs.SetFloat("CarRotY", wheelbarrow.transform.eulerAngles.y);
        }

        PlayerPrefs.Save();
        if (warningText != null) ShowWarning("<color=green>Oyun Kaydedildi...</color>");
    }

    public void LoadGameData()
    {
        taxPaidSoFar = PlayerPrefs.GetInt("SavedTax", 100);
        currentMoney = PlayerPrefs.GetInt("SavedMoney", 0);
        currentDay = PlayerPrefs.GetInt("SavedDay", 1);
        foodStock = PlayerPrefs.GetInt("SavedFood", 25);
        taxAmount = PlayerPrefs.GetInt("SavedTax", 5);

        if (ObjectInspector.Instance != null)
        {
            ObjectInspector.Instance.batteryCount = PlayerPrefs.GetInt("SavedBatteries", 1);
            ObjectInspector.Instance.currentCharge = PlayerPrefs.GetFloat("SavedCharge", 100f);
        }

        // 1. OYUNCUYU ESKÝ KONUMUNA IÞINLA (Havadan Býrakma Düzeltmesi)
        if (PlayerStats.Instance != null && PlayerPrefs.HasKey("PlayerX"))
        {
            float x = PlayerPrefs.GetFloat("PlayerX");
            float y = PlayerPrefs.GetFloat("PlayerY");
            float z = PlayerPrefs.GetFloat("PlayerZ");

            CharacterController cc = PlayerStats.Instance.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // DÝKKAT: y + 1.5f yaparak karakteri zeminin 1.5 metre yukarýsýndan býrakýyoruz!
            PlayerStats.Instance.transform.position = new Vector3(x, y + 1.5f, z);

            if (cc != null) cc.enabled = true;
        }

        // 2. EL ARABASINI ESKÝ KONUMUNA IÞINLA (Havadan Býrakma Düzeltmesi)
        WheelbarrowController wheelbarrow = FindFirstObjectByType<WheelbarrowController>();
        if (wheelbarrow != null && PlayerPrefs.HasKey("CarX"))
        {
            float cx = PlayerPrefs.GetFloat("CarX");
            float cy = PlayerPrefs.GetFloat("CarY");
            float cz = PlayerPrefs.GetFloat("CarZ");
            float crotY = PlayerPrefs.GetFloat("CarRotY");

            Rigidbody rb = wheelbarrow.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                // Arabayý da zemine gömülmesin diye 0.5 metre havadan býrakýyoruz
                wheelbarrow.transform.position = new Vector3(cx, cy + 0.5f, cz);
                wheelbarrow.transform.rotation = Quaternion.Euler(0, crotY, 0);
                rb.isKinematic = false;
            }
        }

        Debug.Log("<color=cyan>Kayýtlý oyun (Oyuncu ve Araba Konumu dahil) baþarýyla yüklendi!</color>");
    }
}