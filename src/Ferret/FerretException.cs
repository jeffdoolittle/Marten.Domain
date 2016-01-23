using System;

namespace Ferret
{
    public class FerretException : Exception
    {
        public FerretException(string message)
            : base(message)
        {
        }
    }
}
