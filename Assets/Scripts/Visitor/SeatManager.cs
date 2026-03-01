using UnityEngine;
using System.Collections.Generic;

public class SeatManager : MonoBehaviour
{
    public static SeatManager Instance;

    [Header("Tüm Koltuklar")]
    public List<Seat> allSeats; // Sahnedeki koltuklarý buraya sürükle

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // Boþ bir koltuk bul ve ver
    public Seat GetFreeSeat()
    {
        foreach (Seat seat in allSeats)
        {
            if (!seat.isOccupied)
            {
                return seat;
            }
        }
        return null; // Hiç yer yoksa null döner
    }
}