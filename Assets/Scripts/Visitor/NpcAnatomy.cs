using UnityEngine;
using System.Collections;

public class NpcAnatomy : MonoBehaviour
{
    [Header("Vücut Parçalarý")]
    public Renderer head;
    public Renderer body;
    public Renderer armL, armR, legL, legR;

    [Header("Renk Tonlarý (Çok Ýnce Ayar)")]
    // Normal saðlýklý insan rengi
    public Color normalSkinColor = new Color(1f, 0.9f, 0.8f);

    // ANOMALÝ RENKLERÝ (Normalden çok az farklý)
    // Biraz grimsi/soluk
    public Color paleSkinColor = new Color(0.9f, 0.92f, 0.9f);
    // Biraz kýrmýzýmsý/ateþli
    public Color feverSkinColor = new Color(1f, 0.85f, 0.8f);

    [Header("Ses")]
    public AudioSource mouthAudio;
    public AudioClip[] coughSounds;

    private bool isSick = false;

    public void BuildBody(bool isAnomali)
    {
        isSick = isAnomali;

        // 1. Önce herkesi saðlýklý yap
        SetAllColors(normalSkinColor);

        // 2. Anomali ise SÝNSÝ belirtiler ekle
        if (isAnomali)
        {
            // Rastgele bir belirti seç (Her anomali ayný deðildir)
            int symptomType = Random.Range(0, 3); // 0, 1, veya 2

            switch (symptomType)
            {
                case 0: // Sadece Rengi Bozuk (Ses yok)
                    ApplySubtleDiscoloration();
                    break;

                case 1: // Sadece Öksürüyor (Rengi normal)
                    if (gameObject.activeInHierarchy) StartCoroutine(RareCoughRoutine());
                    break;

                case 2: // Hem Rengi Bozuk Hem Titriyor (Ses yok)
                    ApplySubtleDiscoloration();
                    if (gameObject.activeInHierarchy) StartCoroutine(MicroTwitchRoutine());
                    break;
            }
        }
    }

    void SetAllColors(Color c)
    {
        SetColor(head, c); SetColor(body, c);
        SetColor(armL, c); SetColor(armR, c);
        SetColor(legL, c); SetColor(legR, c);
    }

    void SetColor(Renderer part, Color color) { if (part != null) part.material.color = color; }

    void ApplySubtleDiscoloration()
    {
        // Ya soluk benizli olsun ya da ateþli
        Color targetColor = (Random.value > 0.5f) ? paleSkinColor : feverSkinColor;

        // Sadece tek bir uzuv deðil, genelde kafa veya tüm vücut hafif deðiþir
        // Zorlaþtýrmak için sadece kafayý deðiþtirelim
        SetColor(head, targetColor);

        // %30 ihtimalle elleri de deðiþsin
        if (Random.value > 0.7f)
        {
            SetColor(armL, targetColor);
            SetColor(armR, targetColor);
        }
    }

    // --- NADÝR ÖKSÜRÜK ---
    IEnumerator RareCoughRoutine()
    {
        while (isSick)
        {
            // 20 ile 45 saniye arasý bekle (Oyuncu acele ederse kaçýrýr)
            yield return new WaitForSeconds(Random.Range(20f, 45f));

            if (mouthAudio != null && coughSounds.Length > 0)
            {
                mouthAudio.pitch = Random.Range(0.9f, 1.1f); // Sesi azýcýk deðiþtir
                mouthAudio.PlayOneShot(coughSounds[Random.Range(0, coughSounds.Length)]);
            }
        }
    }

    // --- MÝKRO TÝTREME (YENÝ BELÝRTÝ) ---
    IEnumerator MicroTwitchRoutine()
    {
        while (isSick)
        {
            // 5-10 saniyede bir
            yield return new WaitForSeconds(Random.Range(5f, 10f));

            // Kafayý çok hýzlýca yamult ve düzelt (Tik gibi)
            Transform t = head != null ? head.transform : transform;

            t.Rotate(0, 0, 15); // Ani bükülme
            yield return new WaitForSeconds(0.05f); // Çok kýsa bekle
            t.Rotate(0, 0, -15); // Düzelme
        }
    }
}