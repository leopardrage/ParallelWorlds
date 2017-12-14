using UnityEngine;

public class UniverseSetter : MonoBehaviour, IUniverseObserver
{
    // ------------- IUniverseObserver ---------------
    public void SetUniverseSettings(UniverseLayerSettings universe)
    {
        SetLayerRecursively(gameObject, universe.layer);
    }

    // ------------- UTILITY ---------------

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}