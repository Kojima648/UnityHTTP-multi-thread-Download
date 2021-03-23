using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

///////////////////////////////////////////////////////////
//
//  ------------------------------------------------------
//  功能描述                ：tcp socket类的封装，支持同步与异步模式
//
///////////////////////////////////////////////////////////

public class CTcp
{
    protected Socket m_hSocket;// socket连接
    long m_nRandSeed = 0;
    
    public CTcp()
    {
        m_nRandSeed = 0;
    }
    ~CTcp()
    {
        Close();
    }
    public void SetRandSeed(long nRandSeed)
    {
        m_nRandSeed = nRandSeed;
    }
    static public int RandInt(ref long nRandSeed, int nMin, int nMax)
    {
        if (nRandSeed == 0 || nMin == nMax)
            return nMin;
        nRandSeed = nRandSeed * 214013L + 2531011L;
        if (nMin > nMax)
        {
            int nTemp = nMin; nMin = nMax; nMax = nTemp;
        }
        long nSize = (nMax - nMin + 1);
        long nIndex = nRandSeed % nSize + nMin;
        if (nIndex < nMin)
            nIndex = nMin;
        if (nIndex > nMax)
            nIndex = nMax;
        return (int)nIndex;
    }
    // 功能：开始测试IPV6，在启动时执行的噢
    static int s_nIPV6State = 0; // 0 是没有测试，1是测试中，2是支持IPV6，3只支持IPV4
    static bool s_bStartTest = false;
    static public bool  IsIPV6Net()
    {
        if(s_nIPV6State == 1)
        {
            long nStartTime = System.DateTime.Now.Ticks;
            while (s_nIPV6State == 1)
            {
                System.Threading.Thread.Sleep(100);
                long nWaitTime = (System.DateTime.Now.Ticks - nStartTime) / 10000;
                if (nWaitTime > 20)
                    break;
            }
        }
        return s_nIPV6State == 2;
    }
    public static void  StartTestIPV6(string szCDNUrl)
    {
        if (s_nIPV6State != 0)
            return;
        s_nIPV6State = 1;
        TestIPV6Logic(szCDNUrl);
    }
    public static int s_ExcNum;//解析IP抛出异常次数
    static void TestIPV6Logic(string szCDNUrl)
    {
        int nPort = 80;
        try 
        {
            IPAddress ipa;
            if (IPAddress.TryParse(szCDNUrl, out ipa))
            {
                s_nIPV6State = 3;
                return;
            }
            else
            {
                s_ExcNum = 0;
                IPHostEntry iph = Dns.GetHostEntry(szCDNUrl);
                bool bFindIPV6 = false;
                for (int i = 0; i < iph.AddressList.Length; ++i)
                {
                    IPAddress pIPAddr = iph.AddressList[i];
                    if (pIPAddr.AddressFamily != AddressFamily.InterNetworkV6)
                        continue;
                    IPAddrTest node = new IPAddrTest();
                    node.m_pIPAddr = pIPAddr;
                    node.m_nPort = nPort;
                    node.m_bIsIPV6 = pIPAddr.AddressFamily == AddressFamily.InterNetworkV6;
                    if(node.m_bIsIPV6)
                    {
                        bFindIPV6 = true;
                    }
                    node.TestConnect();
                }
                if(!bFindIPV6)
                {
                    s_nIPV6State = 3;
                }
            }
        }
        catch(System.Exception ex)
        {
            UnityEngine.Debug.LogError(ex.ToString());
            s_ExcNum++;
        }
       
        
    }
    static public bool IsIPV6Addr(string szAddr)
    {
        if (string.IsNullOrEmpty(szAddr))
            return false;
        return szAddr.IndexOf(':') != -1;
    }
    class IPAddrTest
    {
        public IPAddress m_pIPAddr;
        public int m_nPort;
        public bool m_bIsIPV6;
        public void TestConnect()
        {
            System.Threading.Thread t = new System.Threading.Thread(Connect);
            t.Start(this);
        }
        void  Connect()
        {
            Socket hSocket = null;
            bool bSuc = false;
            try
            {
                hSocket = new Socket(m_pIPAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                hSocket.Connect(m_pIPAddr, m_nPort);
                if(hSocket.Connected)
                {
                    bSuc = true;
                }
            }
            catch(Exception e)
            {
                bSuc = false;
                hSocket = null;
            }
            if(bSuc)
            {
                // IPV4与IPV6竞赛连接，谁先连接上，就按谁的连，不过测试发现其实在IPV6的网络下，其实两个都是可以连接上的，所以这个结果可能是随机的, 但能使用IPV4连接会更好
                if(s_nIPV6State == 1)
                {
                    if (m_bIsIPV6)
                        s_nIPV6State = 2; // 这是支持IPV6的
                    else
                        s_nIPV6State = 3; // 这个只支持IPV4
                }
                else if(!m_bIsIPV6)
                {
                    s_nIPV6State = 3; // 如果IPV4能时，强制IPV4
                }
                if(hSocket != null)
                {
                    hSocket.Shutdown(SocketShutdown.Both);
                    hSocket.Close();
                    hSocket = null;
                }
            }
        }
    }

