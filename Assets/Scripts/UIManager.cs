using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textHp;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject cameraController;
    [SerializeField] private GameObject losePanel;

    public void ReloadScene()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    private void Update()
    {
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null) return;

        int currentHp = playerHealth.CurrentHP;
        textHp.text = currentHp.ToString();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            FirstPersonController fpc = cameraController.GetComponent<FirstPersonController>();
            fpc.cameraCanMove = false;

            pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void BackMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }

    public void ResumeGame()
    {
        FirstPersonController fpc = cameraController.GetComponent<FirstPersonController>();
        fpc.cameraCanMove = true;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }


}