using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GitCommands;
using GitCommands.UserRepositoryHistory;
using NSubstitute;
using NUnit.Framework;

namespace GitCommandsTests.UserRepositoryHistory
{
    [TestFixture]
    public class RemoteRepositoryManagerTests
    {
        private const string Key = "history remote";
        private IRepositoryStorage _repositoryStorage;
        private RemoteRepositoryManager _manager;

        [SetUp]
        public void Setup()
        {
            _repositoryStorage = Substitute.For<IRepositoryStorage>();
            _manager = new RemoteRepositoryManager(_repositoryStorage);
        }

        [Test]
        public async Task RemoveFromHistoryAsync_should_remove_if_exists()
        {
            var repoToDelete = new Repository("path to delete");
            var history = new List<Repository>
            {
                new Repository("path1"),
                repoToDelete,
                new Repository("path3"),
                new Repository("path4"),
                new Repository("path5"),
            };
            _repositoryStorage.Load(Key).Returns(x => history);

            var newHistory = await _manager.RemoveFromHistoryAsync(repoToDelete);

            newHistory.Count.Should().Be(4);
            newHistory.Should().NotContain(repoToDelete);

            _repositoryStorage.Received(1).Load(Key);
            _repositoryStorage.Received(1).Save(Key, Arg.Is<IEnumerable<Repository>>(h => !h.Contains(repoToDelete)));
        }

        [Test]
        public async Task RemoveFromHistoryAsync_should_not_crash_if_not_exists()
        {
            var repoToDelete = new Repository("path");
            var history = new List<Repository>
            {
                new Repository("path1"),
                new Repository("path2"),
                new Repository("path3"),
                new Repository("path4"),
                new Repository("path5"),
            };
            _repositoryStorage.Load(Key).Returns(x => history);

            var newHistory = await _manager.RemoveFromHistoryAsync(repoToDelete);

            newHistory.Count.Should().Be(5);
            newHistory.Should().NotContain(repoToDelete);

            _repositoryStorage.Received(1).Load(Key);
            _repositoryStorage.DidNotReceive().Save(Key, Arg.Any<IEnumerable<Repository>>());
        }

        [Test]
        public async Task SaveHistoryAsync_should_trim_history_size()
        {
            const int size = 3;
            AppSettings.RecentRepositoriesHistorySize = size;
            var history = new List<Repository>
            {
                new Repository("path1"),
                new Repository("path2"),
                new Repository("path3"),
                new Repository("path4"),
                new Repository("path5"),
            };

            await _manager.SaveHistoryAsync(history);

            _repositoryStorage.Received(1).Save(Key, Arg.Is<IEnumerable<Repository>>(h => h.Count() == size));
        }
    }
}