    // 功能：连接服务器
    // 参数：szServerAddr - 服务器的地址
    //       nPort  - 端口号
    public void  ConnectToServer(string  szServerAddr, int nPort, long nRandSeed = 0)
    {
        try
        {
            if (nRandSeed != 0)
                m_nRandSeed = nRandSeed;
            Connect(szServerAddr, nPort);
        }
        catch (Exception e)
        {
            push_debug_info(e.ToString() + ":" + szServerAddr);
            m_hSocket = null;
        }
    }

    void push_debug_info(string szError)
    {
        try
        {
            if (s_push_debug_info != null)
            {
                s_push_debug_info(szError);
            }
        }
        catch(Exception e)
        {

        }
    }

    // 功能：将一个域名转换成IP
    public long GetServerIP(string szServerAddr)
    {
        try
        {
            IPAddress ipa;
            IPAddress.TryParse(szServerAddr, out ipa);
            return ipa.Address;
        }
        catch (Exception e)
        {
            push_debug_info(e.ToString());
        }
        return -1;
    }
    // 功能：连接指定的服务器
    public void ConnectToServer(long nIP, int nPort)
    {
        IPAddress ipa = new IPAddress(nIP);
        Connect(ipa, nPort);
    }
    public delegate IPAddress[] GetHostAddressInterface(string szIP);
    public static GetHostAddressInterface s_pGetHostAddressInterface = null;
    public static int s_DnsExceptionCount = 0;

    public delegate void push_debug_info_backback(string szInfo);
    public static push_debug_info_backback s_push_debug_info = null;

