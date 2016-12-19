using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ogam.Remote.Tcp {
    public class TcpClientProcess : IDisposable{
        private NetworkStream _networkStream;

        private readonly IEvaluator _receivEvaluator;

        public int BufferSize = 1048576;
        public int ReadTimeout = 60000;

        public string Host;
        public int Port;

        public string Name;
        public bool IsLog = false;

        public bool IsBusy;

        private readonly object _sync = new object();

        private readonly Thread _reconnector;

        private bool _isReconnectorRun = false;

        public TcpClientProcess(string host, int port, string name = "") {
            Host = host;
            Port = port;

            if (string.IsNullOrWhiteSpace(name)) {
                Name = Guid.NewGuid().ToString("N").Substring(0, 5);
            }

            _receivEvaluator = new Evaluator();
            _receivEvaluator.Extend("throw-exception", ThrowException);

            _reconnector = new Thread(ConnectorHandler);
            _reconnector.Priority = ThreadPriority.Highest;
            _reconnector.IsBackground = true;
            _reconnector.Start();

            while (!_isReconnectorRun) {
                Thread.Sleep(500);
            }
        }

        private static object ThrowException(object[] arg) {
            return new Exception(arg.StringAt(0));
        }

        public object MakeCall(string operation, params object[] args) {

            var cmd = "(" + operation;
            foreach (var o in args) {
                cmd += string.Format(" {0}", OgamSerializer.Serialize(o));
            }
            cmd += ")";

            var result = Call(cmd);

            if (result is Exception) {
                throw result as Exception; // !!! REMOTE SERVER RETURN EXCEPTION !!!
            }

            return result;
        }

        public object Call(string cmd) {
            lock (_sync) {
                IsBusy = true;

                if (string.IsNullOrWhiteSpace(cmd)) {
                    IsBusy = false;
                    return "";
                }

                try {
                    if (_networkStream == null) {
                        Console.WriteLine($"(connection-error \"{Name} -- {Host}:{Port}\" \"Connection is fault\")");
                        IsBusy = false;
                        return null;
                    }

                    if (_networkStream.CanWrite && _networkStream.CanRead) {

                        var transactLog = new StringBuilder();
                        transactLog.AppendLine($"(start-transaction \"{Name} -- {Host}:{Port}\")");

                        var buff = Encoding.Unicode.GetBytes(cmd + '\0'); // add EOS
                        _networkStream.Write(buff, 0, buff.Length);

                        transactLog.AppendLine($"<< {cmd}");

                        buff = new byte[BufferSize];
                        var bytes = _networkStream.Read(buff, 0, buff.Length);

                        var rcvMsg = Encoding.Unicode.GetString(buff, 0, bytes);

                        var timeBefore = DateTime.Now;
                        
                        while (true) {
                            while (_networkStream.DataAvailable) {
                                bytes = _networkStream.Read(buff, 0, buff.Length);
                                rcvMsg += Encoding.Unicode.GetString(buff, 0, bytes);
                                //Thread.Sleep(100); // TODO make end sequenses support
                            }

                            if (rcvMsg.Length > 0 && rcvMsg.Last() == '\0') break; // EOS

                            if (timeBefore.AddSeconds(10) <= DateTime.Now) break; // timeOut

                            if (!_networkStream.DataAvailable) {
                                Thread.Sleep(50);
                            }
                        }


                        transactLog.AppendLine($">> {rcvMsg}");

                        if (IsLog) {
                            Console.WriteLine(transactLog.ToString().Trim());
                        }

                        var resultO = _receivEvaluator.Eval(rcvMsg);

                        IsBusy = false;

                        return resultO;
                    } 
                    else {
                        Console.WriteLine($"(connection-error \"{Name} -- {Host}:{Port}\" \"Data can't write\")");
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"(connection-error \"{Name} -- {Host}:{Port}\" \"{ex.Message}\")");
                }

                IsBusy = false;

                return null;
            }
        }

        private TcpClient Connect(string hostname, int port) {
            lock (_sync) {
                Console.WriteLine($"(connecting \"{Name} -- {Host}:{Port}\")");
                var client = new TcpClient();
                client.ReceiveTimeout = ReadTimeout;
                client.SendTimeout = ReadTimeout;

                try {
                    client.Connect(hostname, port);

                    _networkStream = client.GetStream();
                    _networkStream.WriteTimeout = ReadTimeout;
                    _networkStream.ReadTimeout = ReadTimeout;
                    Console.WriteLine($"(connection-stabilized \"{Name} -- {Host}:{Port}\")");
                }
                catch (Exception ex) {
                    Console.WriteLine($"(connection-error \"{ex.Message}\")");
                }

                return client;
            }
        }

        private bool _isReconnect = true;
        private TcpClient _tcpClient;

        private void ConnectorHandler(object o) {
            _isReconnectorRun = true;
            _tcpClient = Connect(Host, Port);

            while (_isReconnect) {
                Thread.Sleep(1000);
                if (!_tcpClient.Connected) {
                    _tcpClient = Connect(Host, Port);
                }
            }

        }

        public void Dispose() {
            lock (_sync) {
                _isReconnect = false;
                _reconnector?.Abort();
                _tcpClient?.Close();
                Console.WriteLine($"(connection-closed-down \"{Name} -- {Host}:{Port}\")");
            }
        }
    }
}
