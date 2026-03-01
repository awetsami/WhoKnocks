using UnityEngine;

public class HatchController : MonoBehaviour
{
    [Header("Ayarlar")]
    public Vector3 slideAmount = new Vector3(0.5f, 0, 0); // Ne tarafa ne kadar kayacak?
    public float speed = 5f;

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isOpen = false;

    void Start()
    {
        // Baþlangýç pozisyonunu "Kapalý" kabul ediyoruz
        closedPos = transform.localPosition;
        openPos = closedPos + slideAmount;
    }

    void Update()
    {
        // Hedef pozisyona doðru kaydýr
        Vector3 target = isOpen ? openPos : closedPos;
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, Time.deltaTime * speed);
    }

    public void ToggleHatch()
    {
        isOpen = !isOpen; // Açýksa kapa, kapalýysa aç

        if (isOpen) Debug.Log("Þlak! (Kapak Açýldý)");
        else Debug.Log("Þlak! (Kapak Kapandý)");
    }
}