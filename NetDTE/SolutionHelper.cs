using System;
using System.Collections.Generic;
using EnvDTE;

namespace NetDTE
{
    public enum SearchType
    {
        FullPath,
        Filename
    }

    internal class SolutionHelper
    {
        public static ProjectItem FindSolutionItemByName(DTE dte, string name, bool recursive, SearchType searchType = SearchType.FullPath)
        {
            ProjectItem projectItem = null;
            foreach (Project project in dte.Solution.Projects)
            {
                projectItem = FindProjectItemInProject(project, name, recursive, searchType);

                if (projectItem != null)
                {
                    break;
                }
            }
            return projectItem;
        }

        public static ProjectItem FindProjectItemInProject(Project project, string name, bool recursive, SearchType searchType = SearchType.FullPath)
        {
            ProjectItem projectItem = null;

            if (project.Kind != Constants.vsProjectKindSolutionItems)
            {
                if (project.ProjectItems != null && project.ProjectItems.Count > 0)
                {
                    return SearchProjectItems(project.ProjectItems, name, recursive, searchType);
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
                        projectItem = FindProjectItemInProject(realProject, name, recursive, searchType);

                        if (projectItem != null)
                        {
                            break;
                        }
                    }
                }
            }

            return projectItem;
        }

        public static ProjectItem SearchProjectItems(EnvDTE.ProjectItems projectItems, string name, bool recursive, SearchType searchType)
        {
            Func<ProjectItem, bool> testPath = item =>
            {
                //var itemName = $"{item.Name}";
                //var properties = new Dictionary<string, object>();

                //foreach (Property prop in item.Properties)
                //{
                //    properties.Add(prop.Name, prop.Value);
                //}

                string searchValue = searchType == SearchType.FullPath
                    ? (string)item.Properties.Item("FullPath").Value
                    : item.Name;

                return name.Equals(searchValue, StringComparison.OrdinalIgnoreCase);
            };

            ProjectItem foundProjectItem = null;

            foreach (ProjectItem item in projectItems)
            {               
                if (item.Kind == Constants.vsProjectItemKindPhysicalFolder)
                {
                    if (testPath(item))
                        foundProjectItem = item;
                    else if (recursive)
                        foundProjectItem = SearchProjectItems(item.ProjectItems, name, recursive, searchType);
                }
                else if (item.Kind == Constants.vsProjectItemKindPhysicalFile)
                {
                    if (testPath(item))
                        foundProjectItem = item;
                }

                if (foundProjectItem != null)
                    break;
            }

            return foundProjectItem;
        }
    }
}
