using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace NetDTE
{
    internal class SolutionHelper
    {
        public static ProjectItem FindSolutionItemByName(DTE dte, string name, bool recursive)
        {
            ProjectItem projectItem = null;
            foreach (Project project in dte.Solution.Projects)
            {
                projectItem = FindProjectItemInProject(project, name, recursive);

                if (projectItem != null)
                {
                    break;
                }
            }
            return projectItem;
        }
        public static ProjectItem FindProjectItemInProject(Project project, string name, bool recursive)
        {
            ProjectItem projectItem = null;

            if (project.Kind != Constants.vsProjectKindSolutionItems)
            {
                if (project.ProjectItems != null && project.ProjectItems.Count > 0)
                {
                    foreach (ProjectItem item in project.ProjectItems)
                    {
                        var path = (string)item.Properties.Item("FullPath").Value;
                        if (name.Equals(path, StringComparison.OrdinalIgnoreCase))
                        {
                            projectItem = item;
                        }
                    }
                }
            }
            else
            {
                // if solution folder, one of its ProjectItems might be a real project
                foreach (ProjectItem item in project.ProjectItems)
                {
                    Project realProject = item.Object as Project;

                    if (realProject != null)
                    {
                        projectItem = FindProjectItemInProject(realProject, name, recursive);

                        if (projectItem != null)
                        {
                            break;
                        }
                    }
                }
            }

            return projectItem;
        }
    }
}
