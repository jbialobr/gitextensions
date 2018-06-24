using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitUI;
using GitUIPluginInterfaces;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Threading;

namespace GitCommands
{
    [Flags]
    public enum RefFilterOptions
    {
        Branches = 1,              // --branches
        Remotes = 2,               // --remotes
        Tags = 4,                  // --tags
        Stashes = 8,               //
        All = 15,                  // --all
        Boundary = 16,             // --boundary
        ShowGitNotes = 32,         // --not --glob=notes --not
        NoMerges = 64,             // --no-merges
        FirstParent = 128,         // --first-parent
        SimplifyByDecoration = 256 // --simplify-by-decoration
    }

    public sealed class RevisionReader : IDisposable
    {
        private readonly CancellationTokenSequence _cancellationTokenSequence = new CancellationTokenSequence();

        public int RevisionCount { get; private set; }

        /// <value>Refs loaded during the last call to <see cref="Execute"/>.</value>
        public IReadOnlyList<IGitRef> LatestRefs { get; private set; } = Array.Empty<IGitRef>();

        public void Execute(
            GitModule module,
            IObserver<GitRevision> subject,
            RefFilterOptions refFilterOptions,
            string branchFilter,
            string revisionFilter,
            string pathFilter,
            [CanBeNull] Func<GitRevision, bool> revisionPredicate)
        {
            ThreadHelper.JoinableTaskFactory
                .RunAsync(() => ExecuteAsync(module, subject, refFilterOptions, branchFilter, revisionFilter, pathFilter, revisionPredicate))
                .FileAndForget(
                    ex =>
                    {
                        subject.OnError(ex);
                        return false;
                    });
        }

        private async Task ExecuteAsync(
            GitModule module,
            IObserver<GitRevision> subject,
            RefFilterOptions refFilterOptions,
            string branchFilter,
            string revisionFilter,
            string pathFilter,
            [CanBeNull] Func<GitRevision, bool> revisionPredicate)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var token = _cancellationTokenSequence.Next();

            RevisionCount = 0;

            await TaskScheduler.Default;

            subject.OnNext(null);

            token.ThrowIfCancellationRequested();

            var branchName = module.IsValidGitWorkingDir()
                ? module.GetSelectedBranch()
                : "";

            token.ThrowIfCancellationRequested();

            LatestRefs = module.GetRefs();
            UpdateSelectedRef(module, LatestRefs, branchName);
            var refsByObjectId = LatestRefs.ToLookup(head => head.Guid);

            token.ThrowIfCancellationRequested();

            const string fullFormat =

                // These header entries can all be decoded from the bytes directly.
                // Each hash is 20 bytes long. There is always a

                /* Object ID       */ "%H" +
                /* Tree ID         */ "%T" +
                /* Parent IDs      */ "%P%n" +
                /* Author date     */ "%at%n" +
                /* Commit date     */ "%ct%n" +
                /* Encoding        */ "%e%n" +

                // Items below here must be decoded as strings to support non-ASCII

                /* Author name     */ "%aN%n" +
                /* Author email    */ "%aE%n" +
                /* Committer name  */ "%cN%n" +
                /* Committer email */ "%cE%n" +
                /* Commit subject  */ "%s%n%n" +
                /* Commit body     */ "%b";

            // TODO add AppBuilderExtensions support for flags enums, starting with RefFilterOptions, then use it in the below construction

            var arguments = BuildArguments();

            var sw = Stopwatch.StartNew();

            // This property is relatively expensive to call for every revision, so
            // cache it for the duration of the loop.
            var logOutputEncoding = module.LogOutputEncoding;

            using (var process = module.RunGitCmdDetached(arguments.ToString(), GitModule.LosslessEncoding))
            {
                token.ThrowIfCancellationRequested();

                // Pool string values likely to form a small set: encoding, authorname, authoremail, committername, committeremail
                var stringPool = new StringPool();

                var buffer = new byte[4096];

                foreach (var chunk in process.StandardOutput.BaseStream.ReadNullTerminatedChunks(ref buffer))
                {
                    token.ThrowIfCancellationRequested();

                    if (TryParseRevision(module, chunk, stringPool, logOutputEncoding, out var revision))
                    {
                        if (revisionPredicate == null || revisionPredicate(revision))
                        {
                            // Remove full commit message to reduce memory consumption (28% for a repo with 69K commits)
                            // Full commit message is used in InMemFilter but later it's not needed
                            revision.Body = null;

                            // Look up any refs associate with this revision
                            revision.Refs = refsByObjectId[revision.Guid].AsReadOnlyList();

                            RevisionCount++;

                            subject.OnNext(revision);
                        }
                    }
                }

                Trace.WriteLine($"**** [{nameof(RevisionReader)}] Emitted {RevisionCount} revisions in {sw.Elapsed.TotalMilliseconds:#,##0.#} ms. bufferSize={buffer.Length} poolCount={stringPool.Count}");
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);

