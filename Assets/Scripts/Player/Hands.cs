using UnityEngine;
using System.Collections;

public class Hands : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer hand;
    [SerializeField] private Material burnFinger;
    [SerializeField] private Animator animator;
    [SerializeField] private int[] fingersNum = {3, 0, 2, 4, 5};

    private int fingerID = 0;
    private bool warnedMissingAnimator;
    private bool warnedMissingHand;
    private bool warnedMissingBurnFinger;
    private bool warnedMissingFingerConfig;
    private bool warnedFullyDamaged;
    private bool warnedInvalidMaterialIndex;

    public bool IsFullyDamaged => fingersNum == null || fingerID >= fingersNum.Length;
    public int DamageStageCount => fingersNum != null ? fingersNum.Length : 0;

    public void PlayTakeItem()
    {
        TrySetAnimatorTrigger("TakeItem");
    }

    public void PlayPressButton()
    {
        TrySetAnimatorTrigger("PressButton");
    }

    public void PlayTakeDamage()
    {
        TryPlayTakeDamage();
    }

    public bool TryPlayTakeDamage()
    {
        if (fingersNum == null || fingersNum.Length == 0)
        {
            WarnOnce(ref warnedMissingFingerConfig, "Hand damage skipped: no finger material indices are configured.");
            return false;
        }

        if (fingerID >= fingersNum.Length)
        {
            WarnOnce(ref warnedFullyDamaged, "Hand damage skipped: all configured fingers are already damaged.");
            return false;
        }

        int materialIndex = fingersNum[fingerID];
        fingerID++;
        StartCoroutine(StartTakeDamageCoroutine(materialIndex));
        return true;
    }

    private IEnumerator StartTakeDamageCoroutine(int materialIndex)
    {
        TrySetAnimatorTrigger("TakeDamage");
        TryApplyBurnMaterial(materialIndex);
        yield return new WaitForSeconds(2f);
    }

    private void TryApplyBurnMaterial(int materialIndex)
    {
        if (hand == null)
        {
            WarnOnce(ref warnedMissingHand, "Hand damage visual skipped: SkinnedMeshRenderer is not assigned.");
            return;
        }

        if (burnFinger == null)
        {
            WarnOnce(ref warnedMissingBurnFinger, "Hand damage visual skipped: burnFinger material is not assigned.");
            return;
        }

        Material[] currentMaterials = hand.materials;
        if (currentMaterials == null || materialIndex < 0 || materialIndex >= currentMaterials.Length)
        {
            WarnOnce(ref warnedInvalidMaterialIndex, $"Hand damage visual skipped: material index {materialIndex} is outside hand materials.");
            return;
        }

        currentMaterials[materialIndex] = burnFinger;
        hand.materials = currentMaterials;
    }

    private bool TrySetAnimatorTrigger(string triggerName)
    {
        if (animator == null)
        {
            WarnOnce(ref warnedMissingAnimator, $"Hand animation '{triggerName}' skipped: Animator is not assigned.");
            return false;
        }

        animator.SetTrigger(triggerName);
        return true;
    }

    private void WarnOnce(ref bool warningFlag, string message)
    {
        if (warningFlag)
        {
            return;
        }

        warningFlag = true;
        Debug.LogWarning(message, this);
    }
}
