using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    [Header("Ayarlar")]
    public float openAngle = 90f;   // Açýlma açýsý
    public float closeAngle = 0f;   // Kapanma açýsý
    public float speed = 2f;        // Açýlma hýzý

    public bool isOpen = false;
    private bool isKnocking = false;
    private Quaternion targetRotation;

    void Start()
    {
        targetRotation = transform.localRotation;
    }

    void Update()
    {
        // Kapýyý yumuþakça (Lerp ile) hedef açýya döndür
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * speed);

        // Kapý çalýnýyorsa titret
        if (isKnocking && !isOpen)
        {
            float shake = Mathf.Sin(Time.time * 50) * 0.5f; // Hýzlý titreme
            transform.localRotation *= Quaternion.Euler(0, 0, shake);
        }
    }

    // Oyuncu týkladýðýnda çalýþacak fonksiyon
    public void Interact()
    {
        if (isOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }
    }

    public void OpenDoor()
    {
      
        isOpen = true;
        isKnocking = false; // Açýnca çalma durur
        targetRotation = Quaternion.Euler(0, openAngle, 0);
        Debug.Log("Gýcýýýrt... (Kapý Açýldý)");
        if (GameManager.Instance) GameManager.Instance.OnDoorOpened();
    }

    public void CloseDoor()
    {
        isOpen = false;
        targetRotation = Quaternion.Euler(0, closeAngle, 0);
        Debug.Log("Bam! (Kapý Kapandý)");
    }

    // Dýþarýdan tetiklenecek (GameManager çaðýracak)
    public void Knock()
    {
        if (!isOpen)
        {
            isKnocking = true;
            Debug.Log("TAK TAK TAK! (Kapý Çalýyor)");
            StartCoroutine(StopKnockingAfter(2f)); // 2 saniye sonra dursun
        }
    }

    IEnumerator StopKnockingAfter(float time)
    {
        yield return new WaitForSeconds(time);
        isKnocking = false;
    }
}