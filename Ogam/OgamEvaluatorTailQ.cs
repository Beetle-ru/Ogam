using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Ogam {
    public class OgamEvaluatorTailQ : IEvaluator {
        private Pair _env;

        public OgamEvaluatorTailQ() {
            _env = new Pair(new Pair("#nil".ToSymbol(), null));
            InitBase();
        }

        private void InitBase() {
            BaseOperations.ExtedEvaluator(this);
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


        private object Eval(object expression, ref Pair enviroment) {
            var queue = new Queue<QueueFrame>();
            queue.Enqueue(new QueueFrame(expression));
            Pair env = enviroment;

            object val = null;


            while (queue.Any()) {
                var qf = queue.Dequeue();

                if (qf.IsEnviroment()) {
                    // enviroment restoration
                    env = qf.GetEnviroment();
                    enviroment = env;


                    continue;
                }

                var exp = qf.GetExpression();

                if (IsSelfEvaluating(exp)) {
                    val = exp;
                    //goto done;
                }
                else if (IsVariable(exp)) {
                    val = LookupVariable(exp as Symbol, env);
                    //goto done;
                }
                else if (IsPair(exp)) {
                    switch (Operator(exp as Pair).Name) {
                        case "quote":
                            val = Car(Cdr(exp));
                            break;
                        case "if":
                            var arguments = Operands(exp as Pair);
                            if (arguments == null) return null;
                            var cond = Eval(arguments.Car, ref env);

                            var chrCond = Convert.ToBoolean(cond);
                            if (chrCond == true) {
                                var cndr = Car(Cdr(arguments)); // as Pair;

                                var items = queue.ToArray();
                                queue.Clear();

                                queue.Enqueue(new QueueFrame(cndr));

                                foreach (var item in items)
                                    queue.Enqueue(item);
                            }
                            else {
                                var cndr = Car(Cdr(Cdr(arguments))); // as Pair;
                                var items = queue.ToArray();
                                queue.Clear();

                                queue.Enqueue(new QueueFrame(cndr));

                                foreach (var item in items)
                                    queue.Enqueue(item);
                            }

                            break;
                        case "begin":
                            var cndr2 = Operands(exp as Pair);

                            var items2 = queue.ToArray();
                            queue.Clear();

                            while (cndr2 != null) {
                                queue.Enqueue(new QueueFrame(cndr2.Car));

                                cndr2 = cndr2.Cdr as Pair;
                            }

                            queue.Enqueue(new QueueFrame(env, true));

                            foreach (var item in items2)
                                queue.Enqueue(item);

                            break;
                        case "def":
                            enviroment = EvDefine(Operands(exp as Pair), env);
                            env = enviroment;
                            val = null;
                            break;
                        case "set":
                            val = EvSet(Operands(exp as Pair), env);
                            break;
                        case "lambda":
                            val = MakeLambda(Operands(exp as Pair), env);
                            break;
                        case "load":
                            val = Load(Operands(exp as Pair), env);
                            break;

                        default: // APPLY

                            var oper = Car(exp);
                            var unev = Operands(exp as Pair);
                            ///////////////////////////////////////

                            var function = Eval(oper, ref env);
                            var call = function as Call;
                            var lambda = function as Lambda;

                            if ((call == null) && (lambda == null)) {
                                throw new Exception(
                                    string.Format(
                                        "EVALUATOR:Eval{0}The symbol \"{1}\" is not callable type is a {2} object",
                                        Environment.NewLine, oper, function != null ? function.GetType() : null));
                                val = null;
                            }
                            else {
                                var argVals = Eprogn(unev, env);

                                if (call != null) {
                                    val = call.Invoke(argVals);
                                }
                                else {
                                    var argNames = lambda.Arguments;
                                    var newEnv = new Pair();
                                    Pair lastCellOfNewEnv = null;
                                    while ((argNames != null) && (argVals != null)) {
                                        if (argNames.Car != null) {
                                            lastCellOfNewEnv = newEnv.Add(new Pair(argNames.Car, argVals.Car));
                                        }

                                        argVals = argVals.Cdr as Pair;
                                        argNames = argNames.Cdr as Pair;
                                    }

                                    if (lastCellOfNewEnv != null) {
                                        lastCellOfNewEnv.Cdr = lambda.Enviroment; // is a lexic link
                                    } else {
                                        newEnv = lambda.Enviroment;
                                    }

                                    var cndr = lambda.Expression;

                                    var items = queue.ToArray();
                                    queue.Clear();

                                    while (cndr != null) {
                                        queue.Enqueue(new QueueFrame(Car(cndr)));

                                        cndr = cndr.Cdr as Pair;
                                    }

                                    queue.Enqueue(new QueueFrame(env, true));

                                    env = newEnv;

                                    foreach (var item in items) queue.Enqueue(item);
                                }
                            }

                            ///////////////////////////////////////
                            break;
                    }
                }
            }
            return val;
        }

        private object Load(Pair arguments, Pair env) {
            if (arguments == null)
                return null;

            object result = null;
            var cndr = arguments;
            var i = 0;

            while (cndr != null) {
                var exp = cndr.Car;
                if (exp != null) {
                    result = Eval(exp, ref env);

                    var fileName = (string) result;
                    if (!string.IsNullOrWhiteSpace(fileName)) {
                        if (File.Exists(fileName)) {
                            var contant = File.ReadAllText(fileName);
                            result = Eval(contant);
                        }
                        else {
                            throw new Exception(string.Format("EVALUATOR:Load{0}file not found: \"{1}\"", Environment.NewLine, fileName));
                        }
                    }
                    else {
                        throw new Exception(string.Format("EVALUATOR:Load{0}argument {1} is not a string", Environment.NewLine, i));
                    }
                }
                
                i++;
                cndr = cndr.Cdr as Pair;
            }

            return result;
        }


        private bool UpdateValue(Symbol name, object value, Pair env) {
            var cndr = env;
            while (cndr != null) {
                var pair = cndr.Car as Pair;

                if (pair != null) {
                    var key = pair.Car as Symbol;

                    if (key != null) {
                        if (key == name) {
                            pair.Cdr = value;
                            return true;
                        }
                    }
                }

                cndr = cndr.Cdr as Pair;
            }

            return false;
        }

        private object MakeLambda(Pair arguments, Pair env) {
            var argNames = Car(arguments) as Pair;

            var expression = Cdr(arguments) as Pair;

            return new Lambda(argNames, expression, env);
        }

        private Pair EvDefine(Pair arguments, Pair env) {
            if (arguments == null)
                return null;

            var name = ((Car(arguments) is Symbol) ? Car(arguments) : Car(Car(arguments))) as Symbol;

            if (name == null)
                throw new Exception(string.Format("EVALUATOR:EvDefine{0}Bad format : {1}", Environment.NewLine, name));

            if (VariableIsExist(name, env)) {
                throw new Exception(string.Format("EVALUATOR:EvDefine{0}Already defined : {1}", Environment.NewLine, name));
            }

            var memCell = new Pair(name);
            object value = null;

            //Create new enviroment
            var newEnv = new Pair(memCell);
            newEnv.Cdr = env;

            if (Car(arguments) is Symbol) { // variable
                value = Eval(Car(Cdr(arguments)), ref env);
            }

            if (Car(arguments) is Pair) { // function
                arguments.Car = Cdr(Car(arguments));
                value = MakeLambda(arguments, newEnv);
            }

            memCell.Cdr = value;

            return newEnv;
        }


        private object EvSet(Pair arguments, Pair env) {
            if (arguments == null)
                return null;

            var name = Car(arguments) as Symbol;
            var value = Eval(Car(Cdr(arguments)), ref env);

            if (name == null) return null;

            var cndr = env;
            while (cndr != null) {
                var nameEnv = Car(Car(cndr)) as Symbol;

                if (nameEnv != null) {
                    if (nameEnv == name) {
                        var p = cndr.Car as Pair;
                        p.Cdr = value;
                    }
                }

                cndr = cndr.Cdr as Pair;
            }

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


        private Pair Eprogn(Pair arguments, Pair env) {
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

        public void Extend(string symbol, Call call) {
            var identifier = symbol.ToSymbol();

            if (VariableIsExist(identifier, _env)) {
                UpdateValue(identifier, call, _env);
            }
            else {
                _env.Add(new Pair(identifier, call));
            }
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

        private object LookupVariable(Symbol symbol, Pair env) {
            var cndr = env;
            while (cndr != null) {
                var pair = cndr.Car as Pair;
                if (pair != null) {
                    var key = pair.Car as Symbol;
                    if (key != null) {
                        if (key == symbol) {
                            return pair.Cdr;
                        }
                    }
                }

                cndr = cndr.Cdr as Pair;
            }

            throw new Exception(string.Format("EVALUATOR:LookupVariable{0}\"{1}\" undefined", Environment.NewLine,
                symbol));

            return null;
        }

        private bool VariableIsExist(Symbol symbol, Pair env) {
            var cndr = env;
            while (cndr != null) {
                var pair = cndr.Car as Pair;
                if (pair != null) {
                    var key = pair.Car as Symbol;
                    if (key != null) {
                        if (key == symbol) {
                            return true;
                        }
                    }
                }

                cndr = cndr.Cdr as Pair;
            }

            return false;
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