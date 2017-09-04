using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ogam
{
    public interface ILogExternal
    {
        void Error(object message);
        void Error(object message, Exception exception);
    }
}
