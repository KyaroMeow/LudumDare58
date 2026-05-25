//
//  Outline.cs
//  QuickOutline
//
//  Created by Chris Nolet on 3/30/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]

public class OutlineEffect : MonoBehaviour {
  private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();
  private static HashSet<int> warnedUnreadableMeshes = new HashSet<int>();

  public enum Mode {
    OutlineAll,
    OutlineVisible,
    OutlineHidden,
    OutlineAndSilhouette,
    SilhouetteOnly
  }

  public Mode OutlineMode {
    get { return outlineMode; }
    set {
      outlineMode = value;
      needsUpdate = true;
    }
  }

  public Color OutlineColor {
    get { return outlineColor; }
    set {
      outlineColor = value;
      needsUpdate = true;
    }
  }

  public float OutlineWidth {
    get { return outlineWidth; }
    set {
      outlineWidth = value;
      needsUpdate = true;
    }
  }

  [Serializable]
  private class ListVector3 {
    public List<Vector3> data;
  }

  [SerializeField]
  private Mode outlineMode;

  [SerializeField]
  private Color outlineColor = Color.white;

  [SerializeField, Range(0f, 10f)]
  private float outlineWidth = 2f;

  [Header("Optional")]

  [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
  + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
  private bool precomputeOutline;

  [SerializeField, HideInInspector]
  private List<Mesh> bakeKeys = new List<Mesh>();

  [SerializeField, HideInInspector]
  private List<ListVector3> bakeValues = new List<ListVector3>();

  private Renderer[] renderers;
  private Material outlineMaskMaterial;
  private Material outlineFillMaterial;

  private bool needsUpdate;

  void Awake() {

    // Cache renderers
    renderers = GetComponentsInChildren<Renderer>();

    // Instantiate outline materials
    var outlineMaskTemplate = Resources.Load<Material>(@"Materials/OutlineMask");
    var outlineFillTemplate = Resources.Load<Material>(@"Materials/OutlineFill");

    if (outlineMaskTemplate == null || outlineFillTemplate == null) {
      Debug.LogWarning("OutlineEffect could not load outline materials from Resources/Materials. Outline materials will not be added.", this);
    } else {
      outlineMaskMaterial = Instantiate(outlineMaskTemplate);
      outlineFillMaterial = Instantiate(outlineFillTemplate);

      outlineMaskMaterial.name = "OutlineMask (Instance)";
      outlineFillMaterial.name = "OutlineFill (Instance)";
    }

    // Retrieve or generate smooth normals
    LoadSmoothNormals();

    // Apply material properties immediately
    needsUpdate = true;
  }

  void OnEnable() {
    if (renderers == null || outlineMaskMaterial == null || outlineFillMaterial == null) {
      return;
    }

    foreach (var renderer in renderers) {
      if (renderer == null) {
        continue;
      }

      // Append outline shaders
      var materials = renderer.sharedMaterials.ToList();

      materials.Add(outlineMaskMaterial);
      materials.Add(outlineFillMaterial);

      renderer.materials = materials.ToArray();
    }
  }

  void OnValidate() {

    // Update material properties
    needsUpdate = true;

    // Clear cache when baking is disabled or corrupted
    if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count) {
      bakeKeys.Clear();
      bakeValues.Clear();
    }

    // Generate smooth normals when baking is enabled
    if (precomputeOutline && bakeKeys.Count == 0) {
      Bake();
    }
  }

  void Update() {
    if (needsUpdate) {
      needsUpdate = false;

      UpdateMaterialProperties();
    }
  }

  void OnDisable() {
    if (renderers == null || outlineMaskMaterial == null || outlineFillMaterial == null) {
      return;
    }

    foreach (var renderer in renderers) {
      if (renderer == null) {
        continue;
      }

      // Remove outline shaders
      var materials = renderer.sharedMaterials.ToList();

      materials.Remove(outlineMaskMaterial);
      materials.Remove(outlineFillMaterial);

      renderer.materials = materials.ToArray();
    }
  }

  void OnDestroy() {

    // Destroy material instances
    if (outlineMaskMaterial != null) {
      Destroy(outlineMaskMaterial);
    }

    if (outlineFillMaterial != null) {
      Destroy(outlineFillMaterial);
    }
  }

