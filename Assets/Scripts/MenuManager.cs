using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{

    public void Play()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
    }

    public void ShowOptionsMenu(GameObject objectToToggle)
    {
        objectToToggle.SetActive(!objectToToggle.activeSelf);
    }

}