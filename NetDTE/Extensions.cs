using EnvDTE;

namespace NetDTE
{
    public static class Extensions
    {
        public static string GetFullPath(this ProjectItem projectItem)
        {
            return (string)projectItem.Properties.Item("FullPath").Value;
        }
    }
}
