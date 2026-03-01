using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    private DialogueNode currentNode;
    private VisitorController activeSpeaker;

    private bool isDialogueActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        if (dialoguePanel) dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Tab))
        {
            ClosePanelTemporary();
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

        // --- DEÐÝÞEN KISIM: Þifreli metni çöz ---
        if (activeSpeaker != null)
        {
            // Ziyaretçiye git ve {CITY_DESC} kodunu çözdür
            npcTextField.text = activeSpeaker.GetProcessedText(node.npcText);
        }
        else
        {
            npcTextField.text = node.npcText;
        }
        // ---------------------------------------

        if (activeSpeaker != null) activeSpeaker.SaveProgress(node);

        if (!string.IsNullOrEmpty(node.actionKey)) HandleAction(node.actionKey);

        if (!string.IsNullOrEmpty(node.textA))
        {
            buttonA.gameObject.SetActive(true);
            if (buttonAText) buttonAText.text = node.textA;
            buttonA.onClick.RemoveAllListeners();
            buttonA.onClick.AddListener(() => OnOptionSelected(true));
        }
        else buttonA.gameObject.SetActive(false);

        if (!string.IsNullOrEmpty(node.textB))
        {
            buttonB.gameObject.SetActive(true);
            if (buttonBText) buttonBText.text = node.textB;
            buttonB.onClick.RemoveAllListeners();
            buttonB.onClick.AddListener(() => OnOptionSelected(false));
        }
        else buttonB.gameObject.SetActive(false);
    }

    void HandleAction(string key)
    {
        if (activeSpeaker == null) return;
        switch (key)
        {
            case "GIVE_ID": activeSpeaker.ForceGiveID(); break;
            case "ENTER": activeSpeaker.EnterShelter(); EndDialogue(); break;
            case "LEAVE": activeSpeaker.LeaveShelter(); EndDialogue(); break;

            // --- YENÝ EKLENEN KEMAL AKSÝYONLARI ---
            case "PAY_TAX":
                // Vergiyi öde (GameManager üzerinden)
                GameManager.Instance.PayTax();
                EndDialogue();
                break;

            case "REFUSE_TAX":
                // Vergiyi reddet (Kemal sinirlenir ve birilerini alýr)
                GameManager.Instance.PunishForTax();
                EndDialogue();
                break;
            case "KEMAL_TAX":
                // Kemal vergi miktarýný söylerken bu key çalýþabilir
                Debug.Log("Kemal vergi miktarýný açýkladý.");
                break;
        }
    }

    void OnOptionSelected(bool isOptionA)
    {
        DialogueNode next = isOptionA ? currentNode.nextNodeA : currentNode.nextNodeB;
        if (next != null && !currentNode.isEndNode) LoadNode(next);
        else EndDialogue();
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