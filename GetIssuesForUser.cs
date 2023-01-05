using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;

namespace groverale.Function
{
    public static class GetIssuesForUser
    {
        [FunctionName("GetIssuesForUser")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string jiraEmail = req.Query["jiraEmail"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            jiraEmail = jiraEmail ?? data?.jiraEmail;

            try
            {
                // Call the API
                var client = JiraHelpers.InitHTTPClient();

                // Issues Assigned to user
                var issuesAssignedToUser = await GetIssues(client, jiraEmail, log, assignedIssues: true);
                if (issuesAssignedToUser == null)
                {
                    return new BadRequestObjectResult($"Error from Jira, check Function logs");
                }
                var openIssuesCount = issuesAssignedToUser.Where(i => !i.Resolved).ToList().Count;

                // Issues Reported by user
                var issuesReportedByUser = await GetIssues(client, jiraEmail, log, assignedIssues: false);
                if (issuesReportedByUser == null)
                {
                    return new BadRequestObjectResult($"Error from Jira, check Function logs");
                }

                return new OkObjectResult(new ACEResponse { OpenIssueCount = openIssuesCount, Assigned = issuesAssignedToUser, Reported = issuesReportedByUser});
            }
            catch (Exception ex)
            {
                // return error
                return new BadRequestObjectResult($"Error in request: {ex.Message}");
            }
        }


        public static async Task<List<JiraIssue>> GetIssues(HttpClient client, string userEmail, ILogger log, bool assignedIssues = true)
        {
            string jqlField = "assignee";

            if (!assignedIssues)
            {
                jqlField = "reporter";
            }

            var issuesRequest = await client.GetAsync($"/rest/api/3/search?jql={jqlField}='{userEmail}'");

            var issueReponseData = await JiraHelpers.ReadResposneData(issuesRequest);

            if (JiraHelpers.ContainsKey(issueReponseData, "warningMessages"))
            {
                foreach (var error in issueReponseData.warningMessages)
                {
                    log.LogInformation($"Jira Error: {error}");
                }

                return null;
            }

            List<JiraIssue> jIssuesAssignedToUser = new List<JiraIssue>();

            foreach (var issue in issueReponseData.issues)
            {
                JiraIssue jIssue = new JiraIssue 
                {
                    Id = issue?.id,
                    Key = issue?.key,
                    Created = issue?.fields?.created,
                    Title = issue?.fields?.summary,
                    Description = string.Empty,
                    StatusText = issue?.fields?.status?.name,
                    StatusCategoryKey = issue?.fields?.status?.statusCategory?.key
                };

                if (issue?.fields?.resolution != null)
                {
                    jIssue.Resolved = true;
                }

                if (issue?.fields?.dueDate != null)
                {
                    jIssue.DueDate = DateTime.Parse(issue?.fields?.dueDate); 

                    // overdue
                    if (DateTime.Now > jIssue.DueDate)  
                    {
                        jIssue.OverDueDays = (DateTime.Now - jIssue.DueDate).Days;
                    }             
                }

                JiraProject jProject = new JiraProject
                {
                    Id = issue?.fields?.project?.id,
                    Key = issue?.fields?.project?.key, 
                    Name = issue?.fields?.project?.name,
                    AvatarUri = issue?.fields?.project?.avatarUrls["24x24"]
                };
                jIssue.Project = jProject;

                // User info
                JiraUser assignee = new JiraUser
                {
                    Name = issue?.fields?.assignee?.displayName,
                    AvatarUri = issue?.fields?.assignee?.avatarUrls["24x24"],
                    TimeZone = issue?.fields?.assignee?.timeZone,
                };
                if (assignedIssues)
                {
                    assignee.Email = userEmail;
                }
                jIssue.Assignee = assignee;

                JiraUser reporter = new JiraUser
                {
                    Name = issue?.fields?.reporter?.displayName,
                    AvatarUri = issue?.fields?.reporter?.avatarUrls["24x24"],
                    Email = issue?.fields?.reporter?.emailAddress,
                    TimeZone = issue?.fields?.reporter?.timeZone,
                };
                jIssue.Reporter = reporter;

                jIssuesAssignedToUser.Add(jIssue);
            }

            return jIssuesAssignedToUser;
        }
    }
}
