namespace Chimney.MPD.Classes
{
    class QueueJob
    {
        public int id = -1;
        public string send = "";
        public bool retry = true;
        public bool silent = false;
        public bool wait = true;
        public string response = "";

        public QueueJob()
        {

        }

        public QueueJob(int id, string send, bool silent = false, bool retry = true, bool wait = true)
        {
            this.id = id;
            this.send = send;
            this.retry = retry;
            this.silent = silent;
            this.wait = wait;
        }

    }
}
