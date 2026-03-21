using UnityEngine;

[DisallowMultipleComponent]
public class GeneratorNeedleJitter : MonoBehaviour
{
    private enum LocalAxis
    {
        X,
        Y,
        Z
    }

    [SerializeField] private LocalAxis rotationAxis = LocalAxis.Z;
    [SerializeField] private float maxAngle = 10f;
    [SerializeField] private float mainAmplitude = 5.5f;
    [SerializeField] private float twitchAmplitude = 1.75f;
    [SerializeField] private float mainFrequency = 1.25f;
    [SerializeField] private float twitchFrequency = 8.5f;
    [SerializeField] private float smoothing = 0.08f;
    [SerializeField] private float minKickInterval = 0.18f;
    [SerializeField] private float maxKickInterval = 0.6f;
    [SerializeField] private float kickStrength = 3.8f;
    [SerializeField] private float kickRecovery = 16f;

    private Quaternion initialRotation;
    private float currentAngle;
    private float currentVelocity;
    private float kickOffset;
    private float noiseSeed;
    private float nextKickTime;

    private void Awake()
    {
        initialRotation = transform.localRotation;
        noiseSeed = Random.Range(0f, 100f);
        ScheduleKick();
    }

    private void Update()
    {
        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        float time = Time.time + noiseSeed;

        if (Time.time >= nextKickTime)
        {
            kickOffset += Random.Range(-kickStrength, kickStrength);
            kickOffset = Mathf.Clamp(kickOffset, -maxAngle, maxAngle);
            ScheduleKick();
        }

        kickOffset = Mathf.MoveTowards(kickOffset, 0f, kickRecovery * deltaTime);

        float mainNoise = (Mathf.PerlinNoise(noiseSeed, time * mainFrequency) - 0.5f) * 2f;
        float twitchNoise = (Mathf.PerlinNoise(noiseSeed + 17.13f, time * twitchFrequency) - 0.5f) * 2f;
        float sineShake = Mathf.Sin(time * (twitchFrequency * 0.75f)) * 0.45f;

        float targetAngle = mainNoise * mainAmplitude;
        targetAngle += twitchNoise * twitchAmplitude;
        targetAngle += sineShake * twitchAmplitude;
        targetAngle += kickOffset;
        targetAngle = Mathf.Clamp(targetAngle, -maxAngle, maxAngle);

        currentAngle = Mathf.SmoothDamp(
            currentAngle,
            targetAngle,
            ref currentVelocity,
            smoothing,
            Mathf.Infinity,
            deltaTime);

        transform.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, GetAxis());
    }

    private void ScheduleKick()
    {
        nextKickTime = Time.time + Random.Range(minKickInterval, maxKickInterval);
    }

    private Vector3 GetAxis()
    {
        switch (rotationAxis)
        {
            case LocalAxis.X:
                return Vector3.right;
            case LocalAxis.Y:
                return Vector3.up;
            default:
                return Vector3.forward;
        }
    }
}
