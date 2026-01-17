using System;

namespace CaseManager.BusinessLogic.Domain.Exceptions
{
    public class ConfictOfInterestDeclarationException : Exception
    {
        public ConfictOfInterestDeclarationException()
            : base("You have already declared conflict of interest for this case.")
        {
        }
    }
}
