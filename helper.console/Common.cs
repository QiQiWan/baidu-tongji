using System;
using System.Text.RegularExpressions;

namespace helper.console
{
    /// <summary>
    /// 全局变量,公共方法
    /// </summary>
    static public class Common
    {
        static readonly public string client_id;
        static readonly public string client_secret;
        static private string access_token = "21.5046570320b07606605880520a9fc0a6.2592000.1585901514.240598184-10726351";
        static private string refresh_token;
        static readonly public string start_date;
        static readonly public string site_id = "14495663";
        static public string GetAccessToken() => access_token;
        static public string GetRefreshToken() => refresh_token;
        /// <summary>
        /// 全局变量初始化
        /// </summary>
        static Common()
        {
            string Json = FileHelper.ReadFile("config.json");

            if (Json == null)
            {
                Console.WriteLine("配置文件不存在!");
                throw new Exception("配置文件不存在!");
            }

            client_id = GetJsonValue(Json, "client_id");
            client_secret = GetJsonValue(Json, "client_secret");
            access_token = GetJsonValue(Json, "access_token");
            if (access_token.Length < 3)
                throw new Exception("必填参数!");
            refresh_token = GetJsonValue(Json, "refresh_token");
            if (refresh_token.Length < 3)
                refresh_token = null;
            start_date = GetJsonValue(Json, "start_date");
            site_id = GetJsonValue(Json, "site_id");
        }

        /// <summary>
        /// 从Json匹配出查询属性的结果
        /// </summary>
        /// <param name="Json"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static public string GetJsonValue(string Json, string name)
        {
            string pattern = "\"" + name + "\": \".*?(?=\")";
            string value = Regex.Match(Json, pattern).Value;
            value = value.Replace("\"" + name + "\":", "");
            value = value.Replace("\"", "");
            return value.Trim();
        }
        static public void UpdateToken(string refresh_Token, string access_Token)
        {
            refresh_token = refresh_Token;
            access_token = access_Token;
        }
        static public string GetTime() => DateTime.Now.ToShortDateString() + "  " + DateTime.Now.ToLongTimeString() + "   ";
        static public bool GetExitCommand()
        {
            string cmd = Console.ReadLine();
            if (cmd.ToLower().Contains("exit"))
                return false;
            return true;
        }
    }
}