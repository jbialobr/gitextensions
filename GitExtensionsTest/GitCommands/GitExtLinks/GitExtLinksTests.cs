using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using GitCommands.GitExtLinks;
using GitCommands;
using GitCommands.Remote;
using NSubstitute;
using System.ComponentModel;

namespace GitExtensionsTest.GitCommands.GitExtLinks
{
    [TestFixture]
    class GitExtLinksTests
    {

        [Test]
        public void ParseGithubIssueForUpstreamLink()
        {
            GitExtLinkDef linkDef = GetGithubIssuesLinkDef();
            GitRevision revision = new GitRevision(null, "");
            revision.Body = "Merge pull request #3657 from RussKie/tweak_FormRemotes_tooltips";
            IGitRemoteController remoteController = Substitute.For<IGitRemoteController>();
            remoteController.Remotes.Returns(GetDefaultRemotes());
            IEnumerable<GitExtLink> actualLinks = linkDef.Parse(revision, remoteController);
            IEnumerable<GitExtLink> expectedLinks = new GitExtLink[]
            {
                new GitExtLink()
                {
                    Caption = "Issue 3657",
                    URI = "https://github.com/gitextensions/gitextensions/issues/3657"
                }
            };
        }

        [Test]
        public void ParseGithubIssueForOriginLink()
        {
            GitExtLinkDef linkDef = GetGithubIssuesLinkDef();
            linkDef.UseRemotesPattern = "origin|upstream";
            GitRevision revision = new GitRevision(null, "");
            revision.Body = "Merge pull request #3657 from RussKie/tweak_FormRemotes_tooltips";
            IGitRemoteController remoteController = Substitute.For<IGitRemoteController>();
            remoteController.Remotes.Returns(GetDefaultRemotes());
            IEnumerable<GitExtLink> actualLinks = linkDef.Parse(revision, remoteController);
            IEnumerable<GitExtLink> expectedLinks = new GitExtLink[]
            {
                new GitExtLink()
                {
                    Caption = "Issue 3657",
                    URI = "https://github.com/jbialobr/gitextensions/issues/3657"
                }
            };
        }


        private static BindingList<GitRemote> GetDefaultRemotes()
        {
            var remotes = new BindingList<GitRemote>();
            remotes.Add(new GitRemote()
            {
                Name = "origin",
                Url = "https://github.com/jbialobr/gitextensions.git"
            });

            remotes.Add(new GitRemote()
            {
                Name = "upstream",
                Url = "https://github.com/gitextensions/gitextensions.git"
            });

            remotes.Add(new GitRemote()
            {
                Name = "RussKie",
                Url = "https://github.com/russkie/gitextensions.git"
            });

            return remotes;
        }

        private static GitExtLinkDef GetGithubIssuesLinkDef()
        {
            return GitExtLinksParser.LoadFromXmlString(GetGithubIssuesXmlDef()).First();
        }

        private static string GetGithubIssuesXmlDef()
        {
            return @"<?xml version=""1.0"" ?>
<ArrayOfGitExtLinkDef>
<GitExtLinkDef>
    <SearchInParts>
        <RevisionPart>Message</RevisionPart>
        <RevisionPart>LocalBranches</RevisionPart>
        <RevisionPart>RemoteBranches</RevisionPart>
    </SearchInParts>
    <RemoteSearchInParts>
        <RemotePart>URL</RemotePart>
    </RemoteSearchInParts>
    <LinkFormats>
        <GitExtLinkFormat>
            <Caption>Issue {1}</Caption>
            <Format>https://github.com/{0}/issues/{1}</Format>
        </GitExtLinkFormat>
    </LinkFormats>
    <Name>GitHub - issues</Name>
    <SearchPattern>(\s*(,|and)?\s*#\d+)+</SearchPattern>
    <NestedSearchPattern>(\d+)+</NestedSearchPattern>
    <Enabled>true</Enabled>
    <RemoteSearchPattern>github.com[:/](.+)\.git</RemoteSearchPattern>
    <UseRemotesPattern>upstream|origin</UseRemotesPattern>
    <UseOnlyFirstRemote>true</UseOnlyFirstRemote>
</GitExtLinkDef>
</ArrayOfGitExtLinkDef>
       ";
        }

    }
}
