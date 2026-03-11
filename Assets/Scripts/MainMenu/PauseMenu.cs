using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;

    [Header("Menü Panelleri")]
    public GameObject pauseMenuUI;
    public GameObject settingsMenuUI;

    public bool isPaused = false;

    void Awake()
    {
        Instance = this;
        if (pauseMenuUI) pauseMenuUI.SetActive(false);
        if (settingsMenuUI) settingsMenuUI.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        if (settingsMenuUI) settingsMenuUI.SetActive(false);

        // Zamanı normal akışına döndür
        Time.timeScale = 1f;
        isPaused = false;

        // EĞER diyalog ekranında DEĞİLSEK, fareyi tekrar kilitle ve gizle
        if (DialogueManager.Instance != null && !DialogueManager.Instance.isDialogueActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        settingsMenuUI.SetActive(false);

        // Zamanı DONDUR!
        Time.timeScale = 0f;
        isPaused = true;

        // Oyuncu menüye tıklayabilsin diye fareyi serbest bırak ve görünür yap
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // --- BUTONLARA BAĞLANACAK FONKSİYONLAR ---

    public void OpenSettings()
    {
        pauseMenuUI.SetActive(false);
        settingsMenuUI.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Oyundan Çıkılıyor...");
        // Unity Editöründe çalışmaz ama build (çıktı) aldığında oyunu kapatır
        Application.Quit();
    }
}