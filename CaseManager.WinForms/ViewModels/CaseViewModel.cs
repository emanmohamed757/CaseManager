using CaseManager.BusinessLogic.Data.CaseManager;
using System.ComponentModel;

namespace CaseManager.WinForms.ViewModels
{
    internal class CaseViewModel
    {
        [Browsable(false)]
        public int Id { get; set; }

        [DisplayName("Case Number")]
        public string CaseNumber { get; set; }

        [Browsable(false)]
        public int StatusId { get; set; }

        [DisplayName("Status Name")]
        public string StatusName { get; set; }

        [Browsable(false)]
        public int DepartmentId { get; set; }

        [DisplayName("Department Name")]
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

        public string DirectorUsername { get; set; }

        public string ManagerUsername { get; set; }

        public string TeamLeaderUsername { get; set; }

        public string TeamAssistantUsername { get; set; }
    }
}
