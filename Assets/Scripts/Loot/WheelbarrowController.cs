using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WheelbarrowController : MonoBehaviour
{
    [Header("Highlight Settings")]
    public float glowIntensity = 2.5f;
    private Material[] materials;
    private Color[] originalColors;

    [Header("Interaction Settings")]
    public bool isHeld = false;
    private Rigidbody rb;
    private Transform targetHoldPoint; // Rigidbody yerine Transform oldu

    [Header("Physics Settings")]
    public float followSpeed = 15f; // Eline gelme hızı
    public float rotationSpeed = 10f;

    [Header("Ağırlık ve Yavaşlama")]
    public float emptyMass = 20f;
    public float maxWeight = 70f;
    public float minSpeedMultiplier = 0.4f;

    public float GetSpeedMultiplier()
    {
        if (!isHeld || rb == null) return 1f;

        // Mevcut ağırlığın oranını hesapla ve hızı ona göre kıs
        float weightRatio = Mathf.InverseLerp(emptyMass, maxWeight, rb.mass);
        return Mathf.Lerp(1f, minSpeedMultiplier, weightRatio);
    }
    // Bu fonksiyonu karakterinin kendi Yürüme (Movement) scriptinden çağıracağız
   

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0); // Devrilmeyi zorlaştırır

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            materials = renderer.materials;
            originalColors = new Color[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].EnableKeyword("_EMISSION");
                originalColors[i] = materials[i].color;
            }
        }
    }

    void FixedUpdate()
    {
        if (isHeld && targetHoldPoint != null)
        {
            // 1. KUSURSUZ POZİSYON TAKİBİ: 
            // Aradaki mesafeyi zaman dilimine (Time.fixedDeltaTime) bölerek, 
            // arabanın o an tam olarak elinde olması için gereken "kusursuz hızı" buluyoruz.
            Vector3 distance = targetHoldPoint.position - rb.position;
            Vector3 exactVelocity = distance / Time.fixedDeltaTime;

            // Araba bir duvara takıldığında fizik motorunun sonsuz güç uygulayıp 
            // dünyayı patlatmasını engellemek için maksimum hızı (örn: 100) sınırlandırıyoruz.
            rb.linearVelocity = Vector3.ClampMagnitude(exactVelocity, 100f);

            // 2. ROTASYON TAKİBİ
            // Dönüş hızını (rotationSpeed) Unity Inspector panelinden 20 veya 30 gibi yüksek bir değere çekebilirsin.
            Quaternion targetRot = targetHoldPoint.rotation;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * rotationSpeed));
        }
    }

    // Artık Rigidbody değil, doğrudan Transform alıyor
    public void ToggleGrab(Transform playerHoldPoint)
    {
        isHeld = !isHeld;

        // Kasanın içindeki Storage scriptini buluyoruz
        WheelbarrowStorage storage = GetComponentInChildren<WheelbarrowStorage>();

        if (isHeld)
        {
            targetHoldPoint = playerHoldPoint;
            rb.isKinematic = false;
            rb.useGravity = false;

            // SENİN MANTIĞIN: Arabayı eline aldığında içindeki tüm eşyaları dondur (sabitlensinler)
            if (storage != null)
            {
                foreach (LootableItem item in storage.storedItems)
                {
                    if (item != null && item.GetComponent<Rigidbody>() != null)
                        item.GetComponent<Rigidbody>().isKinematic = true;
                }
            }

            Debug.Log("<color=green>Araba Tutuldu, İçindeki Eşyalar Kilitlendi.</color>");
        }
        else
        {
            targetHoldPoint = null;
            rb.useGravity = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // SENİN MANTIĞIN: Arabayı bıraktığında içindeki tüm eşyaların fiziğini serbest bırak
            if (storage != null)
            {
                foreach (LootableItem item in storage.storedItems)
                {
                    if (item != null && item.GetComponent<Rigidbody>() != null)
                        item.GetComponent<Rigidbody>().isKinematic = false;
                }
            }

            Debug.Log("<color=red>Araba Bırakıldı, Eşyaların Fiziği Açıldı.</color>");
        }
    }

    public void SetHighlight(bool isHighlighted)
    {
        if (materials == null || isHeld) return;
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetColor("_EmissionColor", isHighlighted ? originalColors[i] * glowIntensity : Color.black);
        }
    }
    // Arabaya eşya eklendiğinde veya çıkarıldığında ağırlığını güncelleyen fonksiyon
    public void AddWeight(float weightAmount)
    {
        if (rb != null)
        {
            rb.mass += weightAmount;

            // Ağırlık sıfırın altına düşmesin diye bir güvenlik sınırı (Varsayılan boş ağırlık 20 diyelim)
            if (rb.mass < 20f) rb.mass = 20f;
        }
    }
}