using UnityEngine;

[RequireComponent(typeof(Collider))]
public class VentHandKeyPickup : MonoBehaviour, IInteractable, IOneShotInteractable
{
    [SerializeField] private VentHandIntroController introController;
    [SerializeField] private ToolCaseLock toolCaseLock;
    [SerializeField] private InventoryItemDefinition inventoryItem;
    [SerializeField] private SfxCue pickupSfx;
    [SerializeField] private AudioClip pickupAudioClip;

    private AudioSource fallbackAudioSource;
    private bool pickedUp;
    private bool warnedMissingPickupSfx;

    public void Configure(
        VentHandIntroController controller,
        ToolCaseLock targetCase,
        InventoryItemDefinition item,
        SfxCue sfxCue,
        AudioClip fallbackClip)
    {
        introController = controller;
        toolCaseLock = targetCase;
        inventoryItem = item;
        pickupSfx = sfxCue;
        pickupAudioClip = fallbackClip;
    }

    public void Interact(Transform holdPosition)
    {
        if (pickedUp)
        {
            return;
        }

        if (introController != null)
        {
            pickedUp = introController.NotifyKeyPickedUp(this, inventoryItem);
        }
        else
        {
            if (inventoryItem != null &&
                (InventorySystem.Instance == null || !InventorySystem.Instance.TryAddItem(inventoryItem)))
            {
                Debug.LogWarning("The tool case key remains in the world because the inventory is full.", this);
                return;
            }

            pickedUp = true;
            PlayPickupSfx();
            toolCaseLock?.UnlockCase(inventoryItem);
            Destroy(gameObject);
        }
    }

    public void StopInteract()
    {
    }

    private void PlayPickupSfx()
    {
        if (pickupSfx != null)
        {
            SfxEmitter emitter = GetComponent<SfxEmitter>();
            if (emitter == null)
            {
                emitter = gameObject.AddComponent<SfxEmitter>();
            }

            emitter.Play(pickupSfx);
            return;
        }

        if (pickupAudioClip == null)
        {
            if (!warnedMissingPickupSfx)
            {
                warnedMissingPickupSfx = true;
                Debug.LogWarning("Vent hand key pickup SFX is not assigned.", this);
            }

            return;
        }

        if (fallbackAudioSource == null)
        {
            fallbackAudioSource = GetComponent<AudioSource>();
            if (fallbackAudioSource == null)
            {
                fallbackAudioSource = gameObject.AddComponent<AudioSource>();
            }

            fallbackAudioSource.playOnAwake = false;
        }

        fallbackAudioSource.PlayOneShot(pickupAudioClip);
    }
}
