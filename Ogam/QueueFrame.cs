using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ogam {
    public class QueueFrame {
        
        private bool _isEnviroment;
        private object _value;

        public QueueFrame(object value, bool isEnviroment = false) {
            _value = value;
            _isEnviroment = isEnviroment;
        }

        public bool IsEnviroment() {
            return _isEnviroment;
        }

        public Pair GetEnviroment() {
            return _value as Pair;
        }

        public object GetExpression() {
            return _value;
        }
    }
}
