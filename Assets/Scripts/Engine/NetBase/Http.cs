using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

///////////////////////////////////////////////////////////
//
//  ------------------------------------------------------
//  功能描述                ：Http下载的封装类，支持http, 支持url跳转
//
///////////////////////////////////////////////////////////

public enum HttpErrorCode
{
    HttpError_ConnectFailed      = -200,  // 不能连接上服务器
    HttpError_no_answer_http     = -300,  // 接收回应包失败
    HttpError_no_answer_file_len = -400,  // 没有返回长度信息
    HttpError_receive_break      = -500,  // 接收中断
    // 剩下就是HTTP本身的返回码了
};

public class CHttp
{
    const int HTTP_CACHE_SIZE = 1024 * 200; //

    string m_szServerAddr;
    int    m_nServerPort;

    CTcp m_tcp = new CTcp();
    byte[] m_PrepareReadBuf = new byte[HTTP_CACHE_SIZE];
    int m_nPrepareReadSize = 0;
    int m_nReadPos = 0;

    int m_nLastErrorCode = 0; // 最后的错误码

    public CHttp()
    {
        m_szServerAddr = string.Empty;
        m_nServerPort = 80;
    }
    public CHttp(long nRandSeed)
    {
        m_tcp.SetRandSeed(nRandSeed);
    }
    ~CHttp()
    {
        m_tcp.Close();
        m_tcp = null;
        m_PrepareReadBuf = null;
    }
    public void Close()
    {
        m_tcp.Close();
        m_nPrepareReadSize = 0;
        m_nReadPos = 0;
    }
    public bool IsConnect()
    {
        return m_tcp.IsConnect();
    }
    public bool IsOpen()
    {
        return m_tcp.IsConnect();
    }
    // 功能：得到最后的错误码
    public int GetLastHttpErrorCode()
    {
        return m_nLastErrorCode;
    }
    public int SendToServer(string szMsg)
    {
        return m_tcp.SendToServer(szMsg);
    }
    // 功能：读取一个字符，优化的函数
    // 参数：chBuf - 输出字符地址
    //       nPrepareSize - 预读取的长度, 小于等于零表示按最大缓冲读取
    public int ReceiveChar(byte []chBuf, int nPrepareSize = -1)
    {
        if (m_nReadPos < m_nPrepareReadSize )
        {
            chBuf[0] = m_PrepareReadBuf[m_nReadPos];
            ++m_nReadPos;
            return 1;
        }
        if (nPrepareSize <= 0)
            nPrepareSize = HTTP_CACHE_SIZE;
        else if (nPrepareSize > HTTP_CACHE_SIZE)
            nPrepareSize = HTTP_CACHE_SIZE;
        
        int nRecLen = m_tcp.Receive(m_PrepareReadBuf, nPrepareSize);
        if (nRecLen <= 0)
        {
            return nRecLen;
        }
        m_nPrepareReadSize = nRecLen;
        m_nReadPos = 1;
        chBuf[0] = m_PrepareReadBuf[0];
        return 1;
    }
    // 功能：快速读取缓冲数据, 优化的函数
    // 参数：nReadSize - 要读取的数据长度
    //       buffer - 输出内部缓冲指针地址, 外部不要修改这个地址噢
    //       nOffset - 有效数据偏移量
    //       nMaxRecvSize - 最大接收大小
    // 返回值：返回读取的长度, -1或0表示网络断开
    // 备注：直接返回预读取的缓冲数据，不拷贝，用于优化性能
    public int FastReceiveMax( ref byte[] buffer, ref int nOffset, int nMaxRecvSize = 0)
    {
        // 如果原来有数据，就使用原有数据
        if (m_nReadPos < m_nPrepareReadSize)
        {
            buffer  = m_PrepareReadBuf;
            nOffset = m_nReadPos;
            int nReadSize = m_nPrepareReadSize - m_nReadPos;
            if (nMaxRecvSize > 0 && nReadSize > nMaxRecvSize)
                nReadSize = nMaxRecvSize;
            m_nReadPos += nReadSize;
            return nReadSize;
        }
        int nRecLen = m_tcp.Receive(m_PrepareReadBuf, nMaxRecvSize > 0 && nMaxRecvSize < HTTP_CACHE_SIZE ? nMaxRecvSize : HTTP_CACHE_SIZE);
        if (nRecLen <= 0)
            return 0;

        buffer  = m_PrepareReadBuf;
        nOffset = 0;
        return nRecLen;
    }
    // 功能：接收指定长度的BUF数据
    // 备注：这个函数作了优化，目的是减少低层socket::receive的次数
    public int Receive(byte[] buffer, int nSize, SocketFlags flags = SocketFlags.None)
    {
        if (m_nReadPos + nSize <= m_nPrepareReadSize)
        {
            Array.Copy(m_PrepareReadBuf, m_nReadPos, buffer, 0, nSize);
            m_nReadPos += nSize;
            return nSize;
        }
        int nReadSize = 0;
        int nCurSize = 0;
        while (nReadSize < nSize)
        {
            if (m_nReadPos >= m_nPrepareReadSize)
            {
                m_nReadPos = 0;
                m_nPrepareReadSize = m_tcp.Receive(m_PrepareReadBuf, HTTP_CACHE_SIZE);
                if (m_nPrepareReadSize <= 0)
                {
                    m_nPrepareReadSize = 0;
                    break;
                }
            }
            nCurSize = nSize - nReadSize;
            if (nCurSize > m_nPrepareReadSize - m_nReadPos)
                nCurSize = m_nPrepareReadSize - m_nReadPos;
            if( nCurSize > 0 )
                Array.Copy(m_PrepareReadBuf, m_nReadPos, buffer, nReadSize, nCurSize);
            m_nReadPos += nCurSize;
            nReadSize += nCurSize;
        }
        return nReadSize;
    }
    
