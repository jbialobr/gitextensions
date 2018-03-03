﻿using System.Collections.Generic;
using System.Windows.Forms;
using GitCommands;
using GitUI.CommandsDialogs;
using GitUIPluginInterfaces;
using ResourceManager;

namespace GitUI.RepoObjectsTree
{
    partial class RepoObjectsTree
    {
        private class TagNode : BaseBranchNode
        {
            private readonly IGitRef _tagInfo;

            public TagNode(Tree aTree, string aFullPath, IGitRef tagInfo) : base(aTree, aFullPath)
            {
                _tagInfo = tagInfo;
            }

            internal override void OnSelected()
            {
                base.OnSelected();
                SelectRevision();
            }

            internal override void OnDoubleClick()
            {
                CreateBranch();
            }

            public void CreateBranch()
            {
                UICommands.StartCreateBranchDialog(TreeViewNode.TreeView, new GitRevision(_tagInfo.Guid));
            }

            public void Delete()
            {
                UICommands.StartDeleteTagDialog(TreeViewNode.TreeView, _tagInfo.Name);
            }

            protected override void ApplyStyle()
            {
                base.ApplyStyle();
                TreeViewNode.ImageKey = TreeViewNode.SelectedImageKey = @"tag.png";
            }

            public void Checkout()
            {
                using (var form = new FormCheckoutRevision(UICommands))
                {
                    form.SetRevision(FullPath);
                    form.ShowDialog(TreeViewNode.TreeView);
                }
            }
        }

        private class TagTree : Tree
        {
            public TagTree(TreeNode aTreeNode, IGitUICommandsSource uiCommands)
                : base(aTreeNode, uiCommands)
            {
                uiCommands.GitUICommandsChanged += UiCommands_GitUICommandsChanged;
            }

            private void UiCommands_GitUICommandsChanged(object sender, GitUICommandsChangedEventArgs e)
            {
                if (TreeViewNode?.TreeView == null)
                {
                    return;
                }
                TreeViewNode.TreeView.SelectedNode = null;
            }

            protected override void LoadNodes(System.Threading.CancellationToken token)
            {
                FillTagTree(Module.GetTagRefs(GitModule.GetTagRefsSortOrder.ByName));
            }

            private void FillTagTree(IEnumerable<IGitRef> tags)
            {
                var nodes = new Dictionary<string, BaseBranchNode>();
                var branchFullPaths = new List<string>();
                foreach (var tag in tags)
                {
                    var branchNode = new TagNode(this, tag.Name, tag);
                    var parent = branchNode.CreateRootNode(nodes,
                        (tree, parentPath) => new BasePathNode(tree, parentPath));
                    if (parent != null)
                        Nodes.AddNode(parent);
                    branchFullPaths.Add(branchNode.FullPath);
                }
                FireBranchAddedEvent(branchFullPaths);
            }

            protected override void FillTreeViewNode()
            {
                base.FillTreeViewNode();

                TreeViewNode.Text = $@"{Strings.TagsText} ({Nodes.Count})";

                TreeViewNode.Collapse();
            }
        }
    }
}
