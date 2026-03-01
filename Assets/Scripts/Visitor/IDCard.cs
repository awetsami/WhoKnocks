using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class IDCard : MonoBehaviour
{
    [Header("UI Baðlantýlarý")]
    public TMP_Text nameText;
    public TMP_Text cityText;
    public TMP_Text ageText;
    public TMP_Text roleText;

    [Header("Gizli UV Katmaný")]
    public GameObject uvLayer;
    public Image infectionStain;   // Yeþil leke
    public TMP_Text watermarkText; // Mavi mühür

    [Header("Veri")]
    public NpcProfile myProfile;
    public CityData myCity;

    public void SetData(NpcProfile profile, CityData city, bool showStain)
    {
        myProfile = profile;
        myCity = city;

        if (nameText) nameText.text = profile.adSoyad;
        if (ageText) ageText.text = "Yaþ: " + profile.yas;
        if (roleText) roleText.text = profile.meslek;
        if (cityText) cityText.text = city != null ? "D. Yeri: " + city.cityName : "BÝLÝNMÝYOR";

        // UV Katmanýný baþta gizle
        if (uvLayer) uvLayer.SetActive(false);

        // Leke ve Mühür Mantýðý
        if (infectionStain) infectionStain.gameObject.SetActive(showStain); // Hastaysa Leke Görünsün
        if (watermarkText) watermarkText.gameObject.SetActive(!showStain);  // Temizse Mühür Görünsün
    }

    // Iþýk açýldýðýnda çaðrýlýr
    public void ToggleUV(bool state)
    {
        if (uvLayer) uvLayer.SetActive(state);
    }
}