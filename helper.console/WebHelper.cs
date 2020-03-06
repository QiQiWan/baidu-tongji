using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Fleck;


namespace helper.console
{
    class WebSocketThread : myThread
    {
        private Socket sc;
        public WebSocketThread(Socket socket)
        {
            sc = socket;
        }
        private byte[] buffer = new byte[1024];
        public override void Run()
        {
            Log.WriteLine(Common.GetTime() + "客户端:" + sc.RemoteEndPoint.ToString() + "已连接");

            //握手
            int length = sc.Receive(buffer);//接受客户端握手信息
            sc.Send(SocketPacker.PackHandShakeData(SocketPacker.GetSecKeyAccetp(buffer, length)));
            Log.WriteLine(Common.GetTime() + "已经发送握手协议");

            //发送数据
            string sendMsg = Program.GetStatics();
            sc.Send(SocketPacker.PackData(sendMsg));
            Log.WriteLine(Common.GetTime() + "已发送：“" + sendMsg);


            Log.WriteLine("----------------------------------------------------------------------------------------------------");
            this.Abort();
        }
        public override void BeforeAbort()
        {
            sc.Close();
            Program.GetServerHelper().RemoveSocket(sc);
        }
    }

    class HttpThread: myThread{
        private HttpListenerContext result;
        public HttpThread(HttpListenerContext result){
            this.result = result;
        }
        public override void Run(){
            HttpServer.SetResponse(result);
            //Abort();
        }
        public override void BeforeAbort(){}
    }
    /// <summary>
    /// 获取百度接口的请求类
    /// </summary>
    class BaiduWebHelper
    {
        private HttpWebRequest request;
        private HttpWebResponse response;
        private string ACCESS_TOKEN;
        private string siteId;
        private string RequestUrl = "https://openapi.baidu.com/rest/2.0/tongji/report/getData?";
        public string resultJson = "";
        public BaiduWebHelper()
        {
            this.ACCESS_TOKEN = Common.GetAccessToken();
            this.siteId = Common.site_id;
        }


        /// <summary>
        /// 获取百度API返回的JSON
        /// </summary>
        /// <returns></returns>
        public void GetResult()
        {
            string url = RequestUrl +
                "access_token=" + ACCESS_TOKEN + "&" +
                "site_id=" + siteId + "&";
            GetUVArgus getUVArgus = new GetUVArgus("20200301");
            string result = "";

            //获取RV    
            request = HttpWebRequest.Create(url + GetRealVisittor.GetMethod()) as HttpWebRequest;
            request.Method = "GET";
            response = request.GetResponse() as HttpWebResponse;
            result += ReadWebStream(response.GetResponseStream());
            //获取UV
            url = url + getUVArgus.ToString();
            request = HttpWebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            response = request.GetResponse() as HttpWebResponse;
            result += ReadWebStream(response.GetResponseStream());

            resultJson = result;
        }
        /// <summary>
        /// 读取web响应流
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public string ReadWebStream(Stream stream)
        {
            StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            stream.Close();
            reader.Close();
            return result;
        }
        /// <summary>
        /// token过期后换取新的token
        /// </summary>
        public void RefreshToken()
        {
            if (Common.GetRefreshToken() == null)
                throw new Exception("未给定更新权限!");
            string url = "http://openapi.baidu.com/oauth/2.0/token?grant_type=refresh_token&";
            url = url + "refresh_token=" + Common.GetRefreshToken() + "&" +
                "client_secret=" + Common.client_secret + "&" +
                "client_id=" + Common.client_id;
            request = HttpWebRequest.Create(url) as HttpWebRequest;
            response = request.GetResponse() as HttpWebResponse;
            string result = ReadWebStream(response.GetResponseStream());
            string REFRESH_TOKEN = Common.GetJsonValue(result, "refresh_token");
            this.ACCESS_TOKEN = Common.GetJsonValue(result, "access_token");
            Common.UpdateToken(REFRESH_TOKEN, ACCESS_TOKEN);
        }
    }

