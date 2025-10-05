using System.Collections;
using UnityEngine;

public class Lights : MonoBehaviour
{
    private Light lightComponent;
    private Coroutine colorChangeCoroutine;
    private Color originalColor;
    void Start()
    {
        lightComponent = GetComponent<Light>();
        originalColor = lightComponent.color;
    }
    public void ChangeColorRed()
    {
        if (colorChangeCoroutine != null)
            StopCoroutine(colorChangeCoroutine);

        colorChangeCoroutine = StartCoroutine(ColorChangeRoutine(Color.red));
    }
    public void ChangeColorGreen()
    {
        if (colorChangeCoroutine != null)
            StopCoroutine(colorChangeCoroutine);

        colorChangeCoroutine = StartCoroutine(ColorChangeRoutine(Color.green));
    }
    private IEnumerator ColorChangeRoutine(Color color)
    {
        lightComponent.color = color;
        yield return new WaitForSeconds(0.5f);
        lightComponent.color = originalColor;
    }
}
