using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.HR;
using CaseManager.BusinessLogic.Domain.Enums;
using Serilog;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace CaseManager.BusinessLogic.Domain.Services
{
    public class HRService
    {
        private UserContext _userContext;
        private ILogger _logger;
        private IDbContextFactory<HRDbContext> _hrDbContextFactory;

        public HRService(
            UserContext userContext,
            ILogger logger,
            IDbContextFactory<HRDbContext> hrDbContextFactory)
        {
            _userContext = userContext;
            _logger = logger.ForContext<CaseService>();
            _hrDbContextFactory = hrDbContextFactory;
        }

        public List<Department> GetDepartments()
        {
            using (var dbContext = _hrDbContextFactory.Create())
            {
                return dbContext.Departments.ToList();
            }
        }

        public List<Employee> GetStaff(DesignationOption designation)
        {
            using (var dbContext = _hrDbContextFactory.Create())
            {
                return dbContext.Employees
                    .Where(employee => employee.DesignationId == (int)designation)
                    .ToList();
            }
        }
    }
}
