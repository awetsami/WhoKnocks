using UnityEngine;

public class ExchangeBox : MonoBehaviour
{
    [Header("Karaborsa Ayarları")]
    public AudioSource exchangeAudio;
    public AudioClip cashSound;

    private void OnTriggerEnter(Collider other)
    {
        PhysicalItem item = other.GetComponent<PhysicalItem>();

        if (item != null && item.itemData != null)
        {
            int itemValue = item.itemData.taxValue;

            // DİKKAT: Vergi ödemiyoruz, oyuncunun kendi cüzdanına (kredisine) ekliyoruz!
            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentMoney += itemValue;
                GameManager.Instance.ShowWarning($"<color=green>Karaborsaya Satıldı!\nCüzdana Eklendi: +{itemValue}$</color>");
            }

            if (exchangeAudio != null && cashSound != null) exchangeAudio.PlayOneShot(cashSound);

            Destroy(other.gameObject);
        }
    }
}