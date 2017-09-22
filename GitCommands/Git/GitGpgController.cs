using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitUIPluginInterfaces;
using System.Threading.Tasks;


namespace GitCommands.Gpg
{
    public enum CommitStatus
    {
        NoSignature = 0,
        GoodSignature = 1,
        SignatureError = 2,
        MissingPublicKey = 3,
    };

    public enum TagStatus
    {
        NoTag = 0,
        OneGood = 1,
        OneBad = 2,
        Many = 3,
        NoPubKey = 4
    };

    public interface IGitGpgController
    {
        /// <summary>
        /// Obtain the commit signature status on current revision.
        /// </summary>
        /// <returns>Enum value that indicate the gpg status for current git revision.</returns>
        CommitStatus GetRevisionCommitSignatureStatus();

        /// <summary>
        /// Obtain the commit verification message, coming from --pretty="format:%GG" 
        /// </summary>
        /// <returns>Full string coming from GPG analysis on current revision.</returns>
        string GetCommitVerificationMessage();

        /// <summary>
        /// Obtain the tag status on current revision.
        /// </summary>
        /// <returns>Enum value that indicate if current git revision has one tag with good signature, one tag with bad signature or more than one tag.</returns>
        TagStatus GetRevisionTagSignatureStatus();

        /// <summary>
        /// Obtain the tag verification message for all the tag in current git revision 
        /// </summary>
        /// <returns>Full concatenated string coming from GPG analysis on all tags on current git revision.</returns>
        string GetTagVerifyMessage();
    }


    public class GitGpgController : IGitGpgController
    {
        private IGitModule _module;
        private GitRevision _revision;
        private List<IGitRef> _usefulTagRefs;

        /* Commit GPG status */
        private const string GOOD_SIGN = "G";
        private const string BAD_SIGN = "B";
        private const string UNK_SIGN_VALIDITY = "U";
        private const string EXPIRED_SIGN = "X";
        private const string EXPIRED_SIGN_KEY = "Y";
        private const string REVOKED_KEY = "R";
        private const string MISSING_PUB_KEY = "E";
        private const string NO_SIGN = "N";


        /* Tag GPG status */
        private const string _goodSignature = "GOODSIG";
        private const string _validTagSign = "VALIDSIG";
        private const string _noTagPubKey = "NO_PUBKEY";

        private static Regex validSignatureTagRegex = new Regex(_validTagSign, RegexOptions.Compiled);
        private static Regex goodSignatureTagRegex = new Regex(_goodSignature, RegexOptions.Compiled);

        /// <summary>
        /// Obtain the tag verification message for all the tag in current git revision 
        /// </summary>
        /// <returns>Full concatenated string coming from GPG analysis on all tags on current git revision.</returns>
        private Lazy<string> _tagVerifyMessage;

        public GitGpgController(IGitModule module, GitRevision revision)
        {
            _module = module;
            _revision = revision;

            _usefulTagRefs = _revision.Refs.Where(x => x.IsTag && x.IsDereference).ToList<IGitRef>();
            _tagVerifyMessage = new Lazy<string>(EvaluateTagVerifyMessage);
        }

        /// <summary>
        /// Obtain the commit signature status on current revision.
        /// </summary>
        /// <returns>Enum value that indicate the gpg status for current git revision.</returns>
        public CommitStatus GetRevisionCommitSignatureStatus()
        {
            CommitStatus cmtStatus = CommitStatus.NoSignature;
            string gpg = _module.RunGitCmd($"log --pretty=\"format:%G?\" -1 {_revision.Guid}");

            switch (gpg)
            {
                case GOOD_SIGN:         // "G" for a good (valid) signature
                    cmtStatus = CommitStatus.GoodSignature;
                    break;
                case BAD_SIGN:          // "B" for a bad signature
                case UNK_SIGN_VALIDITY: // "U" for a good signature with unknown validity 
                case EXPIRED_SIGN:      // "X" for a good signature that has expired
                case EXPIRED_SIGN_KEY:  // "Y" for a good signature made by an expired key
                case REVOKED_KEY:       // "R" for a good signature made by a revoked key
                    cmtStatus = CommitStatus.SignatureError;
                    break;
                case MISSING_PUB_KEY:   // "E" if the signature cannot be checked (e.g.missing key)
                    cmtStatus = CommitStatus.MissingPublicKey;
                    break;
                case NO_SIGN:           // "N" for no signature
                default:
                    cmtStatus = CommitStatus.NoSignature;
                    break;
            }

            return cmtStatus;
        }


        /// <summary>
        /// Obtain the tag status on current revision.
        /// </summary>
        /// <returns>Enum value that indicate if current git revision has one tag with good signature, one tag with bad signature or more than one tag.</returns>
        public TagStatus GetRevisionTagSignatureStatus()
        {
            TagStatus tagStatus = TagStatus.NoTag;

            /* No Tag present, exit */
            if (_usefulTagRefs.Count == 0)
            {
                return tagStatus;
            }

            Match goodSignatureMatch = null;
            Match validSignatureMatch = null;

            /* More than one tag on the revision */
            if (_usefulTagRefs.Count > 1)
            {
                tagStatus = TagStatus.Many;
            }
            else
            {
                /* Raw message to be checked */
                string rawGpgMessage = GetTagVerificationMessage(_usefulTagRefs[0], true);

                /* Look for icon to be shown */
                goodSignatureMatch = goodSignatureTagRegex.Match(rawGpgMessage);
                validSignatureMatch = validSignatureTagRegex.Match(rawGpgMessage);

                Regex noPubKeyTagRegex = new Regex(_noTagPubKey);
                Match noPubKeyMatch = noPubKeyTagRegex.Match(rawGpgMessage);

                if (goodSignatureMatch.Success && validSignatureMatch.Success)
                {
                    tagStatus = TagStatus.OneGood;
                }
                else
                {
                    if (noPubKeyMatch.Success)
                    {
                        tagStatus = TagStatus.NoPubKey;
                    }
                }
            }

            return tagStatus;
        }


        /// <summary>
        /// Obtain the commit verification message, coming from --pretty="format:%GG" 
        /// </summary>
        /// <returns>Full string coming from GPG analysis on current revision.</returns>
        public string GetCommitVerificationMessage()
        {
            return _module.RunGitCmd($"log --pretty=\"format:%GG\" -1 {_revision.Guid}");
        }

        private string GetTagVerificationMessage(IGitRef tagRef, bool raw = true)
        {
            string tagName = tagRef.LocalName;
            if (string.IsNullOrWhiteSpace(tagName))
                return null;

            string rawFlag = raw == true ? "--raw" : "";

            return _module.RunGitCmd($"verify-tag {rawFlag} {tagName}");
        }

        public string GetTagVerifyMessage()
        {
            return _tagVerifyMessage.Value;
        }

        private string EvaluateTagVerifyMessage()
        {
            if (_usefulTagRefs.Count == 0)
            {
                return "";
            }
            else if (_usefulTagRefs.Count == 1)
            {
                return GetTagVerificationMessage(_usefulTagRefs[0], false);
            }
            else
            {
                string tagVerifyMessage = "";

                /* Only to populate TagVerifyMessage */
                foreach (var tagRef in _usefulTagRefs)
                {
                    /* String printed in dialog box */
                    tagVerifyMessage = $"{tagVerifyMessage}{tagRef.LocalName}\r\n{GetTagVerificationMessage(tagRef, false)}\r\n\r\n";
                }
                return tagVerifyMessage;
            }
        }
    }
}
