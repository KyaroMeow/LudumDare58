using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VentHandSceneBinder
{
    private const string MenuPath = "Tools/Sorter/Bind Vent Hand Intro Scene References";

    [MenuItem(MenuPath)]
    public static void BindSceneReferences()
    {
        Scene scene = SceneManager.GetActiveScene();
        List<string> warnings = new List<string>();

        if (!scene.IsValid() || scene.name != "Main")
        {
            Debug.LogWarning($"Vent hand binder expected Main scene, but active scene is '{scene.name}'. Open Main first.");
            return;
        }

        GameObject systemsRoot = FindSceneObject("Systems_Root");
        GameObject controllerObject = FindOrCreateChild(systemsRoot, "VentHandIntroController");
        VentHandIntroController introController = EnsureComponent<VentHandIntroController>(controllerObject);
        GameObject exitControllerObject = FindOrCreateChild(systemsRoot, "ConveyorExitController");
        ConveyorExitController exitController = EnsureComponent<ConveyorExitController>(exitControllerObject);

        GameObject ceilingVent = FindSceneObject("CeilingVent");
        GameObject hand = FindSceneObject("hand_low");
        Transform hiddenPose = FindSceneObject("VentHand_HiddenPose")?.transform;
        Transform introPose = FindSceneObject("VentHand_IntroPose")?.transform;
        Transform idlePose = FindSceneObject("VentHand_IdlePose")?.transform;
        Transform keyDropPoint = FindSceneObject("KeyDropPoint")?.transform;
        GameObject caseObject = FindSceneObject("\u042F\u0449\u0438\u043A");
        GameObject conveyorObject = FindSceneObject("Conveyor");
        GameObject itemSpawnerObject = FindSceneObject("ItemSpawner");
        GameObject panelObject = FindByPath("\u0423\u043F\u0440\u0430\u0432\u043B\u0435\u043D\u0438\u0435/ElectricPanelController") ?? FindSceneObject("ElectricPanelController");

        ToolCaseLock caseLock = null;
        if (caseObject != null)
        {
            caseLock = EnsureComponent<ToolCaseLock>(caseObject);
            EnsureClickableCollider(caseObject);
            ConfigureToolCase(caseLock, caseObject, warnings);
        }
        else
        {
            warnings.Add("Tool case object 'Ящик' was not found.");
        }

        VentHandInteractable handInteractable = null;
        if (hand != null)
        {
            handInteractable = EnsureComponent<VentHandInteractable>(hand);
            EnsureClickableCollider(hand);
            EnsureComponent<OutlineEffect>(hand);
        }
        else
        {
            warnings.Add("Hand object 'hand_low' was not found.");
        }

        ConfigureIntroController(
            introController,
            ceilingVent,
            hand,
            hiddenPose,
            introPose,
            idlePose,
            keyDropPoint,
            caseLock,
            conveyorObject,
            itemSpawnerObject,
            panelObject,
            warnings);

        ConfigureHandInteractable(handInteractable, introController);
        ConfigureConveyorExit(exitController, conveyorObject, itemSpawnerObject, warnings);
        ConfigureGameManager(exitController);
        ConfigureElectricPanel(panelObject, warnings);

        EditorUtility.SetDirty(controllerObject);
        if (caseObject != null)
        {
            EditorUtility.SetDirty(caseObject);
        }

        if (hand != null)
        {
            EditorUtility.SetDirty(hand);
        }

        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log(BuildSummary(
            ceilingVent,
            hand,
            hiddenPose,
            introPose,
            idlePose,
            keyDropPoint,
            caseObject,
            conveyorObject,
            itemSpawnerObject,
            panelObject,
            caseLock,
            warnings));
    }

    public static void BindMainSceneReferencesAndSave()
    {
        const string mainScenePath = "Assets/Scenes/Main.unity";
        Scene scene = EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || scene.name != "Main")
        {
            Debug.LogError("Vent hand binder could not open Main scene for batch binding.");
            return;
        }

        BindSceneReferences();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Vent hand intro binding complete and Main scene saved.");
    }

    private static void ConfigureIntroController(
        VentHandIntroController controller,
        GameObject ceilingVent,
        GameObject hand,
        Transform hiddenPose,
        Transform introPose,
        Transform idlePose,
        Transform keyDropPoint,
        ToolCaseLock caseLock,
        GameObject conveyorObject,
        GameObject itemSpawnerObject,
        GameObject panelObject,
        List<string> warnings)
    {
        if (controller == null)
        {
            warnings.Add("VentHandIntroController component could not be created.");
            return;
        }

        SerializedObject so = new SerializedObject(controller);
        SetObjectIfNull(so, "gameManager", FindFirstObject<GameManager>());
        SetObjectIfNull(so, "electricPanelController", panelObject != null ? panelObject.GetComponent<ElectricPanelController>() : FindFirstObject<ElectricPanelController>());
        SetObjectIfNull(so, "conveyor", conveyorObject != null ? conveyorObject.GetComponent<Conveyor>() : FindFirstObject<Conveyor>());
        SetObjectIfNull(so, "itemSpawner", itemSpawnerObject != null ? itemSpawnerObject.GetComponent<ItemSpawner>() : FindFirstObject<ItemSpawner>());
        SetObjectIfNull(so, "toolCaseLock", caseLock);
        SetObjectIfNull(so, "handObject", hand);
        SetObjectIfNull(so, "hiddenPose", hiddenPose);
        SetObjectIfNull(so, "introPose", introPose);
        SetObjectIfNull(so, "idlePose", idlePose);
        SetObjectIfNull(so, "keyDropPoint", keyDropPoint);
        SetObjectIfNull(so, "ventAnimator", ceilingVent != null ? ceilingVent.GetComponent<Animator>() : null);
        SetObjectIfNull(so, "handAnimator", hand != null ? hand.GetComponentInChildren<Animator>(true) : null);
        SetObjectArrayIfEmpty(so, "handInteractionColliders", hand != null ? hand.GetComponentsInChildren<Collider>(true) : null);
        SetFloat(so, "minIntroDelay", 15f);
        SetFloat(so, "maxIntroDelay", 30f);
        SetBool(so, "enableCraftInteractionAfterIntro", false);
        SetBool(so, "appearDuringRegularBlackout", true);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(controller);

        if (ceilingVent == null) warnings.Add("CeilingVent was not found.");
        if (hiddenPose == null) warnings.Add("VentHand_HiddenPose was not found.");
        if (introPose == null) warnings.Add("VentHand_IntroPose was not found.");
        if (idlePose == null) warnings.Add("VentHand_IdlePose was not found.");
        if (keyDropPoint == null) warnings.Add("KeyDropPoint was not found.");
    }

    private static void ConfigureHandInteractable(VentHandInteractable handInteractable, VentHandIntroController controller)
    {
        if (handInteractable == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(handInteractable);
        SetObjectIfNull(so, "introController", controller);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(handInteractable);
    }

    private static void ConfigureGameManager(ConveyorExitController exitController)
    {
        GameManager gameManager = FindFirstObject<GameManager>();
        if (gameManager == null || exitController == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(gameManager);
        SetObjectIfNull(so, "conveyorExitController", exitController);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(gameManager);
    }

    private static void ConfigureConveyorExit(
        ConveyorExitController exitController,
        GameObject conveyorObject,
        GameObject itemSpawnerObject,
        List<string> warnings)
    {
        if (exitController == null)
        {
            warnings.Add("ConveyorExitController component could not be created.");
            return;
        }

        Animator exitDoorAnimator = FindExitDoorAnimator(conveyorObject, warnings);
        GameObject itemTriggerObject = FindSceneObject("ItemTrigger");
        ConveyorExitTrigger exitTrigger = null;
        if (itemTriggerObject != null)
        {
            exitTrigger = EnsureComponent<ConveyorExitTrigger>(itemTriggerObject);
            SerializedObject triggerSo = new SerializedObject(exitTrigger);
            SetObjectIfNull(triggerSo, "controller", exitController);
            triggerSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(exitTrigger);
        }

        if (exitDoorAnimator == null)
        {
            warnings.Add("Exit conveyor door 'Conveyor/door_conveyo_function (2)' was not found; assign ConveyorExitController.exitDoorAnimator manually.");
        }

        if (itemTriggerObject == null)
        {
            warnings.Add("ItemTrigger was not found; ConveyorExitController will use timeout fallback unless assigned manually.");
        }

        SerializedObject so = new SerializedObject(exitController);
        SetObjectIfNull(so, "exitDoorAnimator", exitDoorAnimator);
        SetObjectIfNull(so, "exitTrigger", exitTrigger);
        SetFloat(so, "doorOpenDelay", 0.2f);
        SetFloat(so, "destroyDelayAfterTrigger", 3f);
        SetFloat(so, "maxExitWaitTime", 12f);
        SetBool(so, "completeWhenTriggerReached", true);
        SetBool(so, "closeDoorAfterDestroy", true);
        SetBool(so, "useAnimatorTriggers", true);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(exitController);
    }

    private static void ConfigureToolCase(ToolCaseLock caseLock, GameObject caseObject, List<string> warnings)
    {
        if (caseLock == null || caseObject == null)
        {
            return;
        }

        EnsureCaseVisualsActive(caseObject);
        Transform lidTransform = FindChildRecursive(caseObject.transform, "Cube.017");
        Transform closedPose = EnsureToolCasePose(caseObject.transform, "ToolCase_ClosedPose", lidTransform, false);
        Transform openPose = EnsureToolCasePose(caseObject.transform, "ToolCase_OpenPose", lidTransform, true);
        Instrument[] instruments = FindControlledInstruments();

        SerializedObject so = new SerializedObject(caseLock);
        SetBool(so, "startsLocked", true);
        SetBool(so, "isUnlocked", false);
        SetBool(so, "isOpen", false);
        SetBool(so, "keepCaseBaseAlwaysVisible", true);
        SetBool(so, "disableCaseColliderWhenOpen", true);
        SetObjectIfNull(so, "lidTransform", lidTransform);
        SetObjectIfNull(so, "closedPose", closedPose);
        SetObjectIfNull(so, "openPose", openPose);
        SetFloat(so, "openDuration", 0.55f);
        SetObject(so, "closedVisual", null);
        SetObject(so, "openVisual", null);
        SetObjectIfNull(so, "caseClosedCollider", caseObject.GetComponent<Collider>());
        SetObjectIfNull(so, "interactCollider", caseObject.GetComponent<Collider>());
        SetObjectArrayIfEmpty(so, "controlledInstruments", instruments);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(caseLock);

        if (lidTransform == null)
        {
            warnings.Add("Tool case lid Cube.017 was not found; assign ToolCaseLock.lidTransform manually.");
        }

        if (closedPose == null || openPose == null)
        {
            warnings.Add("Tool case pose helpers could not be created; create ToolCase_ClosedPose and ToolCase_OpenPose manually.");
        }

        if (instruments == null || instruments.Length == 0)
        {
            warnings.Add("No Instrument components were found under 'istruments'; assign ToolCaseLock controlled tools manually.");
        }
    }

    private static void EnsureCaseVisualsActive(GameObject caseObject)
    {
        if (caseObject == null)
        {
            return;
        }

        string[] visualNames = { "Cube.010", "Cube.017" };
        for (int i = 0; i < visualNames.Length; i++)
        {
            Transform visual = FindChildRecursive(caseObject.transform, visualNames[i]);
            if (visual == null || visual.gameObject.activeSelf)
            {
                continue;
            }

            Undo.RecordObject(visual.gameObject, $"Show {visualNames[i]}");
            visual.gameObject.SetActive(true);
            EditorUtility.SetDirty(visual.gameObject);
        }
    }

    private static Transform EnsureToolCasePose(Transform caseTransform, string poseName, Transform lidTransform, bool openPose)
    {
        if (caseTransform == null)
        {
            return null;
        }

        Transform existing = FindDirectChild(caseTransform, poseName);
        if (existing != null)
        {
            return existing;
        }

        GameObject poseObject = new GameObject(poseName);
        Undo.RegisterCreatedObjectUndo(poseObject, $"Create {poseName}");
        poseObject.transform.SetParent(caseTransform, false);

        if (lidTransform != null)
        {
            poseObject.transform.localPosition = lidTransform.localPosition;
            poseObject.transform.localRotation = openPose
                ? lidTransform.localRotation * Quaternion.Euler(-75f, 0f, 0f)
                : lidTransform.localRotation;
            poseObject.transform.localScale = lidTransform.localScale;
        }

        return poseObject.transform;
    }

    private static void ConfigureElectricPanel(GameObject panelObject, List<string> warnings)
    {
        ElectricPanelController panel = panelObject != null ? panelObject.GetComponent<ElectricPanelController>() : null;
        if (panel == null)
        {
            warnings.Add("ElectricPanelController was not found. Vent hand intro will still run, but story blackout cannot control the panel.");
            return;
        }

        SerializedObject so = new SerializedObject(panel);
        SetBool(so, "debugAllowPanelBeforeHandIntro", false);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(panel);
    }

    private static Animator FindExitDoorAnimator(GameObject conveyorObject, List<string> warnings)
    {
        if (conveyorObject == null)
        {
            warnings.Add("Conveyor object was not found; ConveyorExitController cannot be configured.");
            return null;
        }

        Transform exactDoor = FindDirectChild(conveyorObject.transform, "door_conveyo_function (2)");
        Animator exactAnimator = exactDoor != null ? exactDoor.GetComponent<Animator>() : null;
        if (exactAnimator != null)
        {
            return exactAnimator;
        }

        warnings.Add("Exact exit door 'door_conveyo_function (2)' was not found under Conveyor.");
        return null;
    }

    private static int GetObjectArraySize(Object target, string propertyName)
    {
        if (target == null)
        {
            return 0;
        }

        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            return 0;
        }

        return property.arraySize;
    }

    private static Instrument[] FindControlledInstruments()
    {
        GameObject instrumentsRoot = FindSceneObject("istruments");
        if (instrumentsRoot != null)
        {
            return instrumentsRoot.GetComponentsInChildren<Instrument>(true);
        }

        return Object.FindObjectsByType<Instrument>(FindObjectsSortMode.None);
    }

    private static void EnsureClickableCollider(GameObject target)
    {
        if (target == null || target.GetComponent<Collider>() != null)
        {
            return;
        }

        BoxCollider collider = Undo.AddComponent<BoxCollider>(target);
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            collider.center = Vector3.zero;
            collider.size = Vector3.one;
            return;
        }

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 localMin = target.transform.InverseTransformPoint(worldBounds.min);
        Vector3 localMax = target.transform.InverseTransformPoint(worldBounds.max);
        collider.center = (localMin + localMax) * 0.5f;
        collider.size = new Vector3(
            Mathf.Abs(localMax.x - localMin.x),
            Mathf.Abs(localMax.y - localMin.y),
            Mathf.Abs(localMax.z - localMin.z));
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        if (target == null)
        {
            return null;
        }

        T component = target.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        return Undo.AddComponent<T>(target);
    }

    private static GameObject FindOrCreateChild(GameObject parent, string childName)
    {
        if (parent != null)
        {
            Transform existing = FindDirectChild(parent.transform, childName);
            if (existing != null)
            {
                return existing.gameObject;
            }
        }
        else
        {
            GameObject rootExisting = FindSceneObject(childName);
            if (rootExisting != null)
            {
                return rootExisting;
            }
        }

        GameObject created = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(created, $"Create {childName}");
        if (parent != null)
        {
            created.transform.SetParent(parent.transform, false);
        }

        return created;
    }

    private static GameObject FindByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string[] parts = path.Split('/');
        if (parts.Length == 0)
        {
            return null;
        }

        GameObject currentObject = FindSceneObject(parts[0]);
        Transform current = currentObject != null ? currentObject.transform : null;
        for (int i = 1; i < parts.Length && current != null; i++)
        {
            current = FindDirectChild(current, parts[i]);
        }

        return current != null ? current.gameObject : null;
    }

    private static GameObject FindSceneObject(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        Scene activeScene = SceneManager.GetActiveScene();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform != null && transform.gameObject.scene == activeScene && transform.name == name)
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static Transform FindDirectChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static T FindFirstObject<T>() where T : Object
    {
        T[] objects = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        return objects != null && objects.Length > 0 ? objects[0] : null;
    }

    private static void SetObject(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetObjectIfNull(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null && property.objectReferenceValue == null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetObjectArray<T>(SerializedObject so, string propertyName, T[] values) where T : Object
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.arraySize = values != null ? values.Length : 0;
        for (int i = 0; values != null && i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetObjectArrayIfEmpty<T>(SerializedObject so, string propertyName, T[] values) where T : Object
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null || property.arraySize > 0)
        {
            return;
        }

        SetObjectArray(so, propertyName, values);
    }

    private static void SetFloat(SerializedObject so, string propertyName, float value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetBool(SerializedObject so, string propertyName, bool value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static string BuildSummary(
        GameObject ceilingVent,
        GameObject hand,
        Transform hiddenPose,
        Transform introPose,
        Transform idlePose,
        Transform keyDropPoint,
        GameObject caseObject,
        GameObject conveyorObject,
        GameObject itemSpawnerObject,
        GameObject panelObject,
        ToolCaseLock caseLock,
        List<string> warnings)
    {
        Instrument[] instruments = FindControlledInstruments();
        bool closedPoseFound = caseObject != null && FindDirectChild(caseObject.transform, "ToolCase_ClosedPose") != null;
        bool openPoseFound = caseObject != null && FindDirectChild(caseObject.transform, "ToolCase_OpenPose") != null;
        ConveyorExitController exitController = Object.FindFirstObjectByType<ConveyorExitController>();
        ConveyorExitTrigger exitTrigger = Object.FindFirstObjectByType<ConveyorExitTrigger>();
        VentHandIntroController introController = Object.FindFirstObjectByType<VentHandIntroController>();
        int handLightCount = introController != null
            ? GetObjectArraySize(introController, "handArrivalLights") +
              GetObjectArraySize(introController, "ventHandLights") +
              GetObjectArraySize(introController, "handHighlightLights") +
              GetObjectArraySize(introController, "handFeedbackLights")
            : 0;
        return
            "Vent hand intro binding complete. Please save Main scene (Ctrl+S).\n" +
            $"CeilingVent found: {ceilingVent != null}\n" +
            $"hand_low found: {hand != null}\n" +
            $"Hidden/Intro/Idle poses found: {hiddenPose != null}/{introPose != null}/{idlePose != null}\n" +
            $"KeyDropPoint found: {keyDropPoint != null}\n" +
            $"Tool case found: {caseObject != null}, ToolCaseLock assigned: {caseLock != null}\n" +
            $"Tool case poses found/created: Closed={closedPoseFound}, Open={openPoseFound}\n" +
            $"Conveyor found: {conveyorObject != null}, ItemSpawner found: {itemSpawnerObject != null}\n" +
            $"ConveyorExitController found/created: {exitController != null}\n" +
            $"ConveyorExitTrigger found/assigned: {exitTrigger != null}\n" +
            $"Conveyor exit trigger assigned: {exitController != null && exitController.HasExitTarget}\n" +
            $"ElectricPanelController found: {panelObject != null}\n" +
            $"Hand lights assigned manually: {handLightCount}\n" +
            $"Controlled instruments assigned: {instruments.Length}\n" +
            $"Warnings: {(warnings.Count == 0 ? "none" : string.Join("; ", warnings))}";
    }
}
