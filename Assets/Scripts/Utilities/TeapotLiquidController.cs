using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
public class TeapotLiquidController : MonoBehaviour
{
    private const string ProxyChildName = "__TeapotLiquidProxy";
    private const string LegacyWindowChildName = "__TeapotLiquidVolume";
    private const string LegacyTopChildName = "__TeapotLiquidTop";

    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int WobbleXId = Shader.PropertyToID("_WobbleX");
    private static readonly int WobbleZId = Shader.PropertyToID("_WobbleZ");
    private static readonly int BoundsCenterId = Shader.PropertyToID("_BoundsCenter");
    private static readonly int VolumeHeightId = Shader.PropertyToID("_VolumeHeight");
    private static readonly int DeepColorId = Shader.PropertyToID("_DeepColor");
    private static readonly int ShallowColorId = Shader.PropertyToID("_ShallowColor");
    private static readonly int SurfaceColorId = Shader.PropertyToID("_SurfaceColor");
    private static readonly int SurfaceLineColorId = Shader.PropertyToID("_SurfaceLineColor");
    private static readonly int FoamColorId = Shader.PropertyToID("_FoamColor");
    private static readonly int BodyAlphaId = Shader.PropertyToID("_BodyAlpha");
    private static readonly int TopAlphaId = Shader.PropertyToID("_TopAlpha");
    private static readonly int SurfaceThicknessId = Shader.PropertyToID("_SurfaceThickness");
    private static readonly int SurfaceLineIntensityId = Shader.PropertyToID("_SurfaceLineIntensity");
    private static readonly int RimPowerId = Shader.PropertyToID("_RimPower");
    private static readonly int RimIntensityId = Shader.PropertyToID("_RimIntensity");
    private static readonly int WaveAmplitudeId = Shader.PropertyToID("_WaveAmplitude");
    private static readonly int WaveFrequencyId = Shader.PropertyToID("_WaveFrequency");
    private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");

    [Header("Source")]
    [SerializeField] private string sourceMeshObjectName = "BASE.main";
    [SerializeField] private string sourceMeshNameHint = "base";
    [SerializeField] private string windowNameHint = "panel";
    [SerializeField] private Material liquidMaterialTemplate;

    [Header("Proxy Shape")]
    [Range(0.4f, 1.1f)]
    [SerializeField] private float widthFit = 0.84f;
    [Range(0.4f, 1.1f)]
    [SerializeField] private float heightFit = 0.92f;
    [Range(0.2f, 1f)]
    [SerializeField] private float depthFit = 0.62f;
    [SerializeField] private Vector3 proxyLocalOffset = new Vector3(0f, -0.015f, 0f);
    [Range(0f, 0.45f)]
    [SerializeField] private float depthInset = 0.08f;
    [Range(0f, 0.35f)]
    [SerializeField] private float topChamfer = 0.14f;
    [Range(0f, 0.45f)]
    [SerializeField] private float bottomChamfer = 0.18f;
    [Range(0f, 0.20f)]
    [SerializeField] private float sideInset = 0.10f;

    [Header("Fill")]
    [Range(0f, 1f)]
    [SerializeField] private float fillPercent = 0.56f;
    [Range(0f, 0.25f)]
    [SerializeField] private float fillPadding = 0.08f;

    [Header("Motion")]
    [SerializeField] private float recovery = 1.35f;
    [SerializeField] private float wobbleAmountToAdd = 0.018f;
    [SerializeField] private float maxWobble = 0.022f;
    [SerializeField] private float wobbleFrequency = 1.05f;
    [SerializeField] private float angularInfluence = 0.15f;

