using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ogam;

namespace Testing {
    class Program {
        static void Main(string[] args) { // TODO
            Base();
            IntrnalState();
            ThreadSafe();

            Console.WriteLine("successful");

            Console.ReadLine();
        }

        static bool IntrnalState() {
            "(def (make-obj) (def message \"It's work it's fun\" )(def (dispatch op) (if (= op 'test) (begin (writeln message) (writeln \"\")) (begin (set message \"Was bad call\") (writeln  \"Bad operation: \") (writeln op) (writeln \"\")))) dispatch)".OgamEval();

            "(def obj (make-obj))".OgamEval();

            "(obj 'test)".OgamEval();

            "(obj 'suka)".OgamEval();

            "(obj 'test)".OgamEval();

            return true;
        }

        static bool Base() {
            object result = null;
            result = "(+ 111 222 333)".OgamEval();
            result = "(if #t 1 2)".OgamEval();
            result = "(if #f 1 2)".OgamEval();

            result = "\"some string\"".OgamEval();
            result = "132".OgamEval();
            result = "#\\q".OgamEval();
            result = "#\\#".OgamEval();
            result = "#\\'".OgamEval();
            result = "'(1 2 3)".OgamEval();
            result = "'1".OgamEval();
            result = "#(1 2 3)".OgamEval();

            "test".OgamExtend(Test);
            result = "(test {0} {1})".OgamEval("qwe\"asd\" \\ ", true);

            "get-date".OgamExtend(GetDate);
            result = "(test (get-date))".OgamEval();
            result = "(- 3 2 1)".OgamEval();

            return true;
        }

        static object Test(Pair args) {
            return "ok";
        }

        static object GetDate(Pair args) {
            return DateTime.Now;
        }

        static bool ThreadSafe() {
            "(def (summ a b) (+ a b))".OgamEval();

            ThreadPool.QueueUserWorkItem(ThreadEva);
            ThreadPool.QueueUserWorkItem(ThreadEva);
            ThreadPool.QueueUserWorkItem(ThreadEva);
            ThreadPool.QueueUserWorkItem(ThreadEva);

            for (var i = 0; i < 5000; i++) {
                Thread.Sleep(1);
            }

            isRun = false;

            return true;
        }

        static Random _rnd = new Random();

        private static bool isRun = true;

        static void ThreadEva(object o) {
            while (isRun) {
                var a = _rnd.Next(100000);
                var b = _rnd.Next(100000);
                var res = (int)Math.Round((double)string.Format("(summ {0} {1})", a, b).OgamEval());
                if (res == (a + b))
                    Console.Write(".");
                else
                    Console.Write("#");

                //System.Threading.Thread.Sleep(1);
            }

        }
    }
}
