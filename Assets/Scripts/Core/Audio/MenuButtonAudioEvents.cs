using UnityEngine;
using UnityEngine.EventSystems;

public enum MenuButtonAudioType
{
    StartGame,
    Exit
}

[DisallowMultipleComponent]
public class MenuButtonAudioEvents : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private MenuAudioManager menuAudioManager;
    [SerializeField] private MenuButtonAudioType buttonType;

    private void Awake()
    {
        ResolveMenuAudioManager();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!ResolveMenuAudioManager())
        {
            return;
        }

        if (buttonType == MenuButtonAudioType.StartGame)
        {
            menuAudioManager.OnStartHoverEnter();
            return;
        }

        menuAudioManager.OnExitHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!ResolveMenuAudioManager())
        {
            return;
        }

        if (buttonType == MenuButtonAudioType.StartGame)
        {
            menuAudioManager.OnStartHoverExit();
            return;
        }

        menuAudioManager.OnExitHoverExit();
    }

    private bool ResolveMenuAudioManager()
    {
        if (menuAudioManager != null)
        {
            return true;
        }

        menuAudioManager = GetComponentInParent<MenuAudioManager>();

        if (menuAudioManager == null)
        {
            menuAudioManager = FindFirstObjectByType<MenuAudioManager>();
        }

        return menuAudioManager != null;
    }
}
