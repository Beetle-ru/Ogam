using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ogam {
    public static class Core {
        public static void ExtedEvaluator(IEvaluator evaluator) {
            evaluator.Extend("+", Add);
            evaluator.Extend("-", Sub);
            evaluator.Extend("*", Mul);
            evaluator.Extend("//", Div);

            evaluator.Extend("vector", Vector);
            evaluator.Extend("=", Eq);
            evaluator.Extend(">", More);
            evaluator.Extend("<", Less);

            evaluator.Extend("cons", Cons);
            evaluator.Extend("car", Car);
            evaluator.Extend("cdr", Cdr);
            evaluator.Extend("set-car!", SetCar);
            evaluator.Extend("set-cdr!", SetCdr);

            evaluator.Extend("read", Read);


            evaluator.Extend("hold-process", HoldProcess);
            evaluator.Extend("exit", Exit);

            evaluator.Extend("writeln", Writeln);
            evaluator.Extend("display", Display);
            evaluator.Extend("newline", NewLine);

            evaluator.Extend("begin-invoke", BeginInvoke);
        }

        private static object Add(object[] args) {
            var result = args.DoubleAt(0);

            for (var i = 1; i < args.Length; i++) {
                result += args.DoubleAt(i);
            }

            return result;
        }

        private static object Sub(object[] args) {
            var result = args.DoubleAt(0);

            for (var i = 1; i < args.Length; i++) {
                result -= args.DoubleAt(i);
            }

            return result;
        }

        private static object Mul(object[] args) {
            var result = args.DoubleAt(0);

            for (var i = 1; i < args.Length; i++) {
                result *= args.DoubleAt(i);
            }

            return result;
        }

        private static object Div(object[] args) {
            var result = args.DoubleAt(0);

            for (var i = 1; i < args.Length; i++) {
                result /= args.DoubleAt(i);
            }

            return result;
        }

        private static object Vector(object[] arguments) {
            return arguments;
        }

        private static object Eq(object[] args) {
            var obj = args.ObjectAt(0);

            for (var i = 1; i < args.Length; i++) {
                if (!Equals(obj, args.ObjectAt(i))) {
                    return false;
                }
            }

            return true;
        }

        private static object Less(object[] args) {
            var a = args.DoubleAt(0);

            for (var i = 1; i < args.Length; i++) {
                if (!(a < args.DoubleAt(i))) {
                    return false;
                }
            }

            return true;
        }

        private static object Cons(object[] args) {
            return new Pair(args.ObjectAt(0), args.ObjectAt(1));
        }

        private static object Car(object[] args) {
            var pair = args.ObjectAt(0) as Pair;

            return pair?.Car;
        }

        private static object Cdr(object[] args) {
            var pair = args.ObjectAt(0) as Pair;

            return pair?.Cdr;
        }

        private static object SetCar(object[] args) {
            var pair = args.ObjectAt(0) as Pair;
            if (pair == null)
                return null;

            pair.Car = args.ObjectAt(1);

            return pair;
        }

        private static object SetCdr(object[] args) {
            var pair = args.ObjectAt(0) as Pair;
            if (pair == null)
                return null;

            pair.Cdr = args.ObjectAt(1);

            return pair;
        }

        private static object Read(object[] args) {
            var text = args.StringAt(0);
            if (string.IsNullOrWhiteSpace(text)) return null;

            return Pair.GetCarOrNull(Reader.Parse(text));
        }

        private static object More(object[] args) {
            var a = args.DoubleAt(0);

            for (var i = 1; i < args.Length; i++) {
                if (!(a > args.DoubleAt(i))) {
                    return false;
                }
            }

            return true;
        }


        static public object HoldProcess(object[] args) {
            var processName = Process.GetCurrentProcess().ProcessName;
            var defColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("The {0} is ready", processName);
            Console.WriteLine("Press <Enter> to terminate {0}", processName);

            Console.ForegroundColor = defColor;

            Console.ReadLine();

            return null;
        }

        static public object Exit(object[] args) {
            Environment.Exit(0);

            return null;
        }

        static public object Writeln(object[] args) {
            Console.WriteLine(args.ObjectAt(0));
            return null;
        }

        static public object Display(object[] args) {
            foreach (var o in args) {
                Console.Write(o);
            }
            
            return null;
        }

        static public object NewLine(object[] args) {
            Console.WriteLine("");
            return null;
        }

        static object BeginInvoke(object[] args) {
            var funct = args.ObjectAt(0) as FunctionCall;

            if (funct == null) {
                return false;
            }

            funct.BeginInvoke((object[])args.ObjectAt(1), null, null);
            return true;
        }
    }
}
