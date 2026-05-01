using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum AIState { Patrol, Alert, Chase, Search, Stunned }

public class EnemyAI : MonoBehaviour
{
    [Header("Can ve Hasar")]
    public float health = 100f;
    public float attackDamage = 15f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime = 0f;

    private float stunTimer = 0f;

    [Header("Göz & Kulak Ayarlarý")]
    public float sightRange = 40f;
    public float fieldOfView = 120f;
    public float hearingRadius = 8f;
    public float attackRange = 1.8f;

    [Header("Hareket & Takip Ayarlarý")]
    public float walkSpeed = 2f;
    public float stalkSpeed = 2.5f;
    public float runSpeed = 5.5f;
    public float sprintDistance = 15f;

    public float searchTime = 4f;
    public float wanderRadius = 15f;
    public float searchTurnSpeed = 400f;

    [Header("Ses ve Reaksiyon Ayarlarý")]
    public AudioSource enemyAudioSource;
    public AudioClip alertSound;
    public Transform meshTransform;
    public float freakOutIntensity = 0.2f;
    public float freakOutDuration = 0.3f; // SADECE BU KADAR SÜRE TÝTREYECEK

    [Header("Kamera Sarsýntýsý (Çýðlýk)")]
    public float shakeRadius = 30f;
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.3f;

    private Vector3 originalMeshLocalPos;
    private Coroutine freakOutCoroutine; // Titremeyi kontrol etmek için

    private NavMeshAgent agent;
    public Transform currentTarget;

    private AIState currentState = AIState.Patrol;
    private Vector3 lastKnownPosition;
    private float searchTimer = 0f;
    private float loseSightTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.angularSpeed = 800f;

        if (meshTransform != null)
        {
            originalMeshLocalPos = meshTransform.localPosition;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.currentPhase == DayPhase.Daytime)
        {
            ChangeState(AIState.Patrol);
            WanderAround();
            return;
        }

        // UPDATE ÝÇÝNDEKÝ SÜREKLÝ TÝTREME KODU BURADAN SÝLÝNDÝ

