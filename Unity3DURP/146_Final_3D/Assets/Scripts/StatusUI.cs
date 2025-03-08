using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusUI : MonoBehaviour
{
    public TextMeshProUGUI attitudeText;
    public TextMeshProUGUI hungerText;
    public TextMeshProUGUI playfulnessText;
    public TextMeshProUGUI irritationText;
    public TextMeshProUGUI sleepinessText;
    public TextMeshProUGUI excitementText;

    private MonsterAnimController monsterScript;

    public Slider attitudeSlider;
    public Slider hungerSlider;
    public Slider playfulnessSlider;
    public Slider irritationSlider;
    public Slider sleepinessSlider;
    public Slider excitementSlider;

    void Start() {
        // Ensure we are referencing the correct instance
        // if (CreatureTracker.selectedCreatureInstance != null)
        // {
        //     if (CreatureTracker.selectedCreatureInstance.TryGetComponent(out monsterScript))
        //     {
        //         Debug.Log("From StatusUI: Successfully linked to MonsterAnimController.");
        //     }
        //     else
        //     {
        //         Debug.LogWarning("From StatusUI: MonsterAnimController component not found in scene.");
        //     }
        // }
        // else
        // {
        //     Debug.LogWarning("From StatusUI: No creature instance found in CreatureTracker.");
        // }
    }

    void Update()
    {
        

        // Keep checking until the instance is assigned
        if (monsterScript == null && CreatureTracker.selectedCreatureInstance != null)
        {
            if (CreatureTracker.selectedCreatureInstance.TryGetComponent(out monsterScript))
            {
                Debug.Log("From StatusUI: Successfully linked to MonsterAnimController.");
                Debug.Log("From StatusUI: attitude: " + monsterScript.Attitude);    
            }
            else
            {
                Debug.LogWarning("From StatusUI: MonsterAnimController component not found.");
            }
        }

        if (monsterScript != null)
        {
            //DisplayStatus();
            UpdateSliders();
        }
        
        // if (Input.GetKeyDown(KeyCode.I)) 
        // {
        //     DisplayStatus();
        // }
    }

    void DisplayStatus() {
        attitudeText.text = "Attitude: " + monsterScript.Attitude.ToString("F1");
        hungerText.text = "Hunger: " + monsterScript.Hunger.ToString("F1");
        playfulnessText.text = "Playfulness: " + monsterScript.Playfulness.ToString("F1");
        irritationText.text = "Irritation: " + monsterScript.Irritation.ToString("F1");
        sleepinessText.text = "Sleepiness: " + monsterScript.Sleepiness.ToString("F1");
        excitementText.text = "Excitement: " + monsterScript.Excitement.ToString("F1");
    }

    void UpdateSliders()
    {
        // Update sliders with the latest values
        attitudeSlider.value = monsterScript.Attitude;
        hungerSlider.value = monsterScript.Hunger;
        playfulnessSlider.value = monsterScript.Playfulness;
        irritationSlider.value = monsterScript.Irritation;
        sleepinessSlider.value = monsterScript.Sleepiness;
        excitementSlider.value = monsterScript.Excitement;

        // Update text labels
        attitudeText.text = "Attitude: " + monsterScript.Attitude.ToString("F1");
        hungerText.text = "Hunger: " + monsterScript.Hunger.ToString("F1");
        playfulnessText.text = "Playfulness: " + monsterScript.Playfulness.ToString("F1");
        irritationText.text = "Irritation: " + monsterScript.Irritation.ToString("F1");
        sleepinessText.text = "Sleepiness: " + monsterScript.Sleepiness.ToString("F1");
        excitementText.text = "Excitement: " + monsterScript.Excitement.ToString("F1");
    }
}
