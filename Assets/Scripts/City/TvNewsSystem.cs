using UnityEngine;
using TMPro;
using System.Collections;
using System.Text;

public class TvNewsSystem : MonoBehaviour
{
    [Header("Ayarlar")]
    public float scrollSpeed = 50f; // Yazýnýn kayma hýzý
    public float textResetPos = -1000f; // Yazý ne kadar sola gidince baþa dönsün?
    public float textStartPos = 1000f;  // Yazý nereden baþlasýn?

    [Header("UI Baðlantýsý")]
    public RectTransform textTransform; // Hareket edecek olan Text objesi
    public TextMeshProUGUI newsText;    // Yazýnýn içeriði

    private StringBuilder sb = new StringBuilder();

    void Start()
    {
        // Oyun baþlar baþlamaz ilk durumu çek
        StartCoroutine(UpdateRoutine());
    }

    void Update()
    {
        // Yazýyý Sola Kaydýr
        if (textTransform != null)
        {
            textTransform.anchoredPosition -= new Vector2(scrollSpeed * Time.deltaTime, 0);

            // Eðer çok sola gittiyse baþa sar
            if (textTransform.anchoredPosition.x < textResetPos)
            {
                textTransform.anchoredPosition = new Vector2(textStartPos, textTransform.anchoredPosition.y);
            }
        }
    }

    // Her sabah veya belirli aralýklarla haberi güncelle
    public void RefreshNews()
    {
        if (GameManager.Instance == null) return;

        sb.Clear();
        int currentDay = GameManager.Instance.currentDay;

        // BAÞLANGIÇ MANÞETÝ (Haber havasý katar)
        sb.Append(" *** SAVAÞ GÜNLÜÐÜ: GÜNCEL DURUM RAPORU *** ");

        // Þehir listesini gez
        foreach (var city in GameManager.Instance.cities)
        {
            if (city.IsInfected(currentDay))
            {
                sb.Append($"<s><color=red>{city.cityName}</color></s> (DÜÞTÜ)   +++   ");
            }
            else
            {
                sb.Append($"<color=green>{city.cityName}</color> (TEMÝZ)   +++   ");
            }
        }

        // SONA EKLEME
        sb.Append("   *** YAYIN SONU *** ");

        newsText.text = sb.ToString();
    }

    // Her 5 saniyede bir veriyi tazeleyen basit bir döngü
    // (Böylece gün deðiþtiðinde otomatik algýlar)
    IEnumerator UpdateRoutine()
    {
        while (true)
        {
            RefreshNews();
            yield return new WaitForSeconds(2f);
        }
    }
}