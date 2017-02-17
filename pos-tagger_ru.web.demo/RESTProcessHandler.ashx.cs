using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;

using lingvo.morphology;
using lingvo.postagger;
using lingvo.sentsplitting;
using lingvo.tokenizing;
using Newtonsoft.Json;
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

        public static readonly string   MORPHO_BASE_DIRECTORY         = ConfigurationManager.AppSettings[ "MORPHO_BASE_DIRECTORY" ];
        public static readonly string[] MORPHO_MORPHOTYPES_FILENAMES  = ConfigurationManager.AppSettings[ "MORPHO_MORPHOTYPES_FILENAMES" ].ToFilesArray();
        public static readonly string[] MORPHO_PROPERNAMES_FILENAMES  = ConfigurationManager.AppSettings[ "MORPHO_PROPERNAMES_FILENAMES" ].ToFilesArray();
        public static readonly string[] MORPHO_COMMON_FILENAMES       = ConfigurationManager.AppSettings[ "MORPHO_COMMON_FILENAMES"      ].ToFilesArray();
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

        public static readonly int    MAX_INPUTTEXT_LENGTH                = ConfigurationManager.AppSettings[ "MAX_INPUTTEXT_LENGTH"                ].ToInt32();
        public static readonly int    CONCURRENT_FACTORY_INSTANCE_COUNT   = ConfigurationManager.AppSettings[ "CONCURRENT_FACTORY_INSTANCE_COUNT"   ].ToInt32();
        public static readonly int    SAME_IP_INTERVAL_REQUEST_IN_SECONDS = ConfigurationManager.AppSettings[ "SAME_IP_INTERVAL_REQUEST_IN_SECONDS" ].ToInt32();
        public static readonly int    SAME_IP_MAX_REQUEST_IN_INTERVAL     = ConfigurationManager.AppSettings[ "SAME_IP_MAX_REQUEST_IN_INTERVAL"     ].ToInt32();        
        public static readonly int    SAME_IP_BANNED_INTERVAL_IN_SECONDS  = ConfigurationManager.AppSettings[ "SAME_IP_BANNED_INTERVAL_IN_SECONDS"  ].ToInt32();
    }
}

namespace lingvo.postagger
{
    /// <summary>
    /// Summary description for RESTProcessHandler
    /// </summary>
    public sealed class RESTProcessHandler : IHttpHandler
    {
        /// <summary>
        /// 
        /// </summary>
        private abstract class result_base
        {
            protected result_base()
            {
            }
            protected result_base( Exception ex )
            {
                exceptionMessage = ex.ToString();
            }

