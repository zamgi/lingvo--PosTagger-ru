using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace lingvo.postagger
{
    /// <summary>
    /// 
    /// </summary>
    public class PosTaggerEnvironmentConfigImpl : PosTaggerEnvironmentConfigBase
    {
        public PosTaggerEnvironmentConfigImpl()
        {
            RESOURCES_BASE_DIRECTORY = ConfigurationManager.AppSettings[ "RESOURCES_BASE_DIRECTORY" ];

            URL_DETECTOR_RESOURCES_XML_FILENAME  = ConfigurationManager.AppSettings[ "URL_DETECTOR_RESOURCES_XML_FILENAME" ].GetPath( RESOURCES_BASE_DIRECTORY );
            SENT_SPLITTER_RESOURCES_XML_FILENAME = ConfigurationManager.AppSettings[ "SENT_SPLITTER_RESOURCES_XML_FILENAME" ].GetPath( RESOURCES_BASE_DIRECTORY );
            TOKENIZER_RESOURCES_XML_FILENAME     = ConfigurationManager.AppSettings[ "TOKENIZER_RESOURCES_XML_FILENAME" ].GetPath( RESOURCES_BASE_DIRECTORY );

            POSTAGGER_MODEL_FILENAME             = ConfigurationManager.AppSettings[ "POSTAGGER_MODEL_FILENAME" ].GetPath( RESOURCES_BASE_DIRECTORY );
            POSTAGGER_TEMPLATE_FILENAME          = ConfigurationManager.AppSettings[ "POSTAGGER_TEMPLATE_FILENAME" ].GetPath( RESOURCES_BASE_DIRECTORY );
            POSTAGGER_RESOURCES_XML_FILENAME     = ConfigurationManager.AppSettings[ "POSTAGGER_RESOURCES_XML_FILENAME" ].GetPath( RESOURCES_BASE_DIRECTORY );

            MORPHO_BASE_DIRECTORY        = ConfigurationManager.AppSettings[ "MORPHO_BASE_DIRECTORY" ].GetPath( RESOURCES_BASE_DIRECTORY );
            MORPHO_MORPHOTYPES_FILENAMES = ConfigurationManager.AppSettings[ "MORPHO_MORPHOTYPES_FILENAMES" ].GetPath( MORPHO_BASE_DIRECTORY ).ToFilesArray();
            MORPHO_PROPERNAMES_FILENAMES = ConfigurationManager.AppSettings[ "MORPHO_PROPERNAMES_FILENAMES" ].GetPath( MORPHO_BASE_DIRECTORY ).ToFilesArray();
            MORPHO_COMMON_FILENAMES      = ConfigurationManager.AppSettings[ "MORPHO_COMMON_FILENAMES"      ].GetPath( MORPHO_BASE_DIRECTORY ).ToFilesArray();

            MORPHO_AMBIGUITY_MODEL_FILENAME       = ConfigurationManager.AppSettings[ "MORPHO_AMBIGUITY_MODEL_FILENAME" ].GetPath( MORPHO_BASE_DIRECTORY );
            MORPHO_AMBIGUITY_TEMPLATE_FILENAME_5G = ConfigurationManager.AppSettings[ "MORPHO_AMBIGUITY_TEMPLATE_FILENAME_5G" ].GetPath( MORPHO_BASE_DIRECTORY );
            MORPHO_AMBIGUITY_TEMPLATE_FILENAME_3G = ConfigurationManager.AppSettings[ "MORPHO_AMBIGUITY_TEMPLATE_FILENAME_3G" ].GetPath( MORPHO_BASE_DIRECTORY );
        }

        public string RESOURCES_BASE_DIRECTORY { get; }
        public string MORPHO_BASE_DIRECTORY    { get; }

        public override string URL_DETECTOR_RESOURCES_XML_FILENAME  { get; }
        public override string SENT_SPLITTER_RESOURCES_XML_FILENAME { get; }
        public override string TOKENIZER_RESOURCES_XML_FILENAME     { get; }

        public override string POSTAGGER_MODEL_FILENAME             { get; }
        public override string POSTAGGER_TEMPLATE_FILENAME          { get; }
        public override string POSTAGGER_RESOURCES_XML_FILENAME     { get; }

        public override string[] MORPHO_MORPHOTYPES_FILENAMES { get; }
        public override string[] MORPHO_PROPERNAMES_FILENAMES { get; }
        public override string[] MORPHO_COMMON_FILENAMES      { get; }

        public override string MORPHO_AMBIGUITY_MODEL_FILENAME       { get; }
        public override string MORPHO_AMBIGUITY_TEMPLATE_FILENAME_5G { get; }
        public override string MORPHO_AMBIGUITY_TEMPLATE_FILENAME_3G { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class PosTaggerEnvironmentConfigExtensions
    {
        public static string GetPath( this string relPath, string basePath ) => Path.Combine( basePath, relPath?.TrimStart( '/', '\\' ) );
        public static string[] ToFilesArray( this string value ) => value.Split( new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries ).Select( f => f.Trim() ).ToArray();
    }
}
