using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Butonları kontrol etmek için şart

public class MainMenuController : MonoBehaviour
{
    [Header("UI Bağlantıları")]
    public Button continueButton; // Inspector'dan Devam Et butonunu buraya sürükle

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Oyunda daha önceden kaydedilmiş bir gün (SavedDay) var mı kontrol et
        if (PlayerPrefs.HasKey("SavedDay"))
        {
            continueButton.interactable = true; // Kayıt varsa buton aktif
        }
        else
        {
            continueButton.interactable = false; // Kayıt yoksa buton silik (tıklanamaz)
        }
    }

    public void NewGame()
    {
        // YENİ OYUN: Eski kayıtları tamamen sil ve oyunu temiz başlat
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("LoadRequested", 0); // Sinyali sıfırla
        SceneManager.LoadScene("GameScene"); // Kendi oyun sahnenin adını yaz
        // EKLENEN KISIM: Oyuna geçerken fareyi hemen kilitler
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ContinueGame()
    {
        // DEVAM ET: Oyun sahnesine "Eski verileri yükle" sinyali (1) gönder
        PlayerPrefs.SetInt("LoadRequested", 1);
        SceneManager.LoadScene("GameScene");
        // EKLENEN KISIM: Oyuna geçerken fareyi hemen kilitler
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitGame()
    {
        Debug.Log("Çıkış Yapıldı");
        Application.Quit();
    }
}