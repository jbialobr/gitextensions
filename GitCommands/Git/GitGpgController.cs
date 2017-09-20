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
        GoodSignature = 0,
        SignatureError = 1,
        MissingPublicKey = 2,
        NoSignature = 3
    };

    public enum TagStatus
    {
        OneGood = 0,
        OneBad = 1,
        Many = 2,
        NoPubKey = 3
    };

    public interface IGitGpgController
    {
        /// <summary>
        /// Obtain the commit signature status on current revision.
        /// </summary>
        /// <returns>Enum value that indicate the gpg status for current git revision.</returns>
        CommitStatus CheckCommitSign();

        /// <summary>
        /// Obtain the commit verification message, coming from --pretty="format:%GG" 
        /// </summary>
        /// <returns>Full string coming from GPG analysis on current revision.</returns>
        string GetCommitVerificationMessage();

        /// <summary>
        /// Obtain the tag status on current revision.
        /// </summary>
        /// <returns>Enum value that indicate if current git revision has one tag with good signature, one tag with bad signature or more than one tag.</returns>
        TagStatus CheckTagSign();

        /// <summary>
        /// Obtain the number of tag on current git revision.
        /// </summary>
        /// <returns>Returns the number of annotated or signed tag on current revision.</returns>
        int NumberOfTag { get; }

        /// <summary>
        /// Obtain the tag verification message for all the tag in current git revision 
        /// </summary>
        /// <returns>Full concatenated string coming from GPG analysis on all tags on current git revision.</returns>
        string TagVerifyMessage { get; }
    }


    public class GitGpgController : IGitGpgController
    {
        private IGitModule _module;
        private GitRevision _revision;

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
        /// Obtain the number of tag on current git revision.
        /// </summary>
        /// <returns>Returns the number of annotated or signed tag on current revision.</returns>
        public int NumberOfTag { get; private set; }

        /// <summary>
        /// Obtain the tag verification message for all the tag in current git revision 
        /// </summary>
        /// <returns>Full concatenated string coming from GPG analysis on all tags on current git revision.</returns>
        public string TagVerifyMessage { get; private set; }

        public GitGpgController(IGitModule module, GitRevision revision)
        {
            _module = module;
            _revision = revision;

            NumberOfTag = _revision.Refs.Count(x => x.IsTag && x.IsDereference);
            TagVerifyMessage = "";
        }

        /// <summary>
        /// Obtain the commit signature status on current revision.
        /// </summary>
        /// <returns>Enum value that indicate the gpg status for current git revision.</returns>
        public CommitStatus CheckCommitSign()
        {
            CheckVariable();

            CommitStatus _cmtStatus = CommitStatus.NoSignature;
            string _gpg = _module.RunGitCmd($"log --pretty=\"format:%G?\" -1 {_revision.Guid}");

            switch (_gpg)
            {
                case GOOD_SIGN:         // "G" for a good (valid) signature
                    _cmtStatus = CommitStatus.GoodSignature;
                    break;
                case BAD_SIGN:          // "B" for a bad signature
                case UNK_SIGN_VALIDITY: // "U" for a good signature with unknown validity 
                case EXPIRED_SIGN:      // "X" for a good signature that has expired
                case EXPIRED_SIGN_KEY:  // "Y" for a good signature made by an expired key
                case REVOKED_KEY:       // "R" for a good signature made by a revoked key
                    _cmtStatus = CommitStatus.SignatureError;
                    break;
                case MISSING_PUB_KEY:   // "E" if the signature cannot be checked (e.g.missing key)
                    _cmtStatus = CommitStatus.MissingPublicKey;
                    break;
                case NO_SIGN:           // "N" for no signature
                default:
                    _cmtStatus = CommitStatus.NoSignature;
                    break;
            }

            return _cmtStatus;
        }


        /// <summary>
        /// Obtain the tag status on current revision.
        /// </summary>
        /// <returns>Enum value that indicate if current git revision has one tag with good signature, one tag with bad signature or more than one tag.</returns>
        public TagStatus CheckTagSign()
        {
            CheckVariable();

            TagStatus _tagStatus = TagStatus.OneBad;

            IEnumerable<IGitRef> _usefulRef = _revision.Refs.Where(x => x.IsTag && x.IsDereference);

            Match goodSignatureMatch = null;
            Match validSignatureMatch = null;

            /* More than one tag on the revision */
            if (_usefulRef.Count() > 1)
            {
                _tagStatus = TagStatus.Many;

                /* Only to populate TagVerifyMessage */
                foreach (var gitRef in _usefulRef)
                {
                    /* String printed in dialog box */
                    TagVerifyMessage = $"{TagVerifyMessage}{gitRef.LocalName}\r\n{GetTagVerificationMessage(gitRef.LocalName, false)}\r\n\r\n";
                }
            }
            else
            {
                /* Only one tag on the revision */
                var singleTag = _usefulRef.ElementAt(0).LocalName;

                /* Raw message to be checked */
                string _rawGpgMessage = GetTagVerificationMessage(singleTag, true);

                /* String printed in dialog box */
                TagVerifyMessage = $"{GetTagVerificationMessage(singleTag, false)}";

                /* Look for icon to be shown */
                goodSignatureMatch = goodSignatureTagRegex.Match(_rawGpgMessage);
                validSignatureMatch = validSignatureTagRegex.Match(_rawGpgMessage);

                Regex noPubKeyTagRegex = new Regex(_noTagPubKey);
                Match noPubKeyMatch = noPubKeyTagRegex.Match(_rawGpgMessage);

                if (goodSignatureMatch.Success && validSignatureMatch.Success)
                {
                    _tagStatus = TagStatus.OneGood;
                }
                else
                {
                    if (noPubKeyMatch.Success)
                    {
                        _tagStatus = TagStatus.NoPubKey;
                    }
                }
            }

            return _tagStatus;
        }


        /// <summary>
        /// Obtain the commit verification message, coming from --pretty="format:%GG" 
        /// </summary>
        /// <returns>Full string coming from GPG analysis on current revision.</returns>
        public string GetCommitVerificationMessage()
        {
            CheckVariable();

            return _module.RunGitCmd($"log --pretty=\"format:%GG\" -1 {_revision.Guid}");
        }


        private void CheckVariable()
        {
            if (_module == null)
            {
                throw new ArgumentNullException("module: Set Module property before.");
            }

            if (_revision == null)
            {
                throw new ArgumentNullException("revision: Set Revision property before.");
            }
        }

        private string GetTagVerificationMessage(string tag, bool raw = true)
        {
            CheckVariable();

            if (string.IsNullOrWhiteSpace(tag))
                return null;

            string rawFlag = raw == true ? "--raw" : "";

            return _module.RunGitCmd($"verify-tag {rawFlag} {tag}");
        }
    }
}
