using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace GitCommands.UserRepositoryHistory
{
    /// <summary>
    /// Manages the history of local git repositories.
    /// </summary>
    public sealed class LocalRepositoryManager : IRepositoryManager
    {
        private const string KeyRecentHistory = "history";
        private readonly IRepositoryStorage _repositoryStorage;

        public LocalRepositoryManager(IRepositoryStorage repositoryStorage)
        {
            _repositoryStorage = repositoryStorage;
        }

        /// <summary>
        /// <para>Saves the provided repository path to history of local git repositories as the "most recent".</para>
        /// <para>If the history contains an entry for the provided path, the entry is physically moved
        /// to the top of the history list.</para>
        /// </summary>
        /// <remarks>
        /// The history is loaded from the persistent storage to ensure the most current version of
        /// the history is updated, as it may have been updated by another instance of GE.
        /// </remarks>
        /// <param name="repositoryPath">A repository path to be save as "most recent".</param>
        /// <returns>The current version of the history of local git repositories after the update.</returns>
        /// <exception cref="ArgumentException"><paramref name="repositoryPath"/> is <see langword="null"/> or <see cref="string.Empty"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="repositoryPath"/> is a URL.</exception>
        [ContractAnnotation("repositoryPath:null=>halt")]
        public Task<IList<Repository>> AddAsMostRecentAsync(string repositoryPath)
        {
            if (string.IsNullOrWhiteSpace(repositoryPath))
            {
                throw new ArgumentException(nameof(repositoryPath));
            }

            if (PathUtil.IsUrl(repositoryPath))
            {
                // TODO: throw a specific exception
                throw new NotSupportedException();
            }

            repositoryPath = repositoryPath.Trim()
                                           .ToNativePath()
                                           .EnsureTrailingPathSeparator();
            return AddAsMostRecentRepositoryAsync(repositoryPath);

            Task<IList<Repository>> AddAsMostRecentRepositoryAsync(string path)
            {
                return Task.Run<IList<Repository>>(async () =>
               {
                   var repositoryHistory = (await LoadHistoryAsync()).ToList();

                   var repository = repositoryHistory.FirstOrDefault(r => r.Path.Equals(path, StringComparison.CurrentCultureIgnoreCase));
                   if (repository != null)
                   {
                       repositoryHistory.Remove(repository);
                   }
                   else
                   {
                       repository = new Repository(path);
                   }

                   repositoryHistory.Insert(0, repository);

                   await SaveHistoryAsync(repositoryHistory);

                   return repositoryHistory;
               });
            }
        }

        /// <summary>
        /// Loads the history of local git repositories from a persistent storage.
        /// </summary>
        /// <returns>The history of local git repositories.</returns>
        public Task<IList<Repository>> LoadHistoryAsync()
        {
            int size = AppSettings.RecentRepositoriesHistorySize;
            return Task.Run<IList<Repository>>(() =>
            {
                var history = _repositoryStorage.Load(KeyRecentHistory);
                if (history == null)
                {
                    return Array.Empty<Repository>();
                }

                return AdjustHistorySize(history, size).ToList();
            });
        }

        /// <summary>
        /// Removes <paramref name="repository"/> from the history of local git repositories in a persistent storage.
        /// </summary>
        /// <param name="repository">A repository to remove.</param>
        /// <returns>The current version of the history of local git repositories after the update.</returns>
        public Task<IList<Repository>> RemoveFromHistoryAsync(Repository repository)
        {
            return Task.Run(async () =>
            {
                var repositoryHistory = await LoadHistoryAsync();
                if (!repositoryHistory.Remove(repository))
                {
                    return repositoryHistory;
                }

                await SaveHistoryAsync(repositoryHistory);
                return repositoryHistory;
            });
        }

        /// <summary>
        /// Loads the history of local git repositories to a persistent storage.
        /// </summary>
        /// <param name="repositoryHistory">A collection of local git repositories.</param>
        /// <returns>An awaitable task.</returns>
        /// <remarks>The size of the history will be adjusted as per <see cref="AppSettings.RecentRepositoriesHistorySize"/> setting.</remarks>
        public Task SaveHistoryAsync(IEnumerable<Repository> repositoryHistory)
        {
            int size = AppSettings.RecentRepositoriesHistorySize;
            return Task.Run(() =>
            {
                _repositoryStorage.Save(KeyRecentHistory, AdjustHistorySize(repositoryHistory, size));
            });
        }

        private static IEnumerable<Repository> AdjustHistorySize(IEnumerable<Repository> repositories, int recentRepositoriesHistorySize)
        {
            return repositories.Take(recentRepositoriesHistorySize);
        }
    }
}