using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ogam.Remote.Tcp {
    public class OClient {
        private NetworkStream _networkStream;

        private readonly IEvaluator _receivEvaluator;

        public int BufferSize = 1048576;
        public int ReadTimeout = 60000;

        public string Host;
        public int Port;

        public string Name;
        public bool IsLog = false;

        private readonly object _sync = new object();

        public OClient(string host, int port, string name = "") {
            Host = host;
            Port = port;

            if (string.IsNullOrWhiteSpace(name)) {
                Name = Guid.NewGuid().ToString("N").Substring(0, 5);
            }

            _receivEvaluator = new Evaluator();
            _receivEvaluator.Extend("throw-exception", ThrowException);

            var connectionThread = new Thread(ConnectorHandler);
            connectionThread.IsBackground = true;
            connectionThread.Start();
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


                if (string.IsNullOrWhiteSpace(cmd)) return "";

                try {
                    if (_networkStream == null) {
                        Console.WriteLine($"(connection-error \"[{Name}]{Host}:{Port}\" \"Connection is fault\")");
                        return null;
                    }

                    if (_networkStream.CanWrite && _networkStream.CanRead) {

                        var transactLog = new StringBuilder();
                        transactLog.AppendLine($"(start-transaction \"[{Name}]{Host}:{Port}\")");

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

                        return resultO;
                    } 
                    else {
                        Console.WriteLine($"(connection-error \"[{Name}]{Host}:{Port}\" \"Data can't write\")");
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"(connection-error \"[{Name}]{Host}:{Port}\" \"{ex.Message}\")");
                }

                return null;
            }
        }

        private TcpClient Connect(string hostname, int port) {
            lock (_sync) {


                
                Console.WriteLine($"(connecting \"{hostname}:{port}\")");
                var client = new TcpClient();
                client.ReceiveTimeout = ReadTimeout;
                client.SendTimeout = ReadTimeout;

                try {
                    client.Connect(hostname, port);

                    _networkStream = client.GetStream();
                    _networkStream.WriteTimeout = ReadTimeout;
                    _networkStream.ReadTimeout = ReadTimeout;
                    Console.WriteLine($"(connection-stabilized \"{hostname}:{port}\")");
                }
                catch (Exception ex) {
                    Console.WriteLine($"(connection-error \"{ex.Message}\")");
                }

                return client;
            }
        }

        private void ConnectorHandler(object o) {
            var client = Connect(Host, Port);

            while (true) {
                Thread.Sleep(1000);
                if (!client.Connected) {
                    client = Connect(Host, Port);
                }
            }

        }
    }
}
