using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public static bool Swapping
    {
        get; private set;
    }

    [SerializeField] private LayerMask UniverseA;
    [SerializeField] private LayerMask UniverseB;
    [SerializeField] private UniversePickMode universePickMode;

    private LayerMask lastUniverse;

    [SerializeField] private int _additiveSceneBuildIndex;

    void Awake()
    {
        SceneManager.LoadScene(_additiveSceneBuildIndex, LoadSceneMode.Additive);
    }

    public int GetSpawnUniverse()
    {
        int universe = 0;
        switch (universePickMode)
        {
            case UniversePickMode.Random:
                universe = ((Random.Range(0, 2) == 0) ? UniverseController.Instance.UniverseA : UniverseController.Instance.UniverseB).value;
                break;
            case UniversePickMode.RoundRobin:
                lastUniverse = ((lastUniverse == UniverseController.Instance.UniverseB) ? UniverseController.Instance.UniverseA : UniverseController.Instance.UniverseB);
                universe = lastUniverse.value;
                break;
        }
        return universe;
    }

    public int GetOppositeUniverse(int currentUniverse)
    {
        return ((currentUniverse == UniverseController.Instance.UniverseB) ? UniverseController.Instance.UniverseA : UniverseController.Instance.UniverseB).value;
    }
}
