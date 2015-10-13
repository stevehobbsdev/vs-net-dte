using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using EnvDTE;
using Newtonsoft.Json;

namespace NetDTE.Handlers
{
    [RequestHandler("/project/files")]
    class UpdateFilesRequestHandler : RequestHandler
    {
        private readonly IEnumerable<Project> nodeProjects;

        public UpdateFilesRequestHandler(DTE dte)
            : base(dte)
        {
            this.nodeProjects = SolutionHelper.FindNodeProjects(dte).ToList();
        }

        public override void HandleRequest(HttpListenerContext context)
        {
            int filesAdded = 0;

            if (context.Request.HttpMethod == "PUT" || context.Request.HttpMethod == "POST")
            {
                using (var reader = new StreamReader(context.Request.InputStream))
                {
                    var json = reader.ReadToEnd();
                    var files = JsonConvert.DeserializeObject<string[]>(json).ToList();

                    if (files.Any())
                    {
                        // Assume for now that all the files being changed are in the same project
                        var filePath = $"{Path.GetDirectoryName(files.First())}\\";
                        var projectItem = SolutionHelper.FindProjectItemInProjects(this.nodeProjects, filePath, true);
                        var project = projectItem.ContainingProject;

                        if (projectItem != null)
                        {
                            files.ForEach(f =>
                            {
                                ProjectItems parent = project.ProjectItems;

                                // If this is a css file, find a sass file with the same name and add it as a
                                // child of that.
                                if (Path.GetExtension(f) == ".css")
                                {
                                    var sassPath = $"{Path.Combine(Path.GetDirectoryName(f), Path.GetFileNameWithoutExtension(f))}.scss";
                                    var sassProjectItem = SolutionHelper.FindProjectItemInProject(project, sassPath, true);

                                    if (sassProjectItem != null)
                                        parent = sassProjectItem.ProjectItems;
                                }

                                parent.AddFromFile(f);
                                filesAdded++;
                            });
                            
                        }
                    }
                }
            }

            using(var writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.WriteLine($"NetDTE: Updated or registered {filesAdded} file/s with the project");
            }

            context.Response.ContentType = "text/plain";
            context.Response.OutputStream.Close();
        }
    }
}
