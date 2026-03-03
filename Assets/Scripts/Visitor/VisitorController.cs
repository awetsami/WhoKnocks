using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class VisitorController : MonoBehaviour
{
    // --- KEMAL (VERGÝ) ÖZEL DEÐÝÞKENLERÝ ---
    [Header("Kemal - Vergi ve Saldýrý")]
    public int requiredTax = 100; // Ýstenen vergi
    public GameObject kemalMachineGun; // Kemal'in elindeki silah (Sadece Kemal'de görünür olacak)
    public ParticleSystem muzzleFlash; // Namlu ateþi
    public AudioSource machineGunAudio; // Tarama sesi

    [Header("Bileþenler")]
    public NavMeshAgent agent;
    public NpcAnatomy anatomy;

    [Header("Kimlik & Profil")]
    public NpcProfile assignedProfile;
    public GameObject idCardPrefab;
    public Transform idDropPoint;

    // --- DEÐÝÞÝKLÝK BURADA: ÝKÝ ÞEHÝR VAR ---
    public CityData realCity;    // Gerçekten geldiði yer (Enfeksiyon buradan bulaþýr)
    public CityData claimedCity; // Aðzýyla söylediði yer (Yalan olabilir)
    // ----------------------------------------

    [Header("Durum")]
    public AnomalyType currentType = AnomalyType.None;
    public bool isResident = false;
    public bool isAnomalyActive = false;
    public bool hasGivenID = false;
    public bool isAtDoor = false;
    public bool hasReceivedIDBack = false;

    // Diyalog ve Koltuk
    private Seat myAssignedSeat;
    public DialogueNode savedCurrentNode;
    private List<Transform> currentPath = new List<Transform>();
    private int pathIndex = 0;
    private bool isWalking = false;
    private List<Transform> mySeatPath;
    private List<Transform> myExitPath;

    // --- SETUP GÜNCELLENDÝ: ARTIK 2 ÞEHÝR KABUL EDÝYOR ---
    // (GameManager artýk buraya hem gerçek hem yalan þehri gönderecek)
    public void SetupVisitor(NpcProfile profile, bool shouldBeAnomaly, CityData _realCity, CityData _claimedCity, List<Transform> seatPath, List<Transform> exitPath)
    {
        assignedProfile = profile;
        realCity = _realCity;       // Gerçek
        claimedCity = _claimedCity; // Ýddia edilen

        mySeatPath = new List<Transform>(seatPath);
        myExitPath = new List<Transform>(exitPath);

        // Anomali Tipi Belirleme
        if (shouldBeAnomaly)
        {
            isAnomalyActive = true;
            int roll = Random.Range(0, 100);

            // %20 Tip 3, %30 Tip 1, %50 Tip 2
            if (roll < 20) { currentType = AnomalyType.Type3; }
            else if (roll < 50) { currentType = AnomalyType.Type1; }
            else { currentType = AnomalyType.Type2; }
        }
        else
        {
            currentType = AnomalyType.None;
            isAnomalyActive = false;
        }

        if (anatomy != null) anatomy.BuildBody(isAnomalyActive);

        if (agent)
        {
            agent.speed = 1.8f;
            agent.angularSpeed = 360;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.1f;
            agent.autoBraking = true;
        }
    }

    // --- KÝMLÝK VERME ÝÞLEMÝ ---
    IEnumerator DropIDRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (idCardPrefab && idDropPoint && assignedProfile)
        {
            GameObject card = Instantiate(idCardPrefab, idDropPoint.position, idDropPoint.rotation);

            // --- LEKE MANTIÐI GÜNCELLENDÝ ---
            // 1. Eðer adam Tip 2 veya Tip 3 anomali ise leke olur.
            // 2. VEYA adamýn geldiði GERÇEK þehir þu anki günde enfekteyse leke olur.
            bool originInfected = realCity != null && realCity.IsInfected(GameManager.Instance.currentDay);
            bool showUVStain = (isAnomalyActive && (currentType == AnomalyType.Type2 || currentType == AnomalyType.Type3)) || originInfected;

            // Kartýn üzerine SÖYLENEN (Yalan) þehir yazýlýr.
            card.GetComponent<IDCard>()?.SetData(assignedProfile, claimedCity, showUVStain);
        }
        hasGivenID = true;
    }

    // --- DÝYALOG FONKSÝYONLARI ---
    public string GetProcessedText(string originalText)
    {

        if (string.IsNullOrEmpty(originalText)) return "";

        string processed = originalText;

        // Þehir açýklamasý kontrolü
        if (claimedCity != null)
            processed = processed.Replace("{CITY_DESC}", claimedCity.geoDescription);

        // Aile kontrolü
        string familyMsg = "";

        // assignedProfile null ise veya familyID boþ ise aile arama
        if (assignedProfile != null && !string.IsNullOrEmpty(assignedProfile.familyID))
        {
            foreach (var r in GameManager.Instance.residents)
            {
                // ÖNEMLÝ: r veya r.assignedProfile null mý diye kontrol ediyoruz (Hata buradaydý)
                if (r != null && r.assignedProfile != null && r != this)
                {
                    if (r.assignedProfile.familyID == assignedProfile.familyID)
                    {
                        familyMsg = $"{r.assignedProfile.relationType}m {r.assignedProfile.adSoyad} burada olduðu için çok mutluyum!";
                        break; // Aileden birini bulduðumuzda döngüden çýkabiliriz
                    }
                }
            }
        }
        // VisitorController.cs -> GetProcessedText içine ekle
        processed = processed.Replace("{TAX_AMOUNT}", GameManager.Instance.taxAmount.ToString());
        processed = processed.Replace("{FAMILY_MSG}", familyMsg);
        return processed;

    }

    public void SaveProgress(DialogueNode node)
    {
        savedCurrentNode = node;
        if (node != null && node.isEndNode) savedCurrentNode = null;
    }

    public void StartConversation()
    {
        if (assignedProfile == null) return;
        if (savedCurrentNode != null)
        {
            DialogueManager.Instance.StartDialogue(null, this, savedCurrentNode);
            return;
        }

        List<DialogueTopic> topics = isAtDoor ? assignedProfile.normalDoorTopics : assignedProfile.normalSeatTopics;
        if (topics != null && topics.Count > 0)
            DialogueManager.Instance.StartDialogue(null, this, topics[0].startNode);
    }

    // --- HAREKET VE YOK OLMA (Aynen Korundu) ---
    public void SetPath(List<Transform> newPath)
    {
        currentPath = new List<Transform>(newPath);
        pathIndex = 0;
        isWalking = true;
        if (agent)
        {
            agent.isStopped = false;
            agent.ResetPath();
            if (currentPath.Count > 0) agent.SetDestination(currentPath[0].position);
        }
    }

    void Update()
    {
        if (!isWalking || currentPath.Count == 0 || agent == null) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                pathIndex++;
                if (pathIndex < currentPath.Count)
                {
                    agent.SetDestination(currentPath[pathIndex].position);
                }
                else
                {
                    isWalking = false;
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    agent.ResetPath();

                    if (pathIndex == 1) isAtDoor = true; else isAtDoor = false;

                    if (pathIndex > 1 && !isResident)
                    {
                        Debug.Log("Ziyaretçi sahneden ayrýldý. Yok ediliyor.");
                        Destroy(gameObject, 0.5f);
                    }
                }
            }
        }
    }

    // --- ETKÝLEÞÝMLER (Aynen Korundu) ---
    public void EnterShelter()
    {
        isAtDoor = false;
        isResident = true;

        GameManager.Instance.ProcessDecision(this, true); // Senin eklediðin kýsým korundu

        Seat freeSeat = SeatManager.Instance.GetFreeSeat();
        if (freeSeat != null)
        {
            freeSeat.Occupy();
            myAssignedSeat = freeSeat;
            List<Transform> finalPath = new List<Transform>(mySeatPath);
            finalPath.Add(freeSeat.sitPoint);
            SetPath(finalPath);
        }
        else { SetPath(mySeatPath); }

        GameManager.Instance.RegisterResident(this);
    }
    public void CheckForFamily()
    {
        if (string.IsNullOrEmpty(assignedProfile.familyID)) return;

        foreach (var resident in GameManager.Instance.residents)
        {
            // Eðer sýðýnaktaki biriyle aile ID'si aynýysa ve o kiþi kendisi deðilse
            if (resident.assignedProfile.familyID == assignedProfile.familyID && resident != this)
            {
                Debug.Log($"{assignedProfile.adSoyad}: Aile üyem {resident.assignedProfile.adSoyad} içeride!");
                // Burada ileride teþekkür diyaloðu tetikleyeceðiz.
            }
        }
    }
    public void LeaveShelter()
    {
        isAtDoor = false;
        isResident = false;
        GameManager.Instance.ProcessDecision(this, false);

        if (isAnomalyActive)
        {
            // Anomali ise silme, dýþarýda avcýya dönüþtür
            Transform outsidePoint = GameManager.Instance.spawnPoint; // Spawn olduðu yere geri dönsün
            SetPath(new List<Transform> { outsidePoint });
            StartCoroutine(BecomeNightHunterAfterLeaving());
        }
        else
        {
            // Masumsa normal çýkýþ yoluna git ve yok ol
            SetPath(myExitPath);
        }
    }

    IEnumerator BecomeNightHunterAfterLeaving()
    {
        // Kapýdan uzaklaþana kadar bekle (Örn: 10 saniye)
        yield return new WaitForSeconds(10f);

        BecomeNightHunter();
    }

    void BecomeNightHunter()
    {
        Debug.Log($"<color=red>TEHLÝKE: Kapýdan kovduðun {assignedProfile.adSoyad} bir anomaliydi ve Gece Avcýsýna dönüþtü!</color>");

        // 1. Düþman Tag'ýný ve Layer'ýný Ayarla (Lazer/Görüþ bug'ýný önlemek için ÞART)
        gameObject.tag = "Untagged"; // Düþmanýn tag'ýný "Player" býrakmamalýyýz.
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); // Lazerin kendisine çarpmasýný engeller

        // 2. Yeni Nesil Yapay Zeka Scriptini Ekle
        EnemyAI ai = gameObject.AddComponent<EnemyAI>();

        // 3. AI Ayarlarýný Otomatik Yapýlandýr (Inspector'da elle girmeyeceðimiz için koddan veriyoruz)
        ai.hearingRadius = 15f;
        ai.sightRange = 20f;
        ai.attackRange = 1.5f;
        ai.walkSpeed = 2f;
        ai.runSpeed = 6f; // Gündüz insan gibi yürüyen þey, gece çok hýzlý koþacak!

        // ÖNEMLÝ: LayerMask atamalarýný kodla yapýyoruz.
        // "Player" layer'ýnýn index'i genellikle 3 veya senin atadýðýn sayýdýr. 
        // Ýsme göre bulmak en garantisidir:
       

        // (Opsiyonel) Eðer kafasýnda çýkacak Ünlem objesi prefab'ý varsa buraya instantiate edebilirsin.
        // Þimdilik boþ býrakýyorum, ünlem olmadan da harika çalýþýr.

        // Artýk agent hýzýný AI scripti kontrol edecek
        if (agent)
        {
            agent.speed = ai.walkSpeed;
            agent.stoppingDistance = 0.5f; // Dibimize kadar girebilsin
        }
    }

    public void ForceGiveID() { if (!hasGivenID) StartCoroutine(DropIDRoutine()); }
    public void TakeCardBack() { if (!hasReceivedIDBack) { hasReceivedIDBack = true; Debug.Log("Ziyaretçi: 'Teþekkürler.'"); } }

    // Diyalog butonlarýna baðlamak için (Opsiyonel)
    public void AcceptVisitor() { EnterShelter(); }
    // --- KEMAL VERGÝ VE SALDIRI SÝSTEMÝ ---

    // Diyalog sistemindeki actionKey (örn: "VergiOde") doðrudan bu fonksiyonu çaðýrmalý
    public void TryPayTax()
    {
        // KEMAL SADECE VERGÝ KUTUSUNUN ÝÇÝNE BAKAR!
        if (GameManager.Instance.taxPaidSoFar >= requiredTax)
        {
            // Kutudaki vergiyi sýfýrla/düþ (Devlet aldý ve gitti)
            GameManager.Instance.taxPaidSoFar -= requiredTax;

            Debug.Log("<color=green>Kemal: 'Kutuyu kontrol ettim. Akýllý çocuk.' (Vergi Ödendi)</color>");
            GameManager.Instance.ShowWarning("<color=green>Kemal Vergiyi Kutudan Tahsil Etti.</color>");

            LeaveShelter();
        }
        else
        {
            // KUTU BOÞ VEYA EKSÝK!
            int eksik = requiredTax - GameManager.Instance.taxPaidSoFar;
            Debug.Log($"<color=red>Kemal: 'Kutuda {eksik}$ eksik var! Benimle dalga mý geçiyorsun?!'</color>");

            TriggerKemalRage(); // Makinalý tüfek ateþlenir!
        }
    }

    private void TriggerKemalRage()
    {
        // 1. Diyalog arayüzünü anýnda kapat (Bunu kendi diyalog yöneticine göre uyarla)
        // DialogueManager.Instance.CloseDialogue();

        // 2. Silahý görünür yap ve animasyonu baþlat
        if (kemalMachineGun != null) kemalMachineGun.SetActive(true);

        // Eðer Kemal'in gövdesinde bir Animator varsa ateþ etme animasyonunu tetikle
        Animator anim = anatomy.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger("StartShooting");

        // 3. Efektleri Patlat
        if (machineGunAudio != null) machineGunAudio.Play();
        if (muzzleFlash != null) muzzleFlash.Play();

        GameManager.Instance.ShowWarning("<color=red>KAPI TARRRANIYOR! SÝPER AL!</color>");

        // 4. OYUN DÜNYASINA "ÝSTÝLA" EMRÝ VER (Anomalileri Çaðýr)
        GameManager.Instance.TriggerAnomalyInvasion();
    }
    public void RejectVisitor() { LeaveShelter(); }
}