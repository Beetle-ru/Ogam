using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ogam {
    public class Continuation {
        public Stack<object> Stack;

        public Continuation(Stack<object> stack) {
            Stack = new Stack<object>(stack);
        }

        public Stack<object> GetStack() {
            return new Stack<object>(Stack);
        }
    }
}
