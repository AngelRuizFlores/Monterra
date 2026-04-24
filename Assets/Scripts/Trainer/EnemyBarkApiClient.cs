using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public sealed class EnemyBarkApiClient : MonoBehaviour
{
    [SerializeField] private string barkEndpoint = "http://localhost:3000/api/enemy/bark";
    [SerializeField] private float timeoutSeconds = 8f;

    public IEnumerator RequestBark(
        EnemyBarkContext context,
        Action<EnemyBarkResponse> onSuccess,
        Action<string> onError)
    {
        if (context == null)
        {
            onError?.Invoke("EnemyBarkContext was null.");
            yield break;
        }

        string json = JsonUtility.ToJson(context);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(barkEndpoint, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = Mathf.CeilToInt(timeoutSeconds);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error);
            yield break;
        }

        EnemyBarkResponse response;

        try
        {
            response = JsonUtility.FromJson<EnemyBarkResponse>(request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
            yield break;
        }

        if (response == null || string.IsNullOrWhiteSpace(response.bark))
        {
            onError?.Invoke("Empty bark response.");
            yield break;
        }

        onSuccess?.Invoke(response);
    }
}