    [Header("Visuals")]
    [SerializeField] private Color deepColor = new Color(0.08f, 0.28f, 0.70f, 1f);
    [SerializeField] private Color shallowColor = new Color(0.40f, 0.78f, 1f, 1f);
    [SerializeField] private Color surfaceColor = new Color(0.76f, 0.94f, 1f, 1f);
    [SerializeField] private Color surfaceLineColor = new Color(0.95f, 0.99f, 1f, 1f);
    [SerializeField] private Color foamColor = new Color(0.93f, 0.99f, 1f, 1f);
    [SerializeField] private float bodyAlpha = 0.82f;
    [SerializeField] private float topAlpha = 0.98f;
    [SerializeField] private float surfaceThickness = 0.03f;
    [SerializeField] private float surfaceLineIntensity = 1.6f;
    [SerializeField] private float rimPower = 4.2f;
    [SerializeField] private float rimIntensity = 0.14f;
    [SerializeField] private float waveAmplitude = 0.0065f;
    [SerializeField] private float waveFrequency = 3.2f;
    [SerializeField] private float waveSpeed = 0.45f;

    private MeshFilter sourceMeshFilter;
    private MeshRenderer sourceMeshRenderer;
    private MeshFilter proxyFilter;
    private MeshRenderer proxyRenderer;
    private Mesh proxyMesh;
    private Material liquidMaterial;
    private bool ownsRuntimeMaterial;
    private MaterialPropertyBlock propertyBlock;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float wobbleX;
    private float wobbleZ;
    private float wobbleTime;

    private void OnEnable()
    {
        EnsureSetup();
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        ApplyProperties(0f);
    }

    private void OnDisable()
    {
        ReleaseRuntimeMaterial();
        ReleaseProxyMesh();
    }

    private void LateUpdate()
    {
        EnsureSetup();
        ApplyProperties(GetDeltaTime());
    }

    private void OnValidate()
    {
        EnsureSetup();
        ApplyProperties(0f);
    }

    private void EnsureSetup()
    {
        ResolveBodySource();

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        CleanupLegacyChildren();

        if (sourceMeshFilter == null)
        {
            proxyFilter = null;
            proxyRenderer = null;
            return;
        }

        EnsureProxyObject();
        RebuildProxyMesh();

        Material targetMaterial = GetOrCreateLiquidMaterial();
        if (targetMaterial == null || proxyRenderer == null)
        {
            return;
        }

        ApplyStaticMaterialSettings(targetMaterial);
        proxyRenderer.sharedMaterial = targetMaterial;
    }

