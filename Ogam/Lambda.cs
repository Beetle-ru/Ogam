using System;
using System.Collections.Generic;

namespace Ogam {
	public class Lambda {
	    public string Id;
        public Symbol[] ArgumentNames;
        public object[] SubProgramm;
		public EnviromentFrame Closure;

		public Lambda(Symbol[] argumentNames, object[] subProgramm, EnviromentFrame closure) {
            ArgumentNames = argumentNames;
		    SubProgramm = subProgramm;
            Closure = closure;
		}

		public Lambda() {
            ArgumentNames = new Symbol[0];
		    SubProgramm = new object[0];
            Closure = new EnviromentFrame();
		}
	}
}

