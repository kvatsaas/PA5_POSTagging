/*
 * Parts of Speech Tagging
 * Created by Kristian Vatsaas
 * November 2021
 * 
 * An important application of language modeling is tagging parts of speech. POS tagging itself is a step
 * to more exciting applications, like comprehending text, but is a complex task in its own right.
 * 
 * For this project, the approach is relatively simple and straightforward. Given a dataset comprised of
 * words and their tags, we create a list of probabilities for each possible tag for each possible word.
 * The input data looks something like this:
 * 
 * with/IN
 * even/RB
 * brief/JJ
 * exposures/NNS
 * to/TO
 * 
 * The algorithm for TaggerTrain is simple. We start with a nested Dictionary, which represents each word
 * and the number of times it appears as each tag. We read the training data and count each appearance of
 * each word/tag combination. Once counting is done, we convert each count into a probability, then write
 * each on its own line in the output file, where a line might look like:
 * 
 * Barclays/NNP 1
 * lend/VB 0.782608695652174
 * screwball/NN 0.5
 * Graphics/NNPS 0.17647058823529413
 * a/JJ 8.081134591296618E-05
 * 
 * The rest of the POS tagger is described in TaggerTest.cs and TaggerEval.cs. However, the list above does
 * raise an interesting note. The last probability implies that at some point in the training data, the word
 * 'a' is labeled with the tag JJ, which refers to an adjective. This is pretty clearly wrong. This occurs
 * here and there in both the training data and the answer key for the test data, simply due to human error.
 * It's not a large percentage of the data, but in part for this reason, POS tagging is generally considered
 * to have a ceiling around 99%.
 * 
 * This program was created for CS 4242 Natural Language Processing at the University of Minnesota-Duluth.
 * Last updated 11/6/21
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace TaggerTrain
{

    class TaggerTrain
    {

        /// <summary>
        /// Counts POS tags for each word that appears in the training file.
        /// </summary>
        /// <param name="filepath">The path of the training file</param>
        /// <returns>A nested dictionary of the words and their respective POS tag counts</returns>
        static Dictionary<string, Dictionary<string, uint>> ParseTrainingFile(string filepath)
        {
            var counts = new Dictionary<string, Dictionary<string, uint>>();
            try
            {
                using var sr = new StreamReader(filepath);

                // read and parse one line (tagged) word at a time
                while (sr.Peek() != -1) // not EOF
                {
                    var input = sr.ReadLine();  // get the next line from the data file

                    var match = Regex.Match(input, @"(?<word>.*)\/(?<tag>.*)", RegexOptions.Compiled);   // match each word in the line
                    var word = match.Groups["word"].Value;
                    var tag = match.Groups["tag"].Value;

                    // add the word to the dictionary if it is not already present
                    if (!counts.ContainsKey(word))
                        counts.Add(word, new Dictionary<string, uint>());

                    // increment the count for WORD/TAG, adding the tag to the word's dictionary if it is not already present
                    if (!counts[word].ContainsKey(tag))
                        counts[word].Add(tag, 1);
                    else
                        counts[word][tag]++;
                }

                return counts;
            }
            catch (Exception e)
            {
                throw new Exception($"Something went wrong while reading {filepath}: {e.Message}");
            }
        }

        /// <summary>
        /// Retruns the sum of all values in the dictionary; that is, the number of times the word represented by the dictionary
        /// appeared in the training data.
        /// </summary>
        /// <param name="tagCounts">A dictionary representing a word's POS tag counts</param>
        /// <returns>The number of times the word appeared in the training data</returns>
        static uint WordCount(Dictionary<string, uint> tagCounts)
        {
            uint wordCount = 0;

            foreach (uint c in tagCounts.Values)
                wordCount += c;

            return wordCount;
        }

        /// <summary>
        /// Computes probabilities for each tag for the given word.
        /// </summary>
        /// <param name="counts">The nested dictionary of the words and their respective POS tag counts</param>
        /// <returns>A list of key-value pairs representing each WORD/TAG and its probability, sorted by ascending probability</returns>
        static List<KeyValuePair<string, double>> WordTagProbabilities(Dictionary<string, Dictionary<string, uint>> counts)
        {

            var tagProbs = new List<KeyValuePair<string, double>>();

            foreach (string word in counts.Keys)
            {
                var tagCounts = counts[word];
                uint wordCount = WordCount(tagCounts);  // get the total number of occurences for the word

                // compute the probability for each tag for this word and add it to the list along with its WORD/TAG
                foreach (string tag in tagCounts.Keys)
                    tagProbs.Add( new KeyValuePair<string, double>(
                        word + '/' + tag,
                        (double)tagCounts[tag] / wordCount));
            }

            // sort by ascending probability
            tagProbs.Sort((x, y) => { return x.Value > y.Value ? 1 : x.Value < y.Value? -1 : 0; });
            return tagProbs;
        }

        /// <summary>
        /// Runs the necessary functions, then writes the results to the output file.
        /// </summary>
        /// <param name="args">The path of the training file and the path of the output file</param>
        static void Main(string[] args)
        {
            // check number of arguments
            if (args.Length != 2)
            {
                Console.WriteLine("Incorrect number of arguments!");
                return;
            }

            // assign filepaths for readability
            var trainFile = args[0];
            var outfile = args[1];

            var counts = ParseTrainingFile(trainFile);
            var tagProbs = WordTagProbabilities(counts);

            try
            {
                using var sw = new StreamWriter(outfile);

                foreach (KeyValuePair<string, double> prob in tagProbs)
                    sw.WriteLine($"{prob.Key} {prob.Value}");
            }
            catch (Exception e)
            {
                throw new Exception($"Something went wrong while writing to {outfile}: {e.Message}");
            }
        }
    }
}
