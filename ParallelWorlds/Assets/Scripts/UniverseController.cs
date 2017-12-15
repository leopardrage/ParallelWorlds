using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Universe
{
    UniverseUndefined,
    UniverseA,
    UniverseB
};

/// <summery>
/// This component load the universe B additively on the current scene and provide utility methods to obtain a new universe for a spawing player
/// either randomly or using round robin logic.
/// </summery>
public class UniverseController : MonoBehaviour
{
    private enum UniversePickMode
    {
        Random,
        RoundRobin
    }

    public static UniverseController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UniverseController>();
            }

            return _instance;
        }
    }

    private static UniverseController _instance;

    [SerializeField] private UniversePickMode _universePickMode = UniversePickMode.RoundRobin;
    [SerializeField] private int _additiveSceneBuildIndex;

    private Universe _lastUniverse = Universe.UniverseUndefined;

    private void Awake()
    {
        SceneManager.LoadScene(_additiveSceneBuildIndex, LoadSceneMode.Additive);
    }

    public Universe GetSpawnUniverse()
    {
        Universe universe = 0;
        switch (_universePickMode)
        {
            case UniversePickMode.Random:
                universe = ((Random.Range(0, 2) == 0) ? Universe.UniverseA : Universe.UniverseB);
                break;
            case UniversePickMode.RoundRobin:
                _lastUniverse = ((_lastUniverse == Universe.UniverseA) ? Universe.UniverseB : Universe.UniverseA);
                universe = _lastUniverse;
                break;
        }
        return universe;
    }
}
