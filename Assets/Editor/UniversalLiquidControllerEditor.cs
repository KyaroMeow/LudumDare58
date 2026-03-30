using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UniversalLiquidController))]
public class UniversalLiquidControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        UniversalLiquidController controller = (UniversalLiquidController)target;

        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Leave the original mesh and material untouched. This component creates a child liquid proxy automatically and applies the water shader to that proxy.",
            MessageType.Info);

        DrawQuickActions(controller);
        EditorGUILayout.Space(6f);
        DrawStatus(controller);
        EditorGUILayout.Space(6f);

        DrawPropertiesExcluding(serializedObject, "m_Script");
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawQuickActions(UniversalLiquidController controller)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Auto Detect Source And Build Water"))
            {
                Undo.RecordObject(controller, "Auto Setup Universal Liquid");
                controller.AutoSetupFromEditor();
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Rebuild Liquid Proxy"))
            {
                Undo.RecordObject(controller, "Rebuild Universal Liquid Proxy");
                controller.RebuildFromEditor();
                EditorUtility.SetDirty(controller);
            }

            using (new EditorGUI.DisabledScope(!controller.HasProxyObject))
            {
                if (GUILayout.Button("Select Liquid Proxy"))
                {
                    GameObject proxy = FindProxyObject(controller);
                    if (proxy != null)
                    {
                        Selection.activeGameObject = proxy;
                        EditorGUIUtility.PingObject(proxy);
                    }
                }
            }
        }
    }

    private void DrawStatus(UniversalLiquidController controller)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            DrawStatusLine("Source Mesh", controller.HasResolvedSourceMesh ? controller.ResolvedSourceName : "Not found");
            DrawStatusLine("Liquid Proxy", controller.HasProxyObject ? controller.ProxyObjectName : "Not built");

            if (!controller.HasResolvedSourceMesh)
            {
                EditorGUILayout.HelpBox(
                    "No source mesh found yet. Press 'Auto Detect Source And Build Water' or set Source Mesh Object Name manually.",
                    MessageType.Warning);
            }

            if (controller.SourceUsesLiquidShader)
            {
                EditorGUILayout.HelpBox(
                    "The original mesh is using the liquid shader directly. Keep a normal material on the original object and let this component render the water proxy.",
                    MessageType.Warning);
            }
        }
    }

    private void DrawStatusLine(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        EditorGUILayout.SelectableLabel(value, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndHorizontal();
    }

    private GameObject FindProxyObject(UniversalLiquidController controller)
    {
        Transform[] children = controller.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == "__UniversalLiquidProxy")
            {
                return child.gameObject;
            }
        }

        return null;
    }
}
