using System;
using UnityEngine;

public class AnomallyController : MonoBehaviour
{
    [SerializeField] private GameObject[] anomallyBalls;
    [SerializeField] private GameObject cube;
    [SerializeField] private GameObject sphere;
    [SerializeField] private GameObject endCube;
    [SerializeField] private InventoryItemDefinition EnergySphere;
    [SerializeField] private InventorySystem inventory;
    [Header("Sphere Feedback")]
    [SerializeField] private SfxEmitter sphereFeedbackEmitter;
    [SerializeField] private SfxCue sphereClickSfx;
    [SerializeField] private GameObject sphereMoveEffectPrefab;
    [SerializeField] private bool spawnMoveEffectAtNextPosition = true;
    [SerializeField] private GameObject[] cubeEnergyVisuals;

    private int positionId;
    private bool isChaseActive;
    private bool isEnergySphereReadyToSteal;
    private bool warnedMissingChaseRefs;
    private bool warnedMissingInventoryRefs;
    private bool warnedMissingGameManager;
    private bool warnedNotCurrentCube;
    private bool warnedSkippedUnavailableBall;
    private bool timerWasRunningBeforeChase;
    private GameObject activeCubeItem;
    private Renderer[] runtimeEnergyRenderers;
    private bool[] runtimeEnergyRendererStates;
    private GameObject[] runtimeEnergyObjects;
    private bool[] runtimeEnergyObjectStates;

    public bool IsChaseActive => isChaseActive;
    public bool IsEnergySphereReadyToSteal => isEnergySphereReadyToSteal;

    private void Awake()
    {
        EnsureBallInteractors();
        HideAllBalls();
        DisableLegacySceneCubeVisuals();
    }

    private void Update()
    {
        if (!isChaseActive && isEnergySphereReadyToSteal && activeCubeItem != null)
        {
            GameObject currentItem = GameManager.Instance != null ? GameManager.Instance.currentItem : null;
            if (currentItem != activeCubeItem)
            {
                ResetStoryAnomalyState();
            }
        }
    }

    public void ClickOnBall()
    {
        if (!isChaseActive)
        {
            return;
        }

        if (!HasValidBallSetup())
        {
            FinishChaseWithoutSoftLock();
            return;
        }

        Debug.Log($"Anomaly ball clicked index {positionId}.");
        PlaySphereClickSfx();
        PlaySphereMoveEffect(positionId < anomallyBalls.Length - 1 ? positionId + 1 : -1);

        if (positionId < anomallyBalls.Length - 1)
        {
            GoToNextPosition();
        }
        else
        {
            CompleteChase();
        }
    }

    public void StartAnomally()
    {
        TryStartAnomalySteal();
    }

    public bool IsStoryAnomalyCube(GameObject candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        if (activeCubeItem != null && IsSameObjectOrChild(candidate, activeCubeItem))
        {
            return true;
        }

        GameObject currentItem = GameManager.Instance != null ? GameManager.Instance.currentItem : null;
        if (currentItem != null && IsSameObjectOrChild(candidate, currentItem) && IsStoryCubeCandidate(currentItem))
        {
            return true;
        }

        return IsStoryCubeCandidate(candidate);
    }

    public bool CanStartAnomalySteal(GameObject candidate)
    {
        return !isChaseActive &&
               !isEnergySphereReadyToSteal &&
               IsStoryAnomalyCube(candidate) &&
               ResolveCurrentStoryCube(candidate) != null;
    }

    public bool TryStartAnomalySteal(GameObject candidate = null)
    {
        GameObject storyCube = ResolveCurrentStoryCube(candidate);
        if (storyCube == null)
        {
            WarnOnce(ref warnedNotCurrentCube, "Cannot start anomaly chase because the story cube is not the current conveyor item.");
            return false;
        }

        if (isChaseActive || isEnergySphereReadyToSteal)
        {
            return false;
        }

        activeCubeItem = storyCube;
        isChaseActive = true;
        isEnergySphereReadyToSteal = false;
        positionId = 0;

        HideAllBalls();
        DisableLegacySceneCubeVisuals();
        SetCubeEnergyVisualsVisible(false);

        if (GameManager.Instance != null)
        {
            timerWasRunningBeforeChase = GameManager.Instance.isTimerWork;
            GameManager.Instance.isTimerWork = false;
        }

        PlayerInteraction.Instance?.HandleStopInteraction();

        if (HasValidBallSetup())
        {
            SetBallActive(positionId, true);
            Debug.Log("Anomaly first steal started.");
        }
        else
        {
            FinishChaseWithoutSoftLock();
        }

        return true;
    }

