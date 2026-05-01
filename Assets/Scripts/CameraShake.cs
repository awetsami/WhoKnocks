using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public IEnumerator Shake(float duration, float magnitude)
    {
        // Kameranın orijinal yerel pozisyonunu hafızaya al
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // X ve Y eksenlerinde rastgele sarsıntı hesapla
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;

            yield return null; // Bir sonraki frame'e kadar bekle
        }

        // Sarsıntı bitince kamerayı tam yerine oturt
        transform.localPosition = originalPos;
    }
}