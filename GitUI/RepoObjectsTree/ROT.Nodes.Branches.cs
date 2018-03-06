﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GitCommands;
using GitUI.Properties;
using ResourceManager;

namespace GitUI.RepoObjectsTree
{
    public partial class RepoObjectsTree
    {
        #region private classes

        private abstract class BaseBranchNode : Node
        {
            protected const char PathSeparator = '/';

            /// <summary>Short name of the branch/branch path. <example>"issue1344"</example></summary>
            protected string Name { get; set; }

            private string ParentPath { get; }

            /// <summary>Full path of the branch. <example>"issues/issue1344"</example></summary>
            public string FullPath => ParentPath.Combine(PathSeparator.ToString(), Name);

            public override int GetHashCode()
            {
                return FullPath.GetHashCode();
            }

            private bool Equals(BaseBranchNode other)
            {
                if (other == null)
                {
                    return false;
                }

                return ReferenceEquals(other, this) || string.Equals(FullPath, other.FullPath);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as BaseBranchNode);
            }

            protected BaseBranchNode(Tree aTree, string aFullPath)
                : base(aTree)
            {
                aFullPath = aFullPath.Trim();
                if (aFullPath.IsNullOrEmpty())
                {
                    throw new ArgumentNullException(nameof(aFullPath));
                }

                var dirs = aFullPath.Split(PathSeparator);
                Name = dirs[dirs.Length - 1];
                ParentPath = dirs.Take(dirs.Length - 1).Join(PathSeparator.ToString());
            }

            internal BaseBranchNode CreateRootNode(IDictionary<string, BaseBranchNode> nodes,
                Func<Tree, string, BaseBranchNode> createPathNode)

            {
                if (ParentPath.IsNullOrEmpty())
                {
                    return this;
                }

                BaseBranchNode result;

                if (nodes.TryGetValue(ParentPath, out var parent))
                {
                    result = null;
                }
                else
                {
                    parent = createPathNode(Tree, ParentPath);
                    nodes.Add(ParentPath, parent);
                    result = parent.CreateRootNode(nodes, createPathNode);
                }

                parent.Nodes.AddNode(this);

                return result;
            }

            public override string DisplayText()
            {
                return Name;
            }

            protected void SelectRevision()
            {
                TreeViewNode.TreeView.BeginInvoke(new Action(() =>
                {
                    UICommands.BrowseGoToRef(FullPath, showNoRevisionMsg: true);
                    TreeViewNode.TreeView?.Focus();
                }));
            }
        }

        private sealed class LocalBranchNode : BaseBranchNode
        {
            public LocalBranchNode(Tree aTree, string aFullPath)
                : base(aTree, aFullPath.TrimStart(GitModule.ActiveBranchIndicator))
            {
                IsActive = aFullPath.StartsWith(GitModule.ActiveBranchIndicator.ToString());
            }

            public bool IsActive { get; }

            protected override void ApplyStyle()
            {
                base.ApplyStyle();
                TreeViewNode.ImageKey = TreeViewNode.SelectedImageKey = nameof(MsVsImages.Branch_16x);
                if (IsActive)
                {
                    TreeViewNode.NodeFont = new Font(TreeViewNode.NodeFont, FontStyle.Bold);
                }
            }

            public override bool Equals(object obj)
            {
                if (!base.Equals(obj)) return false;
                return obj is LocalBranchNode localBranchNode && IsActive == localBranchNode.IsActive;

            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            internal override void OnDoubleClick()
            {
                base.OnDoubleClick();
                Checkout();
            }

            internal override void OnSelected()
            {
                base.OnSelected();

                SelectRevision();
            }

            public void Checkout()
            {
                UICommands.StartCheckoutBranch(FullPath, false);
            }

            public void Delete()
            {
                UICommands.StartDeleteBranchDialog(ParentWindow(), new[] { FullPath });
            }

            public void DeleteForce()
            {
                var branchHead = GitRef.CreateBranchRef(UICommands.Module, null, FullPath);
                var cmd = new GitDeleteBranchCmd(new[] { branchHead }, true);
                UICommands.StartCommandLineProcessDialog(cmd, null);
            }
        }

        private class BasePathNode : BaseBranchNode
        {
            public BasePathNode(Tree aTree, string aFullPath) : base(aTree, aFullPath)
            {
            }

            protected override void ApplyStyle()
            {
                base.ApplyStyle();
                TreeViewNode.ImageKey = TreeViewNode.SelectedImageKey = nameof(MsVsImages.Folder_grey_16x);
            }
        }

        private class BranchPathNode : BasePathNode
        {
            public BranchPathNode(Tree aTree, string aFullPath)
                : base(aTree, aFullPath)
            {
            }

            public override string ToString()
            {
                return $"{Name}{PathSeparator}";
            }

            public void DeleteAll()
            {
                var branches = Nodes.DepthEnumerator<LocalBranchNode>().Select(branch => branch.FullPath);
                UICommands.StartDeleteBranchDialog(ParentWindow(), branches);
            }

            public void DeleteAllForce()
            {
                var branches = Nodes.DepthEnumerator<LocalBranchNode>();
                var branchHeads =
                    branches.Select(branch => GitRef.CreateBranchRef(UICommands.Module, null, branch.FullPath));
                var cmd = new GitDeleteBranchCmd(branchHeads, true);
                UICommands.StartCommandLineProcessDialog(cmd, null);
            }
        }

        private class BranchTree : Tree
        {
            public BranchTree(TreeNode aTreeNode, IGitUICommandsSource uiCommands)
                : base(aTreeNode, uiCommands)
            {
                uiCommands.GitUICommandsChanged += UiCommands_GitUICommandsChanged;
            }

            private void UiCommands_GitUICommandsChanged(object sender, GitUICommandsChangedEventArgs e)
            {
                TreeViewNode.TreeView.SelectedNode = null;
            }

            protected override void LoadNodes(System.Threading.CancellationToken token)
            {
                FillBranchTree(Module.GetBranchNames());
            }

            private void FillBranchTree(IEnumerable<string> branches)
            {
                #region ex

                // (input)
                // a-branch
                // develop/crazy-branch
                // develop/features/feat-next
                // develop/features/feat-next2
                // develop/issues/iss444
                // develop/wild-branch
                // issues/iss111
                // master
                //
                // ->
                // (output)
                // 0 a-branch
                // 0 develop/
                // 1   features/
                // 2      feat-next
                // 2      feat-next2
                // 1   issues/
                // 2      iss444
                // 1   wild-branch
                // 1   wilds/
                // 2      card
                // 0 issues/
                // 1     iss111
                // 0 master

                #endregion ex

                var nodes = new Dictionary<string, BaseBranchNode>();
                var branchFullPaths = new List<string>();
                foreach (var branch in branches)
                {
                    var localBranchNode = new LocalBranchNode(this, branch);
                    var parent = localBranchNode.CreateRootNode(nodes,
                        (tree, parentPath) => new BranchPathNode(tree, parentPath));
                    if (parent != null)
                        Nodes.AddNode(parent);
                    branchFullPaths.Add(localBranchNode.FullPath);
                }

                FireBranchAddedEvent(branchFullPaths);
            }

            protected override void FillTreeViewNode()
            {
                base.FillTreeViewNode();

                TreeViewNode.Text = $@"{Strings.BranchesText} ({Nodes.Count})";

                var activeBranch = Nodes.DepthEnumerator<LocalBranchNode>().FirstOrDefault(b => b.IsActive);
                if (activeBranch == null)
                {
                    TreeViewNode.TreeView.SelectedNode = null;
                }
            }
        }
        #endregion private classes

    }
}
