using System.Collections.Generic;

namespace Ogam {
    public class Symbol {
        public static List<string> Dictionary = new List<string>();
        public string Name;

        public Symbol() {
        }

        public Symbol(string name) {
            Name = name;
        }

        public override string ToString() {
            return Name;
        }

        public override bool Equals(object obj) {
            var symbol = obj as Symbol;
            if (symbol == null)
                return false;

            return Name == symbol.Name;
        }

        public static bool operator ==(Symbol a, Symbol b) {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(a, b)) {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object) a == null) || ((object) b == null)) {
                return false;
            }

            // Return true if the fields match:
            return a.Name == b.Name;
        }

        public static bool operator !=(Symbol a, Symbol b) {
            return !(a == b);
        }
    }
}