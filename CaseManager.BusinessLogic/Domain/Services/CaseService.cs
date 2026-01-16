using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data;
using CaseManager.BusinessLogic.Enums;
using CaseManager.BusinessLogic.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaseManager.BusinessLogic.Services
{
    public class CaseService
    {
        // TODO: Is it okay to inject this in the WinForms scenario?
        private readonly CaseManagerDbContext _dbContext;

        private readonly ILogger _logger;

        private readonly UserContext _userContext;

        public CaseService(CaseManagerDbContext dbContext, ILogger logger, UserContext userContext)
        {
            _dbContext = dbContext;
            _logger = logger;
            _userContext = userContext;
        }

        // TODO: Should the presentation layer pass a DTO instead of the data/domain model itself? For now I am passing the data/domain model. Btw, is it "data" or "domain" model?
        public void CreateCase(Case @case)
        {
            // Initial status is "Proposed".
            @case.StatusId = (int)CaseStatusOption.Proposed;

            SetAuditProperties(@case);

            _dbContext.Cases.Add(@case);
            _dbContext.SaveChanges();

            _logger.LogEvent("Case created.");
        }

        public void ApproveCase(int caseId)
        {
            Case @case = _dbContext.Cases.Find(caseId);
            @case.StatusId = (int)CaseStatusOption.Approved;
            _dbContext.SaveChanges();

            _logger.LogEvent($"Case (Id: {caseId}) approved.");
        }

        public List<Case> GetUnassignedCases()
        {
            IQueryable<Case> unassignedCasesQuery = _dbContext.Cases
                .Where(@case => @case.StatusId == (int)CaseStatusOption.Proposed
                    || @case.StatusId == (int)CaseStatusOption.Approved);

            bool canUserViewAllCasesInTheirDepartment =
                _userContext.HasPermission((int)PermissionOption.ViewAllUnassignedCasesInDepartment);

            if (canUserViewAllCasesInTheirDepartment)
            {
                // Filter by department of user.
                return unassignedCasesQuery
                    .Where(@case => @case.DepartmentId == _userContext.DepartmentId)
                    .ToList();
            }
            else
            {
                // Filter by created by user.
                return unassignedCasesQuery
                    .Where(@case => @case.CreatedBy == _userContext.Username)
                    .ToList();
            }
        }

        /// <summary>
        /// Audit properties are properties like created at, created by.
        /// </summary>
        private void SetAuditProperties(Case @case)
        {
            @case.CreatedAt = DateTime.Today;
            @case.CreatedBy = "test";
            @case.UpdatedAt = DateTime.Today;
            @case.UpdatedBy = "test";
        }

        public void RejectCase(int id)
        {
            //Case @case = _dbContext
        }
    }
}
