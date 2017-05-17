using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using lingvo.morphology;
using lingvo.sentsplitting;
using lingvo.tokenizing;
using Newtonsoft.Json;
using TreeDictionaryTypeEnum = lingvo.morphology.MorphoModelConfig.TreeDictionaryTypeEnum;

namespace lingvo.postagger
{
    /// <summary>
    /// 
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
        private static class ConcurrentFactoryHelper
        {
            private static readonly object _SyncLock = new object();

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
                        /*
    #if DEBUG
                        //Debug.WriteLine( s1 + " - " + s2 );
                        Console.WriteLine( s1 + " - " + s2 );
    #endif
                        */
                    }
                };

                return (config);
            }
            private static MorphoAmbiguityResolverModel  CreateMorphoAmbiguityResolverModel()
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
                                var morphoAmbiguityModel = CreateMorphoAmbiguityResolverModel();
                                var morphoModelConfig    = CreateMorphoModelConfig();
                                var morphoModel          = MorphoModelFactory.Create( morphoModelConfig );
                                var config               = CreatePosTaggerProcessorConfig();

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
            #region [.log.]
            if ( Log.ProcessViewCommand( context ) )
            {
                return;
            }
            #endregion

            var text = default(string);
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

                    text          = context.GetRequestStringParam( "text", Config.MAX_INPUTTEXT_LENGTH );
                var splitBySmiles = context.Request[ "splitBySmiles" ].Try2Bool( true );                

                #region [.anti-bot.]
                antiBot.MarkRequestEx( text );
                #endregion

                var words = ConcurrentFactoryHelper.GetConcurrentFactory().Run_Debug( text, splitBySmiles );

                Log.Info( context, text );
                SendJsonResponse( context, words );
            }
            catch ( Exception ex )
            {
                Log.Error( context, text, ex );
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
}