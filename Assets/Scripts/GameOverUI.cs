using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public GameObject restartButton; // drag RestartButton here
    public GameObject quitButton;    // drag QuitButton here

    public void Show()
    {
        gameObject.SetActive(true);
        restartButton.SetActive(true);  // show restart
        quitButton.SetActive(true);    // hide quit
    }

    public void OnRestartButton()
    {
        Debug.Log("Restart clicked!");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnQuitButton()
    {
        Debug.Log("Quit clicked!");
        Time.timeScale = 1f;


        UnityEditor.EditorApplication.isPlaying = false;

    }
}