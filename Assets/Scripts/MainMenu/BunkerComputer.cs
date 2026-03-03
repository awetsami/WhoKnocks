using UnityEngine;
using TMPro; // UI yazıları için

public class BunkerComputer : MonoBehaviour
{
    [Header("Market UI Bağlantıları")]
    public GameObject computerPanel; // Ekranda açılacak olan tam ekran bilgisayar menüsü
    public TMP_Text moneyText; // Ekranda güncel bakiyeyi göstereceğimiz Text

    [Header("Satın Alınacak Eşyalar (Spawn)")]
    public Transform spawnPoint; // Eşyaların havadan düşeceği yer (Bilgisayarın hemen yanı veya altı)
    public GameObject batteryPrefab; // Satın alınacak UV Pili objesi
    public GameObject foodPrefab; // Satın alınacak Konserve/Yemek objesi

    // Ekonomik Fiyatlandırma
    private int batteryPrice = 15;
    private int foodPrice = 10;

    private bool isComputerOpen = false;

    // Oyuncu bilgisayara "E" ile baktığında çalışacak ana fonksiyon
    public void ToggleComputer()
    {
        isComputerOpen = !isComputerOpen;
        computerPanel.SetActive(isComputerOpen);

        if (isComputerOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // DÜZELTİLEN KISIM: CharacterController'ı değil, PlayerMovement'ı kapat
            if (PlayerStats.Instance != null)
            {
                PlayerMovement movementScript = PlayerStats.Instance.GetComponent<PlayerMovement>();
                if (movementScript != null) movementScript.enabled = false;
            }
            UpdateUI();
        }
        else
        {
            CloseComputer();
        }
    }

    public void CloseComputer()
    {
        isComputerOpen = false;
        computerPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // DÜZELTİLEN KISIM: Menü kapanınca PlayerMovement'ı geri aç
        if (PlayerStats.Instance != null)
        {
            PlayerMovement movementScript = PlayerStats.Instance.GetComponent<PlayerMovement>();
            if (movementScript != null) movementScript.enabled = true;
        }
    }

    void UpdateUI()
    {
        if (moneyText != null)
        {
            // Bakiyeyi GameManager'dan çekip ekrana yazdırıyoruz
            moneyText.text = $"Hesap Bakiyesi: <color=green>{GameManager.Instance.currentMoney}$</color>\n" +
                             $"[1] UV Pili: {batteryPrice}$\n" +
                             $"[2] Konserve Yemek: {foodPrice}$";
        }
    }

    // --- SATIN ALMA İŞLEMLERİ (Butonlara bağlanacak) ---

    public void BuyBattery()
    {
        if (GameManager.Instance.currentMoney >= batteryPrice)
        {
            // Parayı kes
            GameManager.Instance.currentMoney -= batteryPrice;

            // Pili spawn noktasında oluştur (Yere düşmesi için prefabında Rigidbody olmalı)
            Instantiate(batteryPrefab, spawnPoint.position, Quaternion.identity);

            UpdateUI(); // Ekrandaki parayı hemen güncelle
            GameManager.Instance.ShowWarning("<color=green>Sipariş alındı: UV Pili.</color>");
        }
        else
        {
            GameManager.Instance.ShowWarning("<color=red>Bakiye yetersiz!</color>");
        }
    }

    public void BuyFood()
    {
        if (GameManager.Instance.currentMoney >= foodPrice)
        {
            // Parayı kes
            GameManager.Instance.currentMoney -= foodPrice;

            // Yemeği spawn noktasında oluştur
            Instantiate(foodPrefab, spawnPoint.position, Quaternion.identity);

            UpdateUI();
            GameManager.Instance.ShowWarning("<color=green>Sipariş alındı: Konserve.</color>");
        }
        else
        {
            GameManager.Instance.ShowWarning("<color=red>Bakiye yetersiz!</color>");
        }
    }
}