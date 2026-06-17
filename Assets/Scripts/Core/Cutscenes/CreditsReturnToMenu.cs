using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsReturnToMenu : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private float autoReturnDelay = 10f;
    [SerializeField] private bool allowAnyKeySkip = true;
    [SerializeField] private bool showCursor = true;

    private float elapsed;
    private bool isLoading;

    private void Start()
    {
        if (showCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void Update()
    {
        if (isLoading)
        {
            return;
        }

        elapsed += Time.unscaledDeltaTime;

        if (elapsed >= autoReturnDelay || ShouldSkip())
        {
            LoadMenu();
        }
    }

    private bool ShouldSkip()
    {
        return allowAnyKeySkip &&
               (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1));
    }

    private void LoadMenu()
    {
        isLoading = true;
        string targetSceneName = string.IsNullOrWhiteSpace(menuSceneName) ? "Menu" : menuSceneName;
        SceneManager.LoadScene(targetSceneName);
    }
}
