using Microsoft.AspNetCore.Html;
using System.Text.RegularExpressions;

namespace StageComputer.Utils
{
    public class ExplainationManager
    {
        private readonly string explainationsFolder;
        private readonly MarkdownSharp.Markdown markdownGenerator = new MarkdownSharp.Markdown();
        private Dictionary<string, string> variables = new Dictionary<string, string>();

        public ExplainationManager(IWebHostEnvironment hostEnvironment)
        {
            explainationsFolder = Path.Combine(hostEnvironment.ContentRootPath, "Explainations");
            Directory.CreateDirectory(explainationsFolder);
        }

        public void RegisterVariable(string name, string value)
        {
            variables[name] = value;
        }

        public IEnumerable<string> GetAllExplainationNames()
        {
            return Directory.GetFiles(explainationsFolder, "*.md").Select(f => Path.GetFileNameWithoutExtension(f));
        }
        public async Task<HtmlString> GetExplainationHtmlAsync(string name)
        {
            var explainationFile = Path.Combine(explainationsFolder, $"{name}.md");
            if (!File.Exists(explainationFile))
                return HtmlString.Empty;

            string resultHtml = markdownGenerator.Transform(await File.ReadAllTextAsync(explainationFile));
            resultHtml = Regex.Replace(resultHtml, "##(?<var>[^#]+)##", m =>
            {
                string variable_content;
                if (!variables.TryGetValue(m.Groups["var"].Value, out variable_content))
                    return string.Empty;
                return variable_content;
            });
            return new HtmlString(resultHtml);
        }

        public async Task SaveExplainationAsync(string name, string content)
        {
            var explainationFile = Path.Combine(explainationsFolder, $"{name}.md");
            await File.WriteAllTextAsync(explainationFile, content);
        }
    }
}