    public bool ClientOpen(string pcsServerIP, int nServerPort)
    {
        m_szServerAddr = pcsServerIP;
        m_nServerPort = nServerPort;
        m_nPrepareReadSize = 0;
        m_nReadPos = 0;
        m_tcp.ConnectToServer(m_szServerAddr, m_nServerPort);
        return IsConnect();
    }
    
    //////////////////////////////////////////////////////
    // 功能：申请一个文件下载
    // 参数：url - 下载地址
    //       nFileOffset - 文件偏移
    //       nDownSize - 下载字节数
    //       bDownAll - 是否下载全部（不是定点下载）
    // 返回：
    // 说明：
    public bool QueryURL(ref string szAnswer, string url, int nFileOffset, int nDownSize, bool bDownAll)
    {
        string szServerIP = string.Empty, szObj = string.Empty;
        int nPort = 80;
        ParseURL(url, ref szServerIP, ref szObj, ref nPort);

        if (!QueryFile(ref szAnswer, szServerIP, nPort, szObj, nFileOffset, nDownSize, bDownAll))
        {
            Close();
            return false;
        }
        return true;
    }

    //////////////////////////////////////////////////////
    // 功能：预备下载一个文件
    // 参数：url - 下载地址
    //       nFileOffset - 文件偏移
    //       nDownSize - 下载字节数
    //       bDownAll - 是否下载全部（不是定点下载）
    // 返回：返回当前要下载的字节数量, -1表示下载失败。
    // 说明：
    public int PrepareDown(string url, int nFileOffset, int nDownSize, bool bDownAll)
    {
        string szAnswer = string.Empty;
        string szServerIP = string.Empty, szObj = string.Empty;
        int nPort = 80;
        ParseURL(url, ref szServerIP, ref szObj, ref nPort);

        if (!QueryFile(ref szAnswer, szServerIP, nPort, szObj, nFileOffset, nDownSize, bDownAll))
        {
            Close();
            return -1;
        }
        int nCurDownSize = 0, nCurFileSize = 0;
        if (!AnlyseFileLengthFromAnswer(szAnswer, ref nCurDownSize, ref nCurFileSize))
        {
            Close();
            return -1;
        }
        if (nDownSize <= 0)
            nDownSize = nCurDownSize;
        else if (nDownSize > nCurDownSize)
            nDownSize = nCurDownSize;
        return nDownSize;
    }

