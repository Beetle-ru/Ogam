using System;
using System.Collections.Generic;
using System.Linq;


namespace Ogam {
    public class Evaluator : IEvaluator{
        private EnviromentFrame _env;

        public Evaluator() {
            _env = new EnviromentFrame();
            _env.Define("#nil".ToSymbol(), null);
            _env.Define("#t".ToSymbol(), true);
            _env.Define("#f".ToSymbol(), false);

            InitBase();
        }

        private void InitBase() {
            Core.ExtedEvaluator(this);
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
                //Console.WriteLine(stack.Count);
                exp = stack.Pop();

                if (exp is Controll) {
                    var controll = (Controll)exp;
                    if (controll == Controll.ApendEv) {
                        argV.Push(val);
                        continue;
                    }

                    if (controll == Controll.Apply) {
                        var function = argV.Pop();
                        var call = function as FunctionCall;
                        var lambda = function as Lambda;

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


                                if (!IsEndOfProc(stack)) {
                                    stack.Push(env); // save current enviroment
                                    stack.Push(Controll.EndOfProc);
                                }

                                foreach (var o in lambda.SubProgramm) {
                                    stack.Push(o);
                                }

                                stack.Push(localEnv);
                            }
                        }
                    }

                    if (controll == Controll.Eval) {
                        stack.Push(val);
                    }

                        continue;
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
                        val = Pair.GetCarOrNull(Pair.GetCdrOrNull(exp));
                        break;
                    case "if":
                        EvIf(Operands(exp as Pair), stack, env);
                        break;
                    case "begin":
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
                    case "call/cc": // (call/cc (lambda (cc) (actions))) 
                        stack.Push(new ArgV(argV));
                        argV.Clear();

                        var cont = new Continuation(stack);

                        stack.Push(Controll.Apply);
                        argV.Push(cont);
                        stack.Push(Controll.ApendEv);
                        stack.Push(Operands(exp as Pair).Car);
                        break;
                    case "eval":
                        stack.Push(Controll.Eval);
                        stack.Push(Operands(exp as Pair).Car);
                        break;

                    default: // APPLY

                        if (!IsEndOfProc(stack)) {
                            stack.Push(new ArgV(argV)); // save current arguments
                        }

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
            Apply,
            EndOfProc,
            Eval
        }

        private bool IsEndOfProc(Stack<object> stack) {
            return stack.Any() && stack.Peek() is Controll && ((Controll) stack.Peek() == Controll.EndOfProc);
        }

        private void EvIf(Pair arguments, Stack<object> stack, EnviromentFrame enviroment) { // todo
            if (arguments == null) return;

            var cond = Eval(arguments.Car, ref enviroment);

            var chrCond = Convert.ToBoolean(cond);

            if (chrCond) {
                stack.Push(Pair.GetCarOrNull(Pair.GetCdrOrNull(arguments)));
                return;
            }

            stack.Push(Pair.GetCarOrNull(Pair.GetCdrOrNull(Pair.GetCdrOrNull(arguments))));
        }

        private object MakeLambda(Pair arguments, EnviromentFrame enviroment) {
            var argNames = new LinkedList<Symbol>();

            var cndr = Pair.GetCarOrNull(arguments) as Pair;
            while (cndr != null) {
                var argumentName = Pair.GetCarOrNull(cndr) as Symbol;

                if (argumentName != null) { argNames.AddLast(argumentName);}

                cndr = cndr.Cdr as Pair;
            }


            var subProgramm = new Stack<object>();

            cndr = Pair.GetCdrOrNull(arguments) as Pair;
            while (cndr != null) {
                subProgramm.Push(Pair.GetCarOrNull(cndr));

                cndr = cndr.Cdr as Pair;
            }

            return new Lambda(argNames.ToArray(), subProgramm.ToArray(), enviroment);
		}

        private void EvDefine(Pair arguments, EnviromentFrame env) {
            if (arguments == null)
                return;

            var name = ((Pair.GetCarOrNull(arguments) is Symbol) ? Pair.GetCarOrNull(arguments) : Pair.GetCarOrNull(Pair.GetCarOrNull(arguments))) as Symbol;

            if (name == null)
                throw new Exception(string.Format("EVALUATOR:EvDefine{0}Bad format : {1}", Environment.NewLine, name));

            object value = null;


            env.Define(name, value);

            if (Pair.GetCarOrNull(arguments) is Symbol) { // variable
                env.Set(name, Eval(Pair.GetCarOrNull(Pair.GetCdrOrNull(arguments)), ref env));
            }

            if (Pair.GetCarOrNull(arguments) is Pair) { // function
                arguments.Car = Pair.GetCdrOrNull(Pair.GetCarOrNull(arguments));
                env.Set(name, MakeLambda(arguments, env));
            }
        }

        private object EvSet(Pair arguments, EnviromentFrame env) {
            if (arguments == null)
                return null;

            var name = Pair.GetCarOrNull(arguments) as Symbol;
            var value = Eval(Pair.GetCarOrNull(Pair.GetCdrOrNull(arguments)), ref env);

            if (name == null) return null;

            env.Set(name, value);

            return null;
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

        public void Extend(string symbol, FunctionCall call) {
            _env.Define(symbol.ToSymbol(), call);
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