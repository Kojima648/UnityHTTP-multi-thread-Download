using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TestDown : MonoBehaviour
{
    CHttpDownMng m_downMng;
    public Button downBtn;
    public Button openExplorerBtn;

    void Start()
    {
        CTcp.StartTestIPV6("ipv6-test.com"); // 启动时测试一下我本地的IPV6环境, 请将这个域名修改你项目的CDN地址
        downBtn.onClick.AddListener(StartDown);
        openExplorerBtn.onClick.AddListener(OpenExplorer);
    }

    void StartDown()
    {
        // 以下只是测试几个下载，麻烦同学修改成您项目的资源url
        List<DownResInfo> downList = new List<DownResInfo>();
        PushDownFile(downList,
            "http://192.168.1.145:8089/%E5%AF%8C%E4%BA%BA%E5%AE%B6%E7%85%A7%E5%A3%81%20(1).jpg");
        PushDownFile(downList,
            "http://192.168.1.145:8089/%E5%AF%8C%E4%BA%BA%E5%AE%B6%E7%85%A7%E5%A3%81%20(2).jpg");
        PushDownFile(downList,
            "http://192.168.1.145:8089/%E5%AF%8C%E4%BA%BA%E5%AE%B6%E7%85%A7%E5%A3%81%20(3).jpg");
        PushDownFile(downList,"http://192.168.1.145:8089/UserLevel.json");
        PushDownFile(downList, "http://www.heao.gov.cn/a/201910/42140.shtml");

        //PushDownFile(downList, "http://static.it1352.com/Content/upload/20170317180310_925aaf68-2087-4102-9286-03f51d5df29a.jpg");
        //PushDownFile(downList, "http://static.it1352.com/Content/upload/fad370454fb441158b55171ac70f2ef3.jpg");
        //PushDownFile(downList, "http://static.it1352.com/Content/upload/4ed6d2990e07438395000000087858a3.jpg");
        //PushDownFile(downList, "http://static.it1352.com/Content/upload/1eda9f0e70134189839a7dd4b5e271ca.jpg");
        //PushDownFile(downList, "http://static.it1352.com/Content/upload/567680127c434a9099dc7f2a705695a5.jpg");
        CHttpDownMng mng = new CHttpDownMng();
        mng.StartDown(downList, 2, 100 * 1024, CTargetPlat.PersistentRootPath);
        m_downMng = mng;
    }

    void PushDownFile(List<DownResInfo> downList, string url)
    {
        DownResInfo node = new DownResInfo();
        node.url = url;
        CHttpDown.GetDownFileSize(url, out node.nFileSize);
        downList.Add(node);
    }

    public void OpenExplorer()
    {
        string szPath = CTargetPlat.PersistentRootPath;
        szPath = szPath.Replace('/', '\\');
        System.Diagnostics.Process.Start("explorer.exe", szPath);
    }
}