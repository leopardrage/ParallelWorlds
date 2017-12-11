using UnityEngine;

public class UniverseSetter : MonoBehaviour
{
    public void SetUniverse(int universe)
    {
        int universeLayer = (int)Mathf.Log(universe, 2);
        SetLayerRecursively(gameObject, universeLayer);
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