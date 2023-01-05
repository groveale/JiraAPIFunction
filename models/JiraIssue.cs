using System.Collections.Generic;

namespace groverale.Function
{
    public class JiraIssue 
    {
        public string Id { get; set; } 
        public string Key { get; set; } 
        public string Created { get; set; } 
        public string Title { get; set; }
        public string Description { get; set; }
        public string StatusText { get; set; }
        public string StatusCategoryKey { get; set; }
        public JiraProject Project  {get; set; }
        public bool Resolved { get; set; }
        public JiraUser Reporter {get;set;}
        public JiraUser Assignee {get;set;}
        public System.DateTime DueDate {get;set;}
    }

    public class JiraProject 
    {
        public string Id { get; set; } 
        public string Key { get; set; } 
        public string Name { get; set; }  
        public string AvatarUri { get; set; }  
    }

    public class JiraUser 
    {
        public string Name { get; set; }  
        public string AvatarUri { get; set; } 
        public string Email { get; set; }  
        public string TimeZone { get; set; }  
    }
    
    public class ACEResponse 
    {
        public int OpenIssueCount {get;set;}
        public List<JiraIssue> Assigned {get;set;}
        public List<JiraIssue> Reported {get;set;}

    }
}