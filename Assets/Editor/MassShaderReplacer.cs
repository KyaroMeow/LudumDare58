using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MassShaderReplacer : EditorWindow
{
    private Shader targetShader;
    private string oldShaderName = "Standard";

    [MenuItem("Tools/Mass Shader Replacer")]
    public static void ShowWindow()
    {
        GetWindow<MassShaderReplacer>("Shader Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace All Materials Shader", EditorStyles.boldLabel);

        targetShader = (Shader)EditorGUILayout.ObjectField("Target Shader", targetShader, typeof(Shader), false);

        if (targetShader == null)
        {
            EditorGUILayout.HelpBox("Select URP Lit shader", MessageType.Info);
        }

        if (GUILayout.Button("Replace All Materials in Project"))
        {
            ReplaceAllMaterials();
        }

        if (GUILayout.Button("Replace Only Selected Materials"))
        {
            ReplaceSelectedMaterials();
        }
    }

    private void ReplaceAllMaterials()
    {
        if (targetShader == null)
        {
            Debug.LogError("Select target shader first!");
            return;
        }

        string[] materialGuids = AssetDatabase.FindAssets("t:Material");
        int count = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat.shader.name.Contains("Standard") || mat.shader.name.Contains("Legacy"))
            {
                mat.shader = targetShader;
                count++;
                EditorUtility.SetDirty(mat);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Updated {count} materials to {targetShader.name}");
    }

    private void ReplaceSelectedMaterials()
    {
        if (targetShader == null)
        {
            Debug.LogError("Select target shader first!");
            return;
        }

        int count = 0;
        foreach (Material mat in Selection.GetFiltered<Material>(SelectionMode.Assets))
        {
            mat.shader = targetShader;
            count++;
            EditorUtility.SetDirty(mat);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Updated {count} selected materials");
    }
}