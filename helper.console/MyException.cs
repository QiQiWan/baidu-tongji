using System;

namespace helper.console
{
    public class MyException: Exception
    {
        static public Exception GetException(Exception err) => err;
    }
}