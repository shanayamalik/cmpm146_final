using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAnimController : MonoBehaviour
{
    private Animator anim;
    private int currentState = 0;

    void Start()
    {
        anim = GetComponent<Animator>();
        anim.SetInteger("state", currentState); // Set initial state
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // On Space key press
        {
            CycleState();
        }
    }

    void CycleState()
    {
        currentState = (currentState + 1) % 5; // Loop from 0 -> 1 -> 2 -> 3 -> 4 -> 0
        anim.SetInteger("state", currentState);
    }
}
