using System;
using System.Collections.Generic;
using System.Linq;


namespace Ogam {
    public class OgamEvaluator : IEvaluator{
        private Pair _env;

        public OgamEvaluator() {
            _env = new Pair(new Pair("#nil".ToSymbol(), null));
            _env.Add(new Pair("#t".ToSymbol(), true));
            _env.Add(new Pair("#f".ToSymbol(), false));

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

            if (IsSelfEvaluating(expression)) {
                return expression;
            }

            if (IsVariable(expression)) {
                return LookupVariableValue(expression as Symbol, enviroment);
            }

            if (IsPair(expression)) {
                switch (Operator(expression as Pair).Name) {
                    case "quote":
                        return Car(Cdr(expression));
                    case "if":
                        return EvIf(Operands(expression as Pair), enviroment);
                    case "begin":
                        return Begin(Operands(expression as Pair), enviroment);
                    case "var":
                        enviroment = EvDefine(Operands(expression as Pair), enviroment);
                        return null;
                    case "set":
                        return EvSet(Operands(expression as Pair), enviroment);
                    case "lambda":
                        return MakeLambda(Operands(expression as Pair), enviroment);


                    default:
                        return Apply(Car(expression), Operands(expression as Pair), enviroment);
                }
            }

            Console.WriteLine("Unknown statement");

            return null;
        }

        private object EvIf(Pair arguments, Pair enviroment) { // todo
            if (arguments == null) return null;
            var cond = Eval(arguments.Car, ref enviroment);

            var chrCond = Convert.ToBoolean(cond);
             if (chrCond == true) {
                    return Eval(Car(Cdr(arguments)), ref enviroment);
             }

            return Eval(Car(Cdr(Cdr(arguments))), ref enviroment);
        }

		private object MakeLambda(Pair arguments, Pair enviroment) {
			var argNames = Car(arguments) as Pair;
			var expression = new Pair("begin".ToSymbol(), Cdr(arguments));
			return new Lambda(argNames, expression, enviroment);
		}

        private Pair EvDefine(Pair arguments, Pair env) {
            if (arguments == null)
                return null;
            var name = Car(arguments) as Symbol;
            var value = Eval(Car(Cdr(arguments)), ref env);

            var newEnv = new Pair(new Pair(name, value));

            newEnv.Cdr = env;

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

        private object Begin(Pair arguments, Pair env) {
            object result = null;
            var cndr = Eprogn(arguments, env);

            while (cndr != null) {
                result = cndr.Car;

                cndr = cndr.Cdr as Pair;
            }

            return result;
        }


		private Object Apply(object oper, Pair unev, Pair env) {
			var function = Eval(oper, ref env);
			var call = function as Call;
			var lambda = function as Lambda;

			if ((call == null) && (lambda == null)) {
				Console.WriteLine("is not callable type");
				return null;
			}

			var argVals = Eprogn(unev, env);

			if (call != null) {
				return call.Invoke(argVals);
			} else if (lambda != null) {
				var newEnv = new Pair();

				var argNames = lambda.Arguments;
				Pair lastCell = null;
				while ((argNames != null) && (argVals != null)) {
					lastCell = newEnv.Add(new Pair(argNames.Car, argVals.Car));

					argVals = argVals.Cdr as Pair;
					argNames = argNames.Cdr as Pair;
				}

				//lastCell.Cdr = lambda.Enviroment;
                lastCell.Cdr = env;

				return Eval(lambda.Expression, ref newEnv);
			}

			return null;
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
            _env.Add(new Pair(symbol.ToSymbol(), call));
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

        private object LookupVariableValue(Symbol symbol, Pair env) {
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

            Console.WriteLine("Symbol is not exist");

            return null;
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