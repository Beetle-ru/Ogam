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
        private Thread listerThread;
        public int BufferSize = 1048576;
        public IEvaluator Evaluator;

        public OServer(int Port, IEvaluator evaluator = null) {

            Console.SetOut(new LogTextWriter(Console.Out));

            if (evaluator == null) {
                Evaluator = StingExtension.Evaluator;
            }
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();

            listerThread = new Thread(ListenerHandler);
            listerThread.IsBackground = true;
            listerThread.Start(_listener);
        }

        private void ListenerHandler(object o) {
            var listener = (TcpListener)o;
            while (true) {
                var client = listener.AcceptTcpClient();
                var Thread = new Thread(ClientThread);
                Thread.IsBackground = true;
                Thread.Start(client);
            }
        }

        private void ClientThread(object o) {
            var client = (TcpClient) o;
            var endpoint = (IPEndPoint)client.Client.RemoteEndPoint;

            //Console.WriteLine("{0}:{1} >> CONNECTED", endpoint.Address, endpoint.Port);
            Log(string.Format("{0}:{1} >> CONNECTED", endpoint.Address, endpoint.Port));

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

                    try {
                        Log(string.Format("{0}:{1} >> {2}", endpoint.Address, endpoint.Port, msg));

                        var result = msg.OgamEval(Evaluator);

                        //var str = result != null ? result.ToString() : "#nil";

                        var str = OgamSerializer.Serialize(result) + '\0'; //add EOS

                        var resp = Encoding.Unicode.GetBytes(str);
                        client.GetStream().Write(resp, 0, resp.Length);

                        //Console.WriteLine("{0}:{1} >> {2}", endpoint.Address, endpoint.Port, msg);
                        //Console.WriteLine("{0}:{1} << {2}", endpoint.Address, endpoint.Port, str);

                        Log(string.Format("{0}:{1} << {2}", endpoint.Address, endpoint.Port, str));
                    } catch (Exception ex) {
                        Console.WriteLine("{0}:{1} !!!>> {2}", endpoint.Address, endpoint.Port, msg);
                        var resp = Encoding.Unicode.GetBytes(string.Format("(throw-exception {0})", OgamSerializer.Serialize(ex.Message)) + '\0');
                        client.GetStream().Write(resp, 0, resp.Length);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex);
                        Console.ResetColor();
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine("{0}:{1} >> {2}", endpoint.Address, endpoint.Port, ex.Message);
            }

            try {
                client.Close();
            }
            catch (Exception ex) {
                Console.WriteLine("{0}:{1} >> {2}", endpoint.Address, endpoint.Port, ex.Message);
            }
        }

        void Log(string msg) {
            //((Action)delegate() {
            //    var standardOutput = new StreamWriter(Console.OpenStandardOutput(), Console.OutputEncoding);
            //    standardOutput.AutoFlush = true;
            //    //var bytes = Encoding.Convert(Encoding.Default, Console.OutputEncoding, Encoding.Default.GetBytes(msg + Environment.NewLine));
            //    standardOutput.Write(msg+ Environment.NewLine);
            //    //standardOutput.Write(Console.OutputEncoding.GetString(bytes));
            //}).BeginInvoke(null, null);

            Console.WriteLine(msg);
        }

        


        static public void HoldProcess() {
            var processName = Process.GetCurrentProcess().ProcessName;
            var defColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("The {0} is ready", processName);
            Console.WriteLine("Press <Enter> to terminate {0}", processName);

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
