using System;
using System.Collections.Generic;
using System.Linq;


namespace Ogam {
    public class OgamEvaluatorTailPro : IEvaluator{
        private EnviromentFrame _env;

        public OgamEvaluatorTailPro() {
            _env = new EnviromentFrame();
            _env.Define("#nil".ToSymbol(), null);
            _env.Define("#t".ToSymbol(), true);
            _env.Define("#f".ToSymbol(), false);

            InitBase();
        }

        private void InitBase() {
            BaseOperationsPro.ExtedEvaluator(this);
        }

        public object Eval(string line) {
            var pr = Reader.Parse(line);
            object val = null;

            var cndr = pr as Pair;
            while (cndr != null) {
                var exp = cndr.Car;

                if (exp != null) {
                    val = Eval(exp, ref _env);
                }

                cndr = cndr.Cdr as Pair;
            }

            return val;
        }

        private object Eval(object expression, ref EnviromentFrame enviroment) {
            var stack = new Stack<object>();
            stack.Push(expression);
            stack.Push(enviroment);

            object exp = null;
            object val = null;
            var argV = new Stack<object>();
            EnviromentFrame env = null;

            while (stack.Any()) {
                Console.WriteLine(stack.Count);
                exp = stack.Pop();

                if (exp is Controll) {
                    var controll = (Controll)exp;
                    if (controll == Controll.ApendEv) {
                        argV.Push(val);
                        continue;
                    }

                    if (controll == Controll.Apply) {
                        var function = argV.Pop();
                        var call = function as CallPro;
                        var lambda = function as LambdaPro;

                        if (function is Continuation) {
                            var argVals = argV.ToArray();

                            stack = (function as Continuation).GetStack(); // recovery stack

                            foreach (var argVal in argVals) {
                                stack.Push(argVal);
                            }

                            continue;
                        }

                        if ((call == null) && (lambda == null)) {
                            Console.WriteLine("is not callable type");
                            val = null;
                        } else {
                            var argVals = argV.ToArray();//EprognPro(unev, env);

                            if (call != null) {
                                val = call.Invoke(argVals);
                            } else if (lambda != null) {
                                var localEnv = new EnviromentFrame(lambda.Closure);

                                if (lambda.ArgumentNames.Length != argVals.Length) {
                                    Console.WriteLine($"*** ERROR: wrong number of arguments for #<lambda> (required {lambda.ArgumentNames.Length}, got {argVals.Length})");
                                    return null;
                                }

                                for (var i = 0; i < lambda.ArgumentNames.Length; i++) {
                                    localEnv.Define(lambda.ArgumentNames[i], argVals[i]);
                                }

                                stack.Push(env); // save current enviroment

                                foreach (var o in lambda.SubProgramm) {
                                    stack.Push(o);
                                }

                                stack.Push(localEnv);
                            }
                        }
                        continue;
                    }
                }

                if (exp is ArgV) {
                    argV = (exp as ArgV).GetArgV();
                    continue;
                }

                if (exp is EnviromentFrame) {
                    env = exp as EnviromentFrame;
                    continue;
                }

                if (IsSelfEvaluating(exp)) {
                    val = exp;
                    continue;
                }

                if (IsVariable(exp)) {
                    val = env.Get(exp as Symbol);
                    continue;
                }

                if (!IsPair(exp)) continue;

                switch (Operator(exp as Pair).Name) {
                    case "quote":
                        val = Car(Cdr(exp));
                        break;
                    case "if":
                        //val = EvIf(Operands(exp as Pair), env);
                        EvIf(Operands(exp as Pair), stack, env);
                        break;
                    case "begin":
                        //val = Begin(Operands(exp as Pair), env);
                        Begin(Operands(exp as Pair), stack, env);
                        break;
                    case "define":
                        EvDefine(Operands(exp as Pair), env);
                        val = null;
                        break;
                    case "set!":
                        val = EvSet(Operands(exp as Pair), env);
                        break;
                    case "lambda":
                        val = MakeLambda(Operands(exp as Pair), env);
                        break;
                    case "call/cc": // (call/cc (lambda (cc) (actions))) // todo
                        stack.Push(new ArgV(argV));
                        argV.Clear();

                        var cont = new Continuation(stack);

                        stack.Push(Controll.Apply);
                        argV.Push(cont);
                        stack.Push(Controll.ApendEv);
                        stack.Push(Operands(exp as Pair).Car);
                        break;

                    default: // APPLY

                        stack.Push(new ArgV(argV)); // save current arguments
                        argV.Clear();

                        stack.Push(Controll.Apply);
                        var cndr = exp as Pair;
                        while (cndr != null) {
                            stack.Push(Controll.ApendEv);
                            stack.Push(cndr.Car);
                            cndr = cndr.Cdr as Pair;
                        }

                        break;
                }
            }
            return val;
        }

        enum Controll {
            ApendEv,
            Apply
        }

        private object EvIf(Pair arguments, EnviromentFrame enviroment) { // todo
            if (arguments == null) return null;
            var cond = Eval(arguments.Car, ref enviroment);

