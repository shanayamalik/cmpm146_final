using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    public Button feedButton;
    public Button playButton;
    public Button trainButton;
    private string apiKey;
    private bool isWaitingForResponse = false;

    private float attitude = 0.5f;
    private float hunger = 0.5f;
    private float playfulness = 0.5f;
    private float irritation = 0.2f;
    private float sleepiness = 0.3f;
    private float excitement = 0.7f;

    void Start()
    {
        LoadAPIKey();
        anim = GetComponent<Animator>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (chatInput != null) chatInput.onEndEdit.AddListener(OnChatSubmit);
        if (feedButton != null) feedButton.onClick.AddListener(() => OnPlayerAction("feed"));
        if (playButton != null) playButton.onClick.AddListener(() => OnPlayerAction("play"));
        if (trainButton != null) trainButton.onClick.AddListener(() => OnPlayerAction("train"));
        StartCoroutine(AutoUpdateAI());
    }

    void LoadAPIKey()
    {
        string envFilePath = Application.dataPath + "/../.env"; // One level above 'Assets'
        if (File.Exists(envFilePath))
        {
            string[] lines = File.ReadAllLines(envFilePath);
            foreach (string line in lines)
            {
                if (line.StartsWith("GEMINI_API_KEY="))
                {
                    apiKey = line.Substring("GEMINI_API_KEY=".Length).Trim();
                    break;
                }
            }
        }
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API Key is missing or not loaded from .env file.");
        }
    }

    void OnPlayerAction(string actionType)
    {
        if (isWaitingForResponse) return;
        isWaitingForResponse = true;
        StartCoroutine(ProcessAIResponse("", actionType));
    }

    void OnChatSubmit(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || isWaitingForResponse) return;
        chatInput.text = "";
        isWaitingForResponse = true;
        StartCoroutine(ProcessAIResponse(message, "player_input"));
    }

    IEnumerator ProcessAIResponse(string message, string eventType)
    {
        string prompt = GenerateSystemPrompt(eventType, message);
        if (chatResponseText != null) chatResponseText.text = "Thinking...";
        string jsonResponse = null;
        var apiCallTask = CallGeminiAPI(prompt);
        while (!apiCallTask.IsCompleted) yield return null;
        jsonResponse = apiCallTask.Result;
        if (!string.IsNullOrEmpty(jsonResponse))
        {
            Debug.Log("Raw API Response: " + jsonResponse);
            if (chatResponseText != null) chatResponseText.text = "API Response:\n" + jsonResponse;
            string processedResponse = HandleAIResponse(jsonResponse);
            if (chatResponseText != null) chatResponseText.text = processedResponse;
        }
        else
        {
            if (chatResponseText != null) chatResponseText.text = "API call failed or returned empty response.";
        }
        isWaitingForResponse = false;
    }

    async Task<string> CallGeminiAPI(string prompt)
    {
        string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}";
        var requestData = new { contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } }, generationConfig = new { temperature = 0.7, topK = 40, topP = 0.95, maxOutputTokens = 1024 } };
        string jsonData = JsonConvert.SerializeObject(requestData);
        try
        {
            using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Delay(10);
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("API Response: " + request.downloadHandler.text);
                    return request.downloadHandler.text;
                }
                else
                {
                    string errorMessage = $"API Error: {request.result}\nHTTP Error: {request.responseCode}\nMessage: {request.error}\nResponse: {request.downloadHandler.text}";
                    Debug.LogError(errorMessage);
                    if (chatResponseText != null) chatResponseText.text = errorMessage;
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            string exceptionMessage = $"Exception in API call: {ex.Message}";
            Debug.LogError(exceptionMessage);
            if (chatResponseText != null) chatResponseText.text = exceptionMessage;
            return null;
        }
    }

    string GenerateSystemPrompt(string eventType, string message)
    {
        return $"You are controlling a virtual monster character with emotions: attitude: {attitude:F2}, hunger: {hunger:F2}, playfulness: {playfulness:F2}, irritation: {irritation:F2}, sleepiness: {sleepiness:F2}, excitement: {excitement:F2}. Event: {eventType}. Player message: \"{message}\". Generate JSON response in EXACTLY this format: {\"dialogue\": \"Your response here\", \"emotion_updates\": {\"attitude\": 0.5, \"hunger\": 0.5, \"playfulness\": 0.5, \"irritation\": 0.2, \"sleepiness\": 0.3, \"excitement\": 0.7}, \"animation\": \"idle\"}. Only use: idle, walk, run, jump, eat, play, train.";
    }

    string HandleAIResponse(string jsonResponse)
    {
        try
        {
            GeminiAIResponse response = JsonConvert.DeserializeObject<GeminiAIResponse>(jsonResponse);
            if (response == null) return "Invalid AI response format.";
            return response.dialogue;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing AI response: {ex.Message}\nJSON: {jsonResponse}");
            return "I'm feeling confused...";
        }
    }
}
