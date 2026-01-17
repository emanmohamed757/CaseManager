using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Data.HR;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Runtime.Remoting.Contexts;

namespace CaseManager.BusinessLogic.Authorization
{
    public class AuthorizationService
    {
        private readonly IDbContextFactory<HRDbContext> _hrDbContextFactory;
        private readonly IDbContextFactory<CaseManagerDbContext> _caseManagerDbContextFactory;

        public AuthorizationService(
            IDbContextFactory<HRDbContext> hrDbContextFactory,
            IDbContextFactory<CaseManagerDbContext> caseManagerDbContextFactory)
        {
            _hrDbContextFactory = hrDbContextFactory;
            _caseManagerDbContextFactory = caseManagerDbContextFactory;
        }

        public bool Authorize(string username)
        {
            using (var hrDbContext = _hrDbContextFactory.Create())
            {
                return hrDbContext.Employees.Any(employee => employee.Username == username);
            }
        }

        public UserContext GetUserInfo(string username)
        {
            Employee user;
            using (var hrDbContext = _hrDbContextFactory.Create())
            {
                user = hrDbContext.Employees.First(employee => employee.Username == username);
            }

            return new UserContext
            {
                DepartmentId = user.DepartmentId,
                Name = user.Name,
                EffectivePermissions = GetUserRoles(username)
            };
        }

        private List<Permission> GetUserRoles(string username)
        {
            using (var dbContext = _caseManagerDbContextFactory.Create())
            {
                // TODO: Incomplete.
                return dbContext.UserRoles
                    .Where(ur => ur.Username == username)
                    .SelectMany(ur => ur.Role.Permissions)
                    .Where(p => !p.IsDeleted)
                    .Distinct()
                    .ToList();
            }
        }
    }
}
