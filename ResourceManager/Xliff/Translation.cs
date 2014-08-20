using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ResourceManager.Xliff
{
    /// <summary>Provides a translation for a specific language.</summary>
    [XmlRoot("xliff")]
    public class Translation : ITranslation
    {
        public Translation()
        {
            Version = "1.0";
            _translationCategories = new List<TranslationCategory>();
        }

        public Translation(string gitExVersion, string languageCode)
            : this()
        {
            GitExVersion = gitExVersion;
            _languageCode = languageCode;
        }

        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlAttribute("GitExVersion")]
        public string GitExVersion { get; set; }

        private string _languageCode;
        [XmlAttribute("LanguageCode")]
        public string LanguageCode { get { return _languageCode; } }

        private IList<TranslationCategory> _translationCategories;
        [XmlElement(ElementName = "file")]
        public IEnumerable<TranslationCategory> TranslationCategories { get { return _translationCategories; } }

        private int CategoryNameComparer(string categoryName, TranslationCategory category)
        { 
            return categoryName.TrimStart('_').CompareTo(categoryName.TrimStart('_'));
        }

        public TranslationCategory FindOrAddTranslationCategory(string translationCategory)
        {
            if (string.IsNullOrEmpty(translationCategory))
                new InvalidOperationException("Cannot add translationCategory without name");

            TranslationCategory tc;


            int index = _translationCategories.
            if (index < 0)
            {
                tc = new TranslationCategory(translationCategory, "en");
                _translationCategories.Insert(~index, tc);
            }
            else
            {
                tc = _translationCategories[index];
            }

            return tc;
        }


        public TranslationCategory GetTranslationCategory(string name)
        {
            TranslationCategory tc = new TranslationCategory(name, "en");
            return null;// TranslationCategories.Find(t => t.Name.TrimStart('_') == name.TrimStart('_'));
        }

        private void Sort()
        {

            foreach (TranslationCategory tc in _translationCategories)
                tc.Body.TranslationItems.Sort();
        }

        public void AddTranslationItem(string category, string item, string property, string neutralValue)
        {
            FindOrAddTranslationCategory(category).Body.AddTranslationItemIfNotExist(new TranslationItem(item, property, neutralValue));
        }

        public string TranslateItem(string category, string item, string property, Func<string> provideDefaultValue)
        {
            TranslationCategory tc = FindOrAddTranslationCategory(category);

            TranslationItem ti = tc.Body.GetTranslationItem(item, property);

            if (ti == null)
            {
                //if there is no item, then store its default value
                //to be able to retrieve it later (eg. when to Text is added additional 
                //information and it needs to be refreshed: like Commit (<number of changes>))
                string defaultValue = provideDefaultValue();
                tc.Body.AddTranslationItemIfNotExist(new TranslationItem(item, property, defaultValue));
                return defaultValue;
            }

            if (string.IsNullOrEmpty(ti.Value))
                return ti.Source;

            return ti.Value;
        }

        [OnDeserialized()]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            Sort();
        }
    }
}
