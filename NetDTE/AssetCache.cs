using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;

namespace NetDTE
{
    public class AssetCache
    {
        private Dictionary<string, ProjectItem> files;
        private readonly IEnumerable<Project> nodeProjects;

        public IEnumerable<Func<ProjectItem, bool>> Predicates { get; private set; }

        public AssetCache(DTE dte)
        {
            this.nodeProjects = SolutionHelper.FindNodeProjects(dte);

            foreach (Project project in this.nodeProjects)
            {
                
            }

            this.Predicates = new List<Func<ProjectItem, bool>>
            {
                item => item.Name.EndsWith(".scss")
            };
        }

        public void Initialise()
        {
            this.files = SolutionHelper.GatherFiles(this.nodeProjects.First().ProjectItems, true, item => this.ShouldCache(item))
                    .ToDictionary(d => MakeKey(d.GetFullPath()).ToLower());
        }

        public bool ShouldCache(ProjectItem item)
        {
            foreach (var p in this.Predicates)
            {
                if (p(item))
                    return true;
            }

            return false;
        }

        public ProjectItem Lookup(string path)
        {
            var key = path.ToLower();

            if (this.files.ContainsKey(key))
                return this.files[key];

            return null;        
        }

        public void Add(ProjectItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var path = item.GetFullPath();
            var existing = Lookup(path);

            if (existing == null)
            {
                this.files[MakeKey(path)] = item;

                Logger.WriteLine($"{item.Name} was added to the cache");
            }
        }

        public void Remove(string path)
        {
            var existing = Lookup(path);

            if (existing != null)
            {
                this.files.Remove(path);
                Logger.WriteLine($"{Path.GetFileName(path)} was removed from the cache");
            }
        }

        public void Remove(ProjectItem projectItem)
        {
            this.Remove(projectItem.GetFullPath());
        }

        public void Clear()
        {
            this.files.Clear();
        }

        private string MakeKey(string path)
        {
            return path.ToLower();
        }
    }
}
