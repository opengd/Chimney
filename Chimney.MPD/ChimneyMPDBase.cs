using Chimney.MPD.Classes;
using Chimney.MPD.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace Chimney.MPD
{
    public class ChimneyMPDBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public StreamSocketListener streamSocketListner;

        protected bool idle = false;

        public delegate void EventHandler(object sender, EventArgs e);
        public event EventHandler ConnectionProblem;
        public event EventHandler ConnectionConnected;
        public event EventHandler CouldNotConnect;

        private Chimney.MPD.Net.Connection _connection;

        protected bool connectionproblem = true;

        public string host = "";
        public string port = "";
        public string password = "";

        public string name = "";

        bool queueInUse = false;
        bool runQue = false;

        List<QueueJob> sendQueue = new List<QueueJob>();

        int queueId = -1;

        enum ConnectionErrors
        {
            Connection, Permission 
        };

        private Object add_send_lock = new object();

        private bool _Connected = false;
        public bool Connected
        {
            get
            {
                return _Connected;
            }
            set
            {
                _Connected = value;
                NotifyPropertyChanged("Connected");
            }
        }

        private bool _Connecting = false;
        public bool Connecting
        {
            get
            {
                return _Connecting;
            }
            set
            {
                _Connecting = value;
                NotifyPropertyChanged("Connecting");
            }
        }
        
        protected void SendConnectionProblem()
        {
           connectionproblem = true;
           if (ConnectionProblem != null) ConnectionProblem(this, new EventArgs());
        }

        public async Task<bool> Password(string password, bool closesocket = true)
        {
            if (_connection == null) return false;

            var success = await Connection.Send(_connection.Socket, 
                MPDKeyWords.Send.Encode(MPDKeyWords.Client.Connection.PASSWORD, "\"" + password + "\""));

            if (!success) return false;
             
            var response = await Connection.Recive(_connection.Socket,
                new List<string>() { MPDKeyWords.Response.SUCCESS_CONNECT },
                new List<string>() { MPDKeyWords.Response.OK + MPDKeyWords.Response.LINEBREAK },
                new List<string>() { MPDKeyWords.Response.ACK },
                new List<string>() { MPDKeyWords.Response.LINEBREAK });

            return (response.Equals(MPDKeyWords.Response.OK_LINEBREAK)) 
                ? true
                : false;
        }

        public async Task<bool> Ping(bool silent = true, bool retry = false)
        {
            if (idle) await NoIdle();

            var qId = await Send(MPDKeyWords.Send.Encode( MPDKeyWords.Client.Connection.PING ), silent, retry, true);

            return ((await Response(qId)).Equals(MPDKeyWords.Response.OK_LINEBREAK))
                ? true
                : false;
        }

        public async Task<bool> CheckConnection(bool silent = true, bool retry = false)
        {
            return await Ping(silent);
        }

        public async Task<Tuple<Stats, Status>> TestPermission()
        {
            //var qId = await Send(MPDKeyWords.Send.Encode( MPDKeyWords.Client.Status.STATS ), true, false, true);

            bool success = await Connection.Send(_connection.Socket,
                MPDKeyWords.Send.Encode(MPDKeyWords.Client.Status.STATS));

            if (!success)
                return null;

            string response = await Connection.Recive(_connection.Socket,
                new List<string>() { MPDKeyWords.Response.SUCCESS_CONNECT },
                new List<string>() { MPDKeyWords.Response.OK + MPDKeyWords.Response.LINEBREAK },
                new List<string>() { MPDKeyWords.Response.ACK },
                new List<string>() { MPDKeyWords.Response.LINEBREAK });

            //var response = await Response(qId);

            if (string.IsNullOrEmpty(response) || response.Contains(MPDKeyWords.Response.ACK))
                return null;

            var stats = new Stats((await MPDKeyWords.Response.Encode(response)).FirstOrDefault());

            //qId = await Send(MPDKeyWords.Send.Encode( MPDKeyWords.Client.Status.STATUS ), true, false, true);

            //response = await Response(qId);

            success = await Connection.Send(_connection.Socket,
                    MPDKeyWords.Send.Encode(MPDKeyWords.Client.Status.STATUS));

            if (!success)
                return null;

            response = await Connection.Recive(_connection.Socket,
                new List<string>() { MPDKeyWords.Response.SUCCESS_CONNECT },
                new List<string>() { MPDKeyWords.Response.OK + MPDKeyWords.Response.LINEBREAK },
                new List<string>() { MPDKeyWords.Response.ACK },
                new List<string>() { MPDKeyWords.Response.LINEBREAK });

            if (string.IsNullOrEmpty(response) || response.Contains(MPDKeyWords.Response.ACK))
                return null;

            var status = new Status((await MPDKeyWords.Response.Encode(response)).FirstOrDefault());

            return new Tuple<Stats, Status>(stats, status);
        }

        public async Task<List<string>> Idle(string subsystems = "")
        {
            this.idle = true;

            //var qId =  
            //    ? await Send(MPDKeyWords.Client.Status.IDLE)
            //    : await Send(MPDKeyWords.Client.Status.IDLE);

            var success = (string.IsNullOrEmpty(subsystems)) 
                ? await Connection.Send(_connection.Socket,
                    MPDKeyWords.Send.Encode(MPDKeyWords.Client.Status.IDLE))
                : await Connection.Send(_connection.Socket,
                    MPDKeyWords.Send.Encode(MPDKeyWords.Client.Status.IDLE, subsystems.Split(new char[] { ' ' }).ToList()));

            if (!success) return new List<string>();

            var response = await Connection.Recive(_connection.Socket,
                new List<string>() { MPDKeyWords.Response.SUCCESS_CONNECT },
                new List<string>() { MPDKeyWords.Response.OK_LINEBREAK },
                new List<string>() { MPDKeyWords.Response.ACK },
                new List<string>() { MPDKeyWords.Response.LINEBREAK });

            var responselist = (await MPDKeyWords.Response.Encode(response)).FirstOrDefault(); 

            return (from kv in responselist select kv.Value).ToList();
        }

        public async Task NoIdle()
        {
            await Connection.Send(_connection.Socket,
                    MPDKeyWords.Send.Encode(MPDKeyWords.Client.Status.NOIDLE));
            //await Send(MPDKeyWords.Send.Encode( MPDKeyWords.Client.Status.NOIDLE ), false, true, false);

            idle = false;
        }

        public async Task<bool> Close(bool connectionproblem = false)
        {
            Debug.WriteLine(name + " : CLOSE");

            queueInUse = idle = runQue = false;

            sendQueue.Clear();
            responseDictionary.Clear();

            this.connectionproblem = connectionproblem;

            if (_connection != null && !this.connectionproblem)
                //await Send( MPDKeyWords.Send.Encode( MPDKeyWords.Client.Connection.CLOSE), false, true, false);
                await Connection.Send(_connection.Socket,
                    MPDKeyWords.Send.Encode(MPDKeyWords.Client.Connection.CLOSE));

            Connected = Connecting = false;

            return true;
        }

        public virtual async Task<bool> RefreshConnection()
        {
            /*
            bool suc = false;
            int i = 3;
            while (i > 0 || !suc)
            {
                suc = await Connection.Send(_connection.Socket, "CLOSE");
                i--;
            }
            */

            bool suc = await Connect(this.host, this.port, this.password, true);

            return suc;
        }

        public async Task<bool> Disconnect()
        {
            return await Close(false);
        }

        //
        // Connect to MPD server
        //
        public async Task<bool> Connect(string host, string port, string password = null, bool silent = false, int timeout = 0)
        {
            
            connectionproblem = false;

            this.host = host;
            this.port = port;
            this.password = password;

            if (!silent)
            {
                Connected = false;
                Connecting = true;
            }

            if (_connection != null)
            {
                _connection.Close();
                Debug.WriteLine(this.name + " : CLOSE CONNECTION");

            }

            _connection = new Connection();

            bool success = await _connection.Connect(host, port, timeout);

            Debug.WriteLine(this.name + " : NEW CONNECTION : " + success);


            if (success)
            {
                var response = await Connection.Recive(_connection.Socket,
                    new List<string>() { MPDKeyWords.Response.SUCCESS_CONNECT },
                    new List<string>() { MPDKeyWords.Response.OK + MPDKeyWords.Response.LINEBREAK },
                    new List<string>() { MPDKeyWords.Response.ACK },
                    new List<string>() { MPDKeyWords.Response.LINEBREAK });

                if (string.IsNullOrEmpty(response) || !response.StartsWith(MPDKeyWords.Response.SUCCESS_CONNECT))
                    success = false;

                Debug.WriteLine(this.name + " : NEW CONNECTION : RESPONSE : " + success);

                //if (!string.IsNullOrEmpty(password) && !await Password(password, false))
                //    return false;

                //if (!await Password(password, false))
                //    return false;

                if (success)
                {
                    await Password(password, false);

                    if (await TestPermission() == null)
                        success = false;
                }

                Debug.WriteLine(this.name + " : NEW CONNECTION : PERMISSION : " + success);
            }

            if (!success)
            {
                Connected = false;
                Connecting = false;
                _connection.Close();

                if (CouldNotConnect != null)
                    CouldNotConnect(this, new EventArgs());
            }
            else if (success && !silent)
            {
                Connected = true;
                Connecting = false;

                if (ConnectionConnected != null)
                    ConnectionConnected(this, new EventArgs());
            }

            Debug.WriteLine(this.name + " : NEW CONNECTION : RETURN : " + success);

            return success;
        }
        
        // Send and recive data to MPD server
        //
        private async Task RunQueue()
        {
            runQue = true;

            while (runQue)
            {
                if (sendQueue.Count > 0)
                {
                    if (!queueInUse)
                    {
                        queueInUse = true;

                        QueueJob queueJob = sendQueue[0];

                        int attemps = (queueJob.retry) ? 5 : 1;

                        int readAttemps = 2;

                        bool suc = false;
                        bool readsuc = false;
                        string responseString = string.Empty;

                        while (!suc && attemps > 0)
                        {
                            Debug.WriteLine(this.name + " : SEND : queueJob.id: " + queueJob.id + " queueJob.send: " + queueJob.send);
                            suc = await Connection.Send(_connection.Socket, queueJob.send);

                            if (suc && queueJob.wait)
                            {
                                responseString = string.Empty;
                                readAttemps = 3;
                                while (string.IsNullOrEmpty(responseString) && readAttemps > 0)
                                {
                                    responseString = await Connection.Recive(_connection.Socket,
                                        new List<string>() { MPDKeyWords.Response.SUCCESS_CONNECT },
                                        new List<string>() { MPDKeyWords.Response.OK_LINEBREAK },
                                        new List<string>() { MPDKeyWords.Response.ACK }, 
                                        new List<string>() { MPDKeyWords.Response.LINEBREAK });

                                    if (responseString.EndsWith(MPDKeyWords.Response.OK_LINEBREAK)
                                        || (responseString.StartsWith(MPDKeyWords.Response.ACK)))
                                    {
                                        queueJob.response = responseString;

                                        this.responseDictionary.Add(queueJob.id, queueJob);
                                        readsuc = true;
                                        Debug.WriteLine(this.name + " : RECIVE : queueJob.id: " + queueJob.id + " queueJob.send: " + queueJob.send);

                                    }

                                    readAttemps--;
                                }

                                if (string.IsNullOrEmpty(responseString)) 
                                    readsuc = false;
                            }

                            attemps--;

                            if ((!suc || !readsuc) && attemps > 0 && queueJob.wait)
                            {
                                Debug.WriteLine(this.name + " : RECONNECT");
                                await Connect(this.host, this.port, this.password, true);
                                suc = false;
                                //attemps--;

                                
                            }
                        }

                        if (readsuc == false && suc == true && string.IsNullOrEmpty(responseString))
                        {
                            queueJob.response = responseString;
                            this.responseDictionary.Add(queueJob.id, queueJob);
                            Debug.WriteLine(this.name + " : EMPTY RECIVE : queueJob.id: " + queueJob.id + " queueJob.send: " + queueJob.send);

                        }

                        sendQueue.RemoveAt(0);
                        queueInUse = false;

                        if (attemps == 0 && !suc && !this.connectionproblem && !queueJob.silent)
                        {
                            this.connectionproblem = true;
                            if (ConnectionProblem != null) 
                                ConnectionProblem(this, new EventArgs());
                        }

                    }
                    
                }
                else runQue = false;
            }
        }

        public async Task<int> Send(string cmd)
        {
            return await Send(MPDKeyWords.Send.Encode(cmd), false, true, true);
        }

        public async Task<int> Send(string cmd, List<string> args)
        {
            return await Send(MPDKeyWords.Send.Encode(cmd, args, new List<string>()), false, true, true);
        }

        public async Task<int> Send(string cmd, List<string> args, List<string> quoted_args, bool reversarguments = false)
        {
            return await Send(MPDKeyWords.Send.Encode(cmd, args, quoted_args, reversarguments), false, true, true);
        }

        private async Task<int> Send(string send, bool silent, bool retry, bool wait)
        {
            if (_connection == null)
            {
                Debug.WriteLine("connection null");
                return -1;
            }

            lock(add_send_lock)
            {
                queueId++;

                Debug.WriteLine(this.name + " : ADD : queueId: " + queueId  + " send: " + send);

                sendQueue.Add(new QueueJob(queueId, send, silent, retry, wait));

                if (!runQue) RunQueue();
            }
            return queueId;
        }

        private Dictionary<int, QueueJob> responseDictionary = new Dictionary<int, QueueJob>(); 

        public async Task<string> Response(int qId)
        {
            QueueJob queueJob = new QueueJob();

            while (responseDictionary.ContainsKey(qId) == false
                && (responseDictionary.Count > 0 
                || sendQueue.Count > 0))
            {
                await Task.Delay(1);
            }

            if (responseDictionary.ContainsKey(qId))
            {
                queueJob = responseDictionary[qId];

                Debug.WriteLine(this.name + " : RETRIVE : qId: " + qId + " queueJob: " + queueJob.id);
                responseDictionary.Remove(qId);
            }

            return queueJob.response;
        }
    }
}
