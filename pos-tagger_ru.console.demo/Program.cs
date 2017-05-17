using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using lingvo.sentsplitting;
using lingvo.morphology;
using lingvo.tokenizing;
using TreeDictionaryTypeEnum = lingvo.morphology.MorphoModelConfig.TreeDictionaryTypeEnum;

namespace lingvo
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Config
    {
        public static readonly string URL_DETECTOR_RESOURCES_XML_FILENAME  = ConfigurationManager.AppSettings[ "URL_DETECTOR_RESOURCES_XML_FILENAME" ];
        public static readonly string SENT_SPLITTER_RESOURCES_XML_FILENAME = ConfigurationManager.AppSettings[ "SENT_SPLITTER_RESOURCES_XML_FILENAME" ];
        public static readonly string TOKENIZER_RESOURCES_XML_FILENAME     = ConfigurationManager.AppSettings[ "TOKENIZER_RESOURCES_XML_FILENAME" ];
        public static readonly string POSTAGGER_MODEL_FILENAME             = ConfigurationManager.AppSettings[ "POSTAGGER_MODEL_FILENAME" ];
        public static readonly string POSTAGGER_TEMPLATE_FILENAME          = ConfigurationManager.AppSettings[ "POSTAGGER_TEMPLATE_FILENAME" ];
        public static readonly string POSTAGGER_RESOURCES_XML_FILENAME     = ConfigurationManager.AppSettings[ "POSTAGGER_RESOURCES_XML_FILENAME" ];

        public static readonly string   MORPHO_BASE_DIRECTORY        = ConfigurationManager.AppSettings[ "MORPHO_BASE_DIRECTORY" ];
        public static readonly string[] MORPHO_MORPHOTYPES_FILENAMES = ConfigurationManager.AppSettings[ "MORPHO_MORPHOTYPES_FILENAMES" ].ToFilesArray();
        public static readonly string[] MORPHO_PROPERNAMES_FILENAMES = ConfigurationManager.AppSettings[ "MORPHO_PROPERNAMES_FILENAMES" ].ToFilesArray();
        public static readonly string[] MORPHO_COMMON_FILENAMES      = ConfigurationManager.AppSettings[ "MORPHO_COMMON_FILENAMES"      ].ToFilesArray();
        private static string[] ToFilesArray( this string value )
        {
            var array = value.Split( new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries )
                             .Select( f => f.Trim() )
                             .ToArray();
            return (array);
        }

        public static readonly string MORPHO_AMBIGUITY_MODEL_FILENAME       = ConfigurationManager.AppSettings[ "MORPHO_AMBIGUITY_MODEL_FILENAME" ];
        public static readonly string MORPHO_AMBIGUITY_TEMPLATE_FILENAME_5G = ConfigurationManager.AppSettings[ "MORPHO_AMBIGUITY_TEMPLATE_FILENAME_5G" ];
        public static readonly string MORPHO_AMBIGUITY_TEMPLATE_FILENAME_3G = ConfigurationManager.AppSettings[ "MORPHO_AMBIGUITY_TEMPLATE_FILENAME_3G" ];
    }
}

