using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

namespace FormatAllFilesPlus
{
    internal sealed class FormatAllFilesPlusCommand : CommandBase
    {
        public const int CommandId = 0x0100;

        public static readonly Guid CommandSet = new Guid("92032ec5-1c2f-4551-9446-3f0a5c866c9f");

        private OutputWindow _outputWindow = new OutputWindow(FormatAllFilesPlusPackage.PackageName);

        public static FormatAllFilesPlusCommand Instance { get; private set; }

        private FormatAllFilesPlusCommand(Package package) : base(package, CommandId, CommandSet)
        {
        }

        public static void Initialize(Package package)
        {
            Instance = new FormatAllFilesPlusCommand(package);
        }

        protected override void Execute(object sender, EventArgs e)
        {
            var dte = (DTE2)ServiceProvider.GetService(typeof(DTE));

            var option = ((FormatAllFilesPlusPackage)Package).GetGeneralOption();
            var fileFilter = option.CreateFileFilter();

            var targetItems = GetSelectedProjectItems(dte.ToolWindows.SolutionExplorer, option.CreateHierarchyFilter())
                .Where(item => item.Kind == VSConstants.ItemTypeGuid.PhysicalFile_string && fileFilter(item.Name))
                .ToArray();

            var itemCount = targetItems.Length;
            var errorCount = 0;
            var commands = option.GetCommands();
            var statusBar = dte.StatusBar;

            _outputWindow.Clear();

            var stopWatch = Stopwatch.StartNew();

            _outputWindow.WriteLine($"{DateTime.Now.ToString("T")} Started. ({itemCount} files)");

            for (var i = 0; i < itemCount; i++)
            {
                var item = targetItems[i];
                var name = item.FileCount != 0 ? item.FileNames[0] : item.Name;
                _outputWindow.WriteLine("Formatting: " + name);
                statusBar.Progress(true, string.Empty, i + 1, itemCount);

                if (ExecuteCommand(item, commands) == false)
                {
                    errorCount++;
                }
            }

            stopWatch.Stop();

            _outputWindow.WriteLine($"{DateTime.Now.ToString("T")} Finished. ({itemCount - errorCount} success. {errorCount} failure.) Formatting took {stopWatch.Elapsed.ToString("T")}");
            statusBar.Progress(false);
            statusBar.Text = $"Format All Files finished. ({itemCount} files)";
        }

        private bool ExecuteCommand(ProjectItem item, IEnumerable<string> commands)
        {
            var result = false;

            var isOpen = item.IsOpen;
            if (isOpen == false)
            {
                if (!item.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        item.Open(VSConstants.LOGVIEWID.TextView_string);
                    }
                    catch (COMException)
                    {
                        _outputWindow.WriteLine("This is not text file.");
                    }
                }
                else
                {
                    _outputWindow.WriteLine("This is not text file.");
                }
            }

            var document = item.Document;
            if (document != null)
            {
                try
                {
                    document.Activate();
                    foreach (var command in commands)
                    {
                        try
                        {
                            item.DTE.ExecuteCommand(command);
                            result = true;
                        }
                        catch (COMException ex)
                        {
                            _outputWindow.WriteLine(ex.Message);
                        }
                    }
                }
                finally
                {
                    if (isOpen)
                    {
                        document.Save();
                    }
                    else
                    {
                        document.Close(vsSaveChanges.vsSaveChangesYes);
                    }
                }
            }

            return result;
        }

        private IEnumerable<ProjectItem> GetProjectItems(Project project, Func<string, bool> filter)
        {
            return GetProjectItems(project.ProjectItems, filter);
        }

        private IEnumerable<ProjectItem> GetProjectItems(ProjectItem item, Func<string, bool> filter)
        {
            var innerItems = GetProjectItems(item.ProjectItems, filter);
            if (item.Kind == VSConstants.ItemTypeGuid.PhysicalFile_string)
            {
                return new[] { item }.Concat(innerItems);
            }
            else
            {
                return innerItems;
            }
        }

        private IEnumerable<ProjectItem> GetProjectItems(ProjectItems items, Func<string, bool> filter)
        {
            return items
                .OfType<ProjectItem>()
                .Recursive(x =>
                {
                    var innerItems = x.ProjectItems;
                    if (innerItems != null)
                    {
                        if (filter(x.Name))
                        {
                            return innerItems.OfType<ProjectItem>();
                        }
                    }
                    else
                    {
                        var subProject = x.SubProject;
                        if (subProject != null)
                        {
                            return GetProjectItems(subProject, filter);
                        }
                    }

                    return Enumerable.Empty<ProjectItem>();
                });
        }

        private IEnumerable<ProjectItem> GetSelectedProjectItems(UIHierarchy solutionExplorer, Func<string, bool> filter)
        {
            if (solutionExplorer.SelectedItems is object[] selectedItems)
            {
                return selectedItems.OfType<UIHierarchyItem>()
                    .SelectMany(x => GetSelectedProjectItems(x, filter));
            }
            else
            {
                return Enumerable.Empty<ProjectItem>();
            }
        }

        private IEnumerable<ProjectItem> GetSelectedProjectItems(UIHierarchyItem hierarchyItem, Func<string, bool> filter)
        {
            var selectedObject = hierarchyItem.Object;

            if (selectedObject is Solution solution)
            {
                return solution.Projects.OfType<Project>()
                    .SelectMany(x => GetProjectItems(x, filter));
            }

            if (selectedObject is Project project)
            {
                return GetProjectItems(project, filter);
            }

            if (selectedObject is ProjectItem item)
            {
                return GetProjectItems(item, filter);
            }

            return Enumerable.Empty<ProjectItem>();
        }
    }
}
