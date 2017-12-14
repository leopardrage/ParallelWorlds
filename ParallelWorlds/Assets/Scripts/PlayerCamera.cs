using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerCamera : MonoBehaviour, IUniverseObserver
{
    [SerializeField] private Camera _camera;

    private void Reset()
    {
        _camera = GetComponent<Camera>();
    }

    // ------------- IUniverseObserver ---------------

    public void SetUniverseSettings(UniverseLayerSettings universe)
    {
        if (_camera != null)
        {
            _camera.cullingMask = universe.cullingMask;
        }
    }
}