    //////////////////////////////////////////////////////
    // 功能：申请一个文件下载
    // 参数：szServerAddr - 服务器地址
    //       nServerPort - 服务器端口号
    //       szSubPathName - 相对路径名
    //       nFileOffset - 文件偏移
    //       nDownSize - 下载字节数
    //       bDownAll - 是否下载全部（不是定点下载）
    // 返回：
    // 说明：
    public bool QueryFile(ref string szAnswer, string szServerAddr, int nServerPort, string szSubPathName, int nFileOffset, int nDownSize, bool bDownAll)
    {
        //if (!IsOpen()
        //    || m_szServerAddr != szServerAddr
        //    || m_nServerPort != nServerPort
        //    )
        {
            Close();
            if (!ClientOpen(szServerAddr, nServerPort))
            {
                m_nLastErrorCode = (int)HttpErrorCode.HttpError_ConnectFailed;
                return false;
            }
        }
        // 第一步, 格式化消息头
        string szMsg = FormatRequestHeader(szSubPathName, szServerAddr, nFileOffset, nDownSize, bDownAll, true);

        // 第二步, 发送到服务器    
        int nSendSize = SendToServer(szMsg);

        // 第三步, 接收回应包
        int dwError = 0;
        if (!ReceiveAnswer(ref szAnswer, ref dwError))
        {
            m_nLastErrorCode = (int)HttpErrorCode.HttpError_no_answer_http;
            // 无法接收
            //string szError;
            //szError.Format("接收回应包失败: %s:%d(ErrorID = %d, nSendSize = %d) 发送包 ：%s, 应答包：%s", szServerAddr, nServerPort, dwError, nSendSize, szMsg, szOutAnswer);
            //lpReportErrorFunc(szError, pMngParam);
            Close();
            return false;
        }
                
        // 分析返回码
        int nCode = GetErroIDFormAnswerMsg(szAnswer);
        m_nLastErrorCode = nCode;
        if (nCode >= 400 && nCode < 500)
        {
            // 出错了
            //ASSERT(0);
            //string szError;
            //szError.Format("服务器错误: %s:%d(ErrorID = %d) 发送包 ：%s, 应答包：%s", szServerAddr, nServerPort, dwError, szMsg, szAnswer);
            //lpReportErrorFunc(szError, pMngParam);
            Close();
            return false;
        }
        if (nCode >= 500 && nCode < 600)
        {
            // 出错了
            //ASSERT(0);
            //DWORD dwError = WSAGetLastError();
            //string szError;
            //szError.Format("服务器错误: %s:%d(ErrorID = %d) 发送包 ：%s, 应答包：%s", szServerAddr, nServerPort, dwError, szMsg, szAnswer);
            //lpReportErrorFunc(szError, pMngParam);
            Close();
            return false;
        }

        // 重定向啊
        if (nCode == 302)
        {
            string szNewServerAddr = string.Empty, szNewURL = string.Empty;
            if (ReContentServer302Error(szAnswer, ref szNewServerAddr, ref szNewURL))
            {
                szMsg = FormatRequestHeader(szNewURL, szNewServerAddr, nFileOffset, nDownSize, bDownAll, true);
                SendToServer(szMsg);
                if (!ReceiveAnswer(ref szAnswer, ref dwError))
                {
                    // 无法接收
                    //DWORD dwError = WSAGetLastError();
                    //string szError;
                    //szError.Format("重定向失败: %s:%d(ErrorID = %d) 发送包 ：%s, 应答包：%s", szNewServerAddr, nServerPort, dwError, szMsg, szAnswer);
                    //lpReportErrorFunc(szError, pMngParam);
                    Close();
                    return false;
                }
            }
        }

        //if( nCode != 200 )
        if (!(nCode == 200 || nCode == 206))
        {
            //ASSERT(0);
            //DWORD dwError = WSAGetLastError();
            //string szError;
            //szError.Format("返回值不是200或206: %s:%d(ErrorID = %d) 发送包 ：%s, 应答包：%s", m_szServerAddr, m_nServerPort, dwError, szMsg, szAnswer);
            //lpReportErrorFunc(szError, pMngParam);
        }
        return true;
    }
    
