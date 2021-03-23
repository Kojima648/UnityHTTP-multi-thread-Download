using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

///////////////////////////////////////////////////////////
//
//  ------------------------------------------------------
//  功能描述                ：Http 下载的接口类
//
///////////////////////////////////////////////////////////

public class CHttpDown
{
    public delegate void LPOnReceiveDownFile(int nRecvSize);

    // 功能：获取下载文件的大小
    // 参数：url - 网络URL地址
    //       nFileSize - 输出文件的大小
    // 返回值：返回true表示得到了文件的大小，URL地址有效; 返回false表示无法得到文件的信息
    // 说明：这个接口是阻塞模式的，请不要在主线程调用
    static public bool  GetDownFileSize(string url, out int nOutFileSize, long nRandSeed = 0)
    {
        nOutFileSize = 0;

        CHttp http = new CHttp(nRandSeed);
        string szServerIP = string.Empty, szObj = string.Empty;
        int nPort = 80;
        http.ParseURL(url, ref szServerIP, ref szObj, ref nPort);

        string szAnswer = string.Empty;
        if (!http.QueryFile(ref szAnswer, szServerIP, nPort, szObj, 0, 1, false))
        {
            http.Close();
            return false;
        }
        int nDownSize = 0, nFileSize = 0;
        if (!http.AnlyseFileLengthFromAnswer(szAnswer, ref nDownSize, ref nFileSize))
        {
            http.Close();
            return false;
        }
        http.Close();
        nOutFileSize = nFileSize;
        return true;
    }

    // 功能：下载文件，并将下载的文件保存到本地
    // 参数：url - 网络URL的地址
    //       szLocalPathName - 本地保存地址
    // 说明：这个接口是阻塞模式的，请不要在主线程调用
    static public bool DownFile(string url, string szLocalPathName, int nNeedDownOffset, int nNeedDownSize, long nRandSeed = 0, LPOnReceiveDownFile lpReceiveFunc = null)
    {
        CHttp http = new CHttp(nRandSeed);
        string  szServerIP = string.Empty, szObj = string.Empty;
        int   nPort = 80;
        http.ParseURL(url, ref szServerIP, ref szObj, ref nPort);

        string  szAnswer = string.Empty;
        if (!http.QueryFile(ref szAnswer, szServerIP, nPort, szObj, nNeedDownOffset, nNeedDownSize, nNeedDownSize <= 0))
        {
            http.Close();
            return false;
        }
        int nDownSize = 0, nFileSize = 0;
        if (!http.AnlyseFileLengthFromAnswer(szAnswer, ref nDownSize, ref nFileSize))
        {
            http.Close();
            return false;
        }
        if( nNeedDownSize <= 0 )
            nDownSize = nFileSize;  // 这是全部下载

        // 打开本地文件
        if (File.Exists(szLocalPathName))
            File.Delete(szLocalPathName);
        FileStream pFile = new FileStream(szLocalPathName, FileMode.CreateNew, FileAccess.Write);

        // 开始下载吧
        byte[] szTempBuf = null;

        int    nRecTotal = 0;
        int    nRecLen = 0;
        int    nOffset = 0;
        while (nRecTotal < nDownSize)
        {
            nRecLen = http.FastReceiveMax(ref szTempBuf, ref nOffset);
            // 写入文件吧
            if (nRecLen > 0)
            {
                if (lpReceiveFunc != null)
                    lpReceiveFunc(nRecLen);
                nRecTotal += nRecLen;
                pFile.Write(szTempBuf, nOffset, nRecLen);
            }
            else
            {
                break;
            }
        }
        http.Close();
        pFile.Close();
        return nRecTotal == nDownSize;
    }
    // 功能：下载整个文件
    // 说明：这个接口是阻塞模式的，请不要在主线程调用
    static public bool DownFile(string url, out byte[] szFileData, int nDownOffset = 0, int nNeedDownSize = 0, long nRandSeed = 0, LPOnReceiveDownFile lpReceiveFunc = null)
    {
        szFileData = null;
        CHttp http = new CHttp(nRandSeed);
        string szServerIP = string.Empty, szObj = string.Empty;
        int nPort = 80;
        http.ParseURL(url, ref szServerIP, ref szObj, ref nPort);

        string szAnswer = string.Empty;
        if (!http.QueryFile(ref szAnswer, szServerIP, nPort, szObj, nDownOffset, nNeedDownSize, nNeedDownSize <= 0))
        {
            http.Close();
            return false;
        }
        int nDownSize = 0, nFileSize = 0;
        if (!http.AnlyseFileLengthFromAnswer(szAnswer, ref nDownSize, ref nFileSize))
        {
            http.Close();
            return false;
        }
        nDownSize = nFileSize;
        byte[] szTempBuf = null;

        szFileData = new byte[nFileSize];

        int nRecTotal = 0;
        int nRecLen = 0;
        int nOffset = 0;
        while (nRecTotal < nDownSize)
        {
            nRecLen = http.FastReceiveMax(ref szTempBuf, ref nOffset);
            // 写入文件吧
            if (nRecLen > 0)
            {
                if( lpReceiveFunc != null )
                    lpReceiveFunc(nRecLen);
                Array.Copy(szTempBuf, nOffset, szFileData, nRecTotal, nRecLen);
                nRecTotal += nRecLen;
            }
            else
            {
                break;
            }
        }
        http.Close();
        return nRecTotal == nDownSize;
    }
}