            if (!token.IsCancellationRequested)
            {
                subject.OnCompleted();
            }

            ArgumentBuilder BuildArguments()
            {
                return new ArgumentBuilder
                {
                    "log",
                    "-z",
                    $"--pretty=format:\"{fullFormat}\"",
                    { AppSettings.OrderRevisionByDate, "--date-order", "--topo-order" },
                    { AppSettings.ShowReflogReferences, "--reflog" },
                    {
                        refFilterOptions.HasFlag(RefFilterOptions.All),
                        "--all",
                        new ArgumentBuilder
                        {
                            {
                                refFilterOptions.HasFlag(RefFilterOptions.Branches) &&
                                !branchFilter.IsNullOrWhiteSpace() &&
                                branchFilter.IndexOfAny(new[] { '?', '*', '[' }) != -1,
                                "--branches=" + branchFilter
                            },
                            { refFilterOptions.HasFlag(RefFilterOptions.Remotes), "--remotes" },
                            { refFilterOptions.HasFlag(RefFilterOptions.Tags), "--tags" },
                        }.ToString()
                    },
                    { refFilterOptions.HasFlag(RefFilterOptions.Boundary), "--boundary" },
                    { refFilterOptions.HasFlag(RefFilterOptions.ShowGitNotes), "--not --glob=notes --not" },
                    { refFilterOptions.HasFlag(RefFilterOptions.NoMerges), "--no-merges" },
                    { refFilterOptions.HasFlag(RefFilterOptions.FirstParent), "--first-parent" },
                    { refFilterOptions.HasFlag(RefFilterOptions.SimplifyByDecoration), "--simplify-by-decoration" },
                    revisionFilter,
                    "--",
                    pathFilter
                };
            }
        }

        private static void UpdateSelectedRef(GitModule module, IReadOnlyList<IGitRef> refs, string branchName)
        {
            var selectedRef = refs.FirstOrDefault(head => head.Name == branchName);

            if (selectedRef != null)
            {
                selectedRef.Selected = true;

                var localConfigFile = module.LocalConfigFile;
                var selectedHeadMergeSource = refs.FirstOrDefault(
                    head => head.IsRemote
                         && selectedRef.GetTrackingRemote(localConfigFile) == head.Remote
                         && selectedRef.GetMergeWith(localConfigFile) == head.LocalName);

                if (selectedHeadMergeSource != null)
                {
                    selectedHeadMergeSource.SelectedHeadMergeSource = true;
                }
            }
        }

        private static bool TryParseRevision(GitModule module, ArraySegment<byte> chunk, StringPool stringPool, Encoding logOutputEncoding, [CanBeNull] out GitRevision revision)
        {
            revision = default;
            return false;
        }

        public void Dispose()
        {
            _cancellationTokenSequence.Dispose();
        }

        #region Nested type: StringLineReader

        /// <summary>
        /// Simple type to walk along a string, line by line, without redundant allocations.
        /// </summary>
        private struct StringLineReader
        {
            private readonly string _s;
            private int _index;

            public StringLineReader(string s)
            {
                _s = s;
                _index = 0;
            }

            public int Remaining => _s.Length - _index;

            [CanBeNull]
            public string ReadLine([CanBeNull] StringPool pool = null, bool advance = true)
            {
                if (_index == _s.Length)
                {
                    return null;
                }

                var startIndex = _index;
                var endIndex = _s.IndexOf('\n', startIndex);

                if (endIndex == -1)
                {
                    return ReadToEnd(advance);
                }

                if (advance)
                {
                    _index = endIndex + 1;
                }

                return pool != null
                    ? pool.Intern(_s, startIndex, endIndex - startIndex)
                    : _s.Substring(startIndex, endIndex - startIndex);
            }

            [CanBeNull]
            public string ReadToEnd(bool advance = true)
            {
                if (_index == _s.Length)
                {
                    return null;
                }

                var s = _s.Substring(_index);

                if (advance)
                {
                    _index = _s.Length;
                }

                return s;
            }
        }

        #endregion
    }
}
