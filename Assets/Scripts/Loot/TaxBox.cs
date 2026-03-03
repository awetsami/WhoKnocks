using UnityEngine;

public class TaxBox : MonoBehaviour
{
    [Header("Görsel ve İşitsel Hook'lar")]
    public AudioSource boxAudio;
    public AudioClip swallowSound;

    private void OnTriggerEnter(Collider other)
    {
        PhysicalItem item = other.GetComponent<PhysicalItem>();

        if (item != null && item.itemData != null)
        {
            int itemValue = item.itemData.taxValue;

            // DİKKAT: Oyuncuya para vermiyoruz! Sadece ödenen vergiyi artırıyoruz.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.taxPaidSoFar += itemValue;
                GameManager.Instance.ShowWarning($"<color=orange>Vergi Kutusuna Atıldı: {item.itemData.itemName}\nÖdenen Vergi: {GameManager.Instance.taxPaidSoFar}$</color>");
            }

            if (boxAudio != null && swallowSound != null) boxAudio.PlayOneShot(swallowSound);

            // Eşya devlete gitti, yok et.
            Destroy(other.gameObject);
        }
    }
}