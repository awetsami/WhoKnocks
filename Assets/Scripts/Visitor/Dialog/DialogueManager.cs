using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI Baðlantýlarý")]
    public GameObject dialoguePanel;
    public TMP_Text npcTextField;

    [Header("Buton A")]
    public Button buttonA;
    public TMP_Text buttonAText;

    [Header("Buton B")]
    public Button buttonB;
    public TMP_Text buttonBText;

    [Header("Daktilo Ayarlarý")]
    public float typingSpeed = 0.04f;

    private DialogueNode currentNode;
    private VisitorController activeSpeaker;
    public bool isDialogueActive = false;

    // --- Daktilo Deðiþkenleri ---
    private Coroutine typingCoroutine;
    private string fullTextToType;
    private bool isTyping = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        if (dialoguePanel) dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (isDialogueActive)
        {
            // TAB ile paneli geçici kapatma
            if (Input.GetKeyDown(KeyCode.Tab)) ClosePanelTemporary();

            // Yazý daktilo ile yazýlýrken SOL TIK (Mouse 1) basýlýrsa anýnda tamamla
            if (isTyping && Input.GetMouseButtonDown(0))
            {
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                FinishTyping();
            }
        }
    }

    public void StartDialogue(DialogueTopic topic, VisitorController speaker, DialogueNode startNode)
    {
        activeSpeaker = speaker;
        dialoguePanel.SetActive(true);
        isDialogueActive = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        LoadNode(startNode);
    }

    void LoadNode(DialogueNode node)
    {
        if (node == null) return;
        currentNode = node;

        // Yazý bitene kadar butonlarý gizle
        if (buttonA != null) buttonA.gameObject.SetActive(false);
        if (buttonB != null) buttonB.gameObject.SetActive(false);

        // Çeviriyi al (Eðer ID yoksa eski metni kullan - Fallback)
        string rawText = string.IsNullOrEmpty(node.npcText) ? "" : node.npcText;

        if (LocalizationManager.Instance != null)
        {
            string translated = LocalizationManager.Instance.GetTranslation(node.npcText);
            if (translated != "KEY NOT FOUND") rawText = translated;
        }

        // Þifreleri çöz ({CITY_DESC} vs.)
        if (activeSpeaker != null) fullTextToType = activeSpeaker.GetProcessedText(rawText);
        else fullTextToType = rawText;

        // Daktiloyu baþlat
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeTextRoutine());
    }

    IEnumerator TypeTextRoutine()
    {
        yield return null; // Sol týk çakýþmasýný önler

        isTyping = true;
        if (npcTextField != null) npcTextField.text = "";

        // SESÝ SADECE BAÞTA 1 KERE ÇAL (The Sims tarzý)
        if (activeSpeaker != null && activeSpeaker.voiceAudioSource != null && activeSpeaker.voiceAudioSource.clip != null)
        {
            activeSpeaker.voiceAudioSource.pitch = Random.Range(0.85f, 1.15f);
            activeSpeaker.voiceAudioSource.Play();
        }

        foreach (char letter in fullTextToType.ToCharArray())
        {
            if (npcTextField != null) npcTextField.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        FinishTyping();
    }

    void FinishTyping()
    {
        isTyping = false;
        if (npcTextField != null) npcTextField.text = fullTextToType;

        // YAZI BÝTTÝÐÝNDE ADAMI SUSTUR
        if (activeSpeaker != null && activeSpeaker.voiceAudioSource != null)
        {
            activeSpeaker.voiceAudioSource.Stop();
        }

        if (activeSpeaker != null) activeSpeaker.SaveProgress(currentNode);

        // --- BUTON A ---
        if (!string.IsNullOrEmpty(currentNode.textA))
        {
            if (buttonA != null)
            {
                buttonA.gameObject.SetActive(true);
                buttonA.interactable = true; // Butonu týklanabilir yap
            }

            string tA = currentNode.textA;
            if (LocalizationManager.Instance != null)
            {
                string transA = LocalizationManager.Instance.GetTranslation(currentNode.textA);
                if (transA != "KEY NOT FOUND") tA = transA;
            }
            if (buttonAText != null) buttonAText.text = tA;

            if (buttonA != null)
            {
                buttonA.onClick.RemoveAllListeners();
                buttonA.onClick.AddListener(() => OnOptionSelected(true));
            }
        }

        // --- BUTON B ---
        if (!string.IsNullOrEmpty(currentNode.textB))
        {
            if (buttonB != null)
            {
                buttonB.gameObject.SetActive(true);
                buttonB.interactable = true; // Butonu týklanabilir yap
            }

            string tB = currentNode.textB;
            if (LocalizationManager.Instance != null)
            {
                string transB = LocalizationManager.Instance.GetTranslation(currentNode.textB);
                if (transB != "KEY NOT FOUND") tB = transB;
            }
            if (buttonBText != null) buttonBText.text = tB;

            if (buttonB != null)
            {
                buttonB.onClick.RemoveAllListeners();
                buttonB.onClick.AddListener(() => OnOptionSelected(false));
            }
        }
    }

    void OnOptionSelected(bool isOptionA)
    {
        // Oyuncu spam yapamasýn diye butonlarý anýnda dondur
        if (buttonA != null) buttonA.interactable = false;
        if (buttonB != null) buttonB.interactable = false;

        DialogueNode next = isOptionA ? currentNode.nextNodeA : currentNode.nextNodeB;

        // Yarým saniyelik sinematik gecikmeyi baþlat
        StartCoroutine(TransitionWithDelay(next));
    }

    IEnumerator TransitionWithDelay(DialogueNode next)
    {
        yield return new WaitForSeconds(0.5f); // 0.5 Saniye Bekle

        // Gecikme bitti, aksiyonu (LEAVE, ENTER vb.) ÞÝMDÝ çalýþtýr
        if (!string.IsNullOrEmpty(currentNode.actionKey))
        {
            HandleAction(currentNode.actionKey);
        }

        // Sonraki diyalog varsa yükle, yoksa kapat
        if (next != null && !currentNode.isEndNode) LoadNode(next);
        else EndDialogue();
    }

    // --- AKSÝYON MERKEZÝ (Yanlýþlýkla silinen kýsým burasýydý) ---
    void HandleAction(string key)
    {
        if (activeSpeaker == null) return;
        switch (key)
        {
            case "GIVE_ID": activeSpeaker.ForceGiveID(); break;
            case "ENTER": activeSpeaker.EnterShelter(); EndDialogue(); break;
            case "LEAVE": activeSpeaker.LeaveShelter(); EndDialogue(); break;

            case "PAY_TAX":
                if (GameManager.Instance != null) GameManager.Instance.PayTax();
                EndDialogue();
                break;
            case "REFUSE_TAX":
                if (GameManager.Instance != null) GameManager.Instance.PunishForTax();
                EndDialogue();
                break;
            case "KEMAL_TAX":
                Debug.Log("Kemal vergi miktarýný açýkladý.");
                break;
        }
    }

    public void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        isDialogueActive = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (activeSpeaker != null) activeSpeaker.SaveProgress(currentNode);
        activeSpeaker = null;
    }

    void ClosePanelTemporary()
    {
        dialoguePanel.SetActive(false);
        isDialogueActive = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}