using System;
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
                    return SearchProjectItems(project.ProjectItems, name);
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

        public static ProjectItem SearchProjectItems(EnvDTE.ProjectItems projectItems, string name)
        {
            Func<ProjectItem, bool> testPath = item =>
            {
                var path = (string)item.Properties.Item("FullPath").Value;
                return name.Equals(path, StringComparison.OrdinalIgnoreCase);
            };

            ProjectItem foundProjectItem = null;

            foreach (ProjectItem item in projectItems)
            {               
                if (item.Kind == Constants.vsProjectItemKindPhysicalFolder)
                {
                    if (testPath(item))
                        return item;
                         
                    foundProjectItem = SearchProjectItems(item.ProjectItems, name);

                    if (foundProjectItem != null)
                        break;
                }
                else if (item.Kind == Constants.vsProjectItemKindPhysicalFile)
                {
                    if (testPath(item))
                        return item;
                }
            }

            return foundProjectItem;
        }
    }
}
