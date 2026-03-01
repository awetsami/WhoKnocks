using UnityEngine;
using System.Collections.Generic;

public class TaxBox : MonoBehaviour
{
    [Header("Günlük Vergi Hedefi")]
    public int requiredTax = 100;    // Devlete ödenmesi gereken tutar
    public int currentPaid = 0;      // Şu ana kadar kutunun yuttuğu eşyaların toplam değeri

    [Header("Yutulan Eşyalar Kaydı")]
    public List<string> depositedItemNames = new List<string>(); // Neleri yuttuğunu görmek için

    private void OnTriggerEnter(Collider other)
    {
        // Kutuya düşen objede "PhysicalItem" kodu var mı?
        PhysicalItem item = other.GetComponent<PhysicalItem>();

        if (item != null && item.itemData != null)
        {
            // Kutunun içine değerli bir şey düştü! Değerini hesapla.
            int itemValue = item.itemData.taxValue;
            currentPaid += itemValue;
            depositedItemNames.Add(item.itemData.itemName);

            Debug.Log($"<color=yellow>[VERGİ KUTUSU] {item.itemData.itemName} yutuldu! (+{itemValue}$). Toplam Ödenen: {currentPaid} / {requiredTax}</color>");

            // İstersen buraya tatmin edici bir "Yazar Kasa" (Cha-Ching!) sesi ekleyebiliriz.

            // Eşyayı fiziksel olarak yok et (Kutu yuttu)
            Destroy(other.gameObject);

            // Vergi tamamlandı mı diye anlık kontrol edebilirsin
            if (currentPaid >= requiredTax)
            {
                Debug.Log("<color=green>BU GÜNLÜK VERGİ TAMAMLANDI! Rahat bir nefes alabilirsin.</color>");
            }
        }
    }

    // Gün sonunda uyuduğunda veya süren bittiğinde bu fonksiyonu çağıracağız
    public void ProcessEndOfDayTaxes()
    {
        if (currentPaid >= requiredTax)
        {
            Debug.Log("<color=green>GÜN SONU: Vergini ödedin, devlet seni bir gün daha rahat bırakacak.</color>");
            // Fazla ödediysen (Para üstü) hesaba eklenebilir
            currentPaid = 0; // Yeni gün için sıfırla
            depositedItemNames.Clear();
        }
        else
        {
            int debt = requiredTax - currentPaid;
            Debug.Log($"<color=red>GÜN SONU: Vergiyi eksik ödedin! Devlete {debt}$ borcun var! Ceza kesiliyor...</color>");
            // Buraya Game Over veya ceza mekanizması gelecek
        }
    }
}