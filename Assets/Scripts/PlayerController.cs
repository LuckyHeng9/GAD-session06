using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 250f;
    public float acceleration = 8f;
    public float deceleration = 12f;
    public float brakeForce = 25f;
    public float maxTurnSpeed = 45f;
    public float turnSmoothTime = 0.15f;

    [Header("Reverse")]
    public float maxReverseSpeed = 8f;

    [Header("Collision")]
    public float bounceForce = 8f;
    public bool gameOverOnHit = true;

    [Header("Spawner Reference")]
    public EnemySpawner enemySpawner;
    [Header("UI")]
    public GameObject gameOverUI;

    private Rigidbody rb;

    private float currentSpeed = 0f;
    private float currentTurn = 0f;
    private float turnVelocity = 0f;
    private bool isGameOver = false;
    private bool isBraking = false;
    private bool hasCrashed = false;

    public float GetCurrentSpeed() => currentSpeed;

    // ─────────────────────────────────────────────────────────────
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // ─────────────────────────────────────────────────────────────
    void Update()
    {
        if (isGameOver) return;

        HandleInput();
    }

    // ─────────────────────────────────────────────────────────────
    void FixedUpdate()
    {
        if (isGameOver || hasCrashed) return;
        ApplyMovement();
    }

    // ─────────────────────────────────────────────────────────────
    void HandleInput()
    {
        float verticalInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        isBraking = Input.GetKey(KeyCode.Space);

        if (isBraking)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeForce * Time.deltaTime);
        }
        else
        {
            float targetSpeed = verticalInput >= 0
                ? verticalInput * maxSpeed
                : verticalInput * maxReverseSpeed;

            float rate = (Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed))
                ? acceleration
                : deceleration;

            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);
        }

        float turnMultiplier = isBraking ? 0.4f : 1f;
        float targetTurn = turnInput * maxTurnSpeed * turnMultiplier;
        currentTurn = Mathf.SmoothDamp(currentTurn, targetTurn, ref turnVelocity, turnSmoothTime);
    }

    // ─────────────────────────────────────────────────────────────
    void ApplyMovement()
    {
        transform.Rotate(Vector3.up * currentTurn * Time.fixedDeltaTime);

        Vector3 move = transform.forward * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
    }

    // ─────────────────────────────────────────────────────────────
    void OnCollisionEnter(Collision collision)
    {
        // ── Enemy hit → crash + flip ──────────────────────────────
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (hasCrashed) return;

            TriggerRagdollCrash(collision);
            return;
        }

        // ── Building hit → simple bounce ─────────────────────────
        if (collision.gameObject.CompareTag("Building"))
        {
            currentSpeed = -currentSpeed * 0.3f;

            Vector3 bounceDir = (transform.position - collision.contacts[0].point).normalized;
            bounceDir.y = 0f;

            rb.velocity = Vector3.zero;
            rb.AddForce(bounceDir * bounceForce, ForceMode.Impulse);
        }
    }

    // ─────────────────────────────────────────────────────────────
    void TriggerRagdollCrash(Collision collision)
    {
        hasCrashed = true;
        currentSpeed = 0f;

        if (enemySpawner != null)
            enemySpawner.StopSpawning();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.freezeRotation = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Vector3 impactDir = (transform.position - collision.contacts[0].point).normalized;
        impactDir.y = 0.5f;

        float speed = Mathf.Max(collision.relativeVelocity.magnitude, 10f);

        rb.AddForce(impactDir * speed * 6f, ForceMode.Impulse);

        Vector3 torque = new Vector3(
            Random.Range(0.5f, 1f),
            Random.Range(-0.3f, 0.3f),
            Random.Range(-1f, 1f)
        ) * speed * 12f;

        rb.AddTorque(torque, ForceMode.Impulse);

        StartCoroutine(GameOverRoutine());
    }

    // ─────────────────────────────────────────────────────────────
    IEnumerator GameOverRoutine()
    {
        yield return new WaitForSecondsRealtime(1f);
        isGameOver = true;
        Time.timeScale = 0f;

        Debug.Log("gameOverUI is: " + gameOverUI);

        if (gameOverUI != null)
        {
            Debug.Log("Calling Show()");
            gameOverUI.GetComponent<GameOverUI>().Show();
        }
        else
        {
            Debug.Log("gameOverUI is NULL - not assigned in Inspector!");
        }

        Debug.Log("Game Over!");
    }
}