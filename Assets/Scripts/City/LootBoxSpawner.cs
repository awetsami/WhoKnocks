using UnityEngine;
using System.Collections.Generic;

public class LootBoxSpawner : MonoBehaviour
{
    [Header("Lootbox Ayarları")]
    public GameObject lootBoxPrefab;
    public int boxesPerNight = 5;

    [Header("Doğma Noktaları")]
    public List<Transform> spawnPoints;

    private List<GameObject> activeBoxes = new List<GameObject>();

    // Sadece bu fonksiyon çağrıldığında çalışacak!
    public void SpawnNightLootBoxes()
    {
        if (lootBoxPrefab == null || spawnPoints.Count == 0) return;

        ClearOldBoxes();

        List<Transform> availablePoints = new List<Transform>(spawnPoints);
        int spawnCount = Mathf.Min(boxesPerNight, availablePoints.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[randomIndex];

            Vector3 spawnPos = selectedPoint.position + Vector3.up * 0.5f;
            GameObject newBox = Instantiate(lootBoxPrefab, spawnPos, selectedPoint.rotation);

            activeBoxes.Add(newBox);
            availablePoints.RemoveAt(randomIndex);
        }
    }

    public void ClearOldBoxes()
    {
        foreach (var box in activeBoxes)
        {
            if (box != null) Destroy(box);
        }
        activeBoxes.Clear();
    }
}