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

public class UniverseController : MonoBehaviour
{
    private enum UniversePickMode
    {
        Random,
        RoundRobin
    }
    private static UniverseController _instance;

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

    [SerializeField] private UniversePickMode universePickMode;

    private Universe lastUniverse = Universe.UniverseUndefined;

    [SerializeField] private int _additiveSceneBuildIndex;

    void Awake()
    {
        SceneManager.LoadScene(_additiveSceneBuildIndex, LoadSceneMode.Additive);
    }

    public Universe GetSpawnUniverse()
    {
        Universe universe = 0;
        switch (universePickMode)
        {
            case UniversePickMode.Random:
                universe = ((Random.Range(0, 2) == 0) ? Universe.UniverseA : Universe.UniverseB);
                break;
            case UniversePickMode.RoundRobin:
                lastUniverse = ((lastUniverse == Universe.UniverseB) ? Universe.UniverseA : Universe.UniverseB);
                universe = lastUniverse;
                break;
        }
        return universe;
    }

    public Universe GetOppositeUniverse(Universe currentUniverse)
    {
        return ((currentUniverse == Universe.UniverseB) ? Universe.UniverseA : Universe.UniverseB);
    }
}
