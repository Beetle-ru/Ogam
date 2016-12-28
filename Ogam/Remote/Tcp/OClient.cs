using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ogam.Remote.Tcp {
    public class OClient {

        public string Host;
        public int Port;

        public List<Flame> OclientPool;

        public OClient(string host, int port) {
            Host = host;
            Port = port;

            OclientPool = new List<Flame>();
        }

        private Flame GetFreeConnection() {
            lock (OclientPool) {
                for (var i = OclientPool.Count - 1; i >= 0; i--) {
                    if (OclientPool[i].IsReadyToDeath && (!OclientPool[i].Client.IsBusy)) {
                        OclientPool[i].Client.Dispose();
                        OclientPool.RemoveAt(i);
                    }
                }

                foreach (var oClient in OclientPool) {
                    if ((!oClient.Client.IsBusy) && (!oClient.IsReadyToDeath)) return oClient;
                }

                var newOClient = new Flame(Host, Port);
                OclientPool.Add(newOClient);

                while (newOClient.Client.IsBusy) {
                    Thread.Sleep(100);
                }

                return newOClient;
            }
        }

        public object MakeCall(string operation, params object[] args) {
            return GetFreeConnection().MakeCall(operation, args);
        }

        public object Call(string cmd) {
            return GetFreeConnection().Call(cmd);
        }

        public class Flame {
            public TcpClientProcess Client;
            private static int timeout = 600;
            private int timeCounter;
            private Timer _tOuTimer;
            public bool IsReadyToDeath = false;

            public Flame(string host, int port) {
                Client = new TcpClientProcess(host, port);

                timeCounter = timeout;

                _tOuTimer = new Timer(state => {
                    if (timeCounter > 0) {
                        timeCounter--;
                    }
                    else {
                        IsReadyToDeath = true;
                        //_tOuTimer.Change(-1, -1);
                        _tOuTimer.Dispose();
                    }
                });
                _tOuTimer.Change(1000, 1000);
            }

            public object MakeCall(string operation, params object[] args) {
                timeCounter = timeout;
                return Client.MakeCall(operation, args);
            }

            public object Call(string cmd) {
                timeCounter = timeout;
                return Client.Call(cmd);
            }
        }
    }
}
