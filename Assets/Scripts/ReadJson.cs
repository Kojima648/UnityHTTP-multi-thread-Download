// 1. 引入SimpleJSON

using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ReadJson : MonoBehaviour
{
    // 2. 设置一个变量,可以在Inspector设置读取的文件名
    public string m_strJsonPath = "";

    private void Start()
    {
        StartCoroutine(Load(Application.streamingAssetsPath + "/" + m_strJsonPath));
    }

    public IEnumerator Load(string uri)
    {
        // 3. 使用System.Uri 格式化uri
        Uri ss = new Uri(uri);

        // 4. 把格式化的字符串放到url变量
        string url = ss.ToString();

        // 5. 发送url请求
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // 6. 请求并等待所需的内容
            yield return webRequest.SendWebRequest();

            // 7. 判断请求是否成功
            if (webRequest.isNetworkError)
            {
                Debug.Log(": Error: " + webRequest.error);
            }
            else
            {
                // 8. 解析json数据
                JSONNode v_jnode = JSON.Parse(webRequest.downloadHandler.text);

                foreach (KeyValuePair<string, JSONNode> valuePair in v_jnode)
                {
                    foreach (var item in valuePair.Value)
                    {
                        Debug.Log(item.Key + "  ---  " + item.Value);
                    }
                }
            }
        }
    }
}