    public bool CanFinalStealEnergySphere(GameObject candidate)
    {
        return !isChaseActive &&
               isEnergySphereReadyToSteal &&
               IsStoryAnomalyCube(candidate) &&
               ResolveCurrentStoryCube(candidate) != null;
    }

    public bool TryFinalStealEnergySphere(GameObject candidate = null)
    {
        GameObject storyCube = ResolveCurrentStoryCube(candidate);
        if (storyCube == null || !isEnergySphereReadyToSteal || isChaseActive)
        {
            return false;
        }

        InventorySystem targetInventory = inventory != null ? inventory : InventorySystem.Instance;
        if (targetInventory == null || EnergySphere == null)
        {
            WarnOnce(ref warnedMissingInventoryRefs, "Cannot steal EnergySphere because inventory or EnergySphere reference is missing.");
            return false;
        }

        if (GameManager.Instance == null)
        {
            WarnOnce(ref warnedMissingGameManager, "Cannot complete EnergySphere steal because GameManager.Instance is missing.");
            return false;
        }

        if (!targetInventory.TryAddItem(EnergySphere))
        {
            Debug.LogWarning("Anomaly final steal fail: inventory is full.");
            Debug.LogWarning("Inventory is full. EnergySphere was not added and the anomaly cube remains active.");
            return false;
        }

        GameManager.Instance.securitySystem?.ReportViolation("Steal item");
        GameObject itemToComplete = activeCubeItem != null ? activeCubeItem : storyCube;
        ResetStoryAnomalyState();
        GameManager.Instance.CompleteCurrentItemAfterToolAction(itemToComplete);
        Debug.Log("Anomaly final steal success.");
        return true;
    }

    private void CompleteChase()
    {
        HideAllBalls();
        isChaseActive = false;
        isEnergySphereReadyToSteal = true;
        SetCubeEnergyVisualsVisible(true);
        RestoreCubeAsCurrentItem();
        DisableLegacySceneCubeVisuals();
        Debug.Log("Anomaly chase completed, energy ready.");
    }

    private void FinishChaseWithoutSoftLock()
    {
        HideAllBalls();
        isChaseActive = false;
        isEnergySphereReadyToSteal = false;
        SetCubeEnergyVisualsVisible(true);
        RestoreCubeAsCurrentItem();
        DisableLegacySceneCubeVisuals();
        ClearCubeEnergyVisualCache();
    }

