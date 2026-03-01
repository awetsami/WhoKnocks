using UnityEngine;

public class ShelterTrigger : MonoBehaviour
{
    // Oyuncu dev sığınak kutusundan ÇIKTIĞINDA
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats.Instance.isOutside = true;
            Debug.Log("<color=red>UYARI: Sığınaktan çıkıldı! Oksijen azalıyor.</color>");
        }
    }

    // Oyuncu dev sığınak kutusuna GİRDİĞİNDE
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats.Instance.isOutside = false;
            Debug.Log("<color=green>BİLGİ: Sığınağa girildi. Güvendesiniz.</color>");
        }
    }
}