    private void ResolveBodySource()
    {
        sourceMeshFilter = null;
        sourceMeshRenderer = null;

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            string objectName = renderer.transform.name;
            if (objectName == ProxyChildName || objectName == LegacyWindowChildName || objectName == LegacyTopChildName)
            {
                continue;
            }

            if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(sourceMeshObjectName) && objectName == sourceMeshObjectName)
            {
                sourceMeshFilter = filter;
                sourceMeshRenderer = renderer;
                return;
            }
        }

        string lowerHint = sourceMeshNameHint.ToLowerInvariant();
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            string objectName = renderer.transform.name;
            if (objectName == ProxyChildName || objectName == LegacyWindowChildName || objectName == LegacyTopChildName)
            {
                continue;
            }

            if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
            {
                continue;
            }

            if (objectName.ToLowerInvariant().Contains(lowerHint))
            {
                sourceMeshFilter = filter;
                sourceMeshRenderer = renderer;
                return;
            }
        }
    }

    private void CleanupLegacyChildren()
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate == null || candidate == transform)
            {
                continue;
            }

            string candidateName = candidate.name;
            bool isLegacy = candidateName == LegacyWindowChildName || candidateName == LegacyTopChildName;
            bool isInvalidProxy = candidateName == ProxyChildName && (sourceMeshFilter == null || candidate.parent != sourceMeshFilter.transform);

            if (!isLegacy && !isInvalidProxy)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(candidate.gameObject);
            }
            else
            {
                DestroyImmediate(candidate.gameObject);
            }
        }
    }

    private void EnsureProxyObject()
    {
        Transform proxyTransform = sourceMeshFilter.transform.Find(ProxyChildName);
        GameObject proxyObject = proxyTransform != null ? proxyTransform.gameObject : new GameObject(ProxyChildName);

        proxyObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        proxyObject.layer = sourceMeshFilter.gameObject.layer;
        proxyObject.transform.SetParent(sourceMeshFilter.transform, false);

        proxyFilter = proxyObject.GetComponent<MeshFilter>();
        if (proxyFilter == null)
        {
            proxyFilter = proxyObject.AddComponent<MeshFilter>();
        }

        proxyRenderer = proxyObject.GetComponent<MeshRenderer>();
        if (proxyRenderer == null)
        {
            proxyRenderer = proxyObject.AddComponent<MeshRenderer>();
        }

        proxyRenderer.shadowCastingMode = ShadowCastingMode.Off;
        proxyRenderer.receiveShadows = false;
        proxyRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        proxyRenderer.allowOcclusionWhenDynamic = false;
        proxyRenderer.lightProbeUsage = LightProbeUsage.Off;
        proxyRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        proxyRenderer.enabled = true;
    }

    private void RebuildProxyMesh()
    {
        if (proxyFilter == null || sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
        {
            return;
        }

        Bounds sourceBounds = sourceMeshFilter.sharedMesh.bounds;
        Bounds fittingBounds = sourceBounds;
        bool hasWindowEnvelope = TryGetWindowEnvelope(out Bounds windowEnvelope);
        if (hasWindowEnvelope)
        {
            fittingBounds = windowEnvelope;
        }

        float width = fittingBounds.size.x * widthFit;
        float height = fittingBounds.size.y * heightFit;
        float depth = fittingBounds.size.z * depthFit;

        width = Mathf.Min(width, sourceBounds.size.x * 0.58f);
        height = Mathf.Min(height, sourceBounds.size.y * 0.84f);
        depth = Mathf.Min(depth, sourceBounds.size.z * 0.34f);

        float usableDepth = depth * (1f - (Mathf.Clamp01(depthInset) * 2f));
        if (usableDepth > 0.05f)
        {
            depth = usableDepth;
        }

        Vector3 proxyCenter = hasWindowEnvelope
            ? new Vector3(
                Mathf.Lerp(sourceBounds.center.x, fittingBounds.center.x, 0.6f),
                Mathf.Lerp(sourceBounds.center.y, fittingBounds.center.y, 0.6f),
                fittingBounds.center.z)
            : sourceBounds.center;

        Transform proxyTransform = proxyFilter.transform;
        proxyTransform.localPosition = proxyCenter + proxyLocalOffset;
        proxyTransform.localRotation = Quaternion.identity;
        proxyTransform.localScale = Vector3.one;

        if (proxyMesh == null)
        {
            proxyMesh = new Mesh
            {
                name = "TeapotLiquidProxyMesh",
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };
        }

        BuildBeveledPrismMesh(proxyMesh, width, height, depth);
        proxyFilter.sharedMesh = proxyMesh;
    }

    private bool TryGetWindowEnvelope(out Bounds envelope)
    {
        envelope = default;

        if (sourceMeshFilter == null)
        {
            return false;
        }

        bool hasEnvelope = false;
        string lowerHint = windowNameHint.ToLowerInvariant();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null || renderer.transform == sourceMeshFilter.transform)
            {
                continue;
            }

            if (!renderer.transform.name.ToLowerInvariant().Contains(lowerHint))
            {
                continue;
            }

            Bounds localBounds = TransformBoundsToLocalSpace(renderer.bounds, sourceMeshFilter.transform);
            if (!hasEnvelope)
            {
                envelope = localBounds;
                hasEnvelope = true;
                continue;
            }

            envelope.Encapsulate(localBounds.min);
            envelope.Encapsulate(localBounds.max);
        }

        return hasEnvelope;
    }

    private Bounds TransformBoundsToLocalSpace(Bounds worldBounds, Transform targetSpace)
    {
        Vector3 min = worldBounds.min;
        Vector3 max = worldBounds.max;

        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, max.y, max.z),
        };

        Bounds localBounds = new Bounds(targetSpace.InverseTransformPoint(corners[0]), Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
        {
            localBounds.Encapsulate(targetSpace.InverseTransformPoint(corners[i]));
        }

        return localBounds;
    }

    private Material GetOrCreateLiquidMaterial()
    {
        if (liquidMaterialTemplate != null)
        {
            liquidMaterial = liquidMaterialTemplate;
            ownsRuntimeMaterial = false;
            return liquidMaterial;
        }

        Shader shader = Shader.Find("Custom/TeapotLiquid");
        if (shader == null)
        {
            return null;
        }

        if (liquidMaterial != null && liquidMaterial.shader == shader)
        {
            return liquidMaterial;
        }

        ReleaseRuntimeMaterial();

        liquidMaterial = new Material(shader)
        {
            name = "Teapot Liquid Runtime",
            hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
        };
        ownsRuntimeMaterial = true;
        return liquidMaterial;
    }

    private void ApplyStaticMaterialSettings(Material targetMaterial)
    {
        targetMaterial.SetColor(DeepColorId, deepColor);
        targetMaterial.SetColor(ShallowColorId, shallowColor);
        targetMaterial.SetColor(SurfaceColorId, surfaceColor);
        targetMaterial.SetColor(SurfaceLineColorId, surfaceLineColor);
        targetMaterial.SetColor(FoamColorId, foamColor);
        targetMaterial.SetFloat(BodyAlphaId, bodyAlpha);
        targetMaterial.SetFloat(TopAlphaId, topAlpha);
        targetMaterial.SetFloat(SurfaceThicknessId, surfaceThickness);
        targetMaterial.SetFloat(SurfaceLineIntensityId, surfaceLineIntensity);
        targetMaterial.SetFloat(RimPowerId, rimPower);
        targetMaterial.SetFloat(RimIntensityId, rimIntensity);
        targetMaterial.SetFloat(WaveAmplitudeId, waveAmplitude);
        targetMaterial.SetFloat(WaveFrequencyId, waveFrequency);
        targetMaterial.SetFloat(WaveSpeedId, waveSpeed);
    }

    private void ApplyProperties(float deltaTime)
    {
        if (proxyRenderer == null)
        {
            return;
        }

        UpdateWobble(deltaTime);

        Bounds bounds = proxyRenderer.bounds;
        float paddedMinY = Mathf.Lerp(bounds.min.y, bounds.max.y, fillPadding);
        float paddedMaxY = Mathf.Lerp(bounds.min.y, bounds.max.y, 1f - fillPadding);
        float fillWorldY = Mathf.Lerp(paddedMinY, paddedMaxY, fillPercent);

        float pulse = Mathf.Sin(wobbleTime * wobbleFrequency * Mathf.PI * 2f);
        float finalWobbleX = wobbleX * pulse;
        float finalWobbleZ = wobbleZ * pulse;

        propertyBlock.Clear();
        propertyBlock.SetFloat(FillAmountId, fillWorldY - bounds.center.y);
        propertyBlock.SetFloat(WobbleXId, finalWobbleX);
        propertyBlock.SetFloat(WobbleZId, finalWobbleZ);
        propertyBlock.SetVector(BoundsCenterId, bounds.center);
        propertyBlock.SetFloat(VolumeHeightId, bounds.size.y);
        proxyRenderer.SetPropertyBlock(propertyBlock);
    }

    private void UpdateWobble(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            wobbleTime = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            wobbleX = 0f;
            wobbleZ = 0f;
            return;
        }

        wobbleTime += deltaTime;
        wobbleX = Mathf.Lerp(wobbleX, 0f, recovery * deltaTime);
        wobbleZ = Mathf.Lerp(wobbleZ, 0f, recovery * deltaTime);

        Vector3 worldVelocity = (transform.position - lastPosition) / deltaTime;
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        Vector3 angularVelocity = GetAngularVelocity(lastRotation, transform.rotation, deltaTime);
        Vector3 localAngularVelocity = transform.InverseTransformDirection(angularVelocity);

        float wobbleAddX = (localVelocity.x + localAngularVelocity.z * angularInfluence) * wobbleAmountToAdd;
        float wobbleAddZ = (localVelocity.z + localAngularVelocity.x * angularInfluence) * wobbleAmountToAdd;

        wobbleX = Mathf.Clamp(wobbleX + wobbleAddX, -maxWobble, maxWobble);
        wobbleZ = Mathf.Clamp(wobbleZ + wobbleAddZ, -maxWobble, maxWobble);

        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    private Vector3 GetAngularVelocity(Quaternion previousRotation, Quaternion currentRotation, float deltaTime)
    {
        Quaternion delta = currentRotation * Quaternion.Inverse(previousRotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);

        if (float.IsNaN(axis.x) || Mathf.Approximately(angle, 0f))
        {
            return Vector3.zero;
        }

        if (angle > 180f)
        {
            angle -= 360f;
        }

        return axis * (angle * Mathf.Deg2Rad / deltaTime);
    }

    private float GetDeltaTime()
    {
        return Application.isPlaying ? Time.deltaTime : 1f / 60f;
    }

    private void BuildBeveledPrismMesh(Mesh targetMesh, float width, float height, float depth)
    {
        targetMesh.Clear();

        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        float halfDepth = depth * 0.5f;

        float topInsetX = halfWidth * Mathf.Clamp01(sideInset + topChamfer * 0.55f);
        float topInsetY = halfHeight * Mathf.Clamp01(topChamfer * 1.15f);
        float bottomInsetX = halfWidth * Mathf.Clamp01(sideInset + bottomChamfer * 0.45f);
        float bottomInsetY = halfHeight * Mathf.Clamp01(bottomChamfer);

        List<Vector2> ring = new List<Vector2>(8)
        {
            new Vector2(-halfWidth + topInsetX, halfHeight),
            new Vector2(halfWidth - topInsetX, halfHeight),
            new Vector2(halfWidth, halfHeight - topInsetY),
            new Vector2(halfWidth, -halfHeight + bottomInsetY),
            new Vector2(halfWidth - bottomInsetX, -halfHeight),
            new Vector2(-halfWidth + bottomInsetX, -halfHeight),
            new Vector2(-halfWidth, -halfHeight + bottomInsetY),
            new Vector2(-halfWidth, halfHeight - topInsetY),
        };

        int sideCount = ring.Count;
        Vector3[] vertices = new Vector3[(sideCount * 2) + 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        List<int> triangles = new List<int>((sideCount * 12) + (sideCount * 6));

        for (int i = 0; i < sideCount; i++)
        {
            Vector2 point = ring[i];
            vertices[i] = new Vector3(point.x, point.y, halfDepth);
            vertices[i + sideCount] = new Vector3(point.x, point.y, -halfDepth);

            uvs[i] = new Vector2((point.x / Mathf.Max(width, 0.0001f)) + 0.5f, (point.y / Mathf.Max(height, 0.0001f)) + 0.5f);
            uvs[i + sideCount] = uvs[i];
        }

        int frontCenterIndex = sideCount * 2;
        int backCenterIndex = frontCenterIndex + 1;
        vertices[frontCenterIndex] = new Vector3(0f, 0f, halfDepth);
        vertices[backCenterIndex] = new Vector3(0f, 0f, -halfDepth);
        uvs[frontCenterIndex] = new Vector2(0.5f, 0.5f);
        uvs[backCenterIndex] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < sideCount; i++)
        {
            int next = (i + 1) % sideCount;

            triangles.Add(frontCenterIndex);
            triangles.Add(i);
            triangles.Add(next);

            triangles.Add(backCenterIndex);
            triangles.Add(next + sideCount);
            triangles.Add(i + sideCount);

            triangles.Add(i);
            triangles.Add(i + sideCount);
            triangles.Add(next + sideCount);

            triangles.Add(i);
            triangles.Add(next + sideCount);
            triangles.Add(next);
        }

        targetMesh.vertices = vertices;
        targetMesh.uv = uvs;
        targetMesh.SetTriangles(triangles, 0);
        targetMesh.RecalculateNormals();
        targetMesh.RecalculateBounds();
    }

    private void ReleaseRuntimeMaterial()
    {
        if (liquidMaterial == null || !ownsRuntimeMaterial)
        {
            liquidMaterial = null;
            ownsRuntimeMaterial = false;
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(liquidMaterial);
        }
        else
        {
            DestroyImmediate(liquidMaterial);
        }

        liquidMaterial = null;
        ownsRuntimeMaterial = false;
    }

    private void ReleaseProxyMesh()
    {
        if (proxyMesh == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(proxyMesh);
        }
        else
        {
            DestroyImmediate(proxyMesh);
        }

        proxyMesh = null;
    }
}
