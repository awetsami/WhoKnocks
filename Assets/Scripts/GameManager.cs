using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Globalization;

public enum DayPhase { Daytime, Nighttime }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Dünyayý Kaydetme (World State)")]
    public GameObject visitorPrefab; // Ýçeri aldýðýmýz insanlarý yeniden yaratmak için ana prefab
    public List<GameObject> allItemPrefabs; // Oyundaki TÜM fiziksel eþyalarýn prefablarý (Unity Editörden sürüklenecek)

    // Not: allCharacters listesi (NpcProfile'larý tutan liste) zaten GameManager'da var varsayýyorum.
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
    public Transform spawnPoint, hatchLedge;
    public DoorController mainDoor;
    public List<Transform> pathToDoor, pathToSeat, pathToExit;

    private int visitorsToday = 0;
    private int visitorsSpawned = 0;
    private GameObject currentVisitor;
    private float knockTimer;

    [Header("Gece/Gündüz Iþýk Ayarlarý")]
    public Light sunLight; // Unity'den dýþarýdaki Directional Light'ý buraya sürükle
    public float nightLightIntensity = 0.05f;
    public float dayLightIntensity = 1f;

    // Eðer sýðýnak içinde gece olunca kýsýlan/kapanan bir lamba varsa onu da buraya ekleyebilirsin:
    // public Light bunkerMainLight;

    [Header("Demo Ayarlarý")]
    public int demoMaxDays = 3; // Demo kaç gün sürecek?
    public GameObject demoEndPanel; // Wishlist panelimiz

    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }
    public void TriggerAnomalyInvasion()
    {
        isUnderSiege = true;
        ShowWarning("<color=red>SÝLAH SESLERÝ ANOMALÝLERÝ ÇEKTÝ! SIÐINAK KUÞATMA ALTINDA!</color>");

        // Gelecekte buraya: Anomali spawn kodunu (Spawner.StartSiege()) ekleyeceðiz.
    }
    void Start()
    {
        // 1. Temel Listeleri Hazýrla
        remainingCharacters = new List<NpcProfile>(allCharacters);
        SimulateWar();
        foodStock = 0; // Trigger'lar sayana kadar sýfýr kalsýn

        // 2. Yükleme mi yoksa Yeni Oyun mu?
        if (PlayerPrefs.GetInt("LoadRequested", 0) == 1)
        {
            LoadGameData();
            PlayerPrefs.SetInt("LoadRequested", 0);
        }
        else
        {
            // Tamamen Yeni Oyun: Baþlangýç erzaklarýný pýt pýt dökelim
            if (PantryShelf.Instance != null) PantryShelf.Instance.SpawnInitialFood(25);
        }

        // 3. UI ve Dinamikleri Hazýrla
        if (transitionPanel) { transitionPanel.alpha = 1; transitionPanel.gameObject.SetActive(true); }
        if (confirmationPanel) confirmationPanel.SetActive(false);

        // Buton Dinleyicileri (Zaten atanmýþlarsa tekrar atamaz)
        btnYes?.onClick.RemoveAllListeners();
        btnYes?.onClick.AddListener(ConfirmSleep);
        btnNo?.onClick.RemoveAllListeners();
        btnNo?.onClick.AddListener(CancelSleep);

        UpdateStatsUI();
        StartCoroutine(DayStartSequence(true));
    }
    public void ThiefStealsSmart()
    {
        // 1. Sahnede duran ve birinin elinde olmayan tüm fiziksel eþyalarý bul
        PhysicalItem[] allItems = FindObjectsByType<PhysicalItem>(FindObjectsSortMode.None);
        if (allItems.Length == 0) return;

        // 2. Eþyalarý kategorilerine göre ayýrmak için boþ listeler oluþtur
        List<GameObject> valuableItems = new List<GameObject>();
        List<GameObject> survivalItems = new List<GameObject>(); // Medikal, Pil, Alet
        List<GameObject> foodItems = new List<GameObject>();

        foreach (var pItem in allItems)
        {
            LootableItem loot = pItem.GetComponent<LootableItem>();

            // Güvenlik Kontrolü: Eþya sahipsiz mi ve verisi var mý?
            if (loot == null || loot.itemData == null || loot.isHeld) continue;

            // Eþyayý ItemData'daki kategorisine göre listeye ekle
            switch (loot.itemData.category)
            {
                case ItemCategory.Valuable:
                    valuableItems.Add(pItem.gameObject);
                    break;
                case ItemCategory.Medical:
                case ItemCategory.Battery:
                case ItemCategory.Tool:
                    survivalItems.Add(pItem.gameObject);
                    break;
                case ItemCategory.Food:
                    foodItems.Add(pItem.gameObject);
                    break;
            }
        }

        // 3. SEÇÝM AÞAMASI (Öncelik Sýrasý)
        GameObject targetToSteal = null;

        // Önce deðerli eþya var mý? (Saat, altýn vb.)
        if (valuableItems.Count > 0)
            targetToSteal = valuableItems[Random.Range(0, valuableItems.Count)];
        // Yoksa hayatta kalma eþyasý var mý? (Pil, bandaj vb.)
        else if (survivalItems.Count > 0)
            targetToSteal = survivalItems[Random.Range(0, survivalItems.Count)];
        // O da yoksa mecbur yemeði çal.
        else if (foodItems.Count > 0)
            targetToSteal = foodItems[Random.Range(0, foodItems.Count)];

        // 4. YOK ETME VE RAPOR
        if (targetToSteal != null)
        {
            string stolenItemName = targetToSteal.GetComponent<LootableItem>().itemData.itemName;
            Destroy(targetToSteal);
            Debug.Log($"<color=red>[HIRSIZLIK]</color> Hýrsýz hedefini seçti ve þunu çaldý: {stolenItemName}");
        }
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

    public void ThiefStealsPhysicalItem()
    {
        // Sahnede oyuncunun etkileþime geçebileceði tüm eþyalarý bul
        // PhysicalItem senin eþya scriptin olduðu için onu tarýyoruz
        PhysicalItem[] allItems = FindObjectsByType<PhysicalItem>(FindObjectsSortMode.None);

        List<PhysicalItem> stealableItems = new List<PhysicalItem>();

        foreach (var item in allItems)
        {
            // KURAL 1: Rafýn (PantryShelf) içinde olmayanlarý çal (Masanýn üstündekiler vs.)
            // KURAL 2: Önemli bir objeyi (El arabasý vb.) çalmasýn diye tip kontrolü yapabilirsin
            LootableItem loot = item.GetComponent<LootableItem>();
            if (loot != null)
            {
                stealableItems.Add(item);
            }
        }

        if (stealableItems.Count > 0)
        {
            // Rastgele birini seç ve yok et
            int randomIndex = Random.Range(0, stealableItems.Count);
            string stolenName = stealableItems[randomIndex].gameObject.name.Replace("(Clone)", "");

            Destroy(stealableItems[randomIndex].gameObject);

            Debug.Log($"<color=red>HIRSIZLIK: Gece gizlice bir {stolenName} çalýndý!</color>");
        }
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
    // --- GÜN DÖNGÜSÜ ---
    // Bu fonksiyonu DayStartSequence içinde currentDay > demoMaxDays olduðunda çaðýrabilirsin

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
            // --- DEMO BÝTÝÞ KONTROLÜ ---
            // Eðer belirlenen gün sýnýrýný geçtiysek, yeni günü baþlatma ve demoyu bitir
            if (currentDay > demoMaxDays)
            {
                ShowDemoEndScreen();
                yield break; // Coroutine'i tamamen durdurur, aþaðýdaki kodlar çalýþmaz.
            }
            // ---------------------------

            if (PlayerStats.Instance != null) PlayerStats.Instance.OnNightPass(false);

            // --- LOOT SÝSTEMÝ (Mühendis Bonusu Dahil) ---
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

            // --- TÜKETÝM VE DEVLET DESTEÐÝ HESAPLAMA ---
            consumedLastNight = CalculateConsumption();
            int dailyGrant = CalculateSocialGrant(); // Yaþa göre hesaplar
            currentMoney += dailyGrant; // Parayý cüzdana ekler


            // --- YEMEK TÜKETÝMÝ VEYA OYUN SONU ---
            if (foodStock >= consumedLastNight)
            {
                foodStock -= consumedLastNight;
                if (PantryShelf.Instance != null) PantryShelf.Instance.ConsumePhysicalFood(consumedLastNight);
            }
            else
            {
                EndGame(false);
                yield break;
            }

            // --- GECE OLAYLARI (Hýrsýzlýk, Kavga, Arýza) ---
            string nightEvent = HandleNightEvents(hasEngineer);

            // --- TÜM VERÝLERÝ RAPORA DÖKME ---
            report = (consumedLastNight > 0 ? $"<color=red>-{consumedLastNight} Yemek Tüketildi.</color>\n" : "Yemek tüketilmedi.\n") +
                     (dailyGrant > 0 ? $"<color=blue>DEVLET DESTEÐÝ: +{dailyGrant} Kredi yatýrýldý.</color>\n" : "") +
                     (batteriesFound > 0 ? $"<color=green>+{batteriesFound} Pil Bulundu {(hasEngineer ? "(Mühendis Etkisi)" : "")}</color>\n" : "") +
                     nightEvent;
        }

        if (transitionText) { transitionText.text = "GÜN " + currentDay; transitionText.gameObject.SetActive(true); yield return new WaitForSeconds(2f); transitionText.gameObject.SetActive(false); }
        if (newspaperText) { newspaperText.text = dailyHeadlines.ContainsKey(currentDay) ? dailyHeadlines[currentDay] : "Cephede durum sakin."; newspaperText.gameObject.SetActive(true); yield return new WaitForSeconds(3f); newspaperText.gameObject.SetActive(false); }

        float f = 0; while (f < 1.5f) { f += Time.deltaTime; transitionPanel.alpha = 1 - (f / 1.5f); yield return null; }
        transitionPanel.gameObject.SetActive(false);
        if (!isFirstDay && transitionLogText) { transitionLogText.text = report; StartCoroutine(ClearLogText()); }

        UpdateStatsUI();

        // YENÝ: Sabah kontrolünü kaldýrdýk, sadece gece ölümü geçerli. Direkt günü baþlatýyoruz.
        StartDayLogic();
    }
    string HandleNightEvents(bool hasEngineer)
    {
        // Meslek taramasý yapalým
        bool hasDoctor = residents.Exists(x => x.assignedProfile != null && x.assignedProfile.occupationRole == OccupationType.Doctor);
         bool hasPolice = residents.Exists(x => x.assignedProfile != null && x.assignedProfile.occupationRole == OccupationType.Police);
        bool hasThief = residents.Exists(x => x.assignedProfile != null && x.assignedProfile.occupationRole == OccupationType.Thief);
        bool hasAggressive = residents.Exists(x => x.assignedProfile != null && x.assignedProfile.isAggressive);

        string report = "";


        if (hasThief)
        {
            if (hasPolice)
            {
                // Polis varsa hýrsýzlýk yapýlamaz
                report += "<color=blue>GÜVENLÝK: Polis gece bir hýrsýzý etkisiz hale getirdi.</color>\n";
            }
            else
            {
                // Polis yoksa önce fiziksel eþya çalmayý dene
                PhysicalItem[] items = FindObjectsByType<PhysicalItem>(FindObjectsSortMode.None);

                if (items.Length > 0)
                {
                    ThiefStealsSmart(); // Az önce yazdýðýmýz zeki fonksiyon çalýþýr
                    report += "<color=red>HIRSIZLIK: Gece eþyalarýndan biri sýðýnaktan çalýnmýþ!</color>\n";
                }
                else if (currentMoney >= 20)
                {
                    // Çalacak eþya yoksa kasaya yönelir
                    currentMoney -= 20;
                    report += "<color=red>HIRSIZLIK: Eþya bulamayan hýrsýz kasaný boþaltmýþ (-20 Kredi)!</color>\n";
                }
            }
        }

        // --- 2. KAVGA VE DOKTOR ---
        float brawlChance = hasAggressive ? 45f : 10f;
        if (hasDoctor) brawlChance /= 3f; // Doktor kavgayý yatýþtýrýr

        if (Random.Range(0, 100) < brawlChance)
        {
            report += "<color=orange>OLAY: Gece içeride kavga çýktý!</color>\n";

            // Eðer doktor yoksa kavgada biri yaralanýp sýðýnaðý terk edebilir (Ölebilir)
            if (!hasDoctor && residents.Count > 0)
            {
                int randomIndex = Random.Range(0, residents.Count);
                VisitorController victim = residents[randomIndex];
                report += $"<color=red>KAYIP: {victim.assignedProfile.adSoyad} kavgada aðýr yaralandýðý için sýðýnaðý terk etti.</color>\n";
                residents.RemoveAt(randomIndex);
                Destroy(victim.gameObject);
            }
        }

        // --- 3. MÜHENDÝS VE ARIZA ---
        if (Random.Range(0, 100) < 25)
        {
            if (hasEngineer) report += "<color=green>TAMÝR: Mühendis havalandýrma arýzasýný gece halletti.</color>\n";
            else
            {
                if (VentilationSystem.Instance != null) VentilationSystem.Instance.isBroken = true;
                report += "<color=red>ARIZA: Havalandýrma bozuldu!</color>\n";
            }
        }

        return report;
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

        // --- YENÝ: IÞIKLARI VE ATMOSFERÝ DEÐÝÞTÝR ---
        if (sunLight != null)
        {
            sunLight.intensity = nightLightIntensity;
            sunLight.color = new Color(0.2f, 0.3f, 0.5f); // Koyu, soðuk ölü bir mavi
        }
        // ---------------------------------------------

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

        // YENÝ: Gece geçiþi bittiðinde fareyi gizle ve ekrana kilitle!
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // --- YARDIMCI FONKSÝYONLAR ---
    int CalculateSocialGrant()
    {
        int totalGrant = 0;
        foreach (var res in residents)
        {
            // Sadece iþsiz olanlarý ve verisi atanmýþ olanlarý kontrol et
            if (res != null && res.assignedProfile != null && res.assignedProfile.occupationRole == OccupationType.Unemployed)
            {
                int age = res.assignedProfile.yas;

                if (age < 20) totalGrant += 3;       // Genç iþsiz
                else if (age < 40) totalGrant += 5;  // Orta yaþlý iþsiz
                else totalGrant += 7;                // Yaþlý iþsiz (Maksimum teþvik)
            }
        }
        return totalGrant;
    }
    int CalculateConsumption()
    {
        residents.RemoveAll(r => r == null);
        int totalConsumption = 0;

        foreach (var r in residents)
        {
            // Herkes ama herkes (Doktor dahil) en az 1 yemek yer.
            int personNeeds = 1;

            if (r.currentType == AnomalyType.Type3) totalConsumption += 5;
            else if (r.currentType == AnomalyType.Type2) totalConsumption += 3;
            else totalConsumption += personNeeds;
        }

        Debug.Log($"<color=cyan>[GECE HESABI]</color> Kiþi: {residents.Count} | Tüketim: {totalConsumption}");
        return totalConsumption;
    }
    void SimulateWar() { dailyHeadlines.Clear(); var shuffled = cities.OrderBy(a => System.Guid.NewGuid()).ToList(); int d = 1; foreach (var c in shuffled) { d += Random.Range(1, 4); c.gasDay = d; string m = $"SON DAKÝKA: {c.cityName} düþtü!"; if (dailyHeadlines.ContainsKey(d)) dailyHeadlines[d] += "\n" + m; else dailyHeadlines.Add(d, m); } }
    CityData GetSafeCity() { return cities.FindAll(c => !c.IsInfected(currentDay)).OrderBy(x => Random.value).FirstOrDefault() ?? cities[0]; }
    CityData GetFallenCity() { return cities.FindAll(c => c.IsInfected(currentDay)).OrderBy(x => Random.value).FirstOrDefault(); }
    public void ProcessDecision(VisitorController v, bool a) { if (currentVisitor == v.gameObject) currentVisitor = null; UpdateStatsUI(); }
    public void RegisterResident(VisitorController p) { if (!residents.Contains(p)) residents.Add(p); }
    public void UnregisterResident(VisitorController p) { residents.Remove(p); }
    public void UpdateStatsUI()
    {
        if (statsText != null)
        {
            statsText.text = "YEMEK: " + foodStock;
            // UI'ýn anýnda ekrana basýlmasýný garanti eder
            Canvas.ForceUpdateCanvases();
        }
    }
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
    void StartDayLogic()
    {
        visitorsSpawned = 0; visitorsToday = Random.Range(1, 4); dayText.text = "GÜN " + currentDay; knockTimer = Random.Range(3f, 6f);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
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
    // --- SAVE / LOAD SÝSTEMÝ (FÝZÝKSEL DÜNYA) ---
    // --- SAVE / LOAD SÝSTEMÝ (FÝZÝKSEL DÜNYA) ---
    public void SaveGameData()
    {
        // 1. TEMEL DEÐERLERÝ KAYDET
        PlayerPrefs.SetInt("SavedTaxPaid", taxPaidSoFar);
        PlayerPrefs.SetInt("SavedMoney", currentMoney);
        PlayerPrefs.SetInt("SavedDay", currentDay);
        PlayerPrefs.SetInt("SavedFood", foodStock); // Stok sayýsýný sayý olarak tutuyoruz
        PlayerPrefs.SetInt("SavedTaxAmount", taxAmount);

        // 2. SIÐINAKTAKÝ ZÝYARETÇÝLERÝ KAYDET
        string residentSave = "";
        foreach (var res in residents)
        {
            if (res != null && res.assignedProfile != null)
            {
                residentSave += $"{res.assignedProfile.name},{res.isAnomalyActive}|";
            }
        }
        PlayerPrefs.SetString("SavedResidents", residentSave);

        // 3. FÝZÝKSEL EÞYALARI KAYDET (Yemekler Hariç!)
        string itemSave = "";
        PhysicalItem[] allItemsInScene = FindObjectsByType<PhysicalItem>(FindObjectsSortMode.None);
        foreach (PhysicalItem item in allItemsInScene)
        {
            // KURAL: Eðer bu bir yemekse (LootableItem.Food), onu fiziksel kaydetme! 
            // Çünkü LoadGameData sonunda 'SavedFood' sayýsý kadarýný rafa baþtan dizeceðiz.
            LootableItem loot = item.GetComponent<LootableItem>();
            if (loot != null && loot.itemType == LootableItem.ItemType.Food) continue;

            string cleanName = item.gameObject.name.Replace("(Clone)", "").Trim();
            itemSave += $"{cleanName},{item.transform.position.x.ToString(CultureInfo.InvariantCulture)}," +
                        $"{item.transform.position.y.ToString(CultureInfo.InvariantCulture)}," +
                        $"{item.transform.position.z.ToString(CultureInfo.InvariantCulture)}," +
                        $"{item.transform.eulerAngles.y.ToString(CultureInfo.InvariantCulture)}|";
        }
        PlayerPrefs.SetString("SavedItems", itemSave);

        // 4. EÞYA ÝNCELEME (INSPECTOR) VERÝLERÝNÝ KAYDET
        if (ObjectInspector.Instance != null)
        {
            PlayerPrefs.SetInt("SavedBatteries", ObjectInspector.Instance.batteryCount);
            PlayerPrefs.SetFloat("SavedCharge", ObjectInspector.Instance.currentCharge);
        }

        // 5. OYUNCUNUN KONUMUNU KAYDET
        if (PlayerStats.Instance != null)
        {
            PlayerPrefs.SetFloat("PlayerX", PlayerStats.Instance.transform.position.x);
            PlayerPrefs.SetFloat("PlayerY", PlayerStats.Instance.transform.position.y);
            PlayerPrefs.SetFloat("PlayerZ", PlayerStats.Instance.transform.position.z);
        }

        // 6. EL ARABASININ KONUMUNU KAYDET
        WheelbarrowController wheelbarrow = FindFirstObjectByType<WheelbarrowController>();
        if (wheelbarrow != null)
        {
            PlayerPrefs.SetFloat("CarX", wheelbarrow.transform.position.x);
            PlayerPrefs.SetFloat("CarY", wheelbarrow.transform.position.y);
            PlayerPrefs.SetFloat("CarZ", wheelbarrow.transform.position.z);
            PlayerPrefs.SetFloat("CarRotY", wheelbarrow.transform.eulerAngles.y);
        }

        PlayerPrefs.Save();
        if (warningText != null) ShowWarning("<color=green>Oyun Kaydedildi...</color>");
    }

    public void LoadGameData()
    {
        // 1. TEMEL DEÐERLERÝ YÜKLE
        taxPaidSoFar = PlayerPrefs.GetInt("SavedTaxPaid", 0);
        currentMoney = PlayerPrefs.GetInt("SavedMoney", 0);
        currentDay = PlayerPrefs.GetInt("SavedDay", 1);
        int loadedFoodAmount = PlayerPrefs.GetInt("SavedFood", 25); // Eski yemek sayýsýný hafýzaya al
        taxAmount = PlayerPrefs.GetInt("SavedTaxAmount", 5);

        // 2. EÞYA ÝNCELEME (INSPECTOR) VERÝLERÝNÝ YÜKLE
        if (ObjectInspector.Instance != null)
        {
            ObjectInspector.Instance.batteryCount = PlayerPrefs.GetInt("SavedBatteries", 1);
            ObjectInspector.Instance.currentCharge = PlayerPrefs.GetFloat("SavedCharge", 100f);
        }

        // 3. OYUNCUYU IÞINLA (Fizik Düzenlemesiyle)
        if (PlayerStats.Instance != null && PlayerPrefs.HasKey("PlayerX"))
        {
            float x = PlayerPrefs.GetFloat("PlayerX");
            float y = PlayerPrefs.GetFloat("PlayerY");
            float z = PlayerPrefs.GetFloat("PlayerZ");

            CharacterController cc = PlayerStats.Instance.GetComponent<CharacterController>();
            PlayerMovement pm = PlayerStats.Instance.GetComponent<PlayerMovement>();

            if (pm != null) pm.enabled = false;
            if (cc != null) cc.enabled = false;

            PlayerStats.Instance.transform.position = new Vector3(x, y + 1.5f, z);
            Physics.SyncTransforms();

            if (cc != null) cc.enabled = true;
            if (pm != null) pm.enabled = true;
        }

        // 4. EL ARABASINI IÞINLA
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
                wheelbarrow.transform.position = new Vector3(cx, cy + 0.5f, cz);
                wheelbarrow.transform.rotation = Quaternion.Euler(0, crotY, 0);
                rb.isKinematic = false;
            }
        }

        // 5. ESKÝ DÜNYAYI TEMÝZLE
        foreach (var res in residents) { if (res != null) Destroy(res.gameObject); }
        residents.Clear();

        PhysicalItem[] oldItems = FindObjectsByType<PhysicalItem>(FindObjectsSortMode.None);
        foreach (var oldItem in oldItems) { Destroy(oldItem.gameObject); }

        // 6. ZÝYARETÇÝLERÝ YÜKLE
        string savedRes = PlayerPrefs.GetString("SavedResidents", "");
        if (!string.IsNullOrEmpty(savedRes))
        {
            string[] resArray = savedRes.Split('|');
            foreach (string r in resArray)
            {
                if (string.IsNullOrEmpty(r)) continue;
                string[] data = r.Split(',');

                NpcProfile profile = allCharacters.Find(x => x.name == data[0]);
                if (profile != null && visitorPrefab != null)
                {
                    GameObject newVisObj = Instantiate(visitorPrefab, transform.position, Quaternion.identity);
                    VisitorController vc = newVisObj.GetComponent<VisitorController>();
                    vc.assignedProfile = profile;
                    vc.isAnomalyActive = bool.Parse(data[1]);
                    vc.isResident = true;

                    if (SeatManager.Instance != null)
                    {
                        Seat freeSeat = SeatManager.Instance.GetFreeSeat();
                        if (freeSeat != null)
                        {
                            freeSeat.Occupy();
                            newVisObj.transform.position = freeSeat.sitPoint.position;
                            newVisObj.transform.rotation = freeSeat.sitPoint.rotation;
                        }
                    }
                    residents.Add(vc);
                }
            }
        }

        // 7. EÞYALARI YÜKLE (Yemekler hariç, koordinat bazlý)
        string savedItems = PlayerPrefs.GetString("SavedItems", "");
        if (!string.IsNullOrEmpty(savedItems))
        {
            string[] itemArray = savedItems.Split('|');
            foreach (string iData in itemArray)
            {
                if (string.IsNullOrEmpty(iData)) continue;
                string[] data = iData.Split(',');

                GameObject prefabToSpawn = allItemPrefabs.Find(x => x.name == data[0]);
                if (prefabToSpawn != null)
                {
                    Vector3 pos = new Vector3(
                        float.Parse(data[1], CultureInfo.InvariantCulture),
                        float.Parse(data[2], CultureInfo.InvariantCulture),
                        float.Parse(data[3], CultureInfo.InvariantCulture));

                    Quaternion rot = Quaternion.Euler(0, float.Parse(data[4], CultureInfo.InvariantCulture), 0);
                    Instantiate(prefabToSpawn, pos, rot);
                }
            }
        }

        // 8. EN SON: YEMEKLERÝ RAFA BAÞTAN DÝZ (Hafýzadaki miktar kadar)
        if (PantryShelf.Instance != null)
        {
            PantryShelf.Instance.SpawnInitialFood(loadedFoodAmount);
        }

        Debug.Log("<color=cyan>Kayýtlý oyun baþarýyla yüklendi! Yemekler rafa pýt pýt diziliyor.</color>");
    }

    //----DEMO BÝTÝÞ EKRANI ---
    public void ShowDemoEndScreen()
    {
        // Paneli aktif et
        if (demoEndPanel != null) demoEndPanel.SetActive(true);

        // Arka plandaki geçiþ veya oyun içi panelleri kapatabilirsin
        // transitionPanel.gameObject.SetActive(false); // Varsa

        // Oyunu tamamen dondur
        Time.timeScale = 0f;

        // Oyuncunun menüde týklayabilmesi için fareyi serbest býrak ve görünür yap
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Demo bitti, Wishlist ekraný açýldý!");
    }

    public void OpenSteamPage()
    {
        // Buraya kendi Steam sayfanýzýn linkini koyacaksýn. Þimdilik boþ kalabilir veya Steam ana sayfasýný test için yazabilirsin.
        Application.OpenURL("https://store.steampowered.com/app/SENIN_OYUNUN_ID_SI");
    }

    public void QuitFromDemo()
    {
        // Editörde çalýþmaz, build alýndýðýnda oyunu kapatýr
        Debug.Log("Demodan Çýkýlýyor...");
        Application.Quit();
    }
}