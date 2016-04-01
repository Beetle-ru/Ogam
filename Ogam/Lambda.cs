using System;

namespace Ogam {
	public class Lambda {
	    public string Id;
		public Pair Arguments;
		public Pair Expression;
		public Pair Enviroment;

		public Lambda(Pair arguments, Pair expression, Pair enviroment) {
			Arguments = arguments;
			Expression = expression;
			Enviroment = enviroment;
		}

		public Lambda() {
			Arguments = new Pair();
			Expression = new Pair();
			Enviroment = new Pair();
		}
	}
}

