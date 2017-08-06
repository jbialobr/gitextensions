using System;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Gravatar;
using GravatarTests.Properties;
using NSubstitute;
using NUnit.Framework;

namespace GravatarTests
{
    [TestFixture]
    public class DirectoryImageCacheTests
    {
        const string FileName = "aa.jpg";
        private const string FolderPath = @"C:\Users\user\AppData\Roaming\GitExtensions\GitExtensions\Images";
        private IFileSystem _fileSystem;
        private DirectoryBase _directory;
        private FileBase _file;
        private FileInfoBase _fileInfo;
        private IFileInfoFactory _fileInfoFactory;
        private DirectoryImageCache _cache;

        [SetUp]
        public void Setup()
        {
            _fileSystem = Substitute.For<IFileSystem>();
            _directory = Substitute.For<DirectoryBase>();
            _fileSystem.Directory.Returns(_directory);
            _file = Substitute.For<FileBase>();
            _fileSystem.File.Returns(_file);
            _fileInfo = Substitute.For<FileInfoBase>();
            _fileInfoFactory = Substitute.For<IFileInfoFactory>();
            _fileInfoFactory.FromFileName(Arg.Any<string>()).Returns(_fileInfo);
            _fileSystem.FileInfo.Returns(_fileInfoFactory);

            _cache = new DirectoryImageCache(FolderPath, 2, _fileSystem);
        }


        [Test]
        public async void AddImage_should_exit_if_stream_null()
        {
            await _cache.AddImageAsync("file", null);

            _directory.DidNotReceive().Exists(FolderPath);
        }

        [Test]
        public async void AddImage_should_create_if_folder_absent()
        {
            var fileSystem = new MockFileSystem();
            _cache = new DirectoryImageCache(FolderPath, 2, fileSystem);
            fileSystem.Directory.Exists(FolderPath).Should().BeFalse();

            using (var s = new MemoryStream())
            {
                await _cache.AddImageAsync("file", s);
            }

            fileSystem.Directory.Exists(FolderPath).Should().BeTrue();
        }

        [Test]
        public async void AddImage_should_create_image_from_stream()
        {
            var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var folderPath = Path.Combine(currentFolder, "Images");
            Console.WriteLine(folderPath);

            using (var imageStream = new MemoryStream())
            {
                var fileSystem = new FileSystem();
                Resources.User.Save(imageStream, ImageFormat.Png);
                imageStream.Position = 0;

                _cache = new DirectoryImageCache(folderPath, 2, fileSystem);
                fileSystem.Directory.Exists(FolderPath).Should().BeFalse();

                await _cache.AddImageAsync("file.png", imageStream);

                fileSystem.Directory.Exists(folderPath).Should().BeTrue();
                fileSystem.File.Exists(Path.Combine(folderPath, "file.png")).Should().BeTrue();
            }
        }

        [Test]
        public async void Clear_should_return_if_folder_absent()
        {
            _directory.Exists(Arg.Any<string>()).Returns(false);

            await _cache.ClearAsync();

            _directory.DidNotReceive().GetFiles(Arg.Any<string>());
        }

        [Test]
        public async void Clear_should_remove_all()
        {
            var fileSystem = new MockFileSystem();
            _cache = new DirectoryImageCache(FolderPath, 2, fileSystem);

            fileSystem.AddFile($"{FolderPath}\\a@a.com.png", new MockFileData(""));
            fileSystem.AddFile($"{FolderPath}\\b@b.com.png", new MockFileData(""));
            fileSystem.AllFiles.Count().Should().Be(2);

            await _cache.ClearAsync();

            fileSystem.AllFiles.Count().Should().Be(0);
        }

        [Test]
        public void Clear_should_ignore_errors()
        {
            _directory.Exists(Arg.Any<string>()).Returns(true);
            _directory.GetFiles(FolderPath).Returns(new[] { "c:\\file.txt", "boot.sys" });
            _file.When(x => x.Delete(Arg.Any<string>()))
                .Do(x =>
                {
                    throw new DivideByZeroException();
                });

            Func<Task> act = async () =>
            {
                await _cache.ClearAsync();
            };
            act.ShouldNotThrow();
        }

        [Test]
        public async void DeleteImage_should_return_if_folder_absent()
        {
            _file.Exists(Arg.Any<string>()).Returns(false);

            await _cache.DeleteImageAsync(FileName);

            _file.DidNotReceive().Delete(Arg.Any<string>());
        }

        [Test]
        public async void DeleteImage_should_delete()
        {
            _file.Exists(Arg.Any<string>()).Returns(true);

            await _cache.DeleteImageAsync(FileName);

            _file.Received(1).Delete(Arg.Any<string>());
        }

        [Test]
        public void DeleteImage_should_ignore_errors()
        {
            _file.Exists(Arg.Any<string>()).Returns(true);
            _file.When(x => x.Delete(FileName))
                .Do(x =>
                {
                    throw new DivideByZeroException();
                });

            Func<Task> act = async () =>
            {
                await _cache.DeleteImageAsync(FileName);
            };
            act.ShouldNotThrow();
        }

        [Test]
        public async void GetImage_return_null_if_file_absent()
        {
            _fileInfo.Exists.Returns(false);

            var image = await _cache.GetImageAsync(FileName, null);

            image.Should().BeNull();
            var not_used = _fileInfo.DidNotReceive().LastWriteTime;
        }

        [Test]
        public async void GetImage_return_null_if_file_expired()
        {
            _fileInfo.Exists.Returns(true);
            _fileInfo.LastWriteTime.Returns(new DateTime(2010, 1, 1));

            var image = await _cache.GetImageAsync(FileName, null);

            image.Should().BeNull();
            var not_used = _fileInfo.Received(1).LastWriteTime;
        }

        [Test]
        public void GetImage_return_null_if_exception()
        {
            _fileInfo.Exists.Returns(true);
            _fileInfo.LastWriteTime.Returns(x =>
            {
                throw new DivideByZeroException();
            });

            Func<Task> act = async () =>
            {
                await _cache.GetImageAsync(FileName, null);
            };
            act.ShouldNotThrow();
        }
    }
}
