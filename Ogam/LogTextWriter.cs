using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ogam {
    public class LogTextWriter : TextWriter {
        private readonly TextWriter _tw;
        private readonly Queue<string> _msgQueue;

        public LogTextWriter(TextWriter tw) {
            _tw = tw;
            _msgQueue = new Queue<string>();

            var bt = new Thread(() => {
                bool hasSmth = false;
                while (true) {
                    var res = string.Empty;

                    lock (_msgQueue) {
                        hasSmth = _msgQueue.Any();
                        if (hasSmth) {
                            res = _msgQueue.Dequeue();
                        }
                    }

                    if (hasSmth)
                        _tw.WriteLine(res);
                    else
                        Thread.Sleep(500);
                }
            });
            bt.IsBackground = true;
            bt.Start();
        }

        public override Encoding Encoding => _tw.Encoding;

        public static string GetHeader() {
            return
                $"{Environment.NewLine}********** EVENT-{Process.GetCurrentProcess().ProcessName}[{Process.GetCurrentProcess().Id}]-{DateTime.Now:HHmmss:fff} **********{Environment.NewLine}";
        }

        public override void WriteLine(string s) {
            var astr = string.Format(s);
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(string s, object obj0) {
            var astr = string.Format(s, obj0);
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(string s, object obj0, object obj1) {
            var astr = string.Format(s, obj0, obj1);
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(string s, object obj0, object obj1, object obj2) {
            var astr = string.Format(s, obj0, obj1, obj2);
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(string s, params object[] obj) {
            var astr = string.Format(s, obj);
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(char c) {
            var astr = string.Format(c.ToString());
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(char[] buffer) {
            var astr = string.Format(buffer.ToString());
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(bool b) {
            var astr = string.Format(b.ToString());
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(int i) {
            var astr = string.Format(i.ToString());
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(uint i) {
            var astr = string.Format(i.ToString());
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(long l) {
            var astr = string.Format(l.ToString());
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(ulong l) {
            var astr = string.Format(l.ToString());
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(float f) {
            var astr = string.Format(f.ToString());
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(double d) {
            var astr = string.Format(d.ToString());
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(decimal dc) {
            var astr = string.Format(dc.ToString());
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }

        public override void WriteLine(object o) {
            var astr = string.Format(o.ToString());
            lock (_msgQueue) {
                _msgQueue.Enqueue(GetHeader() + astr);
            }
        }
    }
}