﻿using System;
using System.Collections.Generic;

namespace Ogam {
    public static class StingExtension {
        //private static IEvaluator Evaluator = new OgamEvaluator();
        public static readonly IEvaluator Evaluator = new OgamEvaluatorTailQ();

        public static Symbol ToSymbol(this string s) {
            return new Symbol(s);
        }

        public static object OgamEval(this string expr, params object[] args) {
            return expr.OgamEval(Evaluator, args);
        }

        public static void OgamExtend(this string name, Call call) {
            name.OgamExtend(Evaluator, call);
        }

        public static void OgamExtend(this string name, IEvaluator evaluator, Call call) {
            name = name.Trim();

            if (name.StartsWith("(") || name.EndsWith(")")) {
                throw new Exception(String.Format("BINDING ERROR: uncorrect name \"{0}\"", name));
            }

            lock (evaluator) {
                evaluator.Extend(name, call);
            }
        }

        public static object OgamEval(this string expr, IEvaluator evaluator, params object[] args) {
            //lock (evaluator) {
                return evaluator.Eval(String.Format(expr, O2Strings(args)));
            //}
        }

        private static object[] O2Strings(object[] args) {
            var strs = new List<object>();
            foreach (var o in args) {
                if (IsBaseType(o)) {
                    strs.Add(string.Format(" {0}", o));
                } else if (o is string) {
                    var str = (string) o;
                    str = str.Replace("\"", "\\\"");
                    strs.Add(string.Format(" \"{0}\"", str));
                } else if (o is bool) {
                    strs.Add(string.Format(" {0}", ((bool)o ? "#t" : "#f")));
                } else {
                    //strs.Add(string.Format(" \"{0}\"", Pack(o)));
                    strs.Add(string.Format(" \"{0}\"", o.ToString()));
                }
            }

            return strs.ToArray();
        }
        private static bool IsBaseType(object o) {
            return (o is byte)
                   || (o is sbyte)
                   || (o is char)
                   || (o is decimal)
                   || (o is double)
                   || (o is float)
                   || (o is int)
                   || (o is uint)
                   || (o is long)
                   || (o is ulong)
                   || (o is short)
                   || (o is ushort);
        }
    }
}