namespace lingvo.postagger
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class Program
    {
        private static void Main( string[] args )
        {
            try
            {
                var text = @"Сергей Собянин напомнил, что в 2011 году в Москве были 143 млрд. руб. приняты масштабные программы развития города, в том числе программа ""Безопасный город"" на пять лет, на которую будет выделено финансирование в размере 143 млрд. рублей.";

                ProcessText( text );

                //---ProcessText_without_Morphology( text );
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine( "  [.......finita fusking comedy.......]" );
            Console.ReadLine();
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class PosTaggerEnvironment : IDisposable
        {
            private PosTaggerEnvironment()
            {
            }
            public void Dispose()
            {
                if ( Processor != null )
                {
                    Processor.Dispose();
                    Processor = null;
                }

                if ( MorphoModel != null )
                {
                    MorphoModel.Dispose();
                    MorphoModel = null;
                }

                if ( MorphoAmbiguityResolverModel  != null )
                {
                    MorphoAmbiguityResolverModel.Dispose();
                    MorphoAmbiguityResolverModel = null;
                }
            }

            private MorphoAmbiguityResolverModel MorphoAmbiguityResolverModel
            {
                get;
                set;
            }
            private IMorphoModel MorphoModel
            {
                get;
                set;
            }
            public PosTaggerProcessor Processor
            {
                get;
                private set;
            }


            public static PosTaggerProcessorConfig      CreatePosTaggerProcessorConfig()
            {
                var sentSplitterConfig = new SentSplitterConfig( Config.SENT_SPLITTER_RESOURCES_XML_FILENAME,
                                                                 Config.URL_DETECTOR_RESOURCES_XML_FILENAME );
                var config = new PosTaggerProcessorConfig( Config.TOKENIZER_RESOURCES_XML_FILENAME,
                                                           Config.POSTAGGER_RESOURCES_XML_FILENAME,
                                                           LanguageTypeEnum.Ru,
                                                           sentSplitterConfig )
                {
                    ModelFilename    = Config.POSTAGGER_MODEL_FILENAME,
                    TemplateFilename = Config.POSTAGGER_TEMPLATE_FILENAME,
                };

                return (config);
            }            
            private static MorphoModelConfig            CreateMorphoModelConfig()
            {
                var config = new MorphoModelConfig()
                {
                    TreeDictionaryType   = TreeDictionaryTypeEnum.Native,
                    BaseDirectory        = Config.MORPHO_BASE_DIRECTORY,
                    MorphoTypesFilenames = Config.MORPHO_MORPHOTYPES_FILENAMES,
                    ProperNamesFilenames = Config.MORPHO_PROPERNAMES_FILENAMES,
                    CommonFilenames      = Config.MORPHO_COMMON_FILENAMES,
                    ModelLoadingErrorCallback = (s1, s2) => { }
                };

                return (config);
            }
            private static MorphoAmbiguityResolverModel CreateMorphoAmbiguityResolverModel()
            {
                var config = new MorphoAmbiguityResolverConfig()
                {
                    ModelFilename       = Config.MORPHO_AMBIGUITY_MODEL_FILENAME,
                    TemplateFilename_5g = Config.MORPHO_AMBIGUITY_TEMPLATE_FILENAME_5G,
                    TemplateFilename_3g = Config.MORPHO_AMBIGUITY_TEMPLATE_FILENAME_3G,
                };

                var model = new MorphoAmbiguityResolverModel( config );
                return (model);
            }
            public static PosTaggerEnvironment Create()
            {
                var morphoAmbiguityModel = CreateMorphoAmbiguityResolverModel();
                var morphoModelConfig    = CreateMorphoModelConfig();
                var morphoModel          = MorphoModelFactory.Create( morphoModelConfig );
                var config               = CreatePosTaggerProcessorConfig();

                var posTaggerProcessor = new PosTaggerProcessor( config, morphoModel, morphoAmbiguityModel );

                var x = new PosTaggerEnvironment()
                {
                    MorphoAmbiguityResolverModel = morphoAmbiguityModel,
                    MorphoModel                  = morphoModel,
                    Processor                    = posTaggerProcessor,
                };
                return (x);
            }
        }

        private static void ProcessText( string text )
        {
            using ( var posTagger = PosTaggerEnvironment.Create() )
            {
                Console.WriteLine( "\r\n-------------------------------------------------\r\n text: '" + text + '\'' );

                var result = posTagger.Processor.Run( text, true );

                Console.WriteLine( "-------------------------------------------------\r\n pos-tagger-entity-count: " + result.Count + Environment.NewLine );
                foreach ( var word in result )
                {
                    Console.WriteLine( word );
                }
                Console.WriteLine();

                var result_debug = posTagger.Processor.Run_Debug( text, true, true, true, true );

                Console.WriteLine( "-------------------------------------------------\r\n pos-tagger-entity-count: " + result_debug.Count + Environment.NewLine );
                foreach ( var r in result_debug )
                {
                    foreach ( var word in r )
                    {
                        Console.WriteLine( word );
                    }
                    Console.WriteLine();
                }
                Console.WriteLine( "-------------------------------------------------\r\n" );
            }
        }

        //=============== Only PoS-Tagger (without Morphology) ===============//
        private static void ProcessText_without_Morphology( string text )
        {
            var config = PosTaggerEnvironment.CreatePosTaggerProcessorConfig();

            using ( var tokenizer = new Tokenizer( config.TokenizerConfig ) )
            using ( var posTaggerScriber = PosTaggerScriber.Create( config.ModelFilename, config.TemplateFilename ) )
            {
                var posTaggerPreMerging = new PosTaggerPreMerging( config.Model );
                var result              = new List< word_t >();

                tokenizer.Run( text, true, words =>
                {
                    //-merge-phrases-abbreviations-numbers-
                    posTaggerPreMerging.Run( words );

                    //directly pos-tagging
                    posTaggerScriber.Run( words );

                    result.AddRange( words );
                });

                Console.WriteLine( "pos-tagger-entity-count: " + result.Count + Environment.NewLine );
                foreach ( var w in result )
                {
                    Console.WriteLine( w );
                }
                Console.WriteLine();

                posTaggerPreMerging = null;
                result              = null;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Ext
    {
        public static T ToEnum< T >( this string value ) where T : struct
        {
            var e = (T) Enum.Parse( typeof( T ), value, true );
            return (e);
        }
    }
}
