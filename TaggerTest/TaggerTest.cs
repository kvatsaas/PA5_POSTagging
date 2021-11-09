/*
 * Parts of Speech Tagging
 * Created by Kristian Vatsaas
 * November 2021
 * 
 * An important application of language modeling is tagging parts of speech. POS tagging itself is a step
 * to more exciting applications, like comprehending text, but is a complex task in its own right.
 * 
 * In this leg of the journey, we take the probabilities created by TaggerTrain and turn them back into
 * an object. Then, we tag a given file of test data using those probabilities and one of two methods,
 * as specified by the first command-line argument ('mode').
 * 
 * In mode 0, we simply tag every known word as its most likely tag, and assume every unknown word is
 * a (non-proper and singular) noun. This is surprisingly successful - ~96.14% for our test data of about
 * 57,000 words. However, we're just getting started.
 * 
 * In mode 1, we expand on this by adding some special rules, some specific to known words and some specific
 * to unknown words. The rules for known words focus on the context - the one or two words before it, or
 * the one after - to make decisions. For unknown words, the focus is on the form of the word itself - a
 * more complex (and linguistically informed) design would probably use both, but I didn't go that far.
 * Each rule is described below next to its implementation, and most are derived by analyzing patterns in
 * the results output by TaggerEval. The goal for this mode was to improve upon mode 0 by at least one
 * percentage point. My current ruleset is ~94.36% accurate, good for an improvement of ~2.22%.
 * 
 * The program outputs its decisions to a file in the same format as the training data read by TaggerTrain.
 * That output is then read by TaggerEval, in order to the answers.
 * 
 * I'd like to make one final note about the structure of this part of the program - more specifically,
 * that of the loop in main and the way it interfaces with TagEnhanced. In hindsight, I should have
 * read in all the data from the system answer file and the key file, stored it into objects, tagged each
 * one, and then performed the output. However, I didn't do this initially because after the previous
 * assignment (PA4 Pointwise Mutual Information, which involved very large sparse matrices), I was a
 * little overly wary of space efficiency. By the time I realized I really should have refactored it,
 * I was putting on the finishing touches, and it didn't seem worth the time (and potential to break
 * something), so instead I'll consider it a lesson learned for next time.
 * 
 * This program was created for CS 4242 Natural Language Processing at the University of Minnesota-Duluth.
 * Last updated 11/8/21
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace TaggerTest
{
    class TaggerTest
    {
        /// <summary>
        /// Converts the probability file into a Dictionary of probabilities for each word and its associated POS tags.
        /// </summary>
        /// <param name="filepath">The path to the file of probabilities</param>
        /// <returns>A dictionary containing the probabilities</returns>
        static Dictionary<string, List<KeyValuePair<string, double>>> ParseProbabilityFile(string filepath)
        {
            var probs = new Dictionary<string, List<KeyValuePair<string, double>>>();
            try
            {
                using var sr = new StreamReader(filepath);

                // read and parse one line (tagged) word at a time
                while (sr.Peek() != -1) // not EOF
                {
                    var input = sr.ReadLine();  // get the next line from the data file

                    // retrieve the word, tag, and probability
                    var match = Regex.Match(input, @"(?<word>.*)\/(?<tag>.*) (?<prob>.*)", RegexOptions.Compiled);
                    var word = match.Groups["word"].Value;
                    var tag = match.Groups["tag"].Value;
                    var prob = Double.Parse(match.Groups["prob"].Value);

                    // add a new dictionary entry if none exists for this word
                    if (!probs.ContainsKey(word))
                        probs.Add(word, new List<KeyValuePair<string, double>>());

                    /* insert the POS tag and its probability at the top of the list so that the most likely tag is first, since
                     * the probability file is ordered by ascending probability*/
                    probs[word].Insert(0, new KeyValuePair<string, double>(tag, prob));
                }

                return probs;
            }
            catch (Exception e)
            {
                throw new Exception($"Something went wrong while reading {filepath}: {e.Message}");
            }
        }

        /// <summary>
        /// Chooses the tag with the baseline algorithm (mode 0); that is, use the highest probability for known words
        /// and always choose NN for unknown words.
        /// </summary>
        /// <param name="probs">The dictionary of words and their POS tag probabilities</param>
        /// <param name="word">The word to be tagged</param>
        /// <returns>The tag for this word</returns>
        static string TagBaseline(Dictionary<string, List<KeyValuePair<string, double>>> probs, string word)
        {
            if (probs.ContainsKey(word))
                return probs[word][0].Key;
            else
                return "NN";

        }


        /// <summary>
        /// Chooses the tag with the enhanced algorithm (mode 1), which means a number of additional features for both known words
        /// and unknown words.
        /// </summary>
        /// <param name="probs">The dictionary of words and their POS tag probabilities</param>
        /// <param name="word">The word to be tagged</param>
        /// <param name="firstPriorWord">The previous word</param>
        /// <param name="firstPriorTag">The tag for the previous word</param>
        /// <param name="secondPriorWord">The word before firstPriorWord</param>
        /// <param name="secondPriorTag">The tag for the word before firstPriorWord</param>
        /// <param name="nextWord">The word that comes after word</param>
        /// <returns></returns>
        static string TagEnhanced(Dictionary<string, List<KeyValuePair<string, double>>> probs, string word, string firstPriorWord, string firstPriorTag, string secondPriorWord, string secondPriorTag, string nextWord)
        {
            if (probs.ContainsKey(word))    // known word
            {
                var topTag = probs[word][0].Key;
                var puncTags = new List<string>() { "#" , "$", "''", "(", ")", ",", ".", ":", "``"};    // made here for use in two later tests

                /* E-1 If the word is most likely a past tense or past participle verb, then we'll assume it's one of those. The latter
                 * usually only occurs after certain other words, so we'll check for those in the two prior words.
                 */
                if ((topTag.Equals("VBD") || topTag.Equals("VBN")) && KPLContainsKey(probs[word], "VBD") && KPLContainsKey(probs[word], "VBN"))
                {
                    var participlePriors = new List<string>() { "had", "has", "have", "been", "be", "are", "am", "was", "were", "do", "did" };
                    if (participlePriors.Contains(firstPriorWord) || participlePriors.Contains(secondPriorWord))
                        return "VBN";
                    else
                        return "VBD";
                }

                /* E-2 If the most likely tag for a word is either noun or verb, and the list of potential tags contains the other of the two,
                 *     then chances are it's one of the two. If the prior word's tag was 'to,' modal, or adverb, then it's probably a verb.
                 *     Otherwise, we'll assume it's a noun.
                 */
                if ((topTag.Equals("NN") && KPLContainsKey(probs[word], "VB")) ||
                    (topTag.Equals("VB") && KPLContainsKey(probs[word], "NN")))
                {
                    if (EqualsOneOf(firstPriorTag, "TO", "MD", "RB") ||
                        (puncTags.Contains(topTag) && (EqualsOneOf(secondPriorTag, "TO", "MD", "RB"))))
                        return "VB";
                    else
                        return "NN";
                }

                /* E-3 The tagger tends to fail to label participles. In fact, although the below condition implies that there are words whose top tag
                 *     is participle, there are in fact none, based on the training data. Testing showed that all of them were instead labeled adverbs.
                 *     So, for words whose top tag is adverb but also have participle in the list, we check to see if the previous word is some sort of
                 *     verb. If it is, we guess participle; if not, we stick with adverb.
                 *     Note: In terms of net correct answers, this is not the most effective rule; under 60% of the words labeled RP are correct.
                 *     However, this rule singlehandedly brings up the recall for RP from 0% to 80%, while only 2% of RB words are tagged incorrectly.
                 */
                if ((topTag.Equals("RB") && KPLContainsKey(probs[word], "RP")) ||
                    (topTag.Equals("RP") && KPLContainsKey(probs[word], "RB")))
                {
                    if (EqualsOneOf(firstPriorTag, "VB", "VBD", "VBG", "VBN", "VBP", "VBZ"))
                        return "RP";
                    else
                        return "RB";
                }

                /* E-4 The word 'that' is given special attention because it is very common but can be WDT, DT, or IN (excepting a few rare occasions).
                 *     Because it is so common, it is a good idea to address it specifically, as not doing so will mean that only the most common tag
                 *     will ever be applied (in this case, that's IN 60% of the time). We'll choose one of these via a few basic rules: if the previous
                 *     word is a verb, choose IN; if the next word is a verb, choose WDT; if the last word was a preposition or punctuation, choose DT.
                 *     If none of these are triggered, we revert to the top tag.
                 *     Note: This ended up being the only rule I used nextWord in. I used TagBaseline when checking its tag to avoid getting stuck in a
                 *     loop; the reasoning is obvious but I figured I should note that somewhere.
                 */
                if (word.Equals("that"))
                {
                    if (EqualsOneOf(firstPriorTag, "VB", "VBD", "VBG", "VBN", "VBP", "VBZ"))
                        return "IN";
                    else if (EqualsOneOf(TagBaseline(probs, nextWord), "VB", "VBD", "VBG", "VBN", "VBP", "VBZ", "MD"))
                        return "WDT";
                    else if (firstPriorTag.Equals("IN") || puncTags.Contains(firstPriorTag))
                        return "DT";
                    else
                        return topTag;
                }

                /* E-5 The token "'s" can either be an indicator of possession (POS) or a contraction of the word 'is' and thus a verb (VBZ). The simplest
                 *     and most effective check is to see if the previous word is a noun; if so, it's probably POS. Otherwise, we choose VBZ. Like the
                 *     previous rule, the absence of a rule would result in only one tag ever being returned, so it's important to take care of this,
                 *     and pretty effective
                 */
                if (word.Equals("'s"))
                {
                    if (EqualsOneOf(firstPriorTag, "NN", "NNS", "NNP", "NNPS"))
                        return "POS";
                    else
                        return "VBZ";
                }


                return topTag;
            }
            else    // unknown word
            {
                
                /* These rules are considered separate rules, but it both saves processing time and reduces the complexity of the
                 * following regular expressions by ensuring that a number is present.
                 */
                if (Regex.IsMatch(word, @"\d", RegexOptions.Compiled))
                {
                    /* U-1 Numbers, times, and dollar amounts are almost certainly cardinal numbers.
                     *     Note: By putting this regex first, the following regexes are simplified since any words that contain only
                     *     numbers are gone.
                     */
                    if (Regex.IsMatch(word, @"^\$?[\d\.,-:]+$", RegexOptions.Compiled))
                        return "CD";

                    /* U-2 A number followed by an 's' is usually considered a plural noun. This may optionally start with an
                     * apostropher; i.e. 1990s or '90s.
                     */
                    if (Regex.IsMatch(word, @"^'?[\d]+s$", RegexOptions.Compiled))
                        return "NNS";

                    /* U-3 A mix of numbers and letters is usually a proper noun, like a street name or a product name.
                     */
                    if (Regex.IsMatch(word, @"^[\dA-Za-z]+$", RegexOptions.Compiled))
                        return "NNP";

                    /* U-4 A mix of numbers and other characters, including punctuation, is most often an adjective, such as 7th-best
                     *     or 50-point.
                     */
                    if (Regex.IsMatch(word, @"\D+", RegexOptions.Compiled))
                        if (Regex.IsMatch(word, @"^[\D\d]+$", RegexOptions.Compiled))
                            return "JJ";
                }


                /* U-5 If the word ends in 's', we remove the 's' and check to see if it is in the list. If it is, then it's  
                 *     probably a noun or proper noun, so we'll assume it's a plural noun or plural proper noun.
                 *     Note: I tried adding another rule that did this for words ending in "es" but it actually performed
                 *     worse by a single incorrect tag. That doesn't really mean a ton off of one set of data, but
                 *     it didn't seem worth keeping.
                 */
                if (word.EndsWith('s') && probs.ContainsKey(word.Remove(word.Length - 1)))
                {
                    var tag = probs[word.Remove(word.Length - 1)][0].Key;
                    if (tag.Equals("NN"))
                        return "NNS";
                    if (tag.Equals("NNP"))
                        return "NNPS";
                }

                /* U-6 If the word starts with an uppercase letter, we convert the word to lowercase and check the list. If it's
                 *     found in there, then we'll act as if it's the same word. If not, we'll assume it's a plural proper noun.
                 *     Note: I tried checking for an 's' at the end to distinguish between NNP and NNPS, but it performed worse
                 *     by 22 incorrect tags, meaning the overall performance increase of this rule took a 5% hit.
                 */
                if (word[0] >= 'A' && word[0] <= 'Z')
                {
                    if (probs.ContainsKey(word.ToLower()))
                        return probs[word.ToLower()][0].Key;
                    else
                        return "NNP";
                }

                /* U-7 If the word is comprised of letters and hyphens, or ends with one of the endings below, it's likely an adjective.
                 */
                if (Regex.IsMatch(word, @"^([A-Za-z]+-)+[A-Za-z]+$", RegexOptions.Compiled) ||
                    word.EndsWith("able") || word.EndsWith("ible") || word.EndsWith("al") || word.EndsWith("ful") ||
                    word.EndsWith("ic") || word.EndsWith("ical") || word.EndsWith("ish") || word.EndsWith("ive") ||
                    word.EndsWith("less") || word.EndsWith("ous") || word.EndsWith("y"))
                    return "JJ";

                /* U-8 If the word ends with 'ing', it's probably a gerund or present participle verb (same tag).
                 */
                if (word.EndsWith("ing"))
                    return "VBG";

                /* U-9 If the word ends with 'ed', it's probably a past tense or past participle verb. The latter usually only occurs
                 * after certain other words, so we'll check for those in the two prior words.
                 */
                if (word.EndsWith("ed"))
                {
                    var participlePriors = new List<string>() { "had", "has", "have", "been", "be", "are", "am", "was", "were", "do", "did"};
                    if (participlePriors.Contains(firstPriorWord) || participlePriors.Contains(secondPriorWord))
                        return "VBN";
                    else
                        return "VBD";
                }

                /* U-10 At this point, we're through all our rules so we'll just mimic the baseline algorithm and guess noun. However,
                 *      we'll try to be slightly smarter than that by guessing it's plural if it ends with an s.
                 */
                if (word.EndsWith('s'))
                    return "NNS";
                else
                    return "NN";
            }
        }

        /// <summary>
        /// Checks to see if the given list of key-value-pairs contains the given key, or one of the given keys.
        /// </summary>
        /// <param name="kvpList">The list of key-value-pairs</param>
        /// <param name="key">The key or keys</param>
        /// <returns>True if the key is present, otherwise false</returns>
        static bool KPLContainsKey(List<KeyValuePair<string, double>> kvpList, params string[] keys)
        {
            foreach (KeyValuePair<string, double> kvp in kvpList)
                foreach (string key in keys)
                    if (key.Equals(kvp.Key))
                        return true;
            return false;
        }

        static bool EqualsOneOf<T>(T tag, params T[] posTags)
        {
            foreach (T posTag in posTags)
                if (tag.Equals(posTag))
                    return true;
            return false;
        }

        /// <summary>
        /// Runs the loop to tag all the input words and outputs the result, using the specified mode. Mode 0 uses the baseline algorithm;
        /// mode 1 uses the enhanced algorithm.
        /// </summary>
        /// <param name="args">The mode, probability file path, test file path, and output file path.</param>
        static void Main(string[] args)
        {
            // check number of arguments
            if (args.Length != 4)
            {
                Console.WriteLine("Incorrect number of arguments!");
                return;
            }

            // assign filepaths for readability
            var mode = Int32.Parse(args[0]) == 1;
            var probsFile = args[1];
            var testFile = args[2];
            var outfile = args[3];

            var probs = ParseProbabilityFile(probsFile);

            try
            {
                using var sr = new StreamReader(testFile);
                using var sw = new StreamWriter(outfile);
                string word, tag, nextWord;
                string firstPriorWord = "", firstPriorTag = "", secondPriorWord = "", secondPriorTag = "";
                bool complete = false;

                nextWord = sr.ReadLine();

                while (!complete) // not EOF
                {
                    word = nextWord;
                    if (sr.Peek() != -1) // not EOF
                        nextWord = sr.ReadLine();  // get the next word from the data file
                    else
                        complete = true;    // exit loop after this tag

                    // do tagging based on the mode
                    if (mode)
                        tag = TagEnhanced(probs, word, firstPriorWord, firstPriorTag, secondPriorWord, secondPriorTag, nextWord);
                    else
                        tag = TagBaseline(probs, word);

                    sw.WriteLine(word + '/' + tag); // output result

                    // store last two words and their tags
                    secondPriorWord = firstPriorWord;
                    secondPriorTag = firstPriorTag;
                    firstPriorWord = word;
                    firstPriorTag = tag;
                }

            }
            catch (Exception e)
            {
                throw new Exception($"Something went wrong in DoBaselineTest: {e.Message}");
            }

        }
    }
}
