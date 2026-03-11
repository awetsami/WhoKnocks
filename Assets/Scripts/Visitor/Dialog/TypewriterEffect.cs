using UnityEngine;
using TMPro;
using System.Collections;

public class TypewriterEffect : MonoBehaviour
{
    private TMP_Text textMesh;
    public float typingSpeed = 0.04f; // Her harf arasındaki hız
    public AudioSource typingSound;   // Opsiyonel: Her harfte çalacak tık sesi

    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string fullContent;

    void Awake()
    {
        textMesh = GetComponent<TMP_Text>();
    }

    public void StartWriting(string text)
    {
        // Eğer zaten yazıyorsa eskisini durdur
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        fullContent = text;
        typingCoroutine = StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        isTyping = true;
        textMesh.text = ""; // Önce içini boşalt

        foreach (char letter in fullContent.ToCharArray())
        {
            textMesh.text += letter;

            // Tık sesi varsa çal
            if (typingSound != null && !typingSound.isPlaying)
            {
                typingSound.pitch = Random.Range(0.8f, 1.2f); // Ses tekdüze olmasın
                typingSound.Play();
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    // Oyuncu tıklarsa yazıyı anında tamamlamak için
    public void Skip()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            textMesh.text = fullContent;
            isTyping = false;
        }
    }
}