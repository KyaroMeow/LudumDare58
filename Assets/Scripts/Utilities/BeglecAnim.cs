using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeglecAnim : MonoBehaviour
{
    [SerializeField] private Animator animatorVent;
    [SerializeField] private Animator animatorHand;

    public void HandOut()
    {
        animatorVent.SetTrigger("Open");
        animatorHand.SetTrigger("Start");
    }
    public void HandIn()
    {
        animatorVent.SetTrigger("Close");
        animatorHand.gameObject.SetActive(false);
    }
}
