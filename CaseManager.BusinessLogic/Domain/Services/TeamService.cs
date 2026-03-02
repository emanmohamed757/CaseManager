using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using CaseManager.BusinessLogic.Data.HR;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;

namespace CaseManager.BusinessLogic.Domain.Services
{
    public class TeamService
    {
        private readonly IDbContextFactory<CaseManagerDbContext> _caseManagerDbContext;

        private readonly IDbContextFactory<HRDbContext> _hrDbContext;

        public TeamService(
            IDbContextFactory<CaseManagerDbContext> caseManagerDbContext, 
            IDbContextFactory<HRDbContext> hrDbContext)
        {
            _caseManagerDbContext = caseManagerDbContext;
            _hrDbContext = hrDbContext;
        }

        public List<string> GetImmediateSubordinates(string supervisor)
        {
            using (var dbContext = _caseManagerDbContext.Create())
            {
                return dbContext.TeamMembers
                    .Where(member => member.Team.SupervisorUsername == supervisor)
                    .Select(member => member.Username)
                    .ToList();
            }
        }

        public async Task<List<Employee>> GetImmediateSubordinatesWithFullName(string supervisor)
        {
            List<string> subordinates;
            using (var dbContext = _caseManagerDbContext.Create())
            {
                subordinates = await Task.Run(() => 
                    dbContext.TeamMembers
                        .Where(member => member.Team.SupervisorUsername == supervisor)
                        .Select(member => member.Username)
                        .ToList());
            }

            using (var hrDbContext = _hrDbContext.Create())
            {
                return await Task.Run(() =>
                    hrDbContext.Employees
                        .Where(employee => subordinates.Contains(employee.Username))
                        .ToList());
            }
        }
    }
}
