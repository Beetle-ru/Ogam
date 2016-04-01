using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ogam {
    public delegate object Call(Pair arguments);

    public interface IEvaluator {
        object Eval(string line);
        void Extend(string symbol, Call call);
    }
}
