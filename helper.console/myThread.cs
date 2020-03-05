using System.Threading;
using System.Timers;

namespace helper.console
{
    abstract class myThread
    {
        Thread thread = null;
        System.Timers.Timer timer = new System.Timers.Timer();
        public myThread()
        {
            timer.Elapsed += new ElapsedEventHandler(AutoShop);
            timer.Interval = 30000;
        }

        private void AutoShop(object sender, ElapsedEventArgs args)
        {
            Abort();
            timer.Dispose();
        }

        abstract public void Run();
        public void Start()
        {
            if (thread == null)
                thread = new Thread(Run);
            thread.Start();
        }
        public void Abort()
        {
            BeforeAbort();
            thread.Abort();
        }
        abstract public void BeforeAbort();
    }
}