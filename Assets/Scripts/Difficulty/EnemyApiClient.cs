using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class EnemyApiClient : MonoBehaviour
{
    [SerializeField] private string decisionEndpoint = "http://localhost:3000/api/enemy/decide";
    [SerializeField] private float timeoutSeconds = 10f;

    public IEnumerator RequestDecision(
        EnemyDecisionContext context,
        Action<EnemyApiDecisionResponse> onSuccess,
        Action<string> onError)
    {
        if (context == null)
        {
            onError?.Invoke("EnemyDecisionContext was null.");
            yield break;
        }

        string json = JsonUtility.ToJson(context);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(decisionEndpoint, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = Mathf.CeilToInt(timeoutSeconds);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"HTTP error: {request.error}");
            yield break;
        }

        string responseText = request.downloadHandler.text;

        if (string.IsNullOrWhiteSpace(responseText))
        {
            onError?.Invoke("Backend returned an empty response.");
            yield break;
        }

        EnemyApiDecisionResponse response;
        try
        {
            response = JsonUtility.FromJson<EnemyApiDecisionResponse>(responseText);
        }
        catch (Exception ex)
        {
            onError?.Invoke($"Failed to parse backend response: {ex.Message}");
            yield break;
        }

        if (response == null)
        {
            onError?.Invoke("Parsed backend response was null.");
            yield break;
        }

        onSuccess?.Invoke(response);
    }
}