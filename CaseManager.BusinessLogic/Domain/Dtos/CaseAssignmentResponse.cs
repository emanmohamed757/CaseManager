namespace CaseManager.BusinessLogic.Domain.Dtos
{
    public class CaseAssignmentResponse
    {
        public string DirectorUsername { get; set; }
        public string ManagerUsername { get; set; }
        public string TeamLeaderUsername { get; set; }
        public string TeamAssistantUsername { get; set; }
    }
}
