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
            Vector3 spawnPosition = new Vector3(0f, 0f, 100f); // adjust this to control where selected creature spawns
            Quaternion spawnRotation = Quaternion.Euler(0f, 180f, 0f); // makes the creature spawn facing the right way
            Instantiate(CreatureTracker.selectedCreaturePrefab, spawnPosition, spawnRotation);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
