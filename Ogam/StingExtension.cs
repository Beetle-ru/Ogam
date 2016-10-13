using System;
using System.Collections.Generic;

namespace Ogam {
    public static class StingExtension {
        public static IEvaluator Evaluator = new Evaluator();
        //public static IEvaluator Evaluator = new OgamEvaluatorTailQ();
        //public static readonly IEvaluator Evaluator = new OgamCompiler();

        public static Symbol ToSymbol(this string s) {
            return new Symbol(s);
        }

        public static object OgamEval(this string expr, params object[] args) => expr.OgamEval(Evaluator, args);

        public static void OgamExtend(this string name, FunctionCall call) {
            name.OgamExtend(Evaluator, call);
        }

        public static void OgamExtend(this string name, IEvaluator evaluator, FunctionCall call) {
            name = name.Trim();

            if (name.StartsWith("(") || name.EndsWith(")")) {
                throw new Exception(String.Format("BINDING ERROR: uncorrect name \"{0}\"", name));
            }

            lock (evaluator) {
                evaluator.Extend(name, call);
            }
        }

        public static object OgamEval(this string expr, IEvaluator evaluator, params object[] args) {
            if (string.IsNullOrWhiteSpace(expr)) return null;

            return evaluator.Eval(String.Format(expr, O2Strings(args)));
        }

        private static object[] O2Strings(object[] args) {
            var strs = new List<string>();
            foreach (var o in args) {
                strs.Add(Pair.O2String(o));
            }

            return strs.ToArray();
        }
    }
}
