using UnityEngine;

// Attach this to Enemy and Building GameObjects
// Make sure they have a Collider (can be trigger or solid)
public class CrashTrigger : MonoBehaviour
{
    [Header("Crash Settings")]
    public float crashSpeedThreshold = 1f;  

    void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.CompareTag("Player")) return;
        HandleCrash(col.gameObject, col.relativeVelocity, col.relativeVelocity.magnitude);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Estimate impact direction from relative positions
        Vector3 impactDir = other.transform.position - transform.position;
        Rigidbody playerRb = other.GetComponent<Rigidbody>();
        float speed = playerRb != null ? playerRb.velocity.magnitude : crashSpeedThreshold + 1f;
        HandleCrash(other.gameObject, impactDir, speed);
    }

    void HandleCrash(GameObject playerObj, Vector3 impactDir, float speed)
    {
        if (speed < crashSpeedThreshold) return;

        PlayerCrash crash = playerObj.GetComponent<PlayerCrash>();
        if (crash != null)
            crash.TriggerCrash(impactDir, speed);
    }
}