using UnityEngine;

[System.Serializable]
public struct UniverseState
{
    public enum TransitionState
    {
        Normal,
        SwapIn,
        SwapOut
    }

    public UniverseState(Universe universe, TransitionState transitionState)
    {
        this.universe = universe;
        this.transitionState = transitionState;
    }

    public Universe universe;
    public TransitionState transitionState;
}

[System.Serializable]
public struct UniverseLayerSettings
{
    public int layer;
    public LayerMask cullingMask;
    public LayerMask shootMask;

    public UniverseLayerSettings(UniverseState currentUniverseState) : this()
    {
        this.layer = GetLayer(currentUniverseState);
        this.cullingMask = GetCullMask(currentUniverseState);
        this.shootMask = GetShootMask(currentUniverseState);
    }
    public UniverseLayerSettings(UniverseState currentUniverseState, UniverseState localUniverseState) : this()
    {
        this.layer = GetLayer(currentUniverseState, localUniverseState);
        this.cullingMask = GetCullMask(currentUniverseState);
        this.shootMask = GetShootMask(currentUniverseState);
    }

    private int GetLayer(UniverseState currentUniverseState)
    {
        // Both visual and physics set to the current universe
        return (currentUniverseState.universe == Universe.UniverseA) ? LayerNameToIndex(Constants.Layer.UniverseA) : LayerNameToIndex(Constants.Layer.UniverseB);
    }
    private int GetLayer(UniverseState currentUniverseState, UniverseState localUniverseState)
    {
        // Normal State: 
        if (currentUniverseState.transitionState == UniverseState.TransitionState.Normal)
        {
            // Both visual and physics set to the current universe
            return (currentUniverseState.universe == Universe.UniverseA) ? LayerNameToIndex(Constants.Layer.UniverseA) : LayerNameToIndex(Constants.Layer.UniverseB);
        }
        // Swapping
        else
        {
            // Same universe as the Local Player:
            if (localUniverseState.universe == currentUniverseState.universe)
            {
                // Both visual and physics set to the current universe
                return (currentUniverseState.universe == Universe.UniverseA) ? LayerNameToIndex(Constants.Layer.UniverseA) : LayerNameToIndex(Constants.Layer.UniverseB);
            }
            // Different universe from the Local Player:
            else
            {
                // Physics set to the current universe but visual set to match the local player's
                return (localUniverseState.universe == Universe.UniverseA) ? LayerNameToIndex(Constants.Layer.UniverseAcollisionB) : LayerNameToIndex(Constants.Layer.UniverseBcollisionA);
            }
        }
    }

    private LayerMask GetCullMask(UniverseState currentUniverseState)
    {
        if (currentUniverseState.universe == Universe.UniverseA)
        {
            return LayerMask.GetMask(Constants.Layer.UniverseA, Constants.Layer.UniverseAcollisionB);
        }
        else
        {
            return LayerMask.GetMask(Constants.Layer.UniverseB, Constants.Layer.UniverseBcollisionA);
        }
    }

    private LayerMask GetShootMask(UniverseState currentUniverseState)
    {
        if (currentUniverseState.universe == Universe.UniverseA)
        {
            return LayerMask.GetMask(Constants.Layer.UniverseA, Constants.Layer.UniverseBcollisionA);
        }
        else
        {
            return LayerMask.GetMask(Constants.Layer.UniverseB, Constants.Layer.UniverseAcollisionB);
        }
    }

    private int LayerNameToIndex(string layerName)
    {
        return Mathf.RoundToInt(Mathf.Log(LayerMask.NameToLayer(layerName), 2));
    }
}