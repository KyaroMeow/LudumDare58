using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class UniversalLiquidController : MonoBehaviour
{
    private const string ProxyChildName = "__UniversalLiquidProxy";
    private const string LiquidShaderName = "Custom/UniversalLiquid";
    private const string DefaultMaterialResourcePath = "Materials/UniversalLiquid";

    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int WobbleXId = Shader.PropertyToID("_WobbleX");
    private static readonly int WobbleZId = Shader.PropertyToID("_WobbleZ");
    private static readonly int BoundsCenterId = Shader.PropertyToID("_BoundsCenter");
    private static readonly int VolumeHeightId = Shader.PropertyToID("_VolumeHeight");
    private static readonly int DeepColorId = Shader.PropertyToID("_DeepColor");
    private static readonly int ShallowColorId = Shader.PropertyToID("_ShallowColor");
    private static readonly int SurfaceColorId = Shader.PropertyToID("_SurfaceColor");
    private static readonly int BodyAlphaId = Shader.PropertyToID("_BodyAlpha");
    private static readonly int TopAlphaId = Shader.PropertyToID("_TopAlpha");
    private static readonly int SurfaceThicknessId = Shader.PropertyToID("_SurfaceThickness");
    private static readonly int WaveAmplitudeId = Shader.PropertyToID("_WaveAmplitude");
    private static readonly int WaveFrequencyId = Shader.PropertyToID("_WaveFrequency");
    private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
    private static readonly int UseWorldSpaceDataId = Shader.PropertyToID("_UseWorldSpaceData");

    [Header("Source")]
    [SerializeField] private string sourceMeshObjectName = "";
    [SerializeField] private string sourceMeshNameHint = "";
    [SerializeField] private Material liquidMaterialTemplate;

    [Header("Shape")]
    [Range(0.2f, 1.1f)]
    [SerializeField] private float widthFit = 0.9f;
    [Range(0.2f, 1.1f)]
    [SerializeField] private float heightFit = 0.9f;
    [Range(0.2f, 1.1f)]
    [SerializeField] private float depthFit = 0.9f;
    [SerializeField] private Vector3 proxyLocalOffset = Vector3.zero;
    [Range(0f, 0.45f)]
    [SerializeField] private float depthInset = 0.04f;

    [Header("Fill")]
    [Range(0f, 1f)]
    [SerializeField] private float fillPercent = 0.5f;
    [Range(0f, 0.25f)]
    [SerializeField] private float fillPadding = 0.08f;

    [Header("Motion")]
    [SerializeField] private float recovery = 1.3f;
    [SerializeField] private float wobbleAmountToAdd = 0.03f;
    [SerializeField] private float maxWobble = 0.03f;
    [SerializeField] private float wobbleFrequency = 1.1f;
    [SerializeField] private float angularInfluence = 0.15f;

    [Header("Visuals")]
    [SerializeField] private Color deepColor = new Color(0.08f, 0.28f, 0.70f, 1f);
    [SerializeField] private Color shallowColor = new Color(0.40f, 0.78f, 1f, 1f);
    [SerializeField] private Color surfaceColor = new Color(0.76f, 0.94f, 1f, 1f);
    [SerializeField] private float bodyAlpha = 0.82f;
    [SerializeField] private float topAlpha = 0.98f;
    [SerializeField] private float surfaceThickness = 0.03f;
    [SerializeField] private float waveAmplitude = 0.0065f;
    [SerializeField] private float waveFrequency = 3.2f;
    [SerializeField] private float waveSpeed = 0.45f;

    private MeshFilter sourceMeshFilter;
    private MeshRenderer sourceMeshRenderer;
    private MeshFilter proxyFilter;
    private MeshRenderer proxyRenderer;
    private Material liquidMaterial;
    private bool ownsRuntimeMaterial;
    private MaterialPropertyBlock propertyBlock;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float wobbleX;
    private float wobbleZ;
    private float wobbleTime;

    private Transform MotionTransform => sourceMeshFilter != null ? sourceMeshFilter.transform : transform;

    public bool HasResolvedSourceMesh => sourceMeshFilter != null && sourceMeshFilter.sharedMesh != null;
    public bool HasProxyObject => proxyRenderer != null;
    public string ResolvedSourceName => sourceMeshFilter != null ? sourceMeshFilter.transform.name : string.Empty;
    public string ProxyObjectName => proxyFilter != null ? proxyFilter.transform.name : ProxyChildName;
    public bool SourceUsesLiquidShader =>
        sourceMeshRenderer != null &&
        sourceMeshRenderer.sharedMaterial != null &&
        sourceMeshRenderer.sharedMaterial.shader != null &&
        sourceMeshRenderer.sharedMaterial.shader.name == LiquidShaderName;

    private void Reset()
    {
        AssignDefaultMaterialIfNeeded();
        ResolveSourceMesh();

        if (sourceMeshFilter != null)
        {
            sourceMeshObjectName = sourceMeshFilter.transform.name;
        }

        EnsureSetup();
        CacheCurrentMotionState();
        ApplyProperties(0f);
    }

    private void OnEnable()
    {
        EnsureSetup();
        CacheCurrentMotionState();
        ApplyProperties(0f);
    }

    private void OnDisable()
    {
        ReleaseRuntimeMaterial();
    }

    private void Update()
    {
        EnsureSetup();
        ApplyProperties(GetDeltaTime());
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView.RepaintAll();
        }
#endif
    }

    private void OnValidate()
    {
        AssignDefaultMaterialIfNeeded();
        EnsureSetup();
        ApplyProperties(0f);
    }

    [ContextMenu("Rebuild Liquid Proxy")]
    private void RebuildLiquidProxy()
    {
        RemoveProxyObject();
        EnsureSetup();
        ApplyProperties(0f);
    }

    public void AutoSetupFromEditor()
    {
        AssignDefaultMaterialIfNeeded();
        ResolveSourceMesh();

        if (sourceMeshFilter != null)
        {
            sourceMeshObjectName = sourceMeshFilter.transform.name;
        }

        EnsureSetup();
        CacheCurrentMotionState();
        ApplyProperties(0f);
    }

    public void RebuildFromEditor()
    {
        RebuildLiquidProxy();
    }

    private void EnsureSetup()
    {
        ResolveSourceMesh();

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        CleanupInvalidProxy();
        if (sourceMeshFilter == null)
        {
            proxyFilter = null;
            proxyRenderer = null;
            return;
        }

        EnsureProxyObject();

        Material targetMaterial = GetOrCreateLiquidMaterial();
        if (targetMaterial == null || proxyRenderer == null)
        {
            return;
        }

        ApplyStaticMaterialSettings(targetMaterial);
        proxyRenderer.sharedMaterial = targetMaterial;
    }

    private void ResolveSourceMesh()
    {
        sourceMeshFilter = null;
        sourceMeshRenderer = null;

        if (TryGetComponent(out MeshFilter ownFilter) && ownFilter.sharedMesh != null && TryGetComponent(out MeshRenderer ownRenderer))
        {
            sourceMeshFilter = ownFilter;
            sourceMeshRenderer = ownRenderer;
            return;
        }

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);

        if (!string.IsNullOrEmpty(sourceMeshObjectName))
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null || renderer.transform.name != sourceMeshObjectName)
                {
                    continue;
                }

                if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                {
                    continue;
                }

                sourceMeshFilter = filter;
                sourceMeshRenderer = renderer;
                return;
            }
        }

        if (!string.IsNullOrEmpty(sourceMeshNameHint))
        {
            string lowerHint = sourceMeshNameHint.ToLowerInvariant();
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null || !renderer.transform.name.ToLowerInvariant().Contains(lowerHint))
                {
                    continue;
                }

                if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                {
                    continue;
                }

                sourceMeshFilter = filter;
                sourceMeshRenderer = renderer;
                return;
            }
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null || renderer.transform.name == ProxyChildName)
            {
                continue;
            }

            if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
            {
                continue;
            }

            sourceMeshFilter = filter;
            sourceMeshRenderer = renderer;
            return;
        }
    }

    private void AssignDefaultMaterialIfNeeded()
    {
        if (liquidMaterialTemplate != null)
        {
            return;
        }

        liquidMaterialTemplate = Resources.Load<Material>(DefaultMaterialResourcePath);
    }

    private void CleanupInvalidProxy()
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.name != ProxyChildName)
            {
                continue;
            }

            if (sourceMeshFilter != null && candidate.parent == sourceMeshFilter.transform)
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

    private void RemoveProxyObject()
    {
        if (sourceMeshFilter == null)
        {
            return;
        }

        Transform proxyTransform = sourceMeshFilter.transform.Find(ProxyChildName);
        if (proxyTransform == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(proxyTransform.gameObject);
        }
        else
        {
            DestroyImmediate(proxyTransform.gameObject);
        }

        proxyFilter = null;
        proxyRenderer = null;
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

        Bounds sourceBounds = sourceMeshFilter.sharedMesh.bounds;
        Vector3 meshScale = new Vector3(widthFit, heightFit, depthFit);
        float depthShrink = 1f - Mathf.Clamp01(depthInset * 1.35f);
        meshScale.z *= Mathf.Max(0.15f, depthShrink);

        Vector3 centerCorrection = new Vector3(
            sourceBounds.center.x * (1f - meshScale.x),
            sourceBounds.center.y * (1f - meshScale.y),
            sourceBounds.center.z * (1f - meshScale.z));

        proxyObject.transform.localPosition = centerCorrection + proxyLocalOffset;
        proxyObject.transform.localRotation = Quaternion.identity;
        proxyObject.transform.localScale = meshScale;

        proxyFilter.sharedMesh = sourceMeshFilter.sharedMesh;
        proxyRenderer.shadowCastingMode = ShadowCastingMode.Off;
        proxyRenderer.receiveShadows = false;
        proxyRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        proxyRenderer.allowOcclusionWhenDynamic = false;
        proxyRenderer.lightProbeUsage = LightProbeUsage.Off;
        proxyRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        proxyRenderer.enabled = true;
    }

    private Material GetOrCreateLiquidMaterial()
    {
        AssignDefaultMaterialIfNeeded();

        if (liquidMaterialTemplate != null)
        {
            liquidMaterial = liquidMaterialTemplate;
            ownsRuntimeMaterial = false;
            return liquidMaterial;
        }

        Shader shader = Shader.Find(LiquidShaderName);
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
            name = "Universal Liquid Runtime",
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
        targetMaterial.SetFloat(BodyAlphaId, bodyAlpha);
        targetMaterial.SetFloat(TopAlphaId, topAlpha);
        targetMaterial.SetFloat(SurfaceThicknessId, surfaceThickness);
        targetMaterial.SetFloat(WaveAmplitudeId, waveAmplitude);
        targetMaterial.SetFloat(WaveFrequencyId, waveFrequency);
        targetMaterial.SetFloat(WaveSpeedId, waveSpeed);
    }

    private void ApplyProperties(float deltaTime)
    {
        if (proxyRenderer == null || proxyFilter == null || proxyFilter.sharedMesh == null)
        {
            return;
        }

        UpdateWobble(deltaTime);

        Bounds worldBounds = proxyRenderer.bounds;
        float paddedMinY = Mathf.Lerp(worldBounds.min.y, worldBounds.max.y, fillPadding);
        float paddedMaxY = Mathf.Lerp(worldBounds.min.y, worldBounds.max.y, 1f - fillPadding);
        float fillWorldY = Mathf.Lerp(paddedMinY, paddedMaxY, fillPercent);

        float pulse = Mathf.Sin(wobbleTime * wobbleFrequency * Mathf.PI * 2f);
        float finalWobbleX = wobbleX * pulse;
        float finalWobbleZ = wobbleZ * pulse;

        propertyBlock.Clear();
        propertyBlock.SetFloat(FillAmountId, fillWorldY - worldBounds.center.y);
        propertyBlock.SetFloat(WobbleXId, finalWobbleX);
        propertyBlock.SetFloat(WobbleZId, finalWobbleZ);
        propertyBlock.SetVector(BoundsCenterId, worldBounds.center);
        propertyBlock.SetFloat(VolumeHeightId, worldBounds.size.y);
        propertyBlock.SetFloat(UseWorldSpaceDataId, 1f);
        proxyRenderer.SetPropertyBlock(propertyBlock);
    }

    private void UpdateWobble(float deltaTime)
    {
        Transform motionTransform = MotionTransform;
        if (deltaTime <= 0f)
        {
            lastPosition = motionTransform.position;
            lastRotation = motionTransform.rotation;
            wobbleTime = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            wobbleX = 0f;
            wobbleZ = 0f;
            return;
        }

        wobbleTime += deltaTime;
        wobbleX = Mathf.Lerp(wobbleX, 0f, recovery * deltaTime);
        wobbleZ = Mathf.Lerp(wobbleZ, 0f, recovery * deltaTime);

        Vector3 worldVelocity = (motionTransform.position - lastPosition) / deltaTime;
        Vector3 localVelocity = motionTransform.InverseTransformDirection(worldVelocity);
        Vector3 angularVelocity = GetAngularVelocity(lastRotation, motionTransform.rotation, deltaTime);
        Vector3 localAngularVelocity = motionTransform.InverseTransformDirection(angularVelocity);

        float wobbleAddX = (localVelocity.x + localAngularVelocity.z * angularInfluence) * wobbleAmountToAdd;
        float wobbleAddZ = (localVelocity.z + localAngularVelocity.x * angularInfluence) * wobbleAmountToAdd;

        wobbleX = Mathf.Clamp(wobbleX + wobbleAddX, -maxWobble, maxWobble);
        wobbleZ = Mathf.Clamp(wobbleZ + wobbleAddZ, -maxWobble, maxWobble);

        lastPosition = motionTransform.position;
        lastRotation = motionTransform.rotation;
    }

    private void CacheCurrentMotionState()
    {
        Transform motionTransform = MotionTransform;
        lastPosition = motionTransform.position;
        lastRotation = motionTransform.rotation;
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
}
