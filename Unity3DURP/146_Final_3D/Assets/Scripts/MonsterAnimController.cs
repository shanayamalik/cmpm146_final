/*
* MonsterAnimController.cs
* -----------------------
* 
* SETUP INSTRUCTIONS:
* 
* 1. ATTACH TO CHARACTER:
*    - Attach this script to the monster character GameObject in the scene
* 
* 2. SETUP UI COMPONENTS:
*    - Create a Canvas with:
*      a) InputField component (for player to type messages)
*      b) Text component (to display monster's responses)
*    - Drag these UI components to the corresponding fields in this script via the Inspector
* 
* 3. API CONFIGURATION:
*    - Create a .env file in the project root with GEMINI_API_KEY="your_key_here"
*    - The script will load this environment variable at runtime
*    - If needed, adjust the API parameters in the CallGeminiAPI method
* 
* 4. ANIMATOR SETUP:
*    - Ensure the character has an Animator component with these animation states:
*      - state 1: idle
*      - state 2: jump
*      - state 3: walk
*      - state 4: run
*    - The Animator should have a "state" integer parameter that this script controls
* 
* 5. TESTING:
*    - Enter Play mode, type a message in the InputField and press Enter
*    - The monster should respond based on its emotional state
*    - The monster will also update its state every 10 seconds when idle
* 
* DEPENDENCIES:
* - Requires Newtonsoft.Json for JSON parsing (install via Unity Package Manager)
* - Uses UnityEngine.Networking for API calls
* - Requires DotEnv package to load environment variables (install via Unity Package Manager)
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.IO;

// Main thread dispatcher for UI updates from async methods
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance = null;

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
}

public class MonsterAnimController : MonoBehaviour
{
    private Animator anim;
    public Camera mainCamera;
    public InputField chatInput;
    public Text chatResponseText;
    // UI Buttons
    public Button feedButton;
    public Button playButton;
    public Button trainButton;
    
    public string apiKey; // Will be loaded from environment variable - made public for access from other scripts
    private bool isWaitingForResponse = false; // Prevent multiple requests at once
    
    // Emotion states (0 to 1 scale)
    private float attitude = 0.5f;
    private float hunger = 0.5f;
    private float playfulness = 0.5f;
    private float irritation = 0.2f;
    private float sleepiness = 0.3f;
    private float excitement = 0.7f;

    public float Attitude => attitude;
    public float Hunger => hunger;
    public float Playfulness => playfulness;
    public float Irritation => irritation;
    public float Sleepiness => sleepiness;
    public float Excitement => excitement;

    void Start()
    {
        // Load API key from environment variable
        LoadApiKey();
        
        anim = GetComponent<Animator>();
        if (mainCamera == null) mainCamera = Camera.main;
        
        if (chatInput != null)
        {
            chatInput.onEndEdit.AddListener(OnChatSubmit);
        }
        else
        {
            Debug.LogError("Chat Input field not assigned to MonsterAnimController!");
        }
        
        if (chatResponseText == null)
        {
            Debug.LogError("Chat Response Text not assigned to MonsterAnimController!");
        }

        // Assign Buttons
        if (feedButton != null) feedButton.onClick.AddListener(() => OnPlayerAction("feed"));
        if (playButton != null) playButton.onClick.AddListener(() => OnPlayerAction("play"));
        if (trainButton != null) trainButton.onClick.AddListener(() => OnPlayerAction("train"));

        // Start periodic AI updates
        StartCoroutine(AutoUpdateAI());
        //UpdateStatus();
    }

    private void LoadApiKey()
    {
        try
        {
            string envFilePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ".env");
            
            if (File.Exists(envFilePath))
            {
                string[] lines = File.ReadAllLines(envFilePath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("GEMINI_API_KEY="))
                    {
                        apiKey = line.Substring("GEMINI_API_KEY=".Length).Trim('"');
                        if (chatResponseText != null)
                        {
                            chatResponseText.text = "API key loaded successfully";
                        }
                        return;
                    }
                }
            }
            
            // Fallback to environment variable if .env file not found or key not in file
            apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            
            if (string.IsNullOrEmpty(apiKey))
            {
                if (chatResponseText != null)
                {
                    chatResponseText.text = "API key not found in environment variables or .env file";
                }
            }
            else
            {
                if (chatResponseText != null)
                {
                    chatResponseText.text = "API key loaded successfully";
                }
            }
        }
        catch (Exception ex)
        {
            if (chatResponseText != null)
            {
                chatResponseText.text = "Error loading API key";
            }
        }
    }

    // Player interacts via buttons
    void OnPlayerAction(string actionType)
    {
        if (isWaitingForResponse) return;
        isWaitingForResponse = true;

        switch (actionType)
        {
            case "feed":
                hunger = Mathf.Clamp01(hunger - 0.2f); // Reduces hunger when fed
                attitude = Mathf.Clamp01(attitude + 0.1f);
                SetAnimation("walk"); // Since eat animation isn't available yet
                chatResponseText.text = "Feeding";
                Debug.Log("Feeding!!");
                Debug.Log("hunger: " + hunger + "\n" + "attitude: " + attitude);
                break;

            case "play":
                playfulness = Mathf.Clamp01(playfulness + 0.2f);
                sleepiness = Mathf.Clamp01(sleepiness - 0.1f);
                SetAnimation("jump"); // Use jump for play since play animation isn't available yet
                chatResponseText.text = "Playing";
                Debug.Log("Playing!!");
                break;

            case "train":
                irritation = Mathf.Clamp01(irritation - 0.2f);
                excitement = Mathf.Clamp01(excitement + 0.2f);
                SetAnimation("run"); // Use run for training since train animation isn't available yet
                chatResponseText.text = "Training";
                Debug.Log("Training!!");
                break;
        }

        StartCoroutine(ProcessAIResponse("", actionType)); // Send to AI
    }
    
    void OnChatSubmit(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || isWaitingForResponse) return;
        
        chatInput.text = ""; // Clear input field
        isWaitingForResponse = true;
        
        StartCoroutine(ProcessAIResponse(message, "player_input"));
    }
    
    IEnumerator ProcessAIResponse(string message, string eventType)
    {
        // Check if API key is available
        if (string.IsNullOrEmpty(apiKey))
        {
            if (chatResponseText != null)
            {
                chatResponseText.text = "API key not available. Please check your environment setup.";
            }
            isWaitingForResponse = false;
            yield break;
        }
        
        string prompt = GenerateSystemPrompt(eventType, message);
        
        // Display thinking message
        if (chatResponseText != null)
        {
            chatResponseText.text = "Thinking...";
        }
        
        string jsonResponse = null;
        
        // Call API and wait for response
        var apiCallTask = CallGeminiAPI(prompt);
        
        // Add a simple loading indicator
        int dots = 0;
        while (!apiCallTask.IsCompleted)
        {
            dots = (dots + 1) % 4;
            string dotAnimation = new string('.', dots);
            
            if (chatResponseText != null)
            {
                chatResponseText.text = $"Thinking{dotAnimation}";
            }
            
            yield return new WaitForSeconds(0.2f);
        }
        
        jsonResponse = apiCallTask.Result;
        
        // Process response
        if (!string.IsNullOrEmpty(jsonResponse))
        {
            try
            {
                string processedResponse = HandleAIResponse(jsonResponse);
                
                if (chatResponseText != null)
                {
                    chatResponseText.text = processedResponse;
                }
            }
            catch (Exception ex)
            {
                if (chatResponseText != null)
                {
                    chatResponseText.text = "I'm having trouble thinking right now.";
                }
            }
        }
        else
        {
            if (chatResponseText != null)
            {
                chatResponseText.text = "Sorry, I couldn't think of a response.";
            }
        }
        
        isWaitingForResponse = false;
    }
    
    string GenerateSystemPrompt(string eventType, string message)
    {
        return $"You are controlling a virtual monster character with emotions: " +
            $"attitude: {attitude:F2}, hunger: {hunger:F2}, " +
            $"playfulness: {playfulness:F2}, irritation: {irritation:F2}, " +
            $"sleepiness: {sleepiness:F2}, excitement: {excitement:F2}. " +
            $"Event: {eventType}. " +
            $"Player message: \"{message}\". " +
            $"Generate JSON response in EXACTLY this format with NO markdown formatting: " +
            $"{{\"dialogue\": \"Your response here\", " +
            $"\"emotion_updates\": {{\"attitude\": 0.5, \"hunger\": 0.5, \"playfulness\": 0.5, " +
            $"\"irritation\": 0.2, \"sleepiness\": 0.3, \"excitement\": 0.7}}, " +
            $"\"animation\": \"idle\"}}. " +
            $"IMPORTANT: No markdown formatting like ```json or ``` around the response. Return ONLY the JSON object itself. " +
            $"Only use animations: idle, walk, run, jump."; // Updated available animations
    }

    
    async Task<string> CallGeminiAPI(string prompt)
    {
        string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";
        
        // Properly format the request for Gemini API
        var requestData = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 1024
            }
        };
        
        string jsonData = JsonConvert.SerializeObject(requestData);
        
        try
        {
            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                // Send the request and await completion
                var operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Delay(10);
                }
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    return request.downloadHandler.text;
                }
                else
                {
                    if (chatResponseText != null)
                    {
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                            chatResponseText.text = "Failed to connect to Gemini API";
                        });
                    }
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            if (chatResponseText != null)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    chatResponseText.text = "Error connecting to Gemini API";
                });
            }
            return null;
        }
    }
    
    string HandleAIResponse(string jsonResponse)
    {
        try
        {
            // Parse the Gemini API response first
            GeminiResponseWrapper geminiWrapper = JsonConvert.DeserializeObject<GeminiResponseWrapper>(jsonResponse);
            
            if (geminiWrapper == null || geminiWrapper.candidates == null || geminiWrapper.candidates.Length == 0)
            {
                return "I'm not sure what to say...";
            }
            
            // Extract the text content from the response
            string textContent = geminiWrapper.candidates[0].content.parts[0].text;
            
            // Now parse our custom JSON format from the text
            try
            {
                // Clean the text content - remove markdown formatting if present
                string cleanJsonText = textContent;
                if (cleanJsonText.StartsWith("```json") && cleanJsonText.EndsWith("```"))
                {
                    cleanJsonText = cleanJsonText.Substring("```json".Length, cleanJsonText.Length - "```json".Length - "```".Length).Trim();
                }
                else if (cleanJsonText.StartsWith("```") && cleanJsonText.EndsWith("```"))
                {
                    cleanJsonText = cleanJsonText.Substring("```".Length, cleanJsonText.Length - "```".Length - "```".Length).Trim();
                }
                
                GeminiAIResponse customResponse = JsonConvert.DeserializeObject<GeminiAIResponse>(cleanJsonText);
                
                if (customResponse == null)
                {
                    return "I'm feeling confused...";
                }
                
                // Check if dialogue is null
                if (customResponse.dialogue == null)
                {
                    return "I'm not sure what to say...";
                }
                
                // Update emotions with validation
                UpdateEmotions(customResponse.emotion_updates);
                
                // Set animation
                SetAnimation(customResponse.animation);
                
                return customResponse.dialogue;
            }
            catch (JsonException)
            {
                // Try fallback parsing method - manual extraction
                string fallbackResponse = ManualJsonExtraction(textContent);
                if (!string.IsNullOrEmpty(fallbackResponse)) {
                    return fallbackResponse;
                }
                
                return "I'm having trouble understanding...";
            }
        }
        catch (Exception)
        {
            // Try fallback parsing method - manual extraction
            string fallbackResponse = ManualJsonExtraction(jsonResponse);
            if (!string.IsNullOrEmpty(fallbackResponse)) {
                return fallbackResponse;
            }
            
            return "I'm not feeling quite right...";
        }
    }
    
    // Manual JSON extraction in case the regular parser fails
    private string ManualJsonExtraction(string text)
    {
        try {
            // Try to find the dialogue field manually
            int dialogueStart = text.IndexOf("\"dialogue\":");
            if (dialogueStart < 0) {
                return null;
            }
            
            dialogueStart += "\"dialogue\":".Length;
            
            // Find the string value (should be between quotes)
            int valueStart = text.IndexOf("\"", dialogueStart);
            if (valueStart < 0) {
                return null;
            }
            
            valueStart++; // Move past the opening quote
            
            // Find the end quote for the dialogue value
            int valueEnd = -1;
            bool escaped = false;
            for (int i = valueStart; i < text.Length; i++) {
                if (text[i] == '\\') {
                    escaped = !escaped;
                } else if (text[i] == '"' && !escaped) {
                    valueEnd = i;
                    break;
                } else {
                    escaped = false;
                }
            }
            
            if (valueEnd < 0) {
                return null;
            }
            
            string dialogue = text.Substring(valueStart, valueEnd - valueStart);
            
            // Try to manually extract animation field
            string animation = "idle"; // Default
            int animationStart = text.IndexOf("\"animation\":");
            if (animationStart >= 0) {
                animationStart += "\"animation\":".Length;
                int animValueStart = text.IndexOf("\"", animationStart);
                if (animValueStart >= 0) {
                    animValueStart++; // Move past opening quote
                    int animValueEnd = text.IndexOf("\"", animValueStart);
                    if (animValueEnd >= 0) {
                        animation = text.Substring(animValueStart, animValueEnd - animValueStart);
                        SetAnimation(animation);
                    }
                }
            }
            
            return dialogue;
        }
        catch (Exception) {
            return null;
        }
    }
    
    void UpdateEmotions(EmotionUpdates updates)
    {
        if (updates == null) return;
        
        // Update with validation to ensure values stay between 0 and 1
        attitude = Mathf.Clamp01(updates.attitude);
        hunger = Mathf.Clamp01(updates.hunger);
        playfulness = Mathf.Clamp01(updates.playfulness);
        irritation = Mathf.Clamp01(updates.irritation);
        sleepiness = Mathf.Clamp01(updates.sleepiness);
        excitement = Mathf.Clamp01(updates.excitement);
    }
    
    void SetAnimation(string animationName)
    {
        if (anim == null) return;
        
        switch (animationName.ToLower())
        {
            case "idle":
                anim.SetInteger("state", 0);
                break;
            case "jump":
                anim.SetInteger("state", 1);
                Invoke("ReturnToIdle", 0.667f * 2); // Adjust the delay based on animation length
                break;
            case "walk":
                anim.SetInteger("state", 2);
                Invoke("ReturnToIdle", 1.0f);
                break;
            case "run":
                anim.SetInteger("state", 3);
                Invoke("ReturnToIdle", 2.0f);
                break;
            default:
                anim.SetInteger("state", 0);
                break;
        }
    }

    void ReturnToIdle()
    {
        anim.SetInteger("state", 0); // Return to idle
    }
    
    IEnumerator AutoUpdateAI()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f); // Send update every 10 seconds
            
            if (!isWaitingForResponse) // Avoid conflicts with player input
            {
                isWaitingForResponse = true;
                yield return StartCoroutine(ProcessAIResponse("", "auto_update"));
                isWaitingForResponse = false;
            }
        }
    }
}

// JSON Parsing Classes
[System.Serializable]
public class GeminiResponseWrapper
{
    public Candidate[] candidates;
}

[System.Serializable]
public class Candidate
{
    public Content content; 
}

[System.Serializable]
public class Content
{
    public Part[] parts;
    public string role;
}

[System.Serializable]
public class Part
{
    public string text;
}

[System.Serializable]
public class GeminiAIResponse
{
    public string dialogue;
    public EmotionUpdates emotion_updates;
    public string animation;
}

[System.Serializable]
public class EmotionUpdates
{
    public float attitude;
    public float hunger;
    public float playfulness;
    public float irritation;
    public float sleepiness;
    public float excitement;
}
