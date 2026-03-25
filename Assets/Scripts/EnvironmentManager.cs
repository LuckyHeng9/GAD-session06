using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Sky Objects")]
    public GameObject skyDome;
    public GameObject mountainSkybox;

    [Header("Ground")]
    public GameObject groundPlane;

    [Header("Sky Scale")]
    public float skyScale = 50f;
    public float mountainScale = 1f;   

    private Transform player;
    private float skyDomeY;
    private float mountainY;
    private float groundY;

    void Awake()
    {
        player = GameObject.FindWithTag("Player").transform;

        if (skyDome != null)
        {
            skyDomeY = skyDome.transform.position.y;
            skyDome.transform.localScale = Vector3.one * skyScale;
        }

        if (mountainSkybox != null)
        {
            // Just remember Y — don't touch position at all
            mountainY = mountainSkybox.transform.position.y;
            mountainSkybox.transform.localScale = Vector3.one * mountainScale;
        }

        if (groundPlane != null)
        {
            groundY = groundPlane.transform.position.y;
            groundPlane.transform.rotation = Quaternion.identity;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        if (skyDome != null)
            skyDome.transform.position = new Vector3(
                player.position.x,
                skyDomeY,
                player.position.z
            );

        if (mountainSkybox != null)
            mountainSkybox.transform.position = new Vector3(
                player.position.x,
                mountainY,
                player.position.z
            );

        if (groundPlane != null)
            groundPlane.transform.position = new Vector3(
                player.position.x,
                groundY,
                player.position.z
            );
    }
}