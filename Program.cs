using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;


namespace VstsToGit
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            const string c_collectionUri = "https://<your TFS url here>/DefaultCollection";

            // Interactively ask the user for credentials, caching them so the user isn't constantly prompted
            string accessToken = "<your personal access token here>";
            VssBasicCredential creds = new VssBasicCredential("", accessToken);
            VssConnection connection = new VssConnection(new Uri(c_collectionUri), creds);

            //connection.GetClient<repositories>()
            var gitclient = connection.GetClient<GitHttpClient>();

            var tfvcSourcePath = "$/<your project name here>/<main branch name here>";
            var destinationTeamProject = "<destination project name here>";
            var newrepo = "<your git repo name to be created here>";

            //creation of the Git Repo + import operation
            var request = CreateGitImportRequest(tfvcSourcePath);
            var repo = ExecuteImportRequest(gitclient, newrepo, destinationTeamProject, request);

        }

        private static GitRepository ExecuteImportRequest(GitHttpClient gitclient, string newrepo, string destinationTeamProject,
            GitImportRequest request)
        {
            var creationResult = gitclient.CreateRepositoryAsync(
                    new GitRepository()
                    {
                        Name = newrepo
                    },
                    destinationTeamProject)
                .Result;
            var executedrequest = gitclient
                .CreateImportRequestAsync(request, destinationTeamProject, newrepo).Result;

            var importatstus = gitclient
                .GetImportRequestAsync(destinationTeamProject, creationResult.Id, executedrequest.ImportRequestId).Result;
            var currentindex = -1;
            do
            {
                if (currentindex != importatstus.DetailedStatus.CurrentStep - 1)
                {
                    currentindex = importatstus.DetailedStatus.CurrentStep - 1;
                    Console.WriteLine(importatstus.DetailedStatus.AllSteps.ElementAt(currentindex));
                }
                else
                {
                    Thread.Sleep(1000);
                    Console.Write(".");
                }

                importatstus = gitclient
                    .GetImportRequestAsync(destinationTeamProject, creationResult.Id, executedrequest.ImportRequestId).Result;
            } while (importatstus.Status == GitAsyncOperationStatus.Queued ||
                     importatstus.Status == GitAsyncOperationStatus.InProgress);

            if (importatstus.Status == GitAsyncOperationStatus.Failed ||
                importatstus.Status == GitAsyncOperationStatus.Abandoned)
            {
                Console.WriteLine("oops, something went wrong");
            }
            Console.WriteLine("Your import has completed!");
            return gitclient.GetRepositoryAsync(creationResult.Id).Result;

        }

        private static GitImportRequest CreateGitImportRequest(string tfvcSourcePath)
        {
            GitImportRequest request = new GitImportRequest()
            {
                Parameters = new GitImportRequestParameters()
                {
                    TfvcSource = new GitImportTfvcSource()
                    {
                        ImportHistory = true,
                        ImportHistoryDurationInDays = 180,
                        Path = tfvcSourcePath
                    }
                }
            };
            return request;
        }


    }
}
