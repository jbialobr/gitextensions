using GitCommands;
using System;
using System.IO;
using System.Collections.Concurrent;
using System.IO.Abstractions;

namespace GitExtensions
{
    public interface IGitWorkingDirService
    {
        string FindGitWorkingDir(string startDir);
        bool IsValidGitWorkingDir(string dir);
    }

    public class GitWorkingDirService : IGitWorkingDirService
    {
        IFileSystem fileSystem;

        public GitWorkingDirService(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public string FindGitWorkingDir(string startDir)
        {
            if (string.IsNullOrEmpty(startDir))
                return "";

            var dir = startDir.Trim();

            do
            {
                if (IsValidGitWorkingDir(dir))
                    return dir.EnsureTrailingPathSeparator();

                dir = PathUtil.GetDirectoryName(dir);
            }
            while (!string.IsNullOrEmpty(dir));
            return startDir;
        }

        public bool IsValidGitWorkingDir(string dir)
        {
            if (string.IsNullOrEmpty(dir))
                return false;

            string dirPath = dir.EnsureTrailingPathSeparator();
            string path = dirPath + ".git";

            if (fileSystem.Directory.Exists(path) || fileSystem.File.Exists(path))
                return true;

            return fileSystem.Directory.Exists(dirPath + "info") &&
                   fileSystem.Directory.Exists(dirPath + "objects") &&
                   fileSystem.Directory.Exists(dirPath + "refs");

        }
    }

    public interface IAppSettings
    {
        bool StartWithRecentWorkingDir { get; set; }
        string RecentWorkingDir { get; set; }
    }

    public class GESettings : IAppSettings
    {
        IFileSystem fileSystem;

        public GESettings(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public bool StartWithRecentWorkingDir { get; set; }
        public string RecentWorkingDir { get; set; }
    }

    public class WorkingPathService
    {
        private IGitWorkingDirService workingDirService;
        private IAppSettings appSettings;
        private IFileSystem fileSystem;

        public WorkingPathService(IGitWorkingDirService aWorkingDirService, IAppSettings appSettings, IFileSystem fileSystem)
        {
            workingDirService = aWorkingDirService;
            this.appSettings = appSettings;
            this.fileSystem = fileSystem;
        }

        public string GetWorkingDir(string[] args)
        {
            string workingDir = string.Empty;
            if (args.Length >= 3)
            {
                //there is bug in .net
                //while parsing command line arguments, it unescapes " incorectly
                //https://github.com/gitextensions/gitextensions/issues/3489
                string dirArg = args[2].TrimEnd('"');
                //Get DirectoryGateway from the global injected instance, needs to call before each test StaticDI.ClearInstances();
                if (fileSystem.Directory.Exists(dirArg))
                    workingDir = workingDirService.FindGitWorkingDir(dirArg);
                else
                {
                    workingDir = Path.GetDirectoryName(dirArg);
                    workingDir = workingDirService.FindGitWorkingDir(workingDir);
                }

                //Do not add this working directory to the recent repositories. It is a nice feature, but it
                //also increases the startup time
                //if (Module.ValidWorkingDir())
                //    Repositories.RepositoryHistory.AddMostRecentRepository(Module.WorkingDir);
            }

            if (args.Length <= 1 && string.IsNullOrEmpty(workingDir) && appSettings.StartWithRecentWorkingDir)
            {
                if (workingDirService.IsValidGitWorkingDir(appSettings.RecentWorkingDir))
                    workingDir = appSettings.RecentWorkingDir;
            }

            if (string.IsNullOrEmpty(workingDir))
            {
                //Get DirectoryGateway from Ext.Directory
                string findWorkingDir = workingDirService.FindGitWorkingDir(fileSystem.Directory.GetCurrentDirectory());
                if (workingDirService.IsValidGitWorkingDir(findWorkingDir))
                    workingDir = findWorkingDir;
            }

            return workingDir;
        }


    }

}
