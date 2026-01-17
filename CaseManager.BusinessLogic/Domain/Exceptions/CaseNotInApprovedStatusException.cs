using System;

namespace CaseManager.BusinessLogic.Domain.Exceptions
{
    public class CaseNotInApprovedStatusException : Exception
    {
        public CaseNotInApprovedStatusException()
            : base("The case must be approved before it can be assigned.")
        {
        }
    }
}
