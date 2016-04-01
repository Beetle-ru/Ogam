using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ogam {
    public class StackFrame {
        public object Expression;
        public Pair Enviroment;
        //public Pair Unev;

        public StackFrame(object exp, Pair env = null) {
            Expression = exp;
            Enviroment = env;
        }
    }
}
