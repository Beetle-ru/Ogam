using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ogam {
    public static class BaseOperations {
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
            evaluator.Extend("set-car", SetCar);
            evaluator.Extend("set-cdr", SetCdr);


            evaluator.Extend("hold-process", HoldProcess);
            evaluator.Extend("exit", Exit);

            evaluator.Extend("writeln", Writeln);

            evaluator.Extend("begin-invoke", BeginInvoke);
        }

        private static object Add(Pair args) {
            var result = args.AsDouble();

            while (args.MoveNext() != null) {
                result += args.AsDouble();
            }

            return result;
        }

        private static object Sub(Pair args) {
            var result = args.AsDouble();

            while (args.MoveNext() != null) {
                result -= args.AsDouble();
            }

            return result;
        }

        private static object Mul(Pair args) {
            var result = args.AsDouble();

            while (args.MoveNext() != null) {
                result *= args.AsDouble();
            }

            return result;
        }

        private static object Div(Pair args) {
            var result = args.AsDouble();

            while (args.MoveNext() != null) {
                result /= args.AsDouble();
            }

            return result;
        }

        private static object Vector(Pair arguments) {
            var list = new List<object>();

            var cndr = arguments;

            while (cndr != null) {

                list.Add(cndr.Car);

                cndr = cndr.Cdr as Pair;
            }

            return list.ToArray();
        }

        private static object Eq(Pair args) {
            var obj = args.AsObject();
            while (args.MoveNext() != null) {
                if (!Equals(obj, args.AsObject())) {
                    return false;
                }

                obj = args.AsObject();
            }

            return true;
        }

        private static object Less(Pair args) {
            var a = args.AsDouble();
            while (args.MoveNext() != null) {
                if (!(a < args.AsDouble())) {
                    return false;
                }

                a = args.AsDouble();
            }

            return true;
        }

        private static object Cons(Pair args) {
            return new Pair(args.AsObject(), args.MoveNext().AsObject());
        }

        private static object Car(Pair args) {
            var pair = args.AsObject() as Pair;
            if (pair == null)
                return null;

            return pair.Car;
        }

        private static object Cdr(Pair args) {
            var pair = args.AsObject() as Pair;
            if (pair == null)
                return null;

            return pair.Cdr;
        }

        private static object SetCar(Pair args) {
            var pair = args.AsObject() as Pair;
            if (pair == null)
                return null;

            pair.Car = args.MoveNext().AsObject();

            return pair;
        }

        private static object SetCdr(Pair args) {
            var pair = args.AsObject() as Pair;
            if (pair == null)
                return null;

            pair.Cdr = args.MoveNext().AsObject();

            return pair;
        }

        private static object More(Pair args) {
            var a = args.AsDouble();
            while (args.MoveNext() != null) {
                if (!(a > args.AsDouble())) {
                    return false;
                }

                a = args.AsDouble();
            }

            return true;
        }


        static public object HoldProcess(Pair args) {
            var processName = Process.GetCurrentProcess().ProcessName;
            var defColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("The {0} is ready", processName);
            Console.WriteLine("Press <Enter> to terminate {0}", processName);

            Console.ForegroundColor = defColor;

            Console.ReadLine();

            return null;
        }

        static public object Exit(Pair args) {
            Environment.Exit(0);

            return null;
        }

        static public object Writeln(Pair args) {
            Console.WriteLine(args.AsString());
            return null;
        }

        static object BeginInvoke(Pair args) {
            var funct = args.AsObject() as Call;

            if (funct == null) {
                return false;
            }

            funct.BeginInvoke(args.MoveNext(), null, null);
            return true;
        }
    }
}
