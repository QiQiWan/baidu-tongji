namespace helper.console
{
    public class ScriptCreator
    {
        static public string ExternalSource(string RVobj, string UVobj){
            RVobj = RVobj.Replace("\"", "\\\"");
            UVobj = UVobj.Replace("\"", "\\\"");
            string script = "var UV = document.getElementById(\"UV\");\n";
            script += "var RV = document.getElementById(\"RV\");\n";
            script += "RV.innerHTML = JSON.parse(\"" + 
                RVobj + "\").result.total;\n";
            script += "UV.innerHTML = JSON.parse(\"" +
                UVobj + "\").result.items[1].reduce(function(x, y){ return parseInt(x) + parseInt(y);});\n";
            return script;
        }
    }
}