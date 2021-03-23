using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

public class Test : MonoBehaviour
{
    public JSONNode TableMapText { get; set; }

    private void Awake()
    {
        StartCoroutine(GetData());
    }

    void Start()
    {
        TableMapText["additional"]["gold"] = "500";
        TableMapText["additional"]["lv"] = "3";
        TableMapText["additional"]["exp"] = "560";
        TableMapText["additional"]["scd"] = "60";
        TableMapText["additional"]["bcd"] = "240";
        foreach (KeyValuePair<string, JSONNode> valuePair in TableMapText)
        {
            foreach (var item in valuePair.Value)
            {
                Debug.Log(item.Key + "  ---  " + item.Value);
                // if (item.Key.Equals("Level"))
                // {
                //     Debug.Log(item.Value.AsFloat);
                // }
            }
        }
    }

    IEnumerator GetData()
    {
        var uri = new System.Uri(Path.Combine(Application.streamingAssetsPath, "UserLevel.json"));
        UnityWebRequest www = UnityWebRequest.Get(uri);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            TableMapText = JSONNode.Parse(www.downloadHandler.text);
        }
    }
}