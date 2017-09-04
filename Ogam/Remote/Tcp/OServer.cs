using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ogam.Remote.Tcp {
    public class OServer {
        private readonly TcpListener _listener;
        private readonly ILogExternal _logExternal;
        private Thread listerThread;
        public int BufferSize = 1048576;
        public IEvaluator Evaluator;

        public OServer(int Port, IEvaluator evaluator = null, ILogExternal logExternal = null) {
            Console.SetOut(new LogTextWriter(Console.Out));

            if (evaluator == null) {
                Evaluator = StingExtension.Evaluator;
            }
            _logExternal = logExternal;
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();

            listerThread = new Thread(ListenerHandler);
            listerThread.IsBackground = true;
            listerThread.Start(_listener);
        }

        private void ListenerHandler(object o) {
            var listener = (TcpListener) o;
            while (true) {
                var client = listener.AcceptTcpClient();
                var Thread = new Thread(ClientThread);
                Thread.IsBackground = true;
                Thread.Start(client);
            }
        }

        private void ClientThread(object o) {
            var client = (TcpClient) o;
            var endpoint = (IPEndPoint) client.Client.RemoteEndPoint;

            Console.WriteLine($"(client-connected \"{endpoint.Address}:{endpoint.Port}\")");

            try {
                var buffer = new byte[BufferSize];
                int count;


                while ((count = client.GetStream().Read(buffer, 0, buffer.Length)) > 0) {
                    var msg = Encoding.Unicode.GetString(buffer, 0, count);

                    //while (client.GetStream().DataAvailable) {
                    //    count = client.GetStream().Read(buffer, 0, buffer.Length);
                    //    msg += Encoding.Unicode.GetString(buffer, 0, count);
                    //}

                    var timeBefore = DateTime.Now;

                    while (true) {
                        while (client.GetStream().DataAvailable) {
                            count = client.GetStream().Read(buffer, 0, buffer.Length);
                            msg += Encoding.Unicode.GetString(buffer, 0, count);
                        }

                        if (msg.Length > 0 && msg.Last() == '\0')
                            break; // EOS

                        if (timeBefore.AddSeconds(10) <= DateTime.Now)
                            break; // timeOut

                        if (!client.GetStream().DataAvailable) {
                            Thread.Sleep(50);
                        }
                    }
                    var transactLog = new StringBuilder();
                    transactLog.AppendLine($"(start-transaction \"{endpoint.Address}:{endpoint.Port}\")");
                    try {
                        transactLog.AppendLine($">> {msg}");

                        var result = msg.OgamEval(Evaluator);

                        var str = OgamSerializer.Serialize(result) + '\0'; //add EOS

                        var resp = Encoding.Unicode.GetBytes(str);
                        client.GetStream().Write(resp, 0, resp.Length);

                        transactLog.AppendLine($"<< {str}");
                    }
                    catch (Exception ex) {
                        var errResp = $"(throw-exception {OgamSerializer.Serialize(ex.Message)})";

                        transactLog.AppendLine($">> {errResp}");

                        var resp = Encoding.Unicode.GetBytes(errResp + '\0');
                        client.GetStream().Write(resp, 0, resp.Length);
                        Console.ForegroundColor = ConsoleColor.Red;

                        transactLog.AppendLine($"(stack-trace \"{ex.StackTrace}\")");

                        Console.ResetColor();
                        if (_logExternal != null)
                            _logExternal.Error(ex.Message, ex);
                    }

                    Console.WriteLine(transactLog.ToString().Trim());
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"(error \"{endpoint.Address}:{endpoint.Port}\" \"{ex.Message}\")");
            }

            try {
                client.Close();
            }
            catch (Exception ex) {
                Console.WriteLine($"(error \"{endpoint.Address}:{endpoint.Port}\" \"{ex.Message}\")");
            }
        }

        static public void HoldProcess() {
            var processName = Process.GetCurrentProcess().ProcessName;
            var defColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Green;

            var msg = new StringBuilder();

            msg.AppendLine($"The {processName} is ready");
            msg.AppendLine($"Press <Enter> to terminate {processName}");
            Console.WriteLine(msg.ToString().Trim());

            Console.ForegroundColor = defColor;

            Console.ReadLine();
        }

        ~OServer() {
            if (_listener != null) {
                _listener.Stop();
            }
        }
    }
}