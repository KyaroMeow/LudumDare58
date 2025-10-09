using UnityEngine;
using System.Collections;

public class Hands : MonoBehaviour
{
    [SerializeField] private GameObject[] hands;
    private int handID = 5;
    private Animator animator;

    void Start()
    {
        GetAnimator();
    }
    
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
        animator.SetTrigger("TakeDamage");
        StartCoroutine(WaitForTakeDamageAnimation());
    }

    private IEnumerator WaitForTakeDamageAnimation()
    {
        yield return null;
        float animationLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animationLength);
        SwapHand();
    }

    public void GetAnimator()
    {
        animator = hands[handID].GetComponent<Animator>();
    }
    
    private void SwapHand()
    {
        if (handID > 0)
        {
            hands[handID].SetActive(false);
            handID--;
            hands[handID].SetActive(true);
            GetAnimator();
        }
    }
}