using CaseManager.BusinessLogic.Data;
using CaseManager.BusinessLogic.Data.CaseManager;
using System.Collections.Generic;
using System.Linq;

namespace CaseManager.BusinessLogic.Authorization
{
    // TODO: I do not have a valid reason to have an interface for this. Wouldn't UserContext class do?
    public class UserContext
    {
        public string Username { get; set; }

        public string Name { get; set; }

        public int DepartmentId { get; set; }

        public List<Permission> EffectivePermissions { get; set; }

        public bool HasPermission(int permissionId)
        {
            return EffectivePermissions
                ?.Select(permission => permission.Id == permissionId)
                .Any() ?? false;
        }
    }
}
