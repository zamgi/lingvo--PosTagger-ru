using System;

using lingvo.morphology;
using lingvo.sentsplitting;
using lingvo.tokenizing;
using TreeDictionaryTypeEnum = lingvo.morphology.MorphoModelConfig.TreeDictionaryTypeEnum;

namespace lingvo.postagger
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPosTaggerEnvironmentConfig
    {
        string TOKENIZER_RESOURCES_XML_FILENAME     { get; }
        string POSTAGGER_MODEL_FILENAME             { get; }
        string POSTAGGER_TEMPLATE_FILENAME          { get; }
        string POSTAGGER_RESOURCES_XML_FILENAME     { get; }
        string SENT_SPLITTER_RESOURCES_XML_FILENAME { get; }
        string URL_DETECTOR_RESOURCES_XML_FILENAME  { get; }

        string[] MORPHO_MORPHOTYPES_FILENAMES  { get; }
        string[] MORPHO_PROPERNAMES_FILENAMES  { get; }
        string[] MORPHO_COMMON_FILENAMES       { get; }

        string MORPHO_AMBIGUITY_MODEL_FILENAME       { get; }
        string MORPHO_AMBIGUITY_TEMPLATE_FILENAME_5G { get; }
        string MORPHO_AMBIGUITY_TEMPLATE_FILENAME_3G { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class PosTaggerEnvironmentConfigBase : IPosTaggerEnvironmentConfig
    {
        public abstract string TOKENIZER_RESOURCES_XML_FILENAME     { get; }
        public abstract string POSTAGGER_MODEL_FILENAME             { get; }
        public abstract string POSTAGGER_TEMPLATE_FILENAME          { get; }
        public abstract string POSTAGGER_RESOURCES_XML_FILENAME     { get; }
        public abstract string SENT_SPLITTER_RESOURCES_XML_FILENAME { get; }
        public abstract string URL_DETECTOR_RESOURCES_XML_FILENAME  { get; }

        public abstract string[] MORPHO_MORPHOTYPES_FILENAMES  { get; }
        public abstract string[] MORPHO_PROPERNAMES_FILENAMES  { get; }
        public abstract string[] MORPHO_COMMON_FILENAMES       { get; }

        public abstract string MORPHO_AMBIGUITY_MODEL_FILENAME       { get; }
        public abstract string MORPHO_AMBIGUITY_TEMPLATE_FILENAME_5G { get; }
        public abstract string MORPHO_AMBIGUITY_TEMPLATE_FILENAME_3G { get; }        


        public (PosTaggerProcessorConfig config, SentSplitterConfig sentSplitterConfig) CreatePosTaggerProcessorConfig( LanguageTypeEnum languageType )
        {
            var sentSplitterConfig = new SentSplitterConfig( SENT_SPLITTER_RESOURCES_XML_FILENAME, URL_DETECTOR_RESOURCES_XML_FILENAME );
            var config = new PosTaggerProcessorConfig( TOKENIZER_RESOURCES_XML_FILENAME, POSTAGGER_RESOURCES_XML_FILENAME, languageType, sentSplitterConfig )
            {
                ModelFilename    = POSTAGGER_MODEL_FILENAME,
                TemplateFilename = POSTAGGER_TEMPLATE_FILENAME,
            };
            return (config, sentSplitterConfig);
        }            
        public MorphoModelConfig CreateMorphoModelConfig( Action< string, string > modelLoadingErrorCallback = null ) => new MorphoModelConfig()
        {
            TreeDictionaryType   = TreeDictionaryTypeEnum.Native,
            MorphoTypesFilenames = MORPHO_MORPHOTYPES_FILENAMES,
            ProperNamesFilenames = MORPHO_PROPERNAMES_FILENAMES,
            CommonFilenames      = MORPHO_COMMON_FILENAMES,
            ModelLoadingErrorCallback = modelLoadingErrorCallback ?? ((s1, s2) => { })
        };
        public MorphoAmbiguityResolverModel CreateMorphoAmbiguityResolverModel()
        {
            var config = new MorphoAmbiguityResolverConfig()
            {
                ModelFilename       = MORPHO_AMBIGUITY_MODEL_FILENAME,
                TemplateFilename_5g = MORPHO_AMBIGUITY_TEMPLATE_FILENAME_5G,
                TemplateFilename_3g = MORPHO_AMBIGUITY_TEMPLATE_FILENAME_3G,
            };
            var model = new MorphoAmbiguityResolverModel( config );
            return (model);
        }
    }
}
