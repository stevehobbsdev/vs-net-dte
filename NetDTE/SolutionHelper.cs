using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;

namespace NetDTE
{
    public enum SearchType
    {
        FullPath,
        Filename
    }

    internal class SolutionHelper
    {
        public static ProjectItem FindSolutionItemByName(DTE2 dte, string name, bool recursive, SearchType searchType = SearchType.FullPath)
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

        /// <summary>
        /// Finds projects that have a package.json file in the root
        /// </summary>
        public static IEnumerable<Project> FindNodeProjects(DTE2 dte)
        {
            foreach (Project project in dte.Solution.Projects)
            {
                var packageFile = FindProjectItemInProject(project, "package.json", false, SearchType.Filename);

                if (packageFile != null)
                    yield return project;
            }

            yield break;
        }

        /// <summary>
        /// Finds web projects (projects which have a web.config file)
        /// </summary>
        public static IEnumerable<Project> FindWebProjects(DTE2 dte)
        {
            var projects = new List<Project>();

            Action<Project> tryAddValidProject = project =>
            {
                var configFile = FindProjectItemInProject(project, "web.config", false, SearchType.Filename);

                if (configFile != null)
                    projects.Add(project);
            };

            foreach (Project project in dte.Solution.Projects)
            {
                if (project.Kind == Constants.vsProjectKindSolutionItems)
                {
                    var solutionItemProjects = FindProjectsInSolutionItem(project);

                    foreach (var sProject in solutionItemProjects)
                    {
                        tryAddValidProject(sProject);
                    }
                }
                else
                {
                    tryAddValidProject(project);
                }
            }

            return projects;
        }

        private static IEnumerable<Project> FindProjectsInSolutionItem(Project solutionFolder)
        {
            var projects = new List<Project>();

            foreach (ProjectItem item in solutionFolder.ProjectItems)
            {
                var subProject = item.SubProject;

                if (subProject == null)
                    continue; 

                if (subProject.Kind == Constants.vsProjectKindSolutionItems)
                {
                    projects.AddRange(FindProjectsInSolutionItem(subProject));
                }
                else
                    projects.Add(subProject);
            }

            return projects;
        }

        public static ProjectItem FindProjectItemInProjects(IEnumerable<Project> projects, string name, bool recursive, SearchType searchType = SearchType.FullPath)
        {
            foreach (Project project in projects)
            {
                var item = FindProjectItemInProject(project, name, recursive, searchType);

                if (item != null)
                {
                    return item;
                }
            }

            return null;
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
                string searchValue = searchType == SearchType.FullPath
                    ? item.GetFullPath()
                    : item.Name;

                return name.Equals(searchValue, StringComparison.OrdinalIgnoreCase);
            };

            ProjectItem foundProjectItem = null;

            foreach (ProjectItem item in projectItems)
            {
                // Exit immediately if this is the node_modules folder
                if (item.Name == "node_modules")
                    return null;

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

        public static IEnumerable<ProjectItem> GatherFiles(ProjectItems items, bool recursive, Func<ProjectItem, bool> predicate)
        {
            foreach (ProjectItem item in items)
            {
                if (item.Kind == Constants.vsProjectItemKindPhysicalFolder && recursive)
                    foreach (ProjectItem innerItem in GatherFiles(item.ProjectItems, recursive, predicate))
                        yield return innerItem;
                else if (item.Kind == Constants.vsProjectItemKindPhysicalFile)
                {
                    if (predicate(item))
                        yield return item;
                }
            }
        }
    }
}
