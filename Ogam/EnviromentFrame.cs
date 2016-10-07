using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ogam {
    public class EnviromentFrame {
        public EnviromentFrame Parent;

        public Dictionary<string, object> Variables;

        public EnviromentFrame() {
            Variables = new Dictionary<string, object>();
        }

        public EnviromentFrame(EnviromentFrame parent) {
            Parent = parent;
            Variables = new Dictionary<string, object>();
        }

        public void Define(Symbol ident, object value) {
            Variables[ident.Name] = value;
        }

        public void Set(Symbol ident, object value) {
            if (Variables.ContainsKey(ident.Name)) {
                Variables[ident.Name] = value;
                return;
            }

            if (Parent == null) throw new Exception($"Undefined \"{ident.Name}\"");

            Parent.Set(ident, value);
        }

        public object Get(Symbol ident) {
            if (Variables.ContainsKey(ident.Name)) return Variables[ident.Name];

            if (Parent == null) throw new Exception($"Undefined \"{ident.Name}\"");

            return Parent.Get(ident);
        }
    }
}
