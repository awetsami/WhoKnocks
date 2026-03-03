using UnityEngine;
using TMPro;

public class InteractionSystem : MonoBehaviour
{
    [Header("Ayarlar")]
    public Transform holdPos;
    public float pickUpRange = 3f;
    public float throwForce = 500f;
    public LayerMask targetLayers;
    public LayerMask wallLayer;
    public TMP_Text infoText;

    [Header("Araba Ayarları")]
    public Transform wheelbarrowHoldPoint;
    private WheelbarrowController heldWheelbarrow;

    private GameObject heldObj;
    private Rigidbody heldObjRb;
    private GameObject lastKnownTarget;
    private float cleanupTimer = 0f;
    private float bufferTime = 0.15f;

    // Oyuncunun yürüme scriptine hız çarpanı gönderen köprü
    public float GetPlayerSpeedMultiplier()
    {
        if (heldWheelbarrow != null)
        {
            return heldWheelbarrow.GetSpeedMultiplier();
        }
        return 1f; // Araba tutulmuyorsa normal hızda (1x) devam et
    }
    void Update()
    {
        GameObject currentTarget = GetBestTarget();
        HandleUIBuffer(currentTarget);

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldWheelbarrow != null)
            {
                heldWheelbarrow.ToggleGrab(wheelbarrowHoldPoint);
                heldWheelbarrow = null;
                if (infoText) infoText.text = "";
                return;
            }

            if (heldObj == null)
            {
                if (currentTarget != null) TryInteract(currentTarget);
            }
            else
            {
                if (currentTarget != null)
                {
                    VisitorController visitor = currentTarget.GetComponentInParent<VisitorController>();
                    IDCard cardInHand = heldObj.GetComponent<IDCard>();

                    if (visitor != null && cardInHand != null)
                    {
                        GiveCardBack(visitor);
                        return;
                    }
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            if (heldObj != null)
            {
                BatteryItem battery = heldObj.GetComponent<BatteryItem>();
                if (battery != null && UVFlashlightController.Instance != null)
                {
                    if (UVFlashlightController.Instance.TryReloadBattery())
                    {
                        Destroy(heldObj);
                        heldObj = null;
                        if (infoText) infoText.text = "";
                    }
                }
            }
        }

        if (Input.GetMouseButtonDown(1) && heldObj != null) DropObject(false);
        if (Input.GetMouseButtonDown(0) && heldObj != null) DropObject(true);
    }

    void LateUpdate()
    {
        if (heldObj != null) MoveObject();
    }

    void GiveCardBack(VisitorController visitor)
    {
        visitor.TakeCardBack();
        Destroy(heldObj);
        heldObj = null;
        if (infoText) infoText.text = "";
    }

    void UpdateUI(GameObject target)
    {
        if (infoText == null) return;

        if (heldWheelbarrow != null)
        {
            infoText.text = "Arabayı Bırak [E]";
            return;
        }

        if (heldObj != null)
        {
            if (heldObj.GetComponent<IDCard>())
            {
                if (target != null && target.GetComponentInParent<VisitorController>()) { infoText.text = "Kimliği Geri Ver [E]"; return; }
                infoText.text = "Kimlik\n[Sağ Tık] Bırak"; return;
            }
            if (heldObj.GetComponent<BatteryItem>()) { infoText.text = "UV Pili\n[G] Cihaza Yükle\n[Sağ Tık] Bırak"; return; }
            if (heldObj.GetComponent<LootableItem>()) { infoText.text = "Ganimet\n[Sol Tık] Fırlat\n[Sağ Tık] Bırak"; return; }

            infoText.text = "Eşya\n[Sağ Tık] Bırak";
            return;
        }

        if (target == null) { infoText.text = ""; return; }

        // 1. ÖNCELİK: Alınabilir Eşyalar (Arabaya öncelik verirsek içindeki eşyayı göremeyiz)
        if (target.GetComponentInParent<LootableItem>()) { infoText.text = "Ganimet\nAl [E]"; return; }
        if (target.GetComponentInParent<BatteryItem>()) { infoText.text = "UV Pili\nAl [E]"; return; }
        if (target.GetComponentInParent<IDCard>()) { infoText.text = "Kimlik\nİncele [E]"; return; }

        // 2. ÖNCELİK: Araba Kontrolü
        if (target.GetComponentInParent<WheelbarrowController>()) { infoText.text = "Arabayı Sür [E]"; return; }

        // Diğer Etkileşimler
        if (target.GetComponentInParent<LootBox>())
        {
            LootBox box = target.GetComponentInParent<LootBox>();
            infoText.text = box.isOpened ? "Sandık (Boş)" : "Sandığı Aç [E]";
            return;
        }

        if (target.GetComponentInParent<VentilationSystem>())
        {
            if (VentilationSystem.Instance != null && VentilationSystem.Instance.isBroken)
                infoText.text = "<color=red>Şalter (Arızalı)</color>";
            else
                infoText.text = "Fanları Çalıştır [E]";
            return;
        }

        if (target.GetComponentInParent<VisitorController>()) { infoText.text = "Konuş [E]"; return; }
        if (target.GetComponentInParent<HatchController>()) { infoText.text = "Sürgü [E]"; return; }
        if (target.GetComponentInParent<DoorController>()) { infoText.text = "Kapı [E]"; return; }
        if (target.GetComponentInParent<BedController>())
        {
            if (GameManager.Instance != null)
                infoText.text = GameManager.Instance.currentPhase == DayPhase.Daytime ? "Geceye Geç [E]" : "Sabahı Bekle [E]";
            return;
        }

        infoText.text = "";
    }

