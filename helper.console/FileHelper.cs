using System;
using System.IO;

namespace helper.console
{
    public class FileHelper
    {
        static public string ReadFile(string filename){
            if(!File.Exists(filename))
                return null;
            return File.ReadAllText(filename);
        }
        static public void WriteFile(string filename, string context){
            if (!File.Exists(filename))
                File.Create(filename);
            File.WriteAllText(filename, context);
        }
        static public void AppendFile(string filename, string appendText)
        {
            if (!File.Exists(filename))
                File.Create(filename);
            File.AppendAllText(filename, appendText);
        }
        
    }
    public class Log{
        static private string filename = "logs.txt";
        static public void WriteLine(string log){
            Console.WriteLine(log);
            FileHelper.AppendFile(filename, log + Environment.NewLine);
        }
    }
}