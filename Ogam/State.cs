using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ogam {
    public class ArgV {
        //public Stack<object> Stack;
        //public object Exp;
        //public object Val;
        private Stack<object> _argV;
        //public EnviromentFrame Env;

        public ArgV(Stack<object> argV) {
            _argV = new Stack<object>(argV);
        }

        public Stack<object> GetArgV() {
            return _argV;
        }
    }
}