    //////////////////////////////////////////////////////
    // 功能：分析URL
    // 参数：szServerURL - 服务器URL
    //       szPathFileURL - 需要下载的文件的相对URL
    //       szServerIP - 输出服务器的IP
    //       szObj - 文件相对目录
    //       nPort - 连接端口号（默认是80）
    // 返回：
    // 说明：
	public  void          ParseURL( string szServerURL, ref string szServerIP, ref string szObj, ref int nPort )
    {
        string  szURL = szServerURL;

        if( szURL.IndexOf("http://", 0) == -1 )
	    {
            szURL = "http://" + szURL;
	    }
        szServerIP = GetMiddle(szURL, "http://", "/");
        if (string.IsNullOrEmpty(szServerIP))
            szServerIP = szURL.Substring(7);

	    int  nIndex  = szURL.IndexOf( '/', 6 + szServerIP.Length );
	    if( nIndex > 7 )
            szObj = szURL.Substring(nIndex);  // 必须有'/'
	    else
		    szObj = "/index.htm";  // 主页    

        nIndex = szServerIP.IndexOf( ':', 0 );
	    if( nIndex != -1 )
	    {
            nPort = 80;
            int.TryParse(szServerIP.Substring(nIndex + 1), out nPort);
            szServerIP = szServerIP.Substring(0, nIndex);
	    }
	    else
		    nPort = 80;
    }

    //////////////////////////////////////////////////////
    // 功能：格式化一个http下载消息包的头头
    // 参数：szPathFileURL - 需要下载的文件的相对URL
    //       szServerIP - 服务器的IP
    //       nFileOffset - 文件偏移
    //       nDownSize - 下载字节数
    //       bDownAll - 是否下载全部（不是定点下载）
    //       bKeekAlive - 下载完成后是否保留链接
    // 返回：
    // 说明：
    string FormatRequestHeader(string szPathFileURL, string szServerIP, int nFileOffset, int nDownSize, bool bDownAll, bool bKeekAlive)
    {
        StringBuilder szBuilder = new StringBuilder(1024);
        ///第1行:方法,请求的路径,版本
        szBuilder.AppendFormat( "GET {0} HTTP/1.1\r\n", szPathFileURL);
        ///第2行:主机
        szBuilder.AppendFormat("Host:{0}\r\n", szServerIP);
        ///第3行:

        ///第4行:接收的数据类型
        szBuilder.Append("Accept:*/*\r\n");
        ///第5行:浏览器类型
        szBuilder.Append("User-Agent:Mozilla/4.0 (compatible; MSIE 6.00; Android)\r\n");

        ///第6行:连接设置,保持
        if (bKeekAlive)
            szBuilder.Append("Connection:Keep-Alive\r\n");
        else
            szBuilder.Append("Connection:close\r\n"); // close 表示暂时的

        ///第7行:Cookie.

        ///第8行:请求的数据起始字节位置(断点续传的关键)
        if (!bDownAll)
        {
            szBuilder.AppendFormat("Range: bytes={0}-{1}\r\n", nFileOffset, nFileOffset + nDownSize - 1);
        }

        ///最后一行:空行
        szBuilder.Append("\r\n");

        ///返回结果
        return szBuilder.ToString();
    }

    static string GetMiddle(string szText, string szLeft, string szRight)
    {
        int nLeft = szText.IndexOf(szLeft, 0);
        if (nLeft == -1)
            return string.Empty;
        nLeft += szLeft.Length;
        int nRight = szText.IndexOf(szRight, nLeft);
        return szText.Substring(nLeft, nRight - nLeft);
    }
    static string DelOut(string szText, char chLow, char chHig)
    {
        char[] chBuf = new char[szText.Length + 1];
        int nLen = szText.Length;
        int nCount = 0;
        for (int i = 0; i < nLen; ++i)
        {
            char ch = szText[i];
            if (ch >= chLow && ch <= chHig)
            {
                chBuf[nCount++] = ch;
            }
        }
        chBuf[nCount] = '\0';
        return new string(chBuf, 0, nCount);
    }
    static int GetNumb(string szText, string szLeft, string szRight)
    {
        int nLeft = szText.IndexOf(szLeft, 0);
        if (nLeft == -1)
            return 0;
        nLeft += szLeft.Length;
        int nRight = szText.IndexOf(szRight, nLeft);
        if (nRight == -1)
            nRight = szText.Length;
        return GetNumb(szText, nLeft, nRight);
    }
    static int  GetNumb(string szText, int nStart, int nEnd)
    {
        int nNumb = 0;
        bool bFind = false;
        for (int i = nStart; i < nEnd; ++i)
        {
            char ch = szText[i];
            if (ch >= '0' && ch <= '9')
            {
                bFind = true;
                nNumb *= 10;
                nNumb += ch - '0';
            }
            else if (bFind)
            {
                break;
            }
        }
        return nNumb;
    }