            var chrCond = Convert.ToBoolean(cond);
             if (chrCond == true) {
                    return Eval(Car(Cdr(arguments)), ref enviroment);
             }

            return Eval(Car(Cdr(Cdr(arguments))), ref enviroment);
        }

        //

        private void EvIf(Pair arguments, Stack<object> stack, EnviromentFrame enviroment) { // todo
            if (arguments == null) return;

            var cond = Eval(arguments.Car, ref enviroment);

            var chrCond = Convert.ToBoolean(cond);

            if (chrCond) {
                stack.Push(Car(Cdr(arguments)));
                return;
            }

            stack.Push(Car(Cdr(Cdr(arguments))));
        }

        private object MakeLambda(Pair arguments, EnviromentFrame enviroment) {
            var argNames = new LinkedList<Symbol>();

            var cndr = Car(arguments) as Pair;
            while (cndr != null) {
                var argumentName = Car(cndr) as Symbol;

                if (argumentName != null) { argNames.AddLast(argumentName);}

                cndr = cndr.Cdr as Pair;
            }


            var subProgramm = new Stack<object>();

            cndr = Cdr(arguments) as Pair;
            while (cndr != null) {
                subProgramm.Push(Car(cndr));

                cndr = cndr.Cdr as Pair;
            }


            return new LambdaPro(argNames.ToArray(), subProgramm.ToArray(), enviroment);
		}

        private void EvDefine(Pair arguments, EnviromentFrame env) {
            if (arguments == null)
                return;

            var name = ((Car(arguments) is Symbol) ? Car(arguments) : Car(Car(arguments))) as Symbol;

            if (name == null)
                throw new Exception(string.Format("EVALUATOR:EvDefine{0}Bad format : {1}", Environment.NewLine, name));

            object value = null;


            env.Define(name, value);

            if (Car(arguments) is Symbol) { // variable
                env.Set(name, Eval(Car(Cdr(arguments)), ref env));
            }

            if (Car(arguments) is Pair) { // function
                arguments.Car = Cdr(Car(arguments));
                env.Set(name, MakeLambda(arguments, env));
            }
        }

        private object EvSet(Pair arguments, EnviromentFrame env) {
            if (arguments == null)
                return null;

            var name = Car(arguments) as Symbol;
            var value = Eval(Car(Cdr(arguments)), ref env);

            if (name == null) return null;

            env.Set(name, value);

            return null;
        }

        private object Car(object obj) {
            var pair = obj as Pair;
            if (pair == null) return null;

            return pair.Car;
        }

        private object Cdr(object obj) {
            var pair = obj as Pair;
            if (pair == null)
                return null;

            return pair.Cdr;
        }

        private object Begin(Pair arguments, EnviromentFrame env) {
            object result = null;
            var cndr = Eprogn(arguments, env);

            while (cndr != null) {
                result = cndr.Car;

                cndr = cndr.Cdr as Pair;
            }

            return result;
        }

        private void Begin(Pair arguments, Stack<object> stack, EnviromentFrame env) {
            var cndr = arguments;

            var subProgramm = new Stack<object>();

            while (cndr != null) {
                subProgramm.Push(cndr.Car);
                cndr = cndr.Cdr as Pair;
            }

            while (subProgramm.Any()) {
                stack.Push(subProgramm.Pop());
            }
        }



        private Pair Eprogn(Pair arguments, EnviromentFrame env) {
            if (arguments == null) return null;

            var result = new Pair();
            var cndr = arguments;

            while (cndr != null) {
                var exp = cndr.Car;
                if (exp != null) {
                    result.Add(Eval(exp, ref env));
                }

                cndr = cndr.Cdr as Pair;
            }

            return result;
        }

        private object[] EprognPro(Pair arguments, EnviromentFrame env) {
            if (arguments == null) return null;

            var result = new LinkedList<object>();
            var cndr = arguments;

            while (cndr != null) {
                var exp = cndr.Car;
                if (exp != null) {
                    result.AddLast(Eval(exp, ref env));
                }

                cndr = cndr.Cdr as Pair;
            }

            return result.ToArray();
        }

        public void Extend(string symbol, CallPro call) {
            _env.Define(symbol.ToSymbol(), call);
        }

        public void Extend(string symbol, Call call) {
            throw new NotImplementedException();
        }

        private bool IsSelfEvaluating(object exp) {
            return !((exp is Pair) || (exp is Symbol));
        }

        private bool IsVariable(object exp) {
            return IsSymbol(exp);
        }

        private bool IsSymbol(object exp) {
            return (exp is Symbol);
        }

        private bool IsPair(object exp) {
            return (exp is Pair);
        }


        private Pair Operands(Pair exp) {
            if (exp == null) return new Pair();

            if (exp.Cdr is Pair) {
                return (exp.Cdr as Pair);
            }
            else {
                return new Pair(exp.Cdr);
            }

            return new Pair();
        }

        private Symbol Operator(Pair exp) {
            if (exp == null)
                return new Symbol();

            if (exp.Car is Symbol) {
                return (exp.Car as Symbol);
            }

            return new Symbol();
        }
    }
}