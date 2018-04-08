using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using GitCommands;
using GitCommands.UserRepositoryHistory;
using NSubstitute;
using NUnit.Framework;

namespace GitCommandsTests.UserRepositoryHistory
{
    [TestFixture]
    public class RemoteRepositoryManagerTests
    {
        private IRepositoryStorage _repositoryStorage;
        private RemoteRepositoryManager _manager;

        [SetUp]
        public void Setup()
        {
            _repositoryStorage = Substitute.For<IRepositoryStorage>();
            _manager = new RemoteRepositoryManager(_repositoryStorage);
        }

        [Test]
        public async Task SaveHistoryAsync_should_trim_history_size()
        {
            const int size = 3;
            AppSettings.RecentRepositoriesHistorySize = size;

            var history = new RepositoryHistory
            {
                Repositories = new BindingList<Repository>
                {
                    new Repository("path1"),
                    new Repository("path2"),
                    new Repository("path3"),
                    new Repository("path4"),
                    new Repository("path5"),
                }
            };

            await _manager.SaveHistoryAsync(history);

            _repositoryStorage.Received(1).Save("history remote", Arg.Is<IEnumerable<Repository>>(h => h.Count() == size));
        }
    }
}