    //////////////////////////////////////////////////////
    // 功能：从回应包中分析当前下载的流量及文件大小
    // 参数：szAnswer - 回应包
    //       nDownSize - 下载的大小
    //       nFileSize - 文件大小
    // 返回：
    // 说明：
    public bool AnlyseFileLengthFromAnswer(string szAnswer, ref int nDownSize, ref int nFileSize)
    {
        nDownSize = GetNumb(szAnswer, "Content-Length", "\r\n");
        string szRange = GetMiddle(szAnswer, "Content-Range", "\r\n");
        if(string.IsNullOrEmpty(szRange))
        {
            nFileSize = nDownSize;
        }
        else
        {
            // Content-Range: bytes 0-0/6402
            int nIndex = szRange.LastIndexOf('/');
            if(nIndex != -1)
            {
                szRange = szRange.Substring(nIndex + 1);
                nFileSize = GetNumb(szRange, 0, szRange.Length);
            }
            else
            {
                nFileSize = nDownSize;
            }
        }
        return true;
    }

    //////////////////////////////////////////////////////
    // 功能：接收回应包
    // 参数：szAnswer - 回应包
    //       dwErro - 错误号
    // 返回：
    // 说明：
    public bool ReceiveAnswer(ref string szAnswer, ref int dwErro)
    {
        // 接收回应包
        bool bRet = false;
        byte []chBuf = new byte[1];
        char[] szTempReceive = new char[1025];
        int nTotalRecLen = 0;
        int nRecLen = 0, nIndex = 0;
        while (nTotalRecLen < 1024)
        {
            nRecLen = ReceiveChar(chBuf);//Receive(chBuf, 1);
            if (nRecLen == -1)
            {
                dwErro = -5; // WSAGetLastError();
                break;
            }
            if (nRecLen == 0) // 接收失败
            {
                dwErro = -4;// WSAGetLastError();
                break;
            }
            szTempReceive[nTotalRecLen++] = (char)chBuf[0];
            if (nTotalRecLen >= 4)
            {
                // 如果出现一个空行,就结束接收
                nIndex = nTotalRecLen - 4;
                if (szTempReceive[nIndex] == '\r' && szTempReceive[nIndex + 1] == '\n'
                    && szTempReceive[nIndex + 2] == '\r' && szTempReceive[nIndex + 3] == '\n')
                {
                    bRet = true;
                    break;
                }
            }
        }
        szTempReceive[nTotalRecLen] = '\0';
        szAnswer = new string(szTempReceive, 0, nTotalRecLen);
        return bRet;
    }

    static int DistillNumb(string szText, int nStart)
    {
        int nNumb = 0;
        int nLen = szText.Length;
        bool bFind = false;
        for (int i = nStart; i < nLen; ++i)
        {
            char ch = szText[i];
            if (ch >= '0' && ch <= '9')
            {
                bFind = true;
                nNumb *= 10;
                nNumb += ch - '0';
            }
            else if (bFind)
                break;
        }
        return nNumb;
    }

    //////////////////////////////////////////////////////
    // 功能：分析回应包的回应码
    // 参数：szAnswer - 回应包
    //       
    // 返回：
    // 说明：
    public int GetErroIDFormAnswerMsg(string szAnswer)
    {
        // 分析返回的错误号  HTTP/1.1 XXX .....
        int nErroID = DistillNumb(szAnswer, 8);
        return nErroID;
    }