    private void RestoreCubeAsCurrentItem()
    {
        if (activeCubeItem != null)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentItem = activeCubeItem;
                GameManager.Instance.isTimerWork = timerWasRunningBeforeChase;
            }
        }
    }

    private void ResetStoryAnomalyState()
    {
        HideAllBalls();
        SetCubeEnergyVisualsVisible(true);
        DisableLegacySceneCubeVisuals();
        isChaseActive = false;
        isEnergySphereReadyToSteal = false;
        activeCubeItem = null;
        ClearCubeEnergyVisualCache();
        positionId = 0;
    }

    private void GoToNextPosition()
    {
        SetBallActive(positionId, false);

        positionId++;

        if (!TryActivateNextAvailableBall())
        {
            CompleteChase();
        }
    }

    private GameObject ResolveCurrentStoryCube(GameObject candidate)
    {
        GameObject currentItem = GameManager.Instance != null ? GameManager.Instance.currentItem : null;
        if (currentItem != null && IsStoryCubeCandidate(currentItem))
        {
            if (candidate == null || IsSameObjectOrChild(candidate, currentItem))
            {
                return currentItem;
            }
        }

        if (activeCubeItem != null && IsStoryCubeCandidate(activeCubeItem))
        {
            if (candidate == null || IsSameObjectOrChild(candidate, activeCubeItem))
            {
                return activeCubeItem;
            }
        }

        return null;
    }

    private bool HasValidBallSetup()
    {
        bool hasSetup = anomallyBalls != null && anomallyBalls.Length > 0 && positionId >= 0 && positionId < anomallyBalls.Length;
        if (!hasSetup)
        {
            WarnOnce(ref warnedMissingChaseRefs, "Anomaly chase has no configured balls. Returning cube without starting chase.");
        }

        return hasSetup;
    }

    private void HideAllBalls()
    {
        if (anomallyBalls == null)
        {
            return;
        }

        for (int i = 0; i < anomallyBalls.Length; i++)
        {
            SetBallActive(i, false);
        }
    }

    private void SetBallActive(int index, bool isActive)
    {
        if (anomallyBalls == null || index < 0 || index >= anomallyBalls.Length || anomallyBalls[index] == null)
        {
            return;
        }

        AnomalyBallInteractable ball = anomallyBalls[index].GetComponent<AnomalyBallInteractable>();
        if (ball != null)
        {
            ball.SetCollidersEnabled(isActive);
        }

        anomallyBalls[index].SetActive(isActive);
    }

    private bool TryActivateNextAvailableBall()
    {
        while (anomallyBalls != null && positionId >= 0 && positionId < anomallyBalls.Length)
        {
            GameObject ballObject = anomallyBalls[positionId];
            if (ballObject != null && CanActivateBallObject(ballObject))
            {
                SetBallActive(positionId, true);
                return true;
            }

            WarnOnce(ref warnedSkippedUnavailableBall, "Anomaly chase skipped an unavailable legacy ball reference.");
            positionId++;
        }

        return false;
    }

    private bool CanActivateBallObject(GameObject ballObject)
    {
        if (ballObject == null)
        {
            return false;
        }

        Transform parent = ballObject.transform.parent;
        while (parent != null)
        {
            if (!parent.gameObject.activeInHierarchy)
            {
                return false;
            }

            parent = parent.parent;
        }

        return true;
    }

    private void EnsureBallInteractors()
    {
        if (anomallyBalls == null)
        {
            return;
        }

        for (int i = 0; i < anomallyBalls.Length; i++)
        {
            GameObject ballObject = anomallyBalls[i];
            if (ballObject == null)
            {
                continue;
            }

            AnomalyBallInteractable interactable = ballObject.GetComponent<AnomalyBallInteractable>();
            if (interactable == null)
            {
                interactable = ballObject.AddComponent<AnomalyBallInteractable>();
            }

            interactable.Configure(this);
            interactable.SetCollidersEnabled(ballObject.activeSelf);
        }
    }

    private void PlaySphereClickSfx()
    {
        if (sphereClickSfx == null)
        {
            return;
        }

        SfxEmitter emitter = sphereFeedbackEmitter;
        if (emitter == null)
        {
            emitter = GetComponent<SfxEmitter>();
            if (emitter == null)
            {
                emitter = gameObject.AddComponent<SfxEmitter>();
            }

            sphereFeedbackEmitter = emitter;
        }

        emitter.Play(sphereClickSfx);
    }

    private void PlaySphereMoveEffect(int nextBallIndex)
    {
        if (sphereMoveEffectPrefab == null)
        {
            return;
        }

        Vector3 effectPosition = ResolveSphereMoveEffectPosition(nextBallIndex);
        Instantiate(sphereMoveEffectPrefab, effectPosition, Quaternion.identity);
    }

    private Vector3 ResolveSphereMoveEffectPosition(int nextBallIndex)
    {
        if (spawnMoveEffectAtNextPosition &&
            anomallyBalls != null &&
            nextBallIndex >= 0 &&
            nextBallIndex < anomallyBalls.Length &&
            anomallyBalls[nextBallIndex] != null)
        {
            return anomallyBalls[nextBallIndex].transform.position;
        }

        if (activeCubeItem != null)
        {
            return activeCubeItem.transform.position;
        }

        if (anomallyBalls != null && positionId >= 0 && positionId < anomallyBalls.Length && anomallyBalls[positionId] != null)
        {
            return anomallyBalls[positionId].transform.position;
        }

        return transform.position;
    }

    private void DisableLegacySceneCubeVisuals()
    {
        if (cube != null)
        {
            cube.SetActive(false);
        }

        if (sphere != null)
        {
            sphere.SetActive(false);
        }

        if (endCube != null)
        {
            endCube.SetActive(false);
        }
    }

    private void SetCubeEnergyVisualsVisible(bool isVisible)
    {
        CacheCubeEnergyVisuals();

        if (runtimeEnergyObjects != null)
        {
            for (int i = 0; i < runtimeEnergyObjects.Length; i++)
            {
                if (runtimeEnergyObjects[i] != null)
                {
                    runtimeEnergyObjects[i].SetActive(isVisible ? runtimeEnergyObjectStates[i] : false);
                }
            }
        }

        if (runtimeEnergyRenderers != null)
        {
            for (int i = 0; i < runtimeEnergyRenderers.Length; i++)
            {
                if (runtimeEnergyRenderers[i] != null)
                {
                    runtimeEnergyRenderers[i].enabled = isVisible ? runtimeEnergyRendererStates[i] : false;
                }
            }
        }
    }

    private void CacheCubeEnergyVisuals()
    {
        if (runtimeEnergyRenderers != null || activeCubeItem == null)
        {
            return;
        }

        if (cubeEnergyVisuals != null && cubeEnergyVisuals.Length > 0)
        {
            runtimeEnergyObjects = cubeEnergyVisuals;
            runtimeEnergyObjectStates = new bool[runtimeEnergyObjects.Length];
            for (int i = 0; i < runtimeEnergyObjects.Length; i++)
            {
                runtimeEnergyObjectStates[i] = runtimeEnergyObjects[i] != null && runtimeEnergyObjects[i].activeSelf;
            }

            runtimeEnergyRenderers = Array.Empty<Renderer>();
            runtimeEnergyRendererStates = Array.Empty<bool>();
            return;
        }

        Renderer[] renderers = activeCubeItem.GetComponentsInChildren<Renderer>(true);
        var candidates = new System.Collections.Generic.List<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer != null && IsEnergyVisualCandidate(renderer))
            {
                candidates.Add(renderer);
            }
        }

        runtimeEnergyRenderers = candidates.ToArray();
        runtimeEnergyRendererStates = new bool[runtimeEnergyRenderers.Length];
        for (int i = 0; i < runtimeEnergyRenderers.Length; i++)
        {
            runtimeEnergyRendererStates[i] = runtimeEnergyRenderers[i] != null && runtimeEnergyRenderers[i].enabled;
        }

        runtimeEnergyObjects = Array.Empty<GameObject>();
        runtimeEnergyObjectStates = Array.Empty<bool>();
    }

    private bool IsEnergyVisualCandidate(Renderer renderer)
    {
        string objectName = renderer.gameObject.name;
        if (ContainsEnergyVisualToken(objectName))
        {
            return true;
        }

        Material[] materials = renderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null && ContainsEnergyVisualToken(materials[i].name))
            {
                return true;
            }
        }

        return false;
    }

    private bool ContainsEnergyVisualToken(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return value.IndexOf("sphere", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("energy", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("force field", StringComparison.OrdinalIgnoreCase) >= 0 ||
               value.IndexOf("blue", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void ClearCubeEnergyVisualCache()
    {
        runtimeEnergyRenderers = null;
        runtimeEnergyRendererStates = null;
        runtimeEnergyObjects = null;
        runtimeEnergyObjectStates = null;
    }

    private bool IsStoryCubeCandidate(GameObject candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        return candidate.name.IndexOf("CubeSphere", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsSameObjectOrChild(GameObject candidate, GameObject parent)
    {
        return candidate != null &&
               parent != null &&
               (candidate == parent || candidate.transform.IsChildOf(parent.transform));
    }

    private void WarnOnce(ref bool flag, string message)
    {
        if (flag)
        {
            return;
        }

        flag = true;
        Debug.LogWarning(message);
    }
}
