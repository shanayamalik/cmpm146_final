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
    }

    private void LoadApiKey()
    {
        try
        {
            string debugMessage = "API Key Loading: ";
            string envFilePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ".env");
            
            debugMessage += $"Looking for .env at: {envFilePath}... ";
            
            if (File.Exists(envFilePath))
            {
                debugMessage += "Found! ";
                string[] lines = File.ReadAllLines(envFilePath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("GEMINI_API_KEY="))
                    {
                        apiKey = line.Substring("GEMINI_API_KEY=".Length).Trim('"');
                        debugMessage += "API key loaded from .env file";
                        Debug.Log(debugMessage);
                        if (chatResponseText != null)
                        {
                            chatResponseText.text = debugMessage;
                        }
                        return;
                    }
                }
                debugMessage += "GEMINI_API_KEY not found in .env. ";
            }
            else
            {
                debugMessage += ".env file not found. ";
            }
            
            // Fallback to environment variable if .env file not found or key not in file
            debugMessage += "Checking environment variables... ";
            apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            
            if (string.IsNullOrEmpty(apiKey))
            {
                debugMessage += "API key not found in environment variables.";
                Debug.LogError(debugMessage);
                if (chatResponseText != null)
                {
                    chatResponseText.text = debugMessage;
                }
            }
            else
            {
                debugMessage += "API key loaded from environment variables.";
                Debug.Log(debugMessage);
                if (chatResponseText != null)
                {
                    chatResponseText.text = debugMessage;
                }
            }
        }
        catch (Exception ex)
        {
            string errorMsg = $"Error loading API key: {ex.Message}";
            Debug.LogError(errorMsg);
            if (chatResponseText != null)
            {
                chatResponseText.text = errorMsg;
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
                SetAnimation("idle"); // Since eat animation isn't available yet
                chatResponseText.text = "Feeding";
                Debug.Log("Feeding!!");
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
            string errorMsg = "API key not available. Please check your environment setup.";
            Debug.LogError(errorMsg);
            if (chatResponseText != null)
            {
                chatResponseText.text = errorMsg;
            }
            isWaitingForResponse = false;
            yield break;
        }
        
        string prompt = GenerateSystemPrompt(eventType, message);
        
        // Display detailed status message
        if (chatResponseText != null)
        {
            chatResponseText.text = $"Starting API call...\nEvent: {eventType}\nInput: {(string.IsNullOrEmpty(message) ? "[none]" : message)}\nUsing API key: {apiKey.Substring(0, 3)}...{apiKey.Substring(apiKey.Length - 3)}";
        }
        
        yield return new WaitForSeconds(0.5f); // Brief pause to display the initial status
        
        if (chatResponseText != null)
        {
            chatResponseText.text += "\n\nCalling Gemini API...";
        }
        
        string jsonResponse = null;
        DateTime startTime = DateTime.Now;
        
        // Call API and wait for response
        var apiCallTask = CallGeminiAPI(prompt);
        
        // Add a counter to show that it's still working
        int dots = 0;
        while (!apiCallTask.IsCompleted)
        {
            dots = (dots + 1) % 4;
            string dotAnimation = new string('.', dots);
            TimeSpan elapsed = DateTime.Now - startTime;
            
            if (chatResponseText != null)
            {
                chatResponseText.text = $"Calling Gemini API{dotAnimation}\nElapsed time: {elapsed.TotalSeconds:F1}s";
            }
            
            yield return new WaitForSeconds(0.2f);
        }
        
        jsonResponse = apiCallTask.Result;
        
        // Process response
        if (!string.IsNullOrEmpty(jsonResponse))
        {
            if (chatResponseText != null)
            {
                chatResponseText.text = "Response received, processing...";
                yield return new WaitForSeconds(0.3f); // Brief pause to show processing message
            }
            
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
                string errorMsg = $"Error processing response: {ex.Message}";
                Debug.LogError(errorMsg);
                if (chatResponseText != null)
                {
                    chatResponseText.text = errorMsg;
                }
            }
        }
        else
        {
            string errorMsg = "Failed to get a response from Gemini API. Check console for details.";
            Debug.LogError(errorMsg);
            if (chatResponseText != null)
            {
                chatResponseText.text = errorMsg;
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
            $"Generate JSON response in EXACTLY this format: " +
            $"{{\"dialogue\": \"Your response here\", " +
            $"\"emotion_updates\": {{\"attitude\": 0.5, \"hunger\": 0.5, \"playfulness\": 0.5, " +
            $"\"irritation\": 0.2, \"sleepiness\": 0.3, \"excitement\": 0.7}}, " +
            $"\"animation\": \"idle\"}}. " +
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
        Debug.Log($"Sending request to Gemini API with prompt length: {prompt.Length} chars");
        
        try
        {
            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                // Send the request and await completion
                Debug.Log("Starting Gemini API request...");
                var operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Delay(10);
                }
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"API request successful. Response length: {request.downloadHandler.text.Length} chars");
                    if (chatResponseText != null)
                    {
                        // We'll update this on the main thread through the coroutine
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                            chatResponseText.text = "API Request successful, parsing response...";
                        });
                    }
                    return request.downloadHandler.text;
                }
                else
                {
                    string errorMessage = $"API Request Failed: {request.error}\nResponse: {request.downloadHandler.text}";
                    Debug.LogError(errorMessage);
                    
                    if (chatResponseText != null)
                    {
                        // Update UI with error on main thread
                        UnityMainThreadDispatcher.Instance().Enqueue(() => {
                            chatResponseText.text = $"Error: {request.error}";
                        });
                    }
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            string errorMessage = $"Exception in API call: {ex.Message}";
            Debug.LogError(errorMessage);
            
            if (chatResponseText != null)
            {
                // Update UI with error on main thread
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    chatResponseText.text = $"Exception: {ex.Message}";
                });
            }
            return null;
        }
    }
    
    string HandleAIResponse(string jsonResponse)
    {
        try
        {
            Debug.Log("Parsing Gemini API response...");
            
            // Parse the Gemini API response first
            GeminiResponseWrapper geminiWrapper = JsonConvert.DeserializeObject<GeminiResponseWrapper>(jsonResponse);
            
            if (geminiWrapper == null || geminiWrapper.candidates == null || geminiWrapper.candidates.Length == 0)
            {
                string errorMsg = "Invalid Gemini API response structure";
                Debug.LogError(errorMsg);
                // Log the raw response for debugging
                Debug.LogError("Raw response: " + jsonResponse);
                return "I'm not sure what to say... (Response structure error)";
            }
            
            // Extract the text content from the response
            string textContent = geminiWrapper.candidates[0].content.parts[0].text;
            Debug.Log($"Extracted text content from Gemini (first 100 chars): {textContent.Substring(0, Math.Min(100, textContent.Length))}...");
            
            // Now parse our custom JSON format from the text
            try
            {
                GeminiAIResponse customResponse = JsonConvert.DeserializeObject<GeminiAIResponse>(textContent);
                
                if (customResponse == null)
                {
                    Debug.LogError("Failed to parse custom response JSON - null result");
                    Debug.LogError("Raw content: " + textContent);
                    return "I'm feeling confused... (Parsing error)";
                }
                
                // Check if dialogue is null
                if (customResponse.dialogue == null)
                {
                    Debug.LogError("Dialogue field is null in parsed response");
                    Debug.LogError("Raw content: " + textContent);
                    return "I'm not sure what to say... (Dialogue missing)";
                }
                
                // Update emotions with validation
                UpdateEmotions(customResponse.emotion_updates);
                
                // Set animation
                SetAnimation(customResponse.animation);
                
                Debug.Log($"Successfully processed AI response. Dialogue: {customResponse.dialogue.Substring(0, Math.Min(50, customResponse.dialogue.Length))}...");
                return customResponse.dialogue;
            }
            catch (JsonException jsonEx)
            {
                string errorMsg = $"JSON parsing error: {jsonEx.Message}";
                Debug.LogError(errorMsg);
                Debug.LogError("Content that failed to parse: " + textContent);
                return $"I'm having trouble understanding... (JSON error: {jsonEx.Message})";
            }
        }
        catch (Exception ex)
        {
            string errorMsg = $"Error parsing AI response: {ex.Message}";
            Debug.LogError(errorMsg);
            Debug.LogError("JSON response preview: " + jsonResponse.Substring(0, Math.Min(500, jsonResponse.Length)));
            return $"I'm not feeling quite right... (Error: {ex.Message})";
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
        
        // Log emotion updates for debugging
        Debug.Log($"Emotions updated - Attitude: {attitude}, Hunger: {hunger}, Playfulness: {playfulness}, " +
                $"Irritation: {irritation}, Sleepiness: {sleepiness}, Excitement: {excitement}");
    }
    
    void SetAnimation(string animationName)
    {
        if (anim == null) return;
        
        switch (animationName.ToLower())
        {
            case "idle":
                anim.SetInteger("state", 1);
                break;
            case "jump":
                anim.SetInteger("state", 2);
                break;
            case "walk":
                anim.SetInteger("state", 3);
                break;
            case "run":
                anim.SetInteger("state", 4);
                break;
            default:
                anim.SetInteger("state", 1); // Default to idle
                break;
        }
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
