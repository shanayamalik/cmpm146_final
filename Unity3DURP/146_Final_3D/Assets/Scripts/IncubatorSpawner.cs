// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class IncubatorSpawner : MonoBehaviour
// {
//     // Start is called before the first frame update
//     void Start()
//     {
//         if (CreatureTracker.selectedCreaturePrefab != null)
//         {
//             Vector3 spawnPosition = new Vector3(0f, 0f, 100f); // adjust this to control where selected creature spawns
//             Quaternion spawnRotation = Quaternion.Euler(0f, 180f, 0f); // makes the creature spawn facing the right way
//             Instantiate(CreatureTracker.selectedCreaturePrefab, spawnPosition, spawnRotation);
//         }
//     }
// }



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IncubatorSpawner : MonoBehaviour
{
    public Camera mainCamera;
    public InputField chatInput;
    public Text chatResponseText;
    public Button feedButton;
    public Button playButton;
    public Button trainButton;
    public string apiKey = "YOUR_GEMINI_API_KEY_HERE"; // Replace with actual API key

    void Start()
    {
        if (CreatureTracker.selectedCreaturePrefab != null)
        {
            Vector3 spawnPosition = new Vector3(0f, 0f, 100f); // Adjust spawn position
            Quaternion spawnRotation = Quaternion.Euler(0f, 180f, 0f); // Adjust spawn rotation

            // Instantiate the selected creature
            GameObject spawnedCreature = Instantiate(CreatureTracker.selectedCreaturePrefab, spawnPosition, spawnRotation);

            CreatureTracker.SetSelectedInstance(spawnedCreature);

            // Enable and configure the MonsterAnimController component
            
            // MonsterAnimController animController = spawnedCreature.GetComponent<MonsterAnimController>();
            // if (animController != null)
            if (spawnedCreature.TryGetComponent<MonsterAnimController>(out var animController))
            {
                animController.enabled = true;
                animController.mainCamera = mainCamera != null ? mainCamera : Camera.main;
                animController.chatInput = chatInput;
                animController.chatResponseText = chatResponseText;
                animController.feedButton = feedButton;
                animController.playButton = playButton;
                animController.trainButton = trainButton;
                animController.apiKey = apiKey;
            }
            else
            {
                Debug.LogWarning("MonsterAnimController component not found on the spawned creature.");
            }
        }
        else
        {
            Debug.LogWarning("No creature prefab selected in CreatureTracker.");
        }
    }
}