    //////////////////////////////////////////////////////
    // 功能：302错误重定向
    // 参数：szAnswer - 回应包
    //       szOutObjURL - 输出真正的引用URL
    // 返回：
    // 说明：
    bool ReContentServer302Error(string szAnswer, ref string szOutServerIP, ref string szOutObjURL)
    {
	    string  szHttp      = GetMiddle(szAnswer,"Location:", "\r\n" );
	    string  szServerIP  = GetMiddle(szHttp, "http://", "/" );
        int  nNewServerPort = 80;
        int  nIndex = szServerIP.IndexOf( ':', 0 );
        if( nIndex != -1 )
        {
            nNewServerPort = DistillNumb(szServerIP, nIndex + 1);
            szServerIP = szServerIP.Substring(0, nIndex);
        }
	    Close();
	    if( !ClientOpen( szServerIP, nNewServerPort ) )
		    return false;

        szOutServerIP = szServerIP;

        string  szRefURL = GetMiddle(szAnswer,"Location:", "\r\n" );
        szRefURL = szRefURL.Trim();
        AnlseURL( ref szOutObjURL, szRefURL );
        return true;
    }
    //------------------------------------------
    // 功能：查找http消息的结束标记
    static bool FindHttpMsgEndFlags(char[] szMsg, int nLen, ref int nStart)
    {
        // 查找 \r\n\r\n
        for (int i = nStart; i < nLen; ++i)
        {
            if (szMsg[i] == '\r' && szMsg[i + 1] == '\n')
            {
                nStart = i;
                if ( i + 3 < nLen && szMsg[i + 2] == '\r' && szMsg[i + 3] == '\n')
                    return true;
            }
        }
        return false;
    }
    static bool     GetInetURL( string pcsURL, ref string szOutURL )
    {
	    // 提取服务器的网址
	    string   szURL = pcsURL;
        if(szURL.IndexOf("http://", 0) != 0 )
	    {
            szURL = "http://" + szURL;
	    }
        string szSerIP = GetMiddle(szURL, "http://", "/" );

	    string  szObj = string.Empty;
        int  nIndex = szURL.IndexOf( '/', 6 + szSerIP.Length );
	    if( nIndex != -1 )
            szObj = szURL.Substring(nIndex); // 必须有'/'
	    else
		    return  false;

        int  nServerPort = 80;
        nIndex = szSerIP.IndexOf( ':', 0 );
        if( nIndex != -1 )
        {
            nServerPort = DistillNumb(szSerIP, nIndex + 1);
            szSerIP = szSerIP.Substring(0, nIndex);
        }

	    // 连接服务器
	    CHttp   tcpURL = new CHttp();
	    if( !tcpURL.ClientOpen(szSerIP, nServerPort) )
	    {
		    return  false;  // 无法连接服务器
	    }

	    // 格式化消息头
        StringBuilder szBuilder = new StringBuilder(512);

	    ///第1行:方法,请求的路径,版本
	    szBuilder.AppendFormat( "GET {0} HTTP/1.1\r\n", szObj );
	    ///第2行:主机
	    szBuilder.AppendFormat( "Host:{0}\r\n", szSerIP );
	    ///第3行: 引用网址
	    //szMsg.AppendFormat( "Referer:%s\r\n", szObj );

	    ///第4行:接收的数据类型
        szBuilder.Append( "Accept:*/*\r\n" );
	    ///第5行:浏览器类型
	    szBuilder.Append( "User-Agent:Mozilla/6.0 (compatible; MSIE 6.00; Android)\r\n" );
	    ///第6行:连接设置,保持
	    //szMsg.AppendFormat( "Connection:Keep-Alive\r\n" );
	    szBuilder.Append( "Connection:close\r\n" );
	    szBuilder.Append( "Range: bytes=0-0\r\n");
	    szBuilder.Append( "\r\n" );
                
        string  szMsg = szBuilder.ToString();
        tcpURL.SendToServer(szMsg);
	    byte   []chBuf = new byte[1];
        char   []szTotalRecBuf = new char[1024]; // 应该不会这么长吧
        int    nTotalRecLen = 0;	    
        int  nStart = 0;
	    while( true )
	    {
            int nRecLen = tcpURL.ReceiveChar(chBuf, 1024);
		    if( nRecLen <= 0 )
		    {
			    break;
		    }
            if( nTotalRecLen + nRecLen > szTotalRecBuf.Length )
            {
                char  []szNewBuf = new char[nTotalRecLen*2];
                Array.Copy(szTotalRecBuf, 0, szNewBuf, 0, nTotalRecLen);
                szTotalRecBuf = szNewBuf;
            }
            szTotalRecBuf[nTotalRecLen++] = (char)chBuf[0];
            nTotalRecLen += 1;

            if( FindHttpMsgEndFlags(szTotalRecBuf, nTotalRecLen, ref nStart) )
                break;
	    }
	    tcpURL.Close();
    
        szTotalRecBuf[nTotalRecLen] = '\0';
        string  szAnswer = new string(szTotalRecBuf, 0, nTotalRecLen);
	    // 分析
        szOutURL = GetMiddle(szAnswer, "Location:", "\r\n");
        szOutURL = szOutURL.Trim();
        tcpURL.Close();
        return szOutURL.Length > 0;
    }
    
