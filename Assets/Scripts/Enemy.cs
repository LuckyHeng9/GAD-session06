using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Lane Setup")]
    public float[] lanePositions = { -3.5f, 0f, 3.5f };

    [Header("Speed")]
    public float baseSpeed = 10f;
    public float maxSpeed = 28f;
    public float speedRampRate = 0.5f;

    [Header("Swerve Settings")]
    public float swerveRange = 12f;         // how close to player before swerving
    public float swerveSpeed = 5f;          // how fast it slides sideways when swerving
    public float swerveDistance = 8f;       // how far sideways it swerves before destroying
    public float smoothAccel = 4f;          // smoothing on all lateral movement

    [Header("Damage Settings")]
    public float damage = 10f;
    public float damageCooldown = 1f;

    [Header("Despawn")]
    public float despawnBehindPlayer = 10f;
    public float despawnZ = -20f;

    [HideInInspector] public Transform player;

    private float currentSpeed;
    private float targetX;
    private float currentX;
    private float smoothVelocityX = 0f;     // used by SmoothDamp
    private float gameTime;
    private bool isSwerving = false;
    private float swerveTargetX;
    private float lastDamageTime = -999f;
    private PlayerHealth playerHealth;

    void Start()
    {
        // Pick a random starting lane
        int startLane = Random.Range(0, lanePositions.Length);
        targetX = lanePositions[startLane];
        currentX = targetX;

        Vector3 pos = transform.position;
        pos.x = currentX;
        transform.position = pos;

        currentSpeed = baseSpeed;

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        StartCoroutine(LaneWanderRoutine());
    }

    void Update()
    {
        gameTime += Time.deltaTime;

        // Speed ramp
        currentSpeed = Mathf.Min(baseSpeed + speedRampRate * gameTime, maxSpeed);

        // Move forward
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime, Space.World);

        // --- Swerve logic ---
        if (player != null && !isSwerving)
        {
            float distAhead = transform.position.z - player.position.z;
            bool playerIsClose = distAhead > 0f && distAhead < swerveRange;

            if (playerIsClose)
            {
                // Pick a swerve direction away from player
                float playerX = player.position.x;
                float swerveDir = (transform.position.x >= playerX) ? 1f : -1f;
                swerveTargetX = transform.position.x + swerveDir * swerveDistance;
                isSwerving = true;
                StopAllCoroutines(); // stop wander while swerving
            }
        }

        // Smooth lateral movement
        float desiredX = isSwerving ? swerveTargetX : targetX;
        currentX = Mathf.SmoothDamp(currentX, desiredX, ref smoothVelocityX, 1f / smoothAccel);

        Vector3 p = transform.position;
        p.x = currentX;
        transform.position = p;

        // Destroy once fully swerved off
        if (isSwerving && Mathf.Abs(currentX - swerveTargetX) < 0.1f)
        {
            Destroy(gameObject);
            return;
        }

        // Despawn behind player
        if (player != null)
        {
            if (transform.position.z < player.position.z - despawnBehindPlayer)
                Destroy(gameObject);
        }
        else
        {
            if (transform.position.z < despawnZ)
                Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        TryDealDamage(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        TryDealDamage(other);
    }

    void TryDealDamage(Collider other)
    {
        if (Time.time - lastDamageTime < damageCooldown) return;
        lastDamageTime = Time.time;

        if (playerHealth == null)
            playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth != null)
            playerHealth.TakeDamage(damage);
    }

    // Wanders between lanes slowly when far from player
    IEnumerator LaneWanderRoutine()
    {
        while (true)
        {
            float wait = Random.Range(2f, 4f);
            yield return new WaitForSeconds(wait);

            if (!isSwerving && lanePositions.Length > 1)
            {
                float closest = float.MaxValue;
                int closestIdx = 0;
                for (int i = 0; i < lanePositions.Length; i++)
                {
                    float d = Mathf.Abs(lanePositions[i] - currentX);
                    if (d < closest) { closest = d; closestIdx = i; }
                }

                // Pick any lane that isn't the current one
                int newLane = closestIdx;
                while (newLane == closestIdx)
                    newLane = Random.Range(0, lanePositions.Length);

                targetX = lanePositions[newLane];
            }
        }
    }
}