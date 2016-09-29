using System;
using System.Collections.Generic;
using System.Linq;

namespace GitUI.UserControls.RevisionGridClasses
{
    class RestoreSelectionInfo
    {
        public readonly List<string> IdsToRestore = new List<string>();
        public HashSet<string> IdsToRestoreWhileInitialLoad = new HashSet<string>();

        public void SetSelectionToRestore(IEnumerable<string> aSelectedIds)
        {
            Clear();
            IdsToRestore.AddAll(aSelectedIds);
            IdsToRestore.ForEach(id => IdsToRestoreWhileInitialLoad.Add(id));
        }

        public RestoreSelectionInfo()
        {
        }

        public bool ThereAreIdsNotRestoredWhileInitialLoad()
        {
            return IdsToRestoreWhileInitialLoad.Count > 0;
        }

        public void Clear()
        {
            IdsToRestore.Clear();
            IdsToRestoreWhileInitialLoad.Clear();
        }
    }
}