    static bool    IsRefURL( string  szURL )
    {
	    bool  bRef = false;
	    for( int i=0, nLen = szURL.Length; i<nLen; i++ )
	    {
		    if( szURL[i] == '%'
			    || szURL[i] == '@'
			    || szURL[i] == '*'
			    || szURL[i] == '='
			    || szURL[i] == ':'
			    || szURL[i] == '#' )
		    {
			    bRef = true;
			    break;
		    }
	    }
	    return  bRef;
    }

    static bool    AnlseURL( string szURL, ref string szPath, ref string szFileName )
    {
        int nIndex = szURL.LastIndexOf('/');
	    if( nIndex != -1 )
	    {
            szPath     = szURL.Substring(0, nIndex);
            szFileName = szURL.Substring(nIndex + 1);
	    }
	    else
	    {
            szPath = string.Empty;
            szFileName = string.Empty;
	    }
	    return  !string.IsNullOrEmpty(szFileName);
    }
    
    static bool    AnlseURL( ref string szURL, ref string szPath, ref string szFileName, ref string szExt )
    {
        szPath = string.Empty;
        szFileName = string.Empty;
	    szExt = "*.*";
	    string  szScrPath = szURL;
	    // 优先查找ftp吧
	    int  nIndex = szURL.IndexOf( "ftp://", 0 );
	    if( nIndex != -1 )
	    {
		    nIndex = szURL.IndexOf( "ftp://", 0 );
            szScrPath = szURL.Substring( nIndex + 6 );
	    }
	    else 
	    {
		    nIndex = szURL.IndexOf( "http://", 0 );
		    if( nIndex != -1 )
		    {
                szScrPath = szURL.Substring( nIndex + 7 );
			    if( szScrPath.IndexOf( "ftp.", 0 ) == 0 )
			    {
				    szFileName = szScrPath;
			    }
			    else
			    {
				    szFileName = "index.htm";
				    szExt      = "*.htm";
			    }
		    }
	    }
	    nIndex = szScrPath.LastIndexOf( '/' );
	    if( nIndex != -1 )
	    {
            szPath = szScrPath.Substring( 0, nIndex );
		    szFileName = szScrPath.Substring( nIndex + 1 );

		    int  nX = szScrPath.LastIndexOf( '.' );
		    if( nX != -1 && nX > nIndex )
		    {
                szExt = szScrPath.Substring( nX + 1 );
			    nX = szFileName.LastIndexOf( '.' );
		    }
	    }

	    // 如果文件名是一个引用的话, 包括非字母(如%,*,:)
	    if( IsRefURL(szFileName) )
	    {
		    if( GetInetURL(szScrPath, ref szURL) )
		    {
			    return   AnlseURL(ref szURL, ref szPath, ref szFileName, ref szExt);  // 使用一个递归
		    }
	    }
	    return  !string.IsNullOrEmpty(szFileName);
    }

    static bool    AnlseURL( ref string  szRealURL, string szOldURL )
    {
        szRealURL = szOldURL;
        string szPath = string.Empty, szFileName = string.Empty, szExt = string.Empty;
        return  AnlseURL( ref szRealURL, ref szPath, ref szFileName, ref szExt );
    }
}
