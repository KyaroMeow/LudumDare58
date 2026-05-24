using UnityEngine;

public class GeneratorLeverInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private SecuritySystem securitySystem;
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue activationSfx;

    public void Interact(Transform holdPosition)
    {
        if (securitySystem == null)
        {
            return;
        }

        bool result = securitySystem.TryManualShutdown();
        if (result)
        {
            PlaySfx(activationSfx);
        }

        Debug.Log(result ? "Manual camera shutdown started." : "Generator is not ready yet.");
    }

    public void StopInteract()
    {
    }

    private void PlaySfx(SfxCue cue)
    {
        if (cue == null)
        {
            return;
        }

        if (sfxEmitter == null)
        {
            sfxEmitter = GetComponent<SfxEmitter>();
            if (sfxEmitter == null)
            {
                sfxEmitter = gameObject.AddComponent<SfxEmitter>();
            }
        }

        sfxEmitter.Play(cue);
    }
}
