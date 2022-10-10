﻿using System;
using System.Collections.Generic;

using lingvo.core;
using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace lingvo.morphology
{
    /// <summary>
    /// Словарь-дерево для слов
    /// </summary>
    internal sealed class TreeDictionaryClassic : ITreeDictionary
    {
        /// слот для дочерних слов
        private readonly Dictionary< char, TreeDictionaryClassic > _Slots;
        /// коллекция информаций о формах слова
        private List< BaseMorphoForm > _BaseMorphoForms;

        public TreeDictionaryClassic() => _Slots = new Dictionary< char, TreeDictionaryClassic >();

        #region [.append words.]
        /// добавление слова и всех его форм в словарь
        /// word - слово
        /// pMorphoType - морфотип
        /// nounType - тип сущетсвительного
        [M(O.AggressiveInlining)] unsafe public void AddWord( string word, MorphoType morphoType, MorphoAttributePair? nounType )
        {
            if ( morphoType.MorphoForms.Length != 0 )
            {
                var len   = word.Length - morphoType.MorphoForms[ 0 ].Ending.Length;
                var _base = (0 <= len) ? word.Substring( 0, len ) : word;
                var baseMorphoForm = new BaseMorphoForm( _base, morphoType, nounType );
                var baseUpper      = StringsHelper.ToUpperInvariant( _base );
                fixed ( char* baseUpper_ptr = baseUpper )
                {
                    AddWordPart( baseUpper_ptr, baseMorphoForm );
                }
            }
        }

        /// добавление части слова
        /// wordPart - оставшася часть слова
        /// pBase - базовая форма
        [M(O.AggressiveInlining)] unsafe private void AddWordPart( char* wordPart, BaseMorphoForm baseMorphoForm )
        {
            var first_char = *wordPart;
            if ( first_char == '\0' ) // сохранение характеристик
            {
                if ( _BaseMorphoForms == null )
                {
                    _BaseMorphoForms = new List< BaseMorphoForm >();
                }
                _BaseMorphoForms.Add( baseMorphoForm );
            }
            else
            {
                if ( !_Slots.TryGetValue( first_char, out var _next ) )
                {
                    // добавление новой буквы
                    _next = new TreeDictionaryClassic();
                    _Slots.Add( first_char, _next );
                }
                _next.AddWordPart( wordPart + 1, baseMorphoForm );
            }
        }
        #endregion

        /// получение морфологических свойств слова
        /// word - слово
        /// result - коллекция информаций о формах слова
        [M(O.AggressiveInlining)] unsafe public bool GetWordFormMorphologies( string wordUpper, ICollection< WordFormMorphology_t > result, WordFormMorphologyModeEnum wordFormMorphologyMode )
        {
            result.Clear();
            fixed ( char* word_ptr = wordUpper )
            {
                var fullWordLength = wordUpper.Length;
                FillWordFormMorphologies( word_ptr, fullWordLength, fullWordLength, result, wordFormMorphologyMode );
            }
            return (result.Count != 0);
        }
        [M(O.AggressiveInlining)] unsafe public bool GetWordFormMorphologies( char*  wordUpper, ICollection< WordFormMorphology_t > result, WordFormMorphologyModeEnum wordFormMorphologyMode )
        {
            result.Clear();            
            {
                var fullWordLength = StringsHelper.GetLength( wordUpper );
                FillWordFormMorphologies( wordUpper, fullWordLength, fullWordLength, result, wordFormMorphologyMode );
            }
            return (result.Count != 0);
        }

        /// получение всех форм слова
        /// word - слово
        /// result - коллекция форм слова
        [M(O.AggressiveInlining)] unsafe public bool GetWordForms( string wordUpper, ICollection< WordForm_t > result )
        {
            result.Clear();
            fixed ( char* word_ptr = wordUpper )
            {
                var fullWordLength = wordUpper.Length;
                FillWordForms( word_ptr, fullWordLength, fullWordLength, result );
            }
            return (result.Count != 0);
        }

        /// поиск слова в словаре
        /// word - слово
        /// letterIndex - индекс буквы
        /// result - коллекция форм слова
        /// 
        unsafe private void FillWordFormMorphologies( char* word, int wordLength, int fullWordLength
            , ICollection< WordFormMorphology_t > result, WordFormMorphologyModeEnum wordFormMorphologyMode )
        {
            FillWordFormMorphologies_Core( word, wordLength, fullWordLength, result, wordFormMorphologyMode );
            var first_char = *word;
            if ( (first_char != '\0') && _Slots.TryGetValue( first_char, out var _next ) )
            {
                _next.FillWordFormMorphologies( word + 1, wordLength - 1, fullWordLength, result, wordFormMorphologyMode );
            }
        }
        unsafe private void FillWordForms( char* word, int wordLength, int fullWordLength, ICollection< WordForm_t > result )
        {
            FillWordForms_Core( word, wordLength, fullWordLength, result );
            var first_char = *word;
            if ( (first_char != '\0') && _Slots.TryGetValue( first_char, out var _next ) )
            {
                _next.FillWordForms( word + 1, wordLength - 1, fullWordLength, result );
            }
        }

        /// поиск слова в слоте
        /// wordPart - оставшаяся часть слова
        /// pSlot - слот
        /// letterIndex - индекс буквы
        unsafe private void FillWordFormMorphologies_Core( char* wordPart, int wordPartLength, int fullWordLength
            , ICollection< WordFormMorphology_t > result, WordFormMorphologyModeEnum wordFormMorphologyMode )
        {
            if ( _BaseMorphoForms == null )
                return;

            foreach ( var baseMorphoForm in _BaseMorphoForms )
            {
                int baseLength = baseMorphoForm.Base.Length;
                if ( (fullWordLength < baseLength) ||
                     (baseLength + baseMorphoForm.MorphoType.MaxEndingLength < fullWordLength)
                   )
                {
                    continue;
                }

                foreach ( var morphoForm in baseMorphoForm.MorphoType.MorphoForms )
                {
                    var endingLength = morphoForm.EndingUpper.Length;
                    if ( baseLength + endingLength != fullWordLength )
                        continue;

                    if ( endingLength != wordPartLength )
                        continue;
                    if ( wordPartLength == 0 )
                    { ; }
                    else
                    if ( !StringsHelper.IsEqual( morphoForm.EndingUpper, wordPart, wordPartLength ) )
                        continue;
                    #region comm.
                    /*
                    if ( baseLength == letterIndex )
                    {
                        if ( !StringsHelper.IsEqual( morphoForm.EndingUpper, wordPart, wordPartLength ) )
                            continue;
                    }
                    else
                    {
                        var wordForm = baseMorphoForm.Base + morphoForm.EndingUpper;
                        if ( !StringsHelper.IsEqual( wordForm, letterIndex, wordPart, wordPartLength ) )
                            continue;
                    }
                    */                    
                    #endregion

                    switch ( wordFormMorphologyMode )
                    {
                        case WordFormMorphologyModeEnum.Default:
                        {
                            var wfmi = new WordFormMorphology_t( baseMorphoForm, MorphoAttributePair.GetMorphoAttribute( baseMorphoForm, morphoForm ) );
                            result.Add( wfmi );
                        }
                        break;

                        case WordFormMorphologyModeEnum.StartsWithLowerLetter:
                        {
                            fixed ( char* normalForm_ptr = baseMorphoForm.NormalForm )
                            {
                                var first_char = *normalForm_ptr;
                                if ( (first_char != '\0') && *(xlat_Unsafe.Inst._UPPER_INVARIANT_MAP + first_char) == first_char )
                                {
                                    continue;
                                }
                            }

                            var wfmi = new WordFormMorphology_t( baseMorphoForm, MorphoAttributePair.GetMorphoAttribute( baseMorphoForm, morphoForm ) );
                            result.Add( wfmi );
                        }
                        break;

                        case WordFormMorphologyModeEnum.StartsWithUpperLetter:
                        {
                            fixed ( char* normalForm_ptr = baseMorphoForm.NormalForm )
                            {
                                var first_char = *normalForm_ptr;
                                if ( (first_char != '\0') && *(xlat_Unsafe.Inst._UPPER_INVARIANT_MAP + first_char) != first_char )
                                {
                                    continue;
                                }
                            }

                            var wfmi = new WordFormMorphology_t( baseMorphoForm, MorphoAttributePair.GetMorphoAttribute( baseMorphoForm, morphoForm ) );
                            result.Add( wfmi );
                        }
                        break;

                        case WordFormMorphologyModeEnum.FirstStartsWithUpperAfterLowerLetter:
                        case WordFormMorphologyModeEnum.FirstStartsWithLowerAfterUpperLetter:
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }
        }
        unsafe private void FillWordForms_Core( char* wordPart, int wordPartLength, int fullWordLength, ICollection< WordForm_t > result )
        {
            if ( _BaseMorphoForms == null )
                return;

            foreach ( var baseMorphoForm in _BaseMorphoForms )
            {
                int baseLength = baseMorphoForm.Base.Length;
                if ( (fullWordLength < baseLength) ||
                     (baseLength + baseMorphoForm.MorphoType.MaxEndingLength < fullWordLength)
                   )
                {
                    continue;
                }

                var morphoForms = baseMorphoForm.MorphoType.MorphoForms;
                foreach ( var morphoForm in morphoForms )
                {
                    var endingLength = morphoForm.EndingUpper.Length;
                    if ( baseLength + endingLength != fullWordLength )
                        continue;

                    if ( endingLength != wordPartLength )
                        continue;
                    if ( wordPartLength == 0 )
                    { ; }
                    else
                    if ( !StringsHelper.IsEqual( morphoForm.EndingUpper, wordPart, wordPartLength ) )
                        continue;
                    #region comm.
                    /*
                    if ( baseLength == letterIndex )
                    {
                        if ( !StringsHelper.IsEqual( morphoForm.EndingUpper, wordPart ) )
                            continue;
                    }
                    else
                    {
                        var wordForm = baseMorphoForm.Base + morphoForm.EndingUpper;
                        if ( !StringsHelper.IsEqual( wordForm, letterIndex, wordPart ) )
                            continue;
                    }
                    */
                    #endregion

                    var partOfSpeech = baseMorphoForm.MorphoType.PartOfSpeech;
                    foreach ( var _morphoForm in morphoForms )
                    {
                        //получение словоформы
                        var wordForm = baseMorphoForm.Base + _morphoForm.Ending;

                        var wf = new WordForm_t( wordForm, partOfSpeech );
                        result.Add( wf );
                    }
                    break;
                }
            }
        }
    }
}