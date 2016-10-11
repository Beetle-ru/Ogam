using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ogam {
    public class ArgV {
        private Stack<object> _argV;

        public ArgV(Stack<object> argV) {
            _argV = new Stack<object>();

            for (int i = argV.Count - 1; i >= 0; i--) {
                _argV.Push(argV.ElementAt(i));
            }
        }

        public Stack<object> GetArgV() {
            return _argV;
        }
    }
}