    void HandleUIBuffer(GameObject currentTarget)
    {
        if (heldObj != null || heldWheelbarrow != null)
        {
            UpdateUI(currentTarget);
            return;
        }

        if (currentTarget != null)
        {
            lastKnownTarget = currentTarget;
            cleanupTimer = bufferTime;
            UpdateUI(currentTarget);
        }
        else
        {
            if (cleanupTimer > 0)
            {
                cleanupTimer -= Time.deltaTime;
                if (heldObj == null && lastKnownTarget != null) UpdateUI(lastKnownTarget);
            }
            else
            {
                lastKnownTarget = null;
                UpdateUI(null);
            }
        }
    }
    void TryInteract(GameObject target)
    {
        //bilgisayar etkileşimi
        BunkerComputer computer = target.GetComponentInParent<BunkerComputer>();
        if (computer != null) { computer.ToggleComputer(); return; }
        // 1. ÖNCELİK: Sığınak Sistemleri (Fan, Sandık, Kapı vb. eline alamayacağın sabit şeyler)
        VentilationSystem vent = target.GetComponentInParent<VentilationSystem>();
        if (vent != null) { vent.ToggleVentilation(); return; }

        WheelbarrowController wheelbarrow = target.GetComponentInParent<WheelbarrowController>();
        if (wheelbarrow != null && wheelbarrowHoldPoint != null)
        {
            heldWheelbarrow = wheelbarrow;
            heldWheelbarrow.ToggleGrab(wheelbarrowHoldPoint);
            return;
        }

        LootBox box = target.GetComponentInParent<LootBox>();
        if (box != null && !box.isOpened) { box.OpenBox(); return; }

        HatchController hatch = target.GetComponentInParent<HatchController>();
        if (hatch != null) { hatch.ToggleHatch(); return; }

        BedController bed = target.GetComponentInParent<BedController>();
        if (bed != null) { bed.Sleep(); return; }

        DoorController door = target.GetComponentInParent<DoorController>();
        if (door != null) { door.Interact(); return; }

        VisitorController visitor = target.GetComponentInParent<VisitorController>();
        if (visitor != null) { visitor.StartConversation(); return; }

        IDCard idCard = target.GetComponentInParent<IDCard>();
        if (idCard != null) { ObjectInspector.Instance.Inspect(idCard.gameObject); return; }

        // 2. ÖNCELİK: Eline alınabilir (Fiziksel) Eşyalar
        if (target.GetComponentInParent<BatteryItem>() ||
            target.GetComponentInParent<LootableItem>() ||
            target.GetComponentInParent<ItemInfo>())
        {
            PickUpObject(target);
            return;
        }
    }

    GameObject GetBestTarget()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, pickUpRange, targetLayers);
        if (hits.Length == 0) return null;
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (RaycastHit hit in hits)
        {
            if (((1 << hit.collider.gameObject.layer) & wallLayer) != 0) return null;

            // Araba
            if (hit.collider.GetComponentInParent<WheelbarrowController>()) return hit.collider.gameObject;

            // Sistemler ve Etkileşimler
            if (hit.collider.GetComponentInParent<VentilationSystem>()) return hit.collider.gameObject;
            if (hit.collider.GetComponentInParent<LootBox>()) return hit.collider.gameObject;
            if (hit.collider.GetComponentInParent<HatchController>()) return hit.collider.gameObject;
            if (hit.collider.GetComponentInParent<BedController>()) return hit.collider.gameObject;
            if (hit.collider.GetComponentInParent<DoorController>()) return hit.collider.gameObject;
            if (hit.collider.GetComponentInParent<VisitorController>()) return hit.collider.gameObject;

            // Eşyalar
            if (hit.collider.GetComponentInParent<LootableItem>()) return hit.collider.gameObject;
            if (hit.collider.GetComponentInParent<BatteryItem>()) return hit.collider.gameObject;
            if (hit.collider.GetComponentInParent<IDCard>()) return hit.collider.gameObject;
            if (hit.collider.GetComponentInParent<ItemInfo>()) return hit.collider.gameObject;
        }
        return null;
    }
    void PickUpObject(GameObject pickObj)
    {
        Rigidbody rb = pickObj.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            // Eğer eşya arabanın içinden alınıyorsa, arabanın envanterinden düşür
            WheelbarrowStorage storage = pickObj.GetComponentInParent<WheelbarrowStorage>();
            LootableItem loot = pickObj.GetComponentInParent<LootableItem>();

            if (storage != null && loot != null)
            {
                storage.RemoveItemFromStorage(loot);
            }

            heldObj = rb.gameObject;
            heldObjRb = rb;
            heldObjRb.isKinematic = true;
            heldObjRb.interpolation = RigidbodyInterpolation.None;
            Collider[] cols = heldObj.GetComponentsInChildren<Collider>();
            foreach (Collider c in cols) c.enabled = false;

            if (loot != null) loot.isHeld = true;
        }
    }

    void DropObject(bool isThrow)
    {
        heldObjRb.isKinematic = false;
        heldObjRb.interpolation = RigidbodyInterpolation.Interpolate;
        Collider[] cols = heldObj.GetComponentsInChildren<Collider>();
        foreach (Collider c in cols) c.enabled = true;

        if (isThrow) heldObjRb.AddForce(transform.forward * throwForce);

        LootableItem loot = heldObj.GetComponent<LootableItem>();
        if (loot != null) loot.isHeld = false;

        heldObj = null;
    }

    void MoveObject()
    {
        Vector3 targetPos = holdPos.position;
        float distanceToHand = Vector3.Distance(transform.position, holdPos.position);
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, distanceToHand, wallLayer))
            heldObj.transform.position = hit.point - (transform.forward * 0.1f);
        else
            heldObj.transform.position = targetPos;

        heldObj.transform.rotation = holdPos.rotation;
    }
}