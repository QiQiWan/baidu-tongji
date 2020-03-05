
namespace helper.console
{
    class Program
    {
        static private BaiduWebHelper baidu;
        /// <summary>
        /// serverHelper,是webscket服务器,没必要长连接
        /// </summary>
        static private WebSocketServerHelper serverHelper;
        static private HttpServer server;
        static void Main(string[] args)
        {   
            try{
                //初始化构造函数
                Common.GetAccessToken();
            }
            catch(MyException err){
                throw err;
            }

            baidu = new BaiduWebHelper();

            server = new HttpServer();
            server.AddDomain("http://localhost:1234/");
            try{
                server.Start();
                server.WaitRequest();
            }
            catch(MyException err)
            {
                Log.WriteLine(Common.GetTime() + err.Message);
            }
            finally{
                server.Stop();
            }
        }
        static public string GetStatics() => baidu.GetResult();
        static public void UpdateToken() => baidu.RefreshToken();
        static public WebSocketServerHelper GetServerHelper() => serverHelper;
    }
}
