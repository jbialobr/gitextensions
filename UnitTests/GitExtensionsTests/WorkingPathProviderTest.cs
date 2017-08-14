using FluentAssertions;
using GitExtensions;
using NSubstitute;
using NSubstitute.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace GitExtensionsTest
{
    [TestFixture]
    public class WorkingPathProviderTests
    {
        private WorkingPathService _workingPathProvider;
        private IGitWorkingDirService workingDirService;
        private IAppSettings appSettings;
        private IFileSystem fileSystem;

        [SetUp]
        public void Setup()
        {
            NSubstituteHelper.Substitute(ref workingDirService);
            NSubstituteHelper.Substitute(ref appSettings);
            NSubstituteHelper.SubstituteFileSystem(ref fileSystem);

            _workingPathProvider = new WorkingPathService(workingDirService, appSettings, fileSystem);
        }

        [Test]
        public void ReturnsRecentDirectory_if_RecentDirectory_IsValidGitWorkingDir()
        {
            //arange
            fileSystem.Directory.GetCurrentDirectory().Returns(string.Empty);
            appSettings.StartWithRecentWorkingDir.Returns(true);
            string unitTestRecentWorkingDir = "unitTestRecentWorkingDir";
            appSettings.RecentWorkingDir.Returns(unitTestRecentWorkingDir);
            workingDirService.IsValidGitWorkingDir(unitTestRecentWorkingDir).Returns(true);
            //act
            string workingDir = _workingPathProvider.GetWorkingDir(new string[0]);
            //assert
            workingDir.Should().Be(unitTestRecentWorkingDir);
        }

        [Test]
        public void ThrowOnUnconfiguredCall()
        {
            Action act = () =>
            {
                //arange
                NSubstituteHelper.ThrowOnUnconfiguredCall();
                //act
                fileSystem.Directory.GetCurrentDirectory();
                //assert
            };

            act.ShouldThrow<UnconfiguredCallException>();
        }

        [Test]
        public void DontThrowOnUnconfiguredCall()
        {
            //arange
            NSubstituteHelper.ThrowOnUnconfiguredCall();
            //act
            fileSystem.Directory.GetCurrentDirectory();
            //assert
        }

    }

    [Serializable()]
    public class UnconfiguredCallException : Exception
    {
        public UnconfiguredCallException() : base() { }
        public UnconfiguredCallException(string message) : base(message) { }
    }

    class ThrowIfUnconfigured : ICallHandler
    {
        public RouteAction Handle(ICall call)
        {            
            throw new UnconfiguredCallException();
        }
    }

    public class NSubstituteHelper
    {
        private static List<object> substitutes = new List<object>();
        private static bool _ThrowOnUnconfiguredCall = false;
        private static CallHandlerFactory throwingFactory = s => new ThrowIfUnconfigured();

        public static void ThrowOnUnconfiguredCall()
        {
            _ThrowOnUnconfiguredCall = true;
            
            foreach (object sut in substitutes)
            {
                RegisterThrowingCallHandlerFactory(sut);
            }
        }

        private static void RegisterThrowingCallHandlerFactory(object substitute)
        {
            var sub = SubstitutionContext.Current.GetCallRouterFor(substitute);
            sub.RegisterCustomCallHandlerFactory(throwingFactory);
        }

        public static void Substitute<T>(ref T field) where T : class
        {
            field = NSubstitute.Substitute.For<T>();
        }

        public static void SubstituteFileSystem(ref IFileSystem fs)
        {
            fs = NSubstitute.Substitute.For<IFileSystem>();
            fs.Directory.Returns(NSubstitute.Substitute.For<DirectoryBase>());
            fs.File.Returns(NSubstitute.Substitute.For<FileBase>());
        }

    }
}

