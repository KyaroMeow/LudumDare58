using UnityEngine;

public class RandomIndicatorBar : MonoBehaviour
{
    [SerializeField] private float minScaleX = 0.15f;
    [SerializeField] private float maxScaleX = 1f;
    [SerializeField] private float changeSpeed = 2.2f;
    [SerializeField] private float minDelay = 0.8f;
    [SerializeField] private float maxDelay = 2.5f;

    private Vector3 initialScale;
    private float currentScaleX;
    private float targetScaleX;
    private float nextChangeTime;

    private void Awake()
    {
        initialScale = transform.localScale;
        currentScaleX = initialScale.x;
        targetScaleX = currentScaleX;
        ScheduleNextChange();
    }

    private void Update()
    {
        if (Time.time >= nextChangeTime)
        {
            targetScaleX = Random.Range(minScaleX, maxScaleX);
            ScheduleNextChange();
        }

        currentScaleX = Mathf.MoveTowards(currentScaleX, targetScaleX, changeSpeed * Time.deltaTime);

        Vector3 newScale = initialScale;
        newScale.x = currentScaleX;
        transform.localScale = newScale;
    }

    private void ScheduleNextChange()
    {
        nextChangeTime = Time.time + Random.Range(minDelay, maxDelay);
    }
}