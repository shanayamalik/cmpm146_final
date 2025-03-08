using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureTracker : MonoBehaviour
{
    public static GameObject selectedCreaturePrefab; // stores creature prefab that user selected for instantiation in next scene
    public static GameObject selectedCreatureInstance; // The actual instantiated instance

    public static void SetSelectedPrefab(GameObject prefab)
    {
        selectedCreaturePrefab = prefab;
    }

    public static void SetSelectedInstance(GameObject instance)
    {
        selectedCreatureInstance = instance;
    }

}
