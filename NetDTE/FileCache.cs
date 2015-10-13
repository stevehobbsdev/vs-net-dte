using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace NetDTE
{
    public class FileCache
    {
        private Dictionary<string, ProjectItem> files;
        private readonly IEnumerable<Project> nodeProjects;

        public FileCache(DTE dte)
        {
            this.nodeProjects = SolutionHelper.FindNodeProjects(dte);
        }

        public void SetupCache()
        {
            this.files = SolutionHelper.GatherFiles(this.nodeProjects.First().ProjectItems, true, item => item.Name.EndsWith(".scss"))
                    .ToDictionary(d => ((string)d.Properties.Item("FullPath").Value).ToLower());
        }

        public ProjectItem Lookup(string path)
        {
            var key = path.ToLower();

            if (this.files.ContainsKey(key))
                return this.files[key];

            return null;        
        }

        public void Add(string path, ProjectItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var existing = Lookup(path);

            if (existing == null)
                this.files[path] = item;
        }

        public void Remove(string path)
        {
            var existing = Lookup(path);

            if (existing != null)
                this.files.Remove(path);
        }

        public void Clear()
        {
            this.files.Clear();
        }
    }
}
