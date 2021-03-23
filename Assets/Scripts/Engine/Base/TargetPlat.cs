using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 目前只定义几个主要的平台，更多的平台请查看帮助，关键字：Platform Dependent Compilation
public enum PALT_TYPE
{
    PLAT_UNKNOW,
    PLAT_WINDOWS,
    PLAT_IOS,         // UNITY_STANDALONE_OSX
    PLAT_IPHONE,
    PLAT_ANDORID,
    PLAT_WP8,
};

public class CTargetPlat
{
    public static PALT_TYPE GetPlatType()
    {
#if UNITY_IPHONE
    return PALT_TYPE.PLAT_IPHONE;
#elif UNITY_ANDROID
    return PALT_TYPE.PLAT_ANDORID;
#elif UNITY_WP8
    return PALT_TYPE.PLAT_WP8;
#elif UNITY_STANDALONE_WIN
        return PALT_TYPE.PLAT_WINDOWS;
#else
    return PALT_TYPE.PLAT_UNKNOW;
#endif
    }
    static public string GetTargetPlatName()
    {
        PALT_TYPE ePlatType = GetPlatType();
        switch (ePlatType)
        {
            case PALT_TYPE.PLAT_IOS:
            case PALT_TYPE.PLAT_IPHONE:
                return "Ios";
            case PALT_TYPE.PLAT_ANDORID:
                return "Android";
            case PALT_TYPE.PLAT_WP8:
                return "wp8";
            default:
                break;
        }
        return "Windows";
    }
    public static string GetStreamAssetsURL(string szFileName)
    {
#if   UNITY_STANDALONE || UNITY_EDITOR
        string url = "file:///" + Application.streamingAssetsPath + "/" + GetTargetPlatName() + '/' + szFileName;
        return url;
#elif   UNITY_ANDROID
        string url = Application.streamingAssetsPath + "/" + GetTargetPlatName() + '/' + szFileName;
        return url;
#elif   UNITY_IPHONE
        string url = "file://" + Application.streamingAssetsPath + "/" + GetTargetPlatName() + '/' + szFileName;
        return url;
#else
        string url = "file:///" + Application.streamingAssetsPath + "/" + GetTargetPlatName() + '/' + szFileName;
        return url;
#endif
    }
    public static string GetAssetPathName(string szFileName)
    {
#if   UNITY_STANDALONE || UNITY_EDITOR
        string url = Application.streamingAssetsPath + "/" + GetTargetPlatName() + '/' + szFileName;
        return url;
#elif   UNITY_ANDROID
        string url = Application.streamingAssetsPath + "/" + GetTargetPlatName() + '/' + szFileName;
        return url;
#elif   UNITY_IPHONE
        string url = Application.streamingAssetsPath + "/" + GetTargetPlatName() + '/' + szFileName;
        return url;
#else
        string url = Application.streamingAssetsPath + "/" + GetTargetPlatName() + '/' + szFileName;
        return url;
#endif
    }

    private static string m_persistent_root_path = string.Empty;
    /// <summary>
    /// persistent地址
    /// </summary>
    public static string PersistentRootPath
    {
        get
        {
            if (m_persistent_root_path == String.Empty)
            {
                string szDataPath = Application.persistentDataPath;
#if UNITY_STANDALONE || UNITY_EDITOR
                szDataPath = Application.dataPath;
                szDataPath = szDataPath.Substring(0, szDataPath.Length - 6) + "tmp_persistent";
#endif

#if UNITY_IPHONE
                m_persistent_root_path = string.Format( "{0}/Ios", szDataPath );
#elif UNITY_ANDROID
                m_persistent_root_path = string.Format( "{0}/Android", szDataPath );
#else
                m_persistent_root_path = szDataPath + "/Windows";
#endif
                if (!System.IO.Directory.Exists(m_persistent_root_path))
                    System.IO.Directory.CreateDirectory(m_persistent_root_path);
            }
            return m_persistent_root_path;
        }
    }
}
