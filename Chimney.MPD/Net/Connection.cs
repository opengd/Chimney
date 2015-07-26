using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Chimney.MPD.Net
{
    class Connection
    {
        private bool _Connected;
        public bool Connected
        {
            get
            {
                return _Connected;
            }
        }

        private StreamSocket _streamSocket;
        public StreamSocket Socket
        {
            get
            {
                return _streamSocket;
            }
        }

        private string _host;
        private string _port;

        public void Close()
        {
            try
            {
                _streamSocket.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public async Task<bool> Connect(string host, string port, int timeout = 0)
        {
            _streamSocket = new StreamSocket();
            _streamSocket.Control.KeepAlive = true;
            _streamSocket.Control.QualityOfService = SocketQualityOfService.Normal;

            var cts = new CancellationTokenSource();

            _host = host;
            _port = port;

            try
            {
                if(timeout > 0)
                {
                    cts.CancelAfter(timeout);
                    await _streamSocket.ConnectAsync(new HostName(host), port).AsTask(cts.Token);
                }
                else
                    await _streamSocket.ConnectAsync(new HostName(host), port);

                _Connected = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                Close();

                _streamSocket = null;

                _host = null;
                _port = null;

                _Connected = false;
            }

            return _Connected;
        }

        public static async Task<string> Recive(StreamSocket streamSocket, List<string> orstarts, List<string> orends, List<string> andstarts, List<string> andends)
        {
            string returnString = string.Empty;
            
            using (var dataReader = new DataReader(streamSocket.InputStream))
            {
                dataReader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                dataReader.InputStreamOptions = InputStreamOptions.Partial;

                var end = false;

                while (!end && await dataReader.LoadAsync(64000) != 0)
                {
                    string readString;
                    var buffer = dataReader.ReadBuffer(dataReader.UnconsumedBufferLength);
                    using (var dr = DataReader.FromBuffer(buffer))
                    {
                        var bytes1251 = new Byte[buffer.Length];
                        dr.ReadBytes(bytes1251);

                        readString = Encoding.GetEncoding("UTF-8").GetString(bytes1251, 0, bytes1251.Length);
                    }

                    if (!string.IsNullOrEmpty(readString))
                        returnString += readString;

                    if (readString == null)
                    {
                        end = true;
                    }
                    else if (orstarts.FirstOrDefault(o => returnString.StartsWith(o)) != null)
                    {
                        end = true;
                    }
                    else if (orends.FirstOrDefault(o => returnString.EndsWith(o)) != null)
                    {
                        end = true;
                    }
                    else if (andstarts.FirstOrDefault(o => returnString.StartsWith(o)) != null
                            && andends.FirstOrDefault(o => returnString.EndsWith(o)) != null)
                    {
                        end = true;
                    }
                }

                dataReader.DetachStream();
            }

            return returnString;
        }



        public static async Task<bool> Send(StreamSocket streamSocket, string send)
        {            
            var bytearray = Encoding.GetEncoding("UTF-8").GetBytes(send);
            
            var success = true;

            using (var dataWriter = new DataWriter(streamSocket.OutputStream))
            {
                dataWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

                dataWriter.WriteBytes(bytearray);

                await dataWriter.StoreAsync();

                await dataWriter.FlushAsync();

                dataWriter.DetachStream();
            }

            return success;
        }
    }
}
