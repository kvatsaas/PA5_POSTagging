/*
 * Parts of Speech Tagging
 * Created by Kristian Vatsaas
 * November 2021
 * 
 * An important application of language modeling is tagging parts of speech. POS tagging itself is a step
 * to more exciting applications, like comprehending text, but is a complex task in its own right.
 * 
 * This final and simplest part of the process compares the system answers to the answer key and computes
 * the total accuracy. It also keeps track of how many times each combination of system tag and key tag
 * occurred, then outputs the accuracy and each (non-zero) tag combination with its counts in alphabetical
 * order by tag. Below is an example of the output; the section shown was selected to give an idea of
 * the variation in tag/key combinations and counts.
 * 
 * Accuracy: 0.9435801773898352
 * [...]
 * RBR JJR: 4
 * RBR RBR: 25
 * RBS JJS: 2
 * RP IN: 4
 * RP RB: 44
 * RP RP: 65
 * SYM SYM: 1
 * TO TO: 1245
 * UH UH: 7
 * VB JJ: 10
 * VB NN: 23
 * VB NNP: 2
 * VB RB: 2
 * VB VB: 1291
 * [...]
 * 
 * The patterns in this data give an idea of the biggest issues in the tagging ruleset that ought to be
 * addressed. Additionally, it's pretty simple to make some quick modifications and get a closer look at
 * something while building a related rule, so TaggerEval turns out to be more than just a scoreboard.
 * 
 * This program was created for CS 4242 Natural Language Processing at the University of Minnesota-Duluth.
 * Last updated 11/6/21
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace TaggerEval
{

    class TaggerEval
    {

        /// <summary>
        /// Performs the evaluation and outputs the results.
        /// </summary>
        /// <param name="args">Filepaths to the system results, the key, and the output file</param>
        static void Main(string[] args)
        {
            // check number of arguments
            if (args.Length != 3)
            {
                Console.WriteLine("Incorrect number of arguments!");
                return;
            }

            // assign filepaths for readability
            var sysfile = args[0];
            var keyfile = args[1];
            var outfile = args[2];

            uint correct = 0;
            uint incorrect = 0;
            var confusion = new SortedDictionary<string, SortedDictionary<string, int>>();

            try
            {
                using var sr_sys = new StreamReader(sysfile);
                using var sr_key = new StreamReader(keyfile);

                while (sr_sys.Peek() != -1) // not EOF - both files should be the same length, so only check one
                {
                    var sys_input = sr_sys.ReadLine();  // get the next line from the system results file
                    var sys_match = Regex.Match(sys_input, @"(?<word>.*)\/(?<tag>.*)", RegexOptions.Compiled);   // match word and tag

                    var key_input = sr_key.ReadLine();  // get the next line from the key file
                    var key_match = Regex.Match(key_input, @"(?<word>.*)\/(?<tag>.*)", RegexOptions.Compiled);   // match word and tag

                    var sys_word = sys_match.Groups["word"].Value;
                    var key_word = key_match.Groups["word"].Value;

                    // this shouldn't happen if TaggerTest does its job, but good to check
                    if (!sys_word.Equals(key_word))
                        throw new Exception($"Word mismatch! System: {sys_word}, Key: {key_word}");

                    var sys_tag = sys_match.Groups["tag"].Value;
                    var key_tag = key_match.Groups["tag"].Value;

                    // check correctness
                    if (sys_tag.Equals(key_tag))
                        correct++;
                    else
                        incorrect++;

                    // add count to confusion matrix
                    if (!confusion.ContainsKey(sys_tag))
                        confusion.Add(sys_tag, new SortedDictionary<string, int>());
                    if (!confusion[sys_tag].ContainsKey(key_tag))
                        confusion[sys_tag].Add(key_tag, 1);
                    else
                        confusion[sys_tag][key_tag]++;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Something went wrong during evaluation: {e.Message}");
            }

            try
            {
                using var sw = new StreamWriter(outfile);

                // compute and print accuracy
                var accuracy = (double)correct / (correct + incorrect);
                sw.WriteLine($"Accuracy: {accuracy}");

                // print confusion matrix counts
                foreach (string sys_tag in confusion.Keys)
                    foreach (string key_tag in confusion[sys_tag].Keys)
                        sw.WriteLine($"{sys_tag} {key_tag}: {confusion[sys_tag][key_tag]}");
            }
            catch (Exception e)
            {
                throw new Exception($"Something went wrong while writing to {outfile}: {e.Message}");
            }
        }
    }
}
