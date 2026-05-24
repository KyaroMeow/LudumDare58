using UnityEngine;

[DisallowMultipleComponent]
public class MenuSettingsAudioState : MonoBehaviour
{
    [SerializeField] private MenuAudioManager menuAudioManager;

    private void OnEnable()
    {
        if (ResolveMenuAudioManager())
        {
            menuAudioManager.EnterSettingsState();
        }
    }

    private void OnDisable()
    {
        if (ResolveMenuAudioManager())
        {
            menuAudioManager.EnterMainMenuState();
        }
    }

    private bool ResolveMenuAudioManager()
    {
        if (menuAudioManager != null)
        {
            return true;
        }

        menuAudioManager = FindFirstObjectByType<MenuAudioManager>();
        return menuAudioManager != null;
    }
}
