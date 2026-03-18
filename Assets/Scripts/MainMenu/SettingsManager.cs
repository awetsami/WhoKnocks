using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("UI Bağlantıları")]
    public Slider volumeSlider;
    public Slider sensitivitySlider;
    public TMP_Dropdown qualityDropdown;

    private void Start()
    {
        // Kaydedilmiş ayarları yükle (Yoksa varsayılan değerleri kullan)
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        int savedQuality = PlayerPrefs.GetInt("QualityLevel", 2); // 2: High

        // UI elemanlarını başlangıçta doğru konuma getir
        if (volumeSlider) volumeSlider.value = savedVolume;
        if (sensitivitySlider) sensitivitySlider.value = savedSensitivity;
        if (qualityDropdown) qualityDropdown.value = savedQuality;

        // Ayarları uygula
        ApplyVolume(savedVolume);
        ApplyQuality(savedQuality);
    }

    // Slider'ın OnValueChanged (Dynamic Float) kısmına bağla
    public void SetVolume(float volume)
    {
        ApplyVolume(volume);
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    // Slider'ın OnValueChanged (Dynamic Float) kısmına bağla
    public void SetSensitivity(float sensitivity)
    {
        // Oyuncu yürüme/bakma scriptinden bu PlayerPrefs'i okuyacak
        PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
        PlayerPrefs.Save();
    }

    // Dropdown'ın OnValueChanged (Dynamic Int) kısmına bağla
    public void SetQuality(int qualityIndex)
    {
        ApplyQuality(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float v)
    {
        // Oyundaki tüm sesleri yönetmenin en pratik ve güvenli yolu
        AudioListener.volume = v;
    }

    private void ApplyQuality(int q)
    {
        QualitySettings.SetQualityLevel(q);
    }
}