    void TryConnect(string szIP, int nPort, bool bDns)
    {
        try
        {
            if (!bDns && s_pGetHostAddressInterface == null)
                bDns = true;
            IPAddress[] ipa = bDns ? Dns.GetHostAddresses(szIP) : s_pGetHostAddressInterface(szIP);
            if (ipa != null)
            {
                bool bIsIPV6Net = IsIPV6Net();
                IPAddress pIPAddr = null;
                for (int i = 0; i<ipa.Length; ++i)
                {
                    pIPAddr = ipa[i];
                    if(pIPAddr.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        if (!bIsIPV6Net)
                            continue;
                    }
                    else
                    {
                        if (bIsIPV6Net)
                            continue;
                    }
                    m_hSocket = new Socket(pIPAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    m_hSocket.Connect(pIPAddr, nPort);
                    break;
                }
                if (m_hSocket == null)
                {
                    m_hSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    m_hSocket.Connect(szIP, nPort);
                }
            }
            else
            {
                m_hSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_hSocket.Connect(szIP, nPort);
            }
        }
        catch(Exception e)
        {
            push_debug_info(e.ToString() + ", Conect IPString, bDns:" + bDns);
            m_hSocket = null;
            if (bDns)
                ++s_DnsExceptionCount;
        }
    }

    void Connect(string szIP, int nPort)
    {
        Close(); // 先总是关闭吧

        if(s_DnsExceptionCount < 10)
            TryConnect(szIP, nPort, true);

        // 尝试在主线程调用
        if (m_hSocket == null)
        {
            TryConnect(szIP, nPort, false);
        }        
        if (m_hSocket == null)
        {
            try
            {
                m_hSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_hSocket.Connect(szIP, nPort);
            }
            catch (Exception e)
            {
                push_debug_info(e.ToString() + ", Third Conect failed");
                m_hSocket = null;
            }
        }
    }
    void Connect(IPAddress ipa, int nPort)
    {
        Close(); // 先总是关闭吧
        try
        {
            if (m_hSocket == null)
                m_hSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_hSocket.Connect(ipa, nPort);
        }
        catch (Exception e)
        {
            push_debug_info(e.ToString() + ", Conect IPAddress");
            m_hSocket = null;
        }
    }
    void Connect(IPEndPoint ipDns)
    {
        Close(); // 先总是关闭吧
        try
        {
            if (m_hSocket == null)
                m_hSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_hSocket.Connect(ipDns);
        }
        catch (Exception e)
        {
            push_debug_info(e.ToString() + ", Conect IPDns");
            m_hSocket = null;
        }
    }
    // 功能：判断连接是不是正常
    public bool IsConnect()
    {
        if (m_hSocket != null)
            return m_hSocket.Connected;
        else
            return false;
    }
    // 功能：判断连接是不是打开
    public bool IsOpen()
    {
        return IsConnect();
    }
    public void Close()
    {
        try
        {
            if (m_hSocket != null)
            {
                if (m_hSocket.Connected)
                {
                    //禁用发送和接受
                    m_hSocket.Shutdown(SocketShutdown.Both);
                    m_hSocket.Close();
                }
                //m_hSocket.Dispose();
                m_hSocket = null;
            }
        }
        catch (Exception e)
        {
            if( m_hSocket != null )
                push_debug_info(e.ToString() + ", Socket is not null.");
            else
                push_debug_info(e.ToString() + ", Socket is null.");
            m_hSocket = null;
        }
    }
    public int Receive(byte[] buffer, int size, SocketFlags flags = SocketFlags.None)
    {
        try
        {
            if (m_hSocket != null && m_hSocket.Connected)
            {
                return m_hSocket.Receive(buffer, size, flags);
            }
        }
        catch (Exception e)
        {
            push_debug_info(e.ToString() + ", Receive.");
        }
        return 0;
    }
    public int Send(byte[] buf, int size, SocketFlags flags)
    {
        try
        {
            if (m_hSocket != null && m_hSocket.Connected)
            {
                return m_hSocket.Send(buf, size, flags);
            }
        }
        catch (Exception e)
        {
            push_debug_info(e.ToString() + ", Send.");
        }
        return 0;
    }
    public int SendToServer(string szMsg)
    {
        if (string.IsNullOrEmpty(szMsg))
            return 0;
        byte[] byBuf = System.Text.Encoding.UTF8.GetBytes(szMsg);
        return Send(byBuf, byBuf.Length, SocketFlags.None);
    }
};

// 异步TCP
class CTcpAsync
{
    Socket m_hSocket;// socket连接
    long m_nRandSeed;

    public delegate IPAddress[] GetHostAddressInterface(string szIP);
    public GetHostAddressInterface m_pGetHostAddressInterface = null;
    int m_DnsExceptionCount = 0;

    protected bool m_bIsIPV6 = false;
    protected bool m_bInitIPV6 = false;
    protected string m_szConnectAddr = string.Empty;

    //---------------
    byte[] m_RecvBuffer;
    public CTcpAsync()
    {
        m_nRandSeed = 0;

        m_RecvBuffer = new byte[8192];
    }

