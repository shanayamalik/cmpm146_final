using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureTracker : MonoBehaviour
{
    public static GameObject selectedCreaturePrefab; // stores creature prefab that user selected for instantiation in next scene

    public static void SetSelectedPrefab(GameObject prefab)
    {
        selectedCreaturePrefab = prefab;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
