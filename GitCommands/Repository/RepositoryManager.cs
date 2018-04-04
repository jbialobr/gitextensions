using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace GitCommands.Repository
{
    public static class RepositoryManager
    {
        private const string KeyRecentHistory = "history";
        private const string KeyRemoteHistory = "history remote";

        private static readonly IRepositoryStorage RepositoryStorage = new RepositoryStorage();

        public static Task<RepositoryHistory> LoadRepositoryHistoryAsync()
        {
            int size = AppSettings.RecentRepositoriesHistorySize;
            return Task.Run(() =>
            {
                var repositoryHistory = new RepositoryHistory(size);

                var history = RepositoryStorage.Load(KeyRecentHistory);
                if (history == null)
                {
                    return repositoryHistory;
                }

                repositoryHistory.Repositories = new BindingList<Repository>(history.ToList());
                return repositoryHistory;
            });
        }

        public static Task RemoveRepositoryHistoryAsync(Repository repository)
        {
            // TODO:
            return Task.CompletedTask;
        }

        public static void AddMostRecentRepository(string repo)
        {
            if (PathUtil.IsUrl(repo))
            {
                ////RemoteRepositoryHistory.AddMostRecentRepository(repo);
            }
            else
            {
                ////RepositoryHistory.AddMostRecentRepository(repo);
            }
        }

        public static Task<RepositoryHistory> LoadRepositoryRemoteHistoryAsync()
        {
            int size = AppSettings.RecentRepositoriesHistorySize;
            return Task.Run(() =>
            {
                var repositoryHistory = new RepositoryHistory(size);

                var history = RepositoryStorage.Load(KeyRemoteHistory);
                if (history == null)
                {
                    return repositoryHistory;
                }

                repositoryHistory.Repositories = new BindingList<Repository>(history.ToList());
                return repositoryHistory;
            });
        }

        public static void AdjustRecentHistorySize(int recentRepositoriesHistorySize)
        {
            // TODO:
        }

        public static Task SaveRepositoryHistoryAsync(RepositoryHistory repositoryHistory)
        {
            return Task.Run(() => RepositoryStorage.Save(KeyRecentHistory, repositoryHistory.Repositories));
        }

        public static Task SaveRepositoryRemoteHistoryAsync(RepositoryHistory repositoryHistory)
        {
            return Task.Run(() => RepositoryStorage.Save(KeyRemoteHistory, repositoryHistory.Repositories));
        }
    }
}
