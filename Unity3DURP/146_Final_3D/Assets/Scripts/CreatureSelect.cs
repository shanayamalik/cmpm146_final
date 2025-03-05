using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreatureSelect : MonoBehaviour
{
    public GameObject creaturePrefab;

    public void OnSelectCreature()
    {
        CreatureTracker.SetSelectedPrefab(creaturePrefab);
        SceneManager.LoadScene("IncubatorScene");
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
