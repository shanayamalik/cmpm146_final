using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IncubatorSpawner : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (CreatureTracker.selectedCreaturePrefab != null)
        {
            Instantiate(CreatureTracker.selectedCreaturePrefab, Vector3.zero, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
