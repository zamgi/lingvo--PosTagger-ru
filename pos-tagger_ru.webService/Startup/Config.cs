using System;
using System.Configuration;
using System.Linq;

using captcha;
using lingvo.morphology;
using lingvo.sentsplitting;
using lingvo.tokenizing;
using TreeDictionaryTypeEnum = lingvo.morphology.MorphoModelConfig.TreeDictionaryTypeEnum;

namespace lingvo.postagger.webService
{
    /// <summary>
    /// 
    /// </summary>
    public interface IConfig : IAntiBotConfig
    {
        int CONCURRENT_FACTORY_INSTANCE_COUNT { get; }

        string TOKENIZER_RESOURCES_XML_FILENAME     { get; }
        string POSTAGGER_MODEL_FILENAME             { get; }
        string POSTAGGER_TEMPLATE_FILENAME          { get; }
        string POSTAGGER_RESOURCES_XML_FILENAME     { get; }
        string SENT_SPLITTER_RESOURCES_XML_FILENAME { get; }
        string URL_DETECTOR_RESOURCES_XML_FILENAME  { get; }

        string   MORPHO_BASE_DIRECTORY         { get; }
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
    internal sealed class Config : IConfig, IAntiBotConfig
    {
        public Config() { }

        public int? SameIpBannedIntervalInSeconds  { get; } = int.TryParse( ConfigurationManager.AppSettings[ "SAME_IP_BANNED_INTERVAL_IN_SECONDS"  ], out var i ) ? i : null;
        public int? SameIpIntervalRequestInSeconds { get; } = int.TryParse( ConfigurationManager.AppSettings[ "SAME_IP_INTERVAL_REQUEST_IN_SECONDS" ], out var i ) ? i : null;
        public int? SameIpMaxRequestInInterval     { get; } = int.TryParse( ConfigurationManager.AppSettings[ "SAME_IP_MAX_REQUEST_IN_INTERVAL"     ], out var i ) ? i : null;
        public string CaptchaPageTitle => "Определение частей речи/Нормализация текста: приведение всех слов к словарной форме";


        public int CONCURRENT_FACTORY_INSTANCE_COUNT { get; } = int.Parse( ConfigurationManager.AppSettings[ "CONCURRENT_FACTORY_INSTANCE_COUNT" ] );
        //public int MAX_INPUTTEXT_LENGTH { get; } = ConfigurationManager.AppSettings[ "MAX_INPUTTEXT_LENGTH" ].ToInt32();


        public string TOKENIZER_RESOURCES_XML_FILENAME     { get; } = ConfigurationManager.AppSettings[ "TOKENIZER_RESOURCES_XML_FILENAME" ];
        public string POSTAGGER_MODEL_FILENAME             { get; } = ConfigurationManager.AppSettings[ "POSTAGGER_MODEL_FILENAME" ];
        public string POSTAGGER_TEMPLATE_FILENAME          { get; } = ConfigurationManager.AppSettings[ "POSTAGGER_TEMPLATE_FILENAME" ];
        public string POSTAGGER_RESOURCES_XML_FILENAME     { get; } = ConfigurationManager.AppSettings[ "POSTAGGER_RESOURCES_XML_FILENAME" ];
        public string SENT_SPLITTER_RESOURCES_XML_FILENAME { get; } = ConfigurationManager.AppSettings[ "SENT_SPLITTER_RESOURCES_XML_FILENAME" ];
        public string URL_DETECTOR_RESOURCES_XML_FILENAME  { get; } = ConfigurationManager.AppSettings[ "URL_DETECTOR_RESOURCES_XML_FILENAME" ];

        public string   MORPHO_BASE_DIRECTORY         { get; } = ConfigurationManager.AppSettings[ "MORPHO_BASE_DIRECTORY" ];
        public string[] MORPHO_MORPHOTYPES_FILENAMES  { get; } = ConfigurationManager.AppSettings[ "MORPHO_MORPHOTYPES_FILENAMES" ].ToFilesArray();
        public string[] MORPHO_PROPERNAMES_FILENAMES  { get; } = ConfigurationManager.AppSettings[ "MORPHO_PROPERNAMES_FILENAMES" ].ToFilesArray();
        public string[] MORPHO_COMMON_FILENAMES       { get; } = ConfigurationManager.AppSettings[ "MORPHO_COMMON_FILENAMES" ].ToFilesArray();        

        public string MORPHO_AMBIGUITY_MODEL_FILENAME       { get; } = ConfigurationManager.AppSettings[ "MORPHO_AMBIGUITY_MODEL_FILENAME" ];
        public string MORPHO_AMBIGUITY_TEMPLATE_FILENAME_5G { get; } = ConfigurationManager.AppSettings[ "MORPHO_AMBIGUITY_TEMPLATE_FILENAME_5G" ];
        public string MORPHO_AMBIGUITY_TEMPLATE_FILENAME_3G { get; } = ConfigurationManager.AppSettings[ "MORPHO_AMBIGUITY_TEMPLATE_FILENAME_3G" ];

        public (PosTaggerProcessorConfig config, SentSplitterConfig sentSplitterConfig) CreatePosTaggerProcessorConfig()
        {
            var sentSplitterConfig = new SentSplitterConfig( SENT_SPLITTER_RESOURCES_XML_FILENAME, URL_DETECTOR_RESOURCES_XML_FILENAME );
            var config = new PosTaggerProcessorConfig( TOKENIZER_RESOURCES_XML_FILENAME, POSTAGGER_RESOURCES_XML_FILENAME, LanguageTypeEnum.Ru, sentSplitterConfig )
            {
                ModelFilename    = POSTAGGER_MODEL_FILENAME,
                TemplateFilename = POSTAGGER_TEMPLATE_FILENAME,
            };
            return (config, sentSplitterConfig);
        }            
        public MorphoModelConfig CreateMorphoModelConfig() => new MorphoModelConfig()
        {
            TreeDictionaryType   = TreeDictionaryTypeEnum.Native,
            BaseDirectory        = MORPHO_BASE_DIRECTORY,
            MorphoTypesFilenames = MORPHO_MORPHOTYPES_FILENAMES,
            ProperNamesFilenames = MORPHO_PROPERNAMES_FILENAMES,
            CommonFilenames      = MORPHO_COMMON_FILENAMES,
            ModelLoadingErrorCallback = (s1, s2) => { }
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

    /// <summary>
    /// 
    /// </summary>
    internal static class ConfigExtensions
    {
        public static string[] ToFilesArray( this string value ) => value.Split( new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries ).Select( f => f.Trim() ).ToArray();

        public static T ToEnum< T >( this string value ) where T : struct => (T) Enum.Parse( typeof(T), value, true );
        public static int ToInt32( this string value ) => int.Parse( value );
    }
}
