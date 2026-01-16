using System.Data.Common;

namespace CaseManager.BusinessLogic.Data
{
    public partial class CaseManagerDbContext
    {
        /// <summary>
        /// This is used by Effort library in unit tests.
        /// </summary>
        public CaseManagerDbContext(DbConnection connection)
            : base(connection, true)
        {
        }
    }
}
