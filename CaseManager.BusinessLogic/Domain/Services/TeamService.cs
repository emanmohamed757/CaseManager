using CaseManager.BusinessLogic.Authorization;
using CaseManager.BusinessLogic.Data.CaseManager;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;

namespace CaseManager.BusinessLogic.Domain.Services
{
    public class TeamService
    {
        private IDbContextFactory<CaseManagerDbContext> _object1;
        private UserContext _object2;

        public TeamService(IDbContextFactory<CaseManagerDbContext> object1, UserContext object2)
        {
            _object1 = object1;
            _object2 = object2;
        }

        public List<string> GetImmediateSubordinates()
        {
            throw new NotImplementedException();
        }
    }
}
