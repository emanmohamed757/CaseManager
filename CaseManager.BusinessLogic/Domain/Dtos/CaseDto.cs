using CaseManager.BusinessLogic.Data.CaseManager;

namespace CaseManager.BusinessLogic.Domain.Dtos
{
    public class CaseDto : Case
    {
        public string DepartmentName { get; set; }
    }
}