    class HttpServer
    {
        private HttpListener server;
        private List<string> domainList = new List<string>();
        private List<HttpThread> threadPools = new List<HttpThread>();
        public HttpServer()
        {
            server = new HttpListener();
        }
        public void Start()
        {
            foreach (var item in domainList)
                server.Prefixes.Add(item);
            server.Start();
            //IAsyncResult result = server.BeginGetContext(new AsyncCallback(SetResponse), server);
        }
        /// <summary>
        /// 阻塞进程
        /// </summary>
        public void WaitRequest(){
            while(true){
                HttpListenerContext result = server.GetContext();
                HttpThread temp = new HttpThread(result);
                AddThread(temp);
                temp.Start();
                threadPools.Remove(temp);
            }
        }
        static public void SetResponse(HttpListenerContext result){
            HttpListenerRequest request = result.Request;
            Log.WriteLine(Common.GetTime() + request.RemoteEndPoint.Address + "  已连接");

            HttpListenerResponse response = result.Response;
            string responseString = Program.GetStatics();
            if (responseString.Contains("error"))
                Program.UpdateToken();
            responseString = Program.GetStatics();
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }
        private void SetResponse(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            Log.WriteLine(Common.GetTime() + request.RemoteEndPoint.Address + "  已连接");

            HttpListenerResponse response = context.Response;
            string responseString = Program.GetStatics();
            if (responseString.Contains("error"))
                Program.UpdateToken();
            responseString = Program.GetStatics();
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

        }
        public void Stop()
        {
            server.Stop();
            server.Close();
            server.Abort();
        }
        public void AddDomain(string domain) => domainList.Add(domain);
        private void AddThread(HttpThread thread){
            this.threadPools.Add(thread);
        }
    }
    class WebSocketServerHelper
    {
        //套接字服务池 
        private List<Socket> SocketPools = new List<Socket>();
        private WebSocketServer server;

        Socket Socket;
        public WebSocketServerHelper(IpAdress ipadress)
        {
            string url = "ws://" + ipadress.ToString();
            server = new WebSocketServer(url);
            IPEndPoint localIEP = new IPEndPoint(IPAddress.Any, ipadress.port);
            Socket = new Socket(localIEP.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Socket.Bind(localIEP);
            Socket.Listen(100);//同时允许100个监听套接字
        }
        public Socket GetSocket() => Socket.Accept();
        //配合主程序采用泛型参数
        public void RemoveSocket(Socket socket) => SocketPools.Remove((Socket)socket);
    }
    class SocketPacker
    {
        static string byte_to_string(byte[] b)
        {
            string s = "";
            foreach (byte _b in b)
            {
                s += _b.ToString();
            }
            return s;
        }
        /// <summary>
        /// 打包握手信息
        /// </summary>
        /// <param name="secKeyAccept">Sec-WebSocket-Accept</param>
        /// <returns>数据包</returns>
        public static byte[] PackHandShakeData(string secKeyAccept)
        {
            var responseBuilder = new StringBuilder();
            responseBuilder.Append("HTTP/1.1 101 Switching Protocols" + Environment.NewLine);
            responseBuilder.Append("Upgrade: websocket" + Environment.NewLine);
            responseBuilder.Append("Connection: Upgrade" + Environment.NewLine);
            //responseBuilder.Append("Sec-WebSocket-Accept: " + secKeyAccept + Environment.NewLine + Environment.NewLine);
            //如果把上一行换成下面两行，才是thewebsocketprotocol-17协议，但居然握手不成功，目前仍没弄明白！
            responseBuilder.Append("Sec-WebSocket-Accept: " + secKeyAccept + Environment.NewLine);
            responseBuilder.Append("Sec-WebSocket-Protocol: chat");

            return Encoding.UTF8.GetBytes(responseBuilder.ToString());
        }

        /// <summary>
        /// 生成Sec-WebSocket-Accept
        /// </summary>
        /// <param name="handShakeText">客户端握手信息</param>
        /// <returns>Sec-WebSocket-Accept</returns>
        public static string GetSecKeyAccetp(byte[] handShakeBytes, int bytesLength)
        {
            string handShakeText = Encoding.UTF8.GetString(handShakeBytes, 0, bytesLength);
            string key = string.Empty;
            Regex r = new Regex(@"Sec\-WebSocket\-Key:(.*?)\r\n");
            Match m = r.Match(handShakeText);
            if (m.Groups.Count != 0)
            {
                key = Regex.Replace(m.Value, @"Sec\-WebSocket\-Key:(.*?)\r\n", "$1").Trim();
            }
            byte[] encryptionString = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
            return Convert.ToBase64String(encryptionString);
        }

