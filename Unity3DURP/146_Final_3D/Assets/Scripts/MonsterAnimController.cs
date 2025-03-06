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
    * 3. API CONFIGURATION (Shanaya can do this, but let me know on Discord if you want me to make and insert the key):
    *    - Get a Gemini API key from Google AI Studio (https://makersuite.google.com/)
    *    - Enter the API key in the Inspector field 'apiKey'
    *    - If needed, adjust the API parameters in the CallGeminiAPI method
    * 
    * 4. ANIMATOR SETUP:
    *    - Ensure the character has an Animator component with these animation states (which were given from Anything World?):
    *      - state 0: idle
    *      - state 1: walk
    *      - state 2: run
    *      - state 3: jump
    *    - The Animator should have an "state" integer parameter that this script controls
    * 
    * 5. TESTING:
    *    - Enter Play mode, type a message in the InputField and press Enter
    *    - The monster should respond based on its emotional state
    *    - The monster will also update its state every 10 seconds when idle
    * 
    * DEPENDENCIES:
    * - Requires Newtonsoft.Json for JSON parsing (install via Unity Package Manager)
    * - Uses UnityEngine.Networking for API calls
    */

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using System.Threading.Tasks;
    using UnityEngine.Networking;
    using Newtonsoft.Json;

    public class MonsterAnimController : MonoBehaviour
    {
        private Animator anim;
        public Camera mainCamera;
        public InputField chatInput;
        public Text chatResponseText;
        // UI Buttons (NEW)
        public Button feedButton;
        public Button playButton;
        public Button trainButton;
        [SerializeField]
        public string apiKey = "YOUR_GEMINI_API_KEY_HERE"; // Replace with actual API key
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

            // Assign Buttons (NEW)
            if (feedButton != null) feedButton.onClick.AddListener(() => OnPlayerAction("feed"));
            if (playButton != null) playButton.onClick.AddListener(() => OnPlayerAction("play"));
            if (trainButton != null) trainButton.onClick.AddListener(() => OnPlayerAction("train"));
    
            // Start periodic AI updates
            StartCoroutine(AutoUpdateAI());
        }

        // Player interacts via buttons (NEW)
        void OnPlayerAction(string actionType)
        {
            if (isWaitingForResponse) return;
            isWaitingForResponse = true;

            switch (actionType)
            {
                case "feed":
                    hunger = Mathf.Clamp01(hunger + 0.2f);
                    attitude = Mathf.Clamp01(attitude + 0.1f);
                    SetAnimation("eat");
                    chatResponseText.text = "Feeding";
                    Debug.Log("Feeding!!");
                    break;

                case "play":
                    playfulness = Mathf.Clamp01(playfulness + 0.2f);
                    sleepiness = Mathf.Clamp01(sleepiness - 0.1f);
                    SetAnimation("play");
                    chatResponseText.text = "Playing";
                    Debug.Log("Playing!!");
                    break;

                case "train":
                    irritation = Mathf.Clamp01(irritation - 0.2f);
                    excitement = Mathf.Clamp01(excitement + 0.2f);
                    SetAnimation("train");
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
            string prompt = GenerateSystemPrompt(eventType, message);
            
            // Display "thinking" message for better UX
            if (chatResponseText != null)
            {
                chatResponseText.text = "Thinking...";
            }
            
            string jsonResponse = null;
            
            // Call API and wait for response
            var apiCallTask = CallGeminiAPI(prompt);
            
            while (!apiCallTask.IsCompleted)
            {
                yield return null;
            }
            
            jsonResponse = apiCallTask.Result;
            
            // Process response
            if (!string.IsNullOrEmpty(jsonResponse))
            {
                string processedResponse = HandleAIResponse(jsonResponse);
                
                if (chatResponseText != null)
                {
                    chatResponseText.text = processedResponse;
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
                $"Generate JSON response in EXACTLY this format: " +
                $"{{\"dialogue\": \"Your response here\", " +
                $"\"emotion_updates\": {{\"attitude\": 0.5, \"hunger\": 0.5, \"playfulness\": 0.5, " +
                $"\"irritation\": 0.2, \"sleepiness\": 0.3, \"excitement\": 0.7}}, " +
                $"\"animation\": \"idle\"}}. " +
                $"Only use: idle, walk, run, jump, eat, play, train.";
        }

        
        async Task<string> CallGeminiAPI(string prompt)
        {
            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}";
            
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
                        Debug.LogError($"API Request Failed: {request.error}\nResponse: {request.downloadHandler.text}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in API call: {ex.Message}");
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
                    Debug.LogError("Invalid Gemini API response structure");
                    return "I'm not sure what to say...";
                }
                
                // Extract the text content from the response
                string textContent = geminiWrapper.candidates[0].content.parts[0].text;
                
                // Now parse our custom JSON format from the text
                GeminiAIResponse customResponse = JsonConvert.DeserializeObject<GeminiAIResponse>(textContent);
                
                if (customResponse == null)
                {
                    Debug.LogError("Failed to parse custom response JSON");
                    return "I'm feeling confused...";
                }
                
                // Update emotions with validation
                UpdateEmotions(customResponse.emotion_updates);
                
                // Set animation
                SetAnimation(customResponse.animation);
                
                return customResponse.dialogue;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing AI response: {ex.Message}\nJSON: {jsonResponse}");
                return "I'm not feeling quite right...";
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
                // case "eat":
                //     anim.SetInteger("state", 6);
                //     break;
                // case "play":
                //     anim.SetInteger("state", 7);
                //     break;
                // case "train":
                //     anim.SetInteger("state", 8);
                //     break;
                // default:
                //     anim.SetInteger("state", 0);
                //     break;
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
                    yield return StartCoroutine(ProcessAIResponse("", "animation_completed"));
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