  void Bake() {

    // Generate smooth normals for each mesh
    var bakedMeshes = new HashSet<Mesh>();

    foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {
      var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
      if (!CanAccessMesh(mesh, "bake smooth normals")) {
        continue;
      }

      // Skip duplicates
      if (!bakedMeshes.Add(mesh)) {
        continue;
      }

      // Serialize smooth normals
      var smoothNormals = SmoothNormals(mesh);

      bakeKeys.Add(mesh);
      bakeValues.Add(new ListVector3() { data = smoothNormals });
    }
  }

  void LoadSmoothNormals() {

    // Retrieve or generate smooth normals
    foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {
      var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
      if (!CanAccessMesh(mesh, "load smooth normals")) {
        continue;
      }

      // Skip if smooth normals have already been adopted
      if (!registeredMeshes.Add(mesh)) {
        continue;
      }

      // Retrieve or generate smooth normals
      var index = bakeKeys.IndexOf(mesh);
      var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(mesh);

      // Store smooth normals in UV3
      mesh.SetUVs(3, smoothNormals);

      // Combine submeshes
      var renderer = meshFilter.GetComponent<Renderer>();

      if (renderer != null) {
        CombineSubmeshes(mesh, renderer.sharedMaterials);
      }
    }

    // Clear UV3 on skinned mesh renderers
    foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>()) {
      var mesh = skinnedMeshRenderer != null ? skinnedMeshRenderer.sharedMesh : null;
      if (!CanAccessMesh(mesh, "clear skinned mesh outline UVs")) {
        continue;
      }

      // Skip if UV3 has already been reset
      if (!registeredMeshes.Add(mesh)) {
        continue;
      }

      // Clear UV3
      mesh.uv4 = new Vector2[mesh.vertexCount];

      // Combine submeshes
      CombineSubmeshes(mesh, skinnedMeshRenderer.sharedMaterials);
    }
  }

  List<Vector3> SmoothNormals(Mesh mesh) {
    if (!CanAccessMesh(mesh, "calculate smooth normals")) {
      return new List<Vector3>();
    }

    // Group vertices by location
    var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);

    // Copy normals to a new list
    var smoothNormals = new List<Vector3>(mesh.normals);

    // Average normals for grouped vertices
    foreach (var group in groups) {

      // Skip single vertices
      if (group.Count() == 1) {
        continue;
      }

      // Calculate the average normal
      var smoothNormal = Vector3.zero;

      foreach (var pair in group) {
        smoothNormal += smoothNormals[pair.Value];
      }

      smoothNormal.Normalize();

      // Assign smooth normal to each vertex
      foreach (var pair in group) {
        smoothNormals[pair.Value] = smoothNormal;
      }
    }

    return smoothNormals;
  }

  void CombineSubmeshes(Mesh mesh, Material[] materials) {
    if (!CanAccessMesh(mesh, "combine submeshes")) {
      return;
    }

    // Skip meshes with a single submesh
    if (mesh.subMeshCount == 1) {
      return;
    }

    // Skip if submesh count exceeds material count
    if (mesh.subMeshCount > materials.Length) {
      return;
    }

    // Append combined submesh
    mesh.subMeshCount++;
    mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
  }

  void UpdateMaterialProperties() {
    if (outlineMaskMaterial == null || outlineFillMaterial == null) {
      return;
    }

    // Apply properties according to mode
    outlineFillMaterial.SetColor("_OutlineColor", outlineColor);

    switch (outlineMode) {
      case Mode.OutlineAll:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineVisible:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineHidden:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineAndSilhouette:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.SilhouetteOnly:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
        outlineFillMaterial.SetFloat("_OutlineWidth", 0f);
        break;
    }
  }

  private bool CanAccessMesh(Mesh mesh, string actionName) {
    if (mesh == null) {
      return false;
    }

    if (mesh.isReadable) {
      return true;
    }

    WarnUnreadableMesh(mesh, actionName);
    return false;
  }

  private void WarnUnreadableMesh(Mesh mesh, string actionName) {
    if (mesh == null) {
      return;
    }

    int meshId = mesh.GetInstanceID();
    if (!warnedUnreadableMeshes.Add(meshId)) {
      return;
    }

    Debug.LogWarning($"OutlineEffect skipped {actionName} for non-readable mesh '{mesh.name}'. Enable Read/Write in import settings for smooth outline normals.", this);
  }
}