        /// <summary>
        /// 解析客户端数据包
        /// </summary>
        /// <param name="recBytes">服务器接收的数据包</param>
        /// <param name="recByteLength">有效数据长度</param>
        /// <returns></returns>
        public static string AnalyticData(byte[] recBytes, int recByteLength)
        {
            if (recByteLength < 2) { return string.Empty; }

            bool fin = (recBytes[0] & 0x80) == 0x80; // 1bit，1表示最后一帧  
            if (!fin)
            {
                return string.Empty;// 超过一帧暂不处理 
            }

            bool mask_flag = (recBytes[1] & 0x80) == 0x80; // 是否包含掩码  
            if (!mask_flag)
            {
                return string.Empty;// 不包含掩码的暂不处理
            }

            int payload_len = recBytes[1] & 0x7F; // 数据长度  

            byte[] masks = new byte[4];
            byte[] payload_data;

            if (payload_len == 126)
            {
                Array.Copy(recBytes, 4, masks, 0, 4);
                payload_len = (UInt16)(recBytes[2] << 8 | recBytes[3]);
                payload_data = new byte[payload_len];
                Array.Copy(recBytes, 8, payload_data, 0, payload_len);

            }
            else if (payload_len == 127)
            {
                Array.Copy(recBytes, 10, masks, 0, 4);
                byte[] uInt64Bytes = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    uInt64Bytes[i] = recBytes[9 - i];
                }
                UInt64 len = BitConverter.ToUInt64(uInt64Bytes, 0);

                payload_data = new byte[len];
                for (UInt64 i = 0; i < len; i++)
                {
                    payload_data[i] = recBytes[i + 14];
                }
            }
            else
            {
                Array.Copy(recBytes, 2, masks, 0, 4);
                payload_data = new byte[payload_len];
                Array.Copy(recBytes, 6, payload_data, 0, payload_len);

            }

            for (var i = 0; i < payload_len; i++)
            {
                payload_data[i] = (byte)(payload_data[i] ^ masks[i % 4]);
            }

            return Encoding.UTF8.GetString(payload_data);
        }


        /// <summary>
        /// 打包服务器数据
        /// </summary>
        /// <param name="message">数据</param>
        /// <returns>数据包</returns>
        public static byte[] PackData(string message)
        {
            byte[] contentBytes = null;
            byte[] temp = Encoding.UTF8.GetBytes(message);

            if (temp.Length < 126)
            {
                contentBytes = new byte[temp.Length + 2];
                contentBytes[0] = 0x81;
                contentBytes[1] = (byte)temp.Length;
                Array.Copy(temp, 0, contentBytes, 2, temp.Length);
            }
            else if (temp.Length < 0xFFFF)
            {
                contentBytes = new byte[temp.Length + 4];
                contentBytes[0] = 0x81;
                contentBytes[1] = 126;
                contentBytes[2] = (byte)(temp.Length & 0xFF);
                contentBytes[3] = (byte)(temp.Length >> 8 & 0xFF);
                Array.Copy(temp, 0, contentBytes, 4, temp.Length);
            }
            else
            {
                // 暂不处理超长内容  
            }

            return contentBytes;
        }
    }
    /// <summary>
    /// 获取UV的请求
    /// Method为请求保留字
    /// </summary>
    class GetUVArgus
    {

        public string Method = "overview/getTimeTrendRpt";
        private string StartDate = "20200301";
        private string EndDate;
        private string Metrics = "visitor_count";
        public GetUVArgus(string startDate)
        {
            this.StartDate = startDate;
            EndDate = GetDateString();
        }
        public string GetDateString()
        {
            DateTime now = DateTime.Now;
            string month = now.Month < 10 ? "0" + now.Month.ToString() : now.Month.ToString();
            string day = now.Day < 10 ? "0" + now.Day : now.Day.ToString();
            return now.Year + month + day;
        }
        public override string ToString()
        {
            string url = "start_date=" + StartDate + "&" +
                "end_date=" + EndDate + "&" +
                "method=" + Method + "&" +
                "metrics=" + Metrics;
            return url;
        }
    }
    /// <summary>
    /// 不需要加什么,定义一个Method即可
    /// </summary>
    class GetRealVisittor
    {
        static public string Method = "trend/latest/a";
        static public string GetMethod() => "method=" + Method;
    }
    /// <summary>
    /// 指定请求类型,UV访问总数,RV实时在线
    /// </summary>
    enum GetWebType { UV, RV };
    /// <summary>
    /// 格式化的IP地址包括port
    /// </summary>
    class IpAdress
    {
        public string ip;
        public int port;
        public IpAdress(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }
        public override string ToString()
        {
            return ip + port.ToString();
        }
    }
    enum Protocol { NoSSL, SSL };
}