        switch (currentState)
        {
            case AIState.Stunned:
                if (agent.isActiveAndEnabled)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }

                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0)
                {
                    if (agent.isActiveAndEnabled) agent.isStopped = false;
                    ChangeState(AIState.Chase);
                }
                break;

            case AIState.Patrol:
                WanderAround();
                ScanEnvironment();
                break;

            case AIState.Alert:
                agent.SetDestination(transform.position);
                transform.Rotate(0, searchTurnSpeed * Time.deltaTime, 0);

                if (currentTarget != null && CanSeeTarget(currentTarget)) ChangeState(AIState.Chase);
                else
                {
                    searchTimer -= Time.deltaTime;
                    if (searchTimer <= 0) ChangeState(AIState.Patrol);
                }
                break;

            case AIState.Chase:
                if (currentTarget == null) { ChangeState(AIState.Patrol); break; }

                if (CanSeeTarget(currentTarget))
                {
                    loseSightTimer = 0f;
                    lastKnownPosition = currentTarget.position;

                    float distToPlayer = Vector3.Distance(transform.position, currentTarget.position);

                    if (distToPlayer > sprintDistance) agent.speed = runSpeed;
                    else agent.speed = stalkSpeed;

                    if (distToPlayer <= attackRange)
                    {
                        agent.isStopped = true;
                        transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));

                        if (Time.time >= lastAttackTime + attackCooldown)
                        {
                            PlayerStats pStats = currentTarget.GetComponentInParent<PlayerStats>();
                            if (pStats != null)
                            {
                                pStats.TakeDamage(attackDamage);
                                Debug.Log("<color=red>Düþman Pençe Attý! Cýýýrt!</color>");
                            }
                            lastAttackTime = Time.time;
                        }
                    }
                    else
                    {
                        agent.isStopped = false;
                        agent.SetDestination(currentTarget.position);
                    }
                }
                else
                {
                    loseSightTimer += Time.deltaTime;
                    if (loseSightTimer > 0.5f) ChangeState(AIState.Search);
                    else agent.SetDestination(lastKnownPosition);
                }
                break;

            case AIState.Search:
                if (currentTarget != null && CanSeeTarget(currentTarget)) { ChangeState(AIState.Chase); break; }

                agent.SetDestination(lastKnownPosition);
                if (agent.remainingDistance < 1f)
                {
                    transform.Rotate(0, searchTurnSpeed * Time.deltaTime, 0);
                    searchTimer -= Time.deltaTime;
                    if (searchTimer <= 0) ChangeState(AIState.Patrol);
                }
                break;
        }
    }

    void ScanEnvironment()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDist = Mathf.Infinity;
        Transform bestTarget = null;
        bool spottedByHearing = false;

        foreach (GameObject pObj in players)
        {
            PlayerStats pStats = pObj.GetComponentInParent<PlayerStats>();
            if (pStats != null && !pStats.isOutside) continue;

            float dist = Vector3.Distance(transform.position, pObj.transform.position);
            if (dist > sightRange) continue;

            if (dist <= hearingRadius) { bestTarget = pObj.transform; spottedByHearing = true; break; }

            Vector3 dirToPlayer = (pObj.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToPlayer);

            if (angle <= fieldOfView / 2f && CanSeeTarget(pObj.transform))
            {
                if (dist < closestDist) { closestDist = dist; bestTarget = pObj.transform; }
            }
        }

        if (bestTarget != null)
        {
            currentTarget = bestTarget;
            if (spottedByHearing) { searchTimer = 2f; ChangeState(AIState.Alert); }
            else ChangeState(AIState.Chase);
        }
    }

    bool CanSeeTarget(Transform targetObj)
    {
        if (targetObj == null) return false;
        Vector3 eyePos = transform.position + Vector3.up * 0.7f + transform.forward * 0.5f;
        Vector3 targetPos = targetObj.position + Vector3.up * 0.7f;
        Vector3 dir = (targetPos - eyePos).normalized;

        RaycastHit hit;
        if (Physics.Raycast(eyePos, dir, out hit, sightRange))
        {
            if (hit.transform == targetObj || hit.transform.CompareTag("Player") || hit.transform.GetComponentInParent<PlayerStats>() != null) return true;
        }
        return false;
    }

    void WanderAround()
    {
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            Vector3 randomDest = transform.position + Random.insideUnitSphere * wanderRadius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDest, out hit, wanderRadius, NavMesh.AllAreas)) agent.SetDestination(hit.position);
        }
    }

    void ChangeState(AIState newState)
    {
        if (currentState == newState) return;

        if ((newState == AIState.Alert || newState == AIState.Chase) && (currentState == AIState.Patrol || currentState == AIState.Search))
        {
            PlayAlertReaction();
        }
        else if (newState == AIState.Patrol || newState == AIState.Stunned)
        {
            StopFreakingOut();
        }

        if (newState == AIState.Chase && currentTarget != null)
        {
            transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));
            loseSightTimer = 0f;
        }

        currentState = newState;

        if (newState == AIState.Patrol) agent.speed = walkSpeed;
        if (newState == AIState.Search) agent.speed = runSpeed;
    }

    // --- YENÝLENEN KISA SÜRELÝ REAKSÝYON ---
    void PlayAlertReaction()
    {
        // 1. Çýðlýk/Ses Çal
        if (enemyAudioSource != null && alertSound != null && !enemyAudioSource.isPlaying)
        {
            enemyAudioSource.PlayOneShot(alertSound);
        }

        // 2. Modeli SADECE BÝR ANLIÐINA titret
        if (meshTransform != null)
        {
            if (freakOutCoroutine != null) StopCoroutine(freakOutCoroutine);
            freakOutCoroutine = StartCoroutine(FreakOutRoutine());
        }

        // 3. Yakýndaki Oyuncularýn Kamerasýný Sars
        Collider[] colliders = Physics.OverlapSphere(transform.position, shakeRadius);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                CameraShake camShake = col.GetComponentInChildren<CameraShake>();
                if (camShake != null)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    float distanceFactor = 1f - (distance / shakeRadius);
                    float finalMagnitude = shakeMagnitude * distanceFactor;

                    if (finalMagnitude > 0)
                    {
                        StartCoroutine(camShake.Shake(shakeDuration, finalMagnitude));
                    }
                }
            }
        }
    }

    // Titreme Ýþlemini Yapan Timer (Süre bitince animasyon bozulmadan yoluna devam eder)
    IEnumerator FreakOutRoutine()
    {
        float elapsed = 0f;
        while (elapsed < freakOutDuration)
        {
            float offsetX = Random.Range(-freakOutIntensity, freakOutIntensity);
            float offsetZ = Random.Range(-freakOutIntensity, freakOutIntensity);
            meshTransform.localPosition = originalMeshLocalPos + new Vector3(offsetX, 0, offsetZ);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Süre dolduðunda modeli tam orijinal yerine oturt ki animatör sapýtmasýn
        meshTransform.localPosition = originalMeshLocalPos;
    }

    void StopFreakingOut()
    {
        if (freakOutCoroutine != null) StopCoroutine(freakOutCoroutine);
        if (meshTransform != null) meshTransform.localPosition = originalMeshLocalPos;
    }

    public void StunEnemy(float duration)
    {
        if (currentState == AIState.Stunned)
        {
            stunTimer = duration;
            return;
        }

        ChangeState(AIState.Stunned);
        stunTimer = duration;
        Debug.Log("<color=cyan>Yaratýk Fenerden KÖR OLDU ve donduruldu!</color>");
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log($"<color=orange>Düþman Hasar Aldý! Kalan Can: {health}</color>");

        if (currentState != AIState.Stunned && currentState != AIState.Chase)
        {
            ChangeState(AIState.Chase);
        }

        if (health <= 0) Die();
    }

    void Die()
    {
        Debug.Log("<color=green>DÜÞMAN ÖLDÜRÜLDÜ!</color>");
        Destroy(gameObject);
    }
}