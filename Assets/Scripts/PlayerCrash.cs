using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerCrash : MonoBehaviour
{
    [Header("Crash Physics")]
    public float minCrashSpeed = 5f;          // minimum speed to trigger crash
    public float impactForceMultiplier = 2f;  // how hard the tumble is
    public float torqueMultiplier = 3f;       // how much it spins
    public float upwardForce = 4f;            // slight lift on impact

    [Header("Game Over Delay")]
    public float gameOverDelay = 2.5f;        // seconds of tumbling before Game Over

    [Header("References")]
    public MonoBehaviour playerController;    // drag your player movement script here
    public GameObject gameOverUI;             // drag your Game Over canvas/panel here

    private Rigidbody rb;
    private bool hasCrashed = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Call this from Enemy or Building collider
    public void TriggerCrash(Vector3 impactDirection, float speed)
    {
        if (hasCrashed) return;
        if (speed < minCrashSpeed) return;

        hasCrashed = true;

        // Disable player control immediately
        if (playerController != null)
            playerController.enabled = false;

        // Hand full control to physics
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;

        // Launch upward + in impact direction
        Vector3 force = impactDirection.normalized * speed * impactForceMultiplier;
        force.y += upwardForce;
        rb.AddForce(force, ForceMode.Impulse);

        // Random realistic tumble torque
        Vector3 randomTorque = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-0.5f, 0.5f),
            Random.Range(-1f, 1f)
        ) * speed * torqueMultiplier;

        rb.AddTorque(randomTorque, ForceMode.Impulse);

        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(gameOverDelay);

        // Freeze tumble
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Show Game Over UI
        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        // Optional: pause game
        Time.timeScale = 0f;
    }

    public bool HasCrashed => hasCrashed;

    public void ResetCrash()
    {
        hasCrashed = false;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        Time.timeScale = 1f;

        if (playerController != null)
            playerController.enabled = true;

        if (gameOverUI != null)
            gameOverUI.SetActive(false);
    }
}