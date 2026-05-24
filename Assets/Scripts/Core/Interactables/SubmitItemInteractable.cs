using UnityEngine;

public class SubmitItemInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue pressSfx;

    public void Interact(Transform holdPosition)
    {
        PlaySfx(pressSfx);
        PlayerInteraction.Instance?.HandleStopInteraction();
        GameManager.Instance?.SubmitCurrentItem();
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
