using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TemplateParser
{
    class TokenNotFoundException :Exception
    {
        public TokenNotFoundException()
        {

        }

        public TokenNotFoundException(String msg)
            :base(msg)
        {

        }

        public TokenNotFoundException(String msg, Exception inner)
            :base(msg,inner)
        {

        }
    }
}
