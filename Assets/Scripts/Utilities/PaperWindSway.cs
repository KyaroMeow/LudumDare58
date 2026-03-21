using UnityEngine;

[DisallowMultipleComponent]
public class PaperWindSway : MonoBehaviour
{
    private enum LocalAxis
    {
        X,
        Y,
        Z
    }

    [Header("Motion")]
    [SerializeField] private LocalAxis bendAxis = LocalAxis.Z;
    [SerializeField] private bool invertDirection;
    [SerializeField] private float bendAmplitude = 2.2f;
    [SerializeField] private float flutterAmplitude = 0.35f;
    [SerializeField] private float swayFrequency = 0.45f;
    [SerializeField] private float flutterFrequency = 1.6f;
    [SerializeField] private float smoothing = 0.16f;

    [Header("Anchor")]
    [SerializeField] private bool autoAnchorToTopEdge = true;
    [SerializeField] private Vector3 pivotOffset;

    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Quaternion currentOffsetRotation = Quaternion.identity;
    private float currentAngle;
    private float currentAngleVelocity;
    private float seed;

    private void Awake()
    {
        initialRotation = transform.localRotation;
        initialPosition = transform.localPosition;
        seed = Random.Range(0f, 100f);

        if (autoAnchorToTopEdge)
        {
            pivotOffset = CalculateTopEdgePivot();
        }
    }

    private void Update()
    {
        float time = Time.time + seed;
        float mainWave = Mathf.Sin(time * swayFrequency);
        float flutterWave = Mathf.Sin(time * flutterFrequency + 0.65f);
        float noise = (Mathf.PerlinNoise(seed, time * 0.55f) - 0.5f) * 2f;

        float direction = invertDirection ? -1f : 1f;
        float targetAngle = direction * (mainWave * bendAmplitude + flutterWave * flutterAmplitude + noise * 0.2f);
        currentAngle = Mathf.SmoothDamp(currentAngle, targetAngle, ref currentAngleVelocity, smoothing);

        currentOffsetRotation = Quaternion.AngleAxis(currentAngle, GetLocalAxisVector());
        transform.localRotation = initialRotation * currentOffsetRotation;
        transform.localPosition = initialPosition + initialRotation * (pivotOffset - currentOffsetRotation * pivotOffset);
    }

    private Vector3 GetLocalAxisVector()
    {
        return bendAxis switch
        {
            LocalAxis.X => Vector3.right,
            LocalAxis.Y => Vector3.up,
            _ => Vector3.forward
        };
    }

    private Vector3 CalculateTopEdgePivot()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return pivotOffset;
        }

        Bounds bounds = meshFilter.sharedMesh.bounds;
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }
}
