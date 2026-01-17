using System.Data.Common;

namespace CaseManager.BusinessLogic.Data.HR
{
    public partial class HRDbContext 
    {
        public HRDbContext(DbConnection connection)
            : base (connection, false)
        {
        }
    }
}