            [JsonProperty(PropertyName="err")]
            public string exceptionMessage
            {
                get;
                private set;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public struct morpho_info
        {
            public morpho_info( WordFormMorphology_t morphology ) : this()
            {
                normalForm      = morphology.NormalForm;
                partOfSpeech    = morphology.PartOfSpeech.ToString();
                morphoAttribute = !morphology.IsEmptyMorphoAttribute() ? morphology.MorphoAttribute.ToString() : "-";
            }

            [JsonProperty(PropertyName="nf")]  public string normalForm
            {
                get;
                set;
            }
            [JsonProperty(PropertyName="pos")] public string partOfSpeech
            {
                get;
                set;
            }                 
            [JsonProperty(PropertyName="ma")]  public string morphoAttribute
            {
                get;
                set;
            }                 
        }
        /// <summary>
        /// 
        /// </summary>
        public sealed class word_info
        {
            [JsonProperty(PropertyName="i")]      public int          startIndex
            {
                get;
                set;
            }
            [JsonProperty(PropertyName="l")]      public int          length
            {
                get;
                set;
            }
            [JsonProperty(PropertyName="v")]      public string       value
            {
                get;
                set;
            }
            [JsonProperty(PropertyName="p")]      public bool         isPunctuation
            {
                get;
                set;
            }
            [JsonProperty(PropertyName="pos")]    public string       posTaggerOutputType
            {
                get;
                set;
            }                 
            [JsonProperty(PropertyName="morpho")] public morpho_info? morpho
            {
                get;
                set;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class result_json_by_sent : result_base
        {
            public result_json_by_sent( Exception ex ) : base( ex )
            {
            }
            public result_json_by_sent( List< word_t[] > _sents )
            {
                sents = new List< word_info[] >( _sents.Count );

                foreach ( var words_by_sent in _sents )
                {
                    var words = (from word in words_by_sent
                                    select
                                        new word_info()
                                        {
                                            startIndex    = word.startIndex,
                                            length        = word.length,
                                            value         = word.valueOriginal,
                                            posTaggerOutputType = word.posTaggerOutputType.ToString(),
                                            isPunctuation = (word.posTaggerExtraWordType == PosTaggerExtraWordType.Punctuation),
                                            morpho        = !word.morphology.IsEmpty()
                                                            ? new morpho_info( word.morphology ) 
                                                            : ((morpho_info?) null),
                                        }
                                ).ToArray();
                    sents.Add( words );
                }


            }

            public List< word_info[] > sents
            {
                get;
                private set;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private struct http_context_data
        {
            private static readonly object _SyncLock = new object();

            private static MorphoModelConfig             CreateMorphoModelConfig()
            {
                //_ModelLoadingErrors.Clear();

                var config = new MorphoModelConfig()
                {
                    TreeDictionaryType   = TreeDictionaryTypeEnum.Native,
                    BaseDirectory        = Config.MORPHO_BASE_DIRECTORY,
                    MorphoTypesFilenames = Config.MORPHO_MORPHOTYPES_FILENAMES,
                    ProperNamesFilenames = Config.MORPHO_PROPERNAMES_FILENAMES,
                    CommonFilenames      = Config.MORPHO_COMMON_FILENAMES,
                    ModelLoadingErrorCallback = (s1, s2) =>
                    {
                        //_ModelLoadingErrors.Append( s1 ).Append( " - " ).Append( s2 ).Append( "<br/>" );
    #if DEBUG
                        //Debug.WriteLine( s1 + " - " + s2 );
                        Console.WriteLine( s1 + " - " + s2 );
    #endif
                    }
                };

                return (config);
            }
            private static PosTaggerProcessorConfig      CreatePosTaggerProcessorConfig()
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
            private static MorphoAmbiguityResolverConfig CreateMorphoAmbiguityConfig()
            {
                var config = new MorphoAmbiguityResolverConfig()
                {
                    ModelFilename       = Config.MORPHO_AMBIGUITY_MODEL_FILENAME,
                    TemplateFilename_5g = Config.MORPHO_AMBIGUITY_TEMPLATE_FILENAME_5G,
                    TemplateFilename_3g = Config.MORPHO_AMBIGUITY_TEMPLATE_FILENAME_3G,
                };

                return (config);
            }
            private static MorphoAmbiguityResolverModel  CreateMorphoAmbiguityResolverModel( MorphoAmbiguityResolverConfig config )
            {
                var model = new MorphoAmbiguityResolverModel( config );
                return (model);
            }

            private static ConcurrentFactory _ConcurrentFactory;

            public static ConcurrentFactory GetConcurrentFactory()
            {
                var f = _ConcurrentFactory;
                if ( f == null )
                {
                    lock ( _SyncLock )
                    {
                        f = _ConcurrentFactory;
                        if ( f == null )
                        {
                            {
                                var config               = CreatePosTaggerProcessorConfig();
                                var morphoModel          = MorphoModelFactory.Create( CreateMorphoModelConfig() );
                                var morphoAmbiguityModel = CreateMorphoAmbiguityResolverModel( CreateMorphoAmbiguityConfig() );

                                f = new ConcurrentFactory( config, morphoModel, morphoAmbiguityModel, Config.CONCURRENT_FACTORY_INSTANCE_COUNT );
                                _ConcurrentFactory = f;
                            }
                            {
                                GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                                GC.WaitForPendingFinalizers();
                                GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                            }
                        }
                    }
                }
                return (f);
            }
        }


        static RESTProcessHandler()
        {
            Environment.CurrentDirectory = HttpContext.Current.Server.MapPath( "~/" );
        }

        public bool IsReusable
        {
            get { return (true); }
        }

        public void ProcessRequest( HttpContext context )
        {
            try
            {
                #region [.anti-bot.]
                var antiBot = context.ToAntiBot();
                if ( antiBot.IsNeedRedirectOnCaptchaIfRequestNotValid() )
                {
                    antiBot.SendGotoOnCaptchaJsonResponse();
                    return;
                }                
                #endregion

                var text          = context.GetRequestStringParam( "text", Config.MAX_INPUTTEXT_LENGTH );
                var splitBySmiles = context.Request[ "splitBySmiles" ].Try2Boolean( true );                

                #region [.anti-bot.]
                antiBot.MarkRequestEx( text );
                #endregion

                var words = http_context_data.GetConcurrentFactory().Run_Debug( text, splitBySmiles );

                SendJsonResponse( context, words );
            }
            catch ( Exception ex )
            {
                SendJsonResponse( context, ex );
            }
        }

        private static void SendJsonResponse( HttpContext context, List< word_t[] > words )
        {
            SendJsonResponse( context, new result_json_by_sent( words ) );
        }
        private static void SendJsonResponse( HttpContext context, Exception ex )
        {
            SendJsonResponse( context, new result_json_by_sent( ex ) );
        }
        private static void SendJsonResponse( HttpContext context, result_base result )
        {
            context.Response.ContentType = "application/json";
            //---context.Response.Headers.Add( "Access-Control-Allow-Origin", "*" );

            var json = JsonConvert.SerializeObject( result );
            context.Response.Write( json );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static bool Try2Boolean( this string value, bool defaultValue )
        {
            if ( value != null )
            {
                var result = default(bool);
                if ( bool.TryParse( value, out result ) )
                    return (result);
            }
            return (defaultValue);
        }

        public static T ToEnum< T >( this string value ) where T : struct
        {
            var result = (T) Enum.Parse( typeof(T), value, true );
            return (result);
        }
        public static int ToInt32( this string value )
        {
            return (int.Parse( value ));
        }

        public static string GetRequestStringParam( this HttpContext context, string paramName, int maxLength )
        {
            var value = context.Request[ paramName ];
            if ( (value != null) && (maxLength < value.Length) && (0 < maxLength) )
            {
                return (value.Substring( 0, maxLength ));
            }
            return (value);
        }
    }
}