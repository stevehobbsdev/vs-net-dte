using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;

namespace NetDTE
{
    public class AssetCache
    {
        private Dictionary<string, ProjectItem> files = new Dictionary<string, ProjectItem>();
        private IEnumerable<Project> nodeProjects = Enumerable.Empty<Project>();
        private readonly DTE2 dte;

        public IEnumerable<Func<ProjectItem, bool>> Predicates { get; private set; }

        public AssetCache(DTE2 dte)
        {
            this.dte = dte;

            this.Predicates = new List<Func<ProjectItem, bool>>
            {
                item => item.Name.EndsWith(".scss")
            };
        }

        public void Initialise()
        {
            this.nodeProjects = SolutionHelper.FindNodeProjects(dte);

            if (this.nodeProjects.Any())
            {
                this.files = SolutionHelper.GatherFiles(this.nodeProjects.First().ProjectItems, true, item => this.ShouldCache(item))
                        .ToDictionary(d => MakeKey(d.GetFullPath()).ToLower());
            }
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
