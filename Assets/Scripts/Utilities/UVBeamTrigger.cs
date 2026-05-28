using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UVBeamTrigger : MonoBehaviour
{
    [SerializeField] private bool revealOnStay = true;

    private readonly HashSet<UVRevealable> trackedRevealables = new HashSet<UVRevealable>();
    private UVLighter owner;
    private bool isBeamActive;

    public void Initialize(UVLighter lighterOwner, bool shouldRevealOnStay)
    {
        owner = lighterOwner;
        revealOnStay = shouldRevealOnStay;
    }

    public void SetBeamActive(bool active)
    {
        isBeamActive = active;
        if (!isBeamActive)
        {
            HideTrackedRevealables();
        }
    }

    public void ClearTrackedRevealables()
    {
        HideTrackedRevealables();
    }

    private void Update()
    {
        RefreshTrackedRevealables();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CanReveal())
        {
            return;
        }

        UVRevealable revealable = ResolveRevealable(other);
        if (revealable == null)
        {
            return;
        }

        trackedRevealables.Add(revealable);
        TryRevealOrHide(revealable);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!revealOnStay)
        {
            return;
        }

        UVRevealable revealable = ResolveRevealable(other);
        if (revealable == null)
        {
            return;
        }

        trackedRevealables.Add(revealable);
        TryRevealOrHide(revealable);
    }

    private void OnTriggerExit(Collider other)
    {
        UVRevealable revealable = ResolveRevealable(other);
        if (revealable == null)
        {
            return;
        }

        if (trackedRevealables.Remove(revealable))
        {
            revealable.Hide();
        }
    }

    private void OnDisable()
    {
        HideTrackedRevealables();
    }

    private bool CanReveal()
    {
        return isBeamActive && owner != null && owner.IsLighterActive;
    }

    private void RefreshTrackedRevealables()
    {
        if (trackedRevealables.Count == 0)
        {
            return;
        }

        foreach (UVRevealable revealable in trackedRevealables)
        {
            TryRevealOrHide(revealable);
        }
    }

    private void TryRevealOrHide(UVRevealable revealable)
    {
        if (revealable == null)
        {
            return;
        }

        if (CanReveal() && owner.TryGetRevealStrength(revealable, out float strength))
        {
            revealable.Reveal(strength);
        }
        else
        {
            revealable.Hide();
        }
    }

    private static UVRevealable ResolveRevealable(Collider other)
    {
        if (other == null)
        {
            return null;
        }

        UVRevealable revealable = other.GetComponent<UVRevealable>();
        if (revealable != null)
        {
            return revealable;
        }

        revealable = other.GetComponentInParent<UVRevealable>();
        if (revealable != null)
        {
            return revealable;
        }

        return other.GetComponentInChildren<UVRevealable>(true);
    }

    private void HideTrackedRevealables()
    {
        foreach (UVRevealable revealable in trackedRevealables)
        {
            if (revealable != null)
            {
                revealable.Hide();
            }
        }

        trackedRevealables.Clear();
    }
}
