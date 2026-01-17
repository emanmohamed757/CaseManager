using System;

namespace CaseManager.BusinessLogic.Domain.Exceptions
{
    public class InvalidStatusException : Exception
    {
        public InvalidStatusException()
            : base("Case cannot be changed to the requested status. Please refresh the page and try again.")
        {
        }
    }
}
