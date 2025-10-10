using UnityEngine;
using System.Collections;

public class Hands : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer hand;
    [SerializeField] private Material burnFinger;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    private int[] fingersNum = {3, 0, 2, 4, 5};
    private int fingerID = 0;

    public void PlayTakeItem()
    {
        animator.SetTrigger("TakeItem");
    }

    public void PlayPressButton()
    {
        animator.SetTrigger("PressButton");
    }

    public void PlayTakeDamage()
    {
        StartCoroutine(StartTakeDamageCorutine());
    }
    private IEnumerator StartTakeDamageCorutine()
    {
        Material[] currentMaterials = hand.materials;
        animator.SetTrigger("TakeDamage");
        currentMaterials[fingersNum[fingerID]] = burnFinger;
        hand.materials = currentMaterials;
        fingerID++;
        yield return new WaitForSeconds(2f);
        audioSource.Play();
    }
}