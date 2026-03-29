using CaseManager.BusinessLogic.Data.CaseManager;
using System.ComponentModel;

namespace CaseManager.WinForms.ViewModels
{
    internal class UnassignedCaseViewModel
    {
        [Browsable(false)]
        public int Id { get; set; }

        [DisplayName("Case Number")]
        public string CaseNumber { get; set; }

        [Browsable(false)]
        public int StatusId { get; set; }

        [DisplayName("Status")]
        public string StatusName { get; set; }

        [Browsable(false)]
        public int DepartmentId { get; set; }

        [DisplayName("Department")]
        public string DepartmentName { get; set; }

        [DisplayName("Created By")]
        public string CreatedBy { get; set; }

        [DisplayName("Created At")]
        public System.DateTime CreatedAt { get; set; }

        [DisplayName("Updated By")]
        public string UpdatedBy { get; set; }

        [DisplayName("Updated At")]
        public System.DateTime UpdatedAt { get; set; }

        [Browsable(false)]
        public bool IsDeleted { get; set; }

        //[DisplayName("Director")]
        //public string DirectorUsername { get; set; }

        //[DisplayName("Manager")]
        //public string ManagerUsername { get; set; }

        //[DisplayName("Team Leader")]
        //public string TeamLeaderUsername { get; set; }

        //[DisplayName("Team Assistant")]
        //public string TeamAssistantUsername { get; set; }
    }
}
