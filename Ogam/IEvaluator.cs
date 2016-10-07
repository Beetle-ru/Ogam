using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ogam {
    public delegate object FunctionCall(object[] arguments);

    public interface IEvaluator {
        object Eval(string line);

        void Extend(string symbol, FunctionCall call);
    }

    public static class ArrayExtension {
        public static object ObjectAt(this object[] lst, int index) {
            if (lst == null) return null;

            if (index > lst.Length || index < 0) return null;

            return lst[index];
        }

        public static string StringAt(this object[] lst, int index) {
            return Convert.ToString(lst.ObjectAt(index));
        }

        public static int IntAt(this object[] lst, int index) {
            return Convert.ToInt32(lst.ObjectAt(index));
        }

        public static double DoubleAt(this object[] lst, int index) {
            return Convert.ToDouble(lst.ObjectAt(index));
        }

        public static bool BoolAt(this object[] lst, int index) {
            return Convert.ToBoolean(lst.ObjectAt(index));
        }
    }
}
