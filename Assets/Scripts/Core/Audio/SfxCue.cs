using System;
using UnityEngine;

[Serializable]
public class SfxCue
{
    [SerializeField] private string label;
    [SerializeField] private AudioClip clip;
    [SerializeField] private bool useVariations;
    [SerializeField] private AudioClip[] variations;

    [Header("Playback")]
    [SerializeField] private bool loop;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool randomizeVolume;
    [SerializeField] private Vector2 volumeRange = new Vector2(0.9f, 1f);
    [Range(0.1f, 3f)]
    [SerializeField] private float pitch = 1f;
    [SerializeField] private bool randomizePitch;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("3D Sound")]
    [SerializeField] private bool spatial;
    [Range(0f, 1f)]
    [SerializeField] private float spatialBlend;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 15f;

    [Header("Advanced")]
    [Min(0f)]
    [SerializeField] private float delay;
    [SerializeField] private bool playIfAlreadyPlaying;
    [SerializeField] private bool stopPreviousLoopBeforeStart;

    public string Label => label;
    public AudioClip Clip => clip;
    public bool UseVariations => useVariations;
    public AudioClip[] Variations => variations;
    public bool Loop => loop;
    public float Volume => volume;
    public bool RandomizeVolume => randomizeVolume;
    public Vector2 VolumeRange => volumeRange;
    public float Pitch => pitch;
    public bool RandomizePitch => randomizePitch;
    public Vector2 PitchRange => pitchRange;
    public bool Spatial => spatial;
    public float SpatialBlend => spatialBlend;
    public float MinDistance => minDistance;
    public float MaxDistance => maxDistance;
    public float Delay => delay;
    public bool PlayIfAlreadyPlaying => playIfAlreadyPlaying;
    public bool StopPreviousLoopBeforeStart => stopPreviousLoopBeforeStart;
}
