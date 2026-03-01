using UnityEngine;

public class Seat : MonoBehaviour
{
    [Header("Ayarlar")]
    public Transform sitPoint; // Karakterin tam poposunun geleceði nokta (Child obje yap)
    public bool isOccupied = false; // Dolu mu?

    // Koltuðu doldur
    public void Occupy()
    {
        isOccupied = true;
    }

    // Koltuðu boþalt
    public void Release()
    {
        isOccupied = false;
    }
}