    void TryConnect(string szIP, int nPort, bool bDns)
    {
        try
        {
            if (!bDns && m_pGetHostAddressInterface == null)
                bDns = true;
            IPAddress[] ipa = bDns ? Dns.GetHostAddresses(szIP) : m_pGetHostAddressInterface(szIP);
            if (ipa != null)
            {
                if(!m_bInitIPV6)
                {
                    m_bInitIPV6 = true;
                    m_bIsIPV6 = CTcp.IsIPV6Net();
                }
                IPAddress pIPAddr = null;
                for (int i = 0; i < ipa.Length; ++i)
                {
                    pIPAddr = ipa[i];
                    if (pIPAddr.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        if (!m_bIsIPV6)
                            continue;
                    }
                    else
                    {
                        if (m_bIsIPV6)
                            continue;
                    }
                    IPEndPoint ipDns = new IPEndPoint(pIPAddr, nPort);
                    m_hSocket = new Socket(pIPAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    m_hSocket.BeginConnect(ipDns, new AsyncCallback(ConnectCallback), m_hSocket);
                    m_szConnectAddr = pIPAddr.ToString();
                    break;
                }
                if (m_hSocket == null && ipa != null)
                {
                    int nLen = ipa.Length;
                    int nIndex = CTcp.RandInt(ref m_nRandSeed, 0, nLen - 1); // 随机一个吧
                    m_hSocket = new Socket(ipa[nIndex].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    m_hSocket.BeginConnect(ipa, nPort, new AsyncCallback(ConnectCallback), m_hSocket);
                }
            }
        }
        catch (Exception)
        {
            m_hSocket = null;
            if (bDns)
                ++m_DnsExceptionCount;
        }
    }

    void TrySafeConnect(string szIP, int nPort)
    {
        if(m_pGetHostAddressInterface != null)
        {
            TryConnect(szIP, nPort, true);
            if(m_hSocket == null)
                TryConnect(szIP, nPort, false);
        }
        else
            TryConnect(szIP, nPort, false);
    }

    // 功能：连接服务器
    // 参数：szServerAddr - 服务器的地址
    //       nPort  - 端口号
    public void  ConnectToServer(string  szServerAddr, int nPort, long nRandSeed = 0)
    {
        if( nRandSeed != 0 )
            m_nRandSeed = nRandSeed;
        bool bSucConnect = false;
        if (IsConnect())
            Quit();
        try
        {
            IPAddress ipa;
            if (IPAddress.TryParse(szServerAddr, out ipa))
            {
                IPEndPoint IP = new IPEndPoint(ipa, nPort);
                m_hSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_hSocket.BeginConnect(IP, new AsyncCallback(ConnectCallback), m_hSocket);
                bSucConnect = true;
            }
            else
            {
                TrySafeConnect(szServerAddr, nPort);
                bSucConnect = m_hSocket != null;
            }
        }
        catch (Exception e)
        {
            //DebugHelp.LogError(e.ToString());
        }
        if(!bSucConnect)
        {
            OnFailedConect();
        }
    }
    public void Quit()
    {
        try
        {
            if (m_hSocket != null)
            {
                if (m_hSocket.Connected)
                {
                    //禁用发送和接受
                    m_hSocket.Shutdown(SocketShutdown.Both);
                }
                m_hSocket.Close();
                m_hSocket = null;
            }
        }
        catch (Exception)
        {
        }
    }

    // 功能：判断连接是不是正常
    public bool IsConnect()
    {
        if (m_hSocket != null)
            return m_hSocket.Connected;
        else
            return false;
    }

    private void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            m_hSocket.EndConnect(ar);
            if (m_hSocket.Connected)
            {
                // 开始收包吧
                m_hSocket.BeginReceive(m_RecvBuffer, 0, m_RecvBuffer.Length, 0, new AsyncCallback(ReceiveCallback), m_hSocket);
                OnSucConnect();
            }
            else
            {
                OnFailedConect();
            }
        }
        catch (Exception)
        {
            Quit();
            OnFailedConect();
        }
    }
    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            int nLen = m_hSocket.EndReceive(ar);
            if (nLen > 0)
            {
                // 开始读取数据
                OnReceiveBuf(m_RecvBuffer, nLen);

                m_hSocket.BeginReceive(m_RecvBuffer, 0, m_RecvBuffer.Length, 0, new AsyncCallback(ReceiveCallback), m_hSocket);
            }
        }
        catch (Exception)
        {
            Quit();
        }
    }
    // 功能：发送消息包到服务器
    public void SendToServer(byte[] szBuf, int nBufSize)
    {
        try
        {
            byte[] bytes = new byte[nBufSize];
            Array.Copy(szBuf, 0, bytes, 0, nBufSize);
            m_hSocket.BeginSend(bytes, 0, bytes.Length, 0, new AsyncCallback(SendCallback), m_hSocket);
        }
        catch (Exception)
        {
            Quit();
        }
    }

    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            SocketError err;
            int nSendSize = m_hSocket.EndSend(ar, out err);
        }
        catch (Exception)
        {
            Quit();
        }
    }
    // 功能：通知收到消息包的事件
    // 注意：这个接口是异步调用的，需要上层自己处理加锁事件
    protected virtual int OnReceiveBuf(byte[] recBuf, int nSize)
    {
        // 在这里做解包的事件, 这个可能是多个包，也可能是不到一个包
        return nSize;
    }
    protected virtual void OnSucConnect()
    {

    }
    protected virtual void OnFailedConect()
    {

    }
}

class CTcpThread
{
    CTcp m_tcp;    
    long m_nRandSeed;

    public delegate IPAddress[] GetHostAddressInterface(string szIP);
    public GetHostAddressInterface m_pGetHostAddressInterface = null;
    int m_DnsExceptionCount = 0;

    protected bool m_bIsIPV6 = false;
    protected bool m_bInitIPV6 = false;
    protected string m_szConnectAddr = string.Empty;
    protected int m_nConnectPort = 0;

    //---------------
    byte[] m_RecvBuffer;
    public CTcpThread()
    {
        m_nRandSeed = 0;

        m_RecvBuffer = new byte[8192];
    }
    // 功能：连接服务器
    // 参数：szServerAddr - 服务器的地址
    //       nPort  - 端口号
    public void ConnectToServer(string szServerAddr, int nPort, long nRandSeed = 0)
    {
        if (nRandSeed != 0)
            m_nRandSeed = nRandSeed;
        m_szConnectAddr = szServerAddr;
        m_nConnectPort = nPort;

        Thread t = new Thread(ThreadRun);
        t.Start();        
    }
    void  ThreadRun()
    {
        if (m_tcp != null)
            m_tcp.Close();
        else
            m_tcp = new CTcp();
        m_tcp.ConnectToServer(m_szConnectAddr, m_nConnectPort, m_nRandSeed);
        if(m_tcp.IsConnect())
        {
            OnSucConnect();
        }
        else
        {
            OnFailedConect();
        }
        // 开始接收消息吧
        BeginRecive();
    }
    void  BeginRecive()
    {
        int nRecvSize = 0;
        while (m_tcp.IsConnect())
        {
            nRecvSize = m_tcp.Receive(m_RecvBuffer, 4096);
            if(nRecvSize > 0)
            {
                OnReceiveBuf(m_RecvBuffer, nRecvSize);
            }
            else
            {
                Quit();
            }
        }
    }

    public void Quit()
    {
        m_tcp.Close();
    }

    // 功能：判断连接是不是正常
    public bool IsConnect()
    {
        if (m_tcp != null)
            return m_tcp.IsConnect();
        return false;
    }
    
    // 功能：发送消息包到服务器
    public void SendToServer(byte[] szBuf, int nBufSize)
    {
        if (m_tcp != null)
            m_tcp.Send(szBuf, nBufSize, SocketFlags.None);
    }
    
    // 功能：通知收到消息包的事件
    // 注意：这个接口是异步调用的，需要上层自己处理加锁事件
    protected virtual int OnReceiveBuf(byte[] recBuf, int nSize)
    {
        // 在这里做解包的事件, 这个可能是多个包，也可能是不到一个包
        return nSize;
    }
    protected virtual void OnSucConnect()
    {

    }
    protected virtual void OnFailedConect()
    {

    }
}
