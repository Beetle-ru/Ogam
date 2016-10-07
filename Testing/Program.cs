using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using Ogam;

namespace Testing {
    class Program {
        static void Main(string[] args) { // TODO
            //"(define (loop step expr) (display step) (if (> step 0) (begin (set! step (- step 1)) (expr) (loop step expr))))".OgamEval();
            //"(loop 10 (lambda () #t))".OgamEval();

            //"(define (l) #t (l))(l)".OgamEval();

            Base();
            IntrnalState();
            ThreadSafe();

            Console.WriteLine("Time meterings...");
            var maxI = 10000000;
            foreach (var timeMeteringExpression in TimeMeteringExpressions()) {
                var watcher = new System.Diagnostics.Stopwatch();
                watcher.Start();
                for (var i = 0; i < maxI; i++) {
                    timeMeteringExpression.OgamEval();
                }
                watcher.Stop();

                Console.WriteLine(watcher.Elapsed);
            }

            Console.WriteLine("successful");

            Console.ReadLine();
        }

        static IEnumerable<string> TimeMeteringExpressions() {
            "(display \"Metering a simple call...\")".OgamEval();
            yield return "(+ 1 2 3)";

            "(display \"Metering a function call...\")".OgamEval();
            "(define (some-function a b) #f)".OgamEval();
            yield return "(some-function 11 33)";

            "(display \"Metering a lamda create...\")".OgamEval();
            yield return "(lambda (a b c) (+ 1 2 3))";
        }

        static bool IntrnalState() {
            "(define (make-obj) (define message \"Init value...ok\" )(define (dispatch op) (if (= op 'test) (begin (display message) (newline)) (begin (set! message \"Test bad call...ok\") (display  \"Unknown operation: \") (display op) (display \"...ok\") (newline)))) dispatch)".OgamEval();

            "(define obj (make-obj))".OgamEval();

            "(obj 'test)".OgamEval();

            "(obj 'SomeOperation)".OgamEval();

            "(obj 'test)".OgamEval();

            "(define x 0) (define y 1)".OgamEval();
            "(display \"Init values: \")(display x) (display \" - \") (display y) (newline)".OgamEval();
            "(define (context y) (define x 3311) (display \"Overload values: \") (display x) (display \" - \") (display y) (newline))".OgamEval();
            "(context 1133)".OgamEval();
            "(display \"Recovery values: \")(display x) (display \" - \") (display y) (newline)".OgamEval();

            return true;
        }

        static bool Base() {
            object result = null;
            result = "(+ 111 222 333)".OgamEval();
            result = "(if #t 1 2)".OgamEval();
            result = "(if #f 1 2)".OgamEval();

            result = "(if #t 1)".OgamEval();
            result = "(if #f 1)".OgamEval();

            result = "\"some string\"".OgamEval();
            result = "132".OgamEval();
            result = "#\\q".OgamEval();
            result = "#\\#".OgamEval();
            result = "#\\'".OgamEval();
            result = "'(1 2 3)".OgamEval();
            result = "'1".OgamEval();
            result = "#(1 2 3)".OgamEval();

            result = "(car '(1 2 3))".OgamEval();
            result = "(cdr '(1 2 3))".OgamEval();

            result = "(set-car! '(1 2 3) 1133)".OgamEval();
            result = "(set-cdr! '(1 2 3) 1133)".OgamEval();

            result = "(read \"(1 2 3 (#t #f) and something else)\")".OgamEval();
            result = "(eval (read \"(+ 1 2 3)\"))".OgamEval();
            result = "(eval (read \"1133\"))".OgamEval();


            "test".OgamExtend(Test);
            result = "(test {0} {1})".OgamEval("qwe\"asd\" \\ ", true);

            "get-date".OgamExtend(GetDate);
            result = "(test (get-date))".OgamEval();
            result = "(- 3 2 1)".OgamEval();

            result = "(define (summ a b) (+ a b)) (summ (call/cc (lambda (cc) (display \"text\")(newline) (cc 1) 100)) 2)".OgamEval();
            result = "(define *env* #f)(call/cc (lambda (cc) (begin(set! *env* cc) (display \"suka\")))) (*env* #t)".OgamEval();

            result = "((lambda () #t))".OgamEval();

            result = "(begin 1 2 3)".OgamEval();

            
            "(newline) (display \"Tail call test...\") (newline)".OgamEval();
            "(define (loop step expr) (display \"iteration: \") (display step) (newline) (if (> step 0) (begin (set! step (- step 1)) (expr) (loop step expr)) \"end of loop\"))".OgamEval();
            result = "(loop 10000 (lambda () #t))".OgamEval();

            return true;
        }

        static object Test(object[] args) {
            return "ok";
        }

        static object GetDate(object[] args) {
            return DateTime.Now;
        }

        static bool ThreadSafe() {
            Console.Write("Test async a function calls...");
            "(define (summ a b) (+ a b))".OgamEval();

            ThreadPool.QueueUserWorkItem(ThreadEva);
            ThreadPool.QueueUserWorkItem(ThreadEva);
            ThreadPool.QueueUserWorkItem(ThreadEva);
            ThreadPool.QueueUserWorkItem(ThreadEva);

            for (var i = 0; i < 5000; i++) {
                Thread.Sleep(1);
            }

            isRun = false;

            Thread.Sleep(1000);

            Console.WriteLine(_isAsyncCallok ? "ok" : "error");

            return true;
        }

        static Random _rnd = new Random();

        private static bool isRun = true;
        private static bool _isAsyncCallok = true;

        static void ThreadEva(object o) {
            while (isRun) {
                var a = _rnd.Next(100000);
                var b = _rnd.Next(100000);
                var res = (int)Math.Round((double)string.Format("(summ {0} {1})", a, b).OgamEval());
                if (res == (a + b)) {
                    //Console.Write(".");
                    continue;
                }
                else {
                    Console.Write("#");
                    _isAsyncCallok = false;
                }

                //System.Threading.Thread.Sleep(1);
            }

        }
    }
}
