# PA5_POSTagging

Below is the text of the original assignment description from CS 4242 Natural Language Processing at the Universeity of Minnesota - Duluth. The source code includes further commentary on the implementation, and the textbook referenced can be found here: https://web.stanford.edu/~jurafsky/slp3/

----

You will write three programs in the language of your choice to implement a simple part of speech tagger. The three programs are tagger-train, tagger-test, and tagger-eval, each described below.  While it would be possible to write a single program that carries out the functionality described below, you must write three separate programs. The reason for this is to guarantee that your test and training data are never used at the same time in the same program. This helps ensure a "fair" evaluation. If this sounds like our rationale for the structure of PA 3, that is no accident! We are using the same supervised learning methodology here for a completely different problem.

The input for this assignment is found in our Google Drive (PA5-wsj-pos.zip). This unpacks to a directory that contains a file of training data which consists of part of speech tagged text (pos-train.txt), a file of text that needs to be part of speech tagged (pos-test.txt), and a file of text which give you the gold standard key of correct part of speech tags for the text to be tagged (pos-key.txt). There is also a README with some information about the data.

The train, test and key data is formatted such that there is one token per line.  The tokens in pos-train.txt and pos-key.txt also have POS tags. Note that in general the format of each line in train and key will be WORD/TAG. However, there are a few cases where words have a '/' as a part of the token (eg, 3/4, 1/2-year, or Dutch/Shell). In those cases you'll see that the '/' is escaped which results in the following : 

3\/4/CD

1\/2-year/JJ

Dutch\/Shell/NNP

Your program tagger-train should determine p(tag|word) for every word type in the training data (pos-train.txt). It should output these probabilities to a file tagger-train-probs.txt. This file will be one of your inputs to tagger-test, along with the data you want to part of speech tag (pos-test.txt).

Your program tagger-test should operate in two modes, most frequent tag (mode 0) and most frequent tag + enhancements (mode 1). You must provide a command line option so that the user can specify the mode in which tagger-test should be run. 

In most frequent tag mode (0), tagger-test should : 

For each word in test data that is found in the training data, assign it the POS tag with the maximum value of p(tag|word). 

For each word in the test data not found in the training data (i.e., an unknown word), assign it the tag NN.

In most frequent tag + enhanced mode (1), tagger-test should still carry out mode 0, and then in addition :

Provide at least 5 manually created rules that improve the handling of unknown words. Rather than always assigning NN, identify cases where NN is not likely to be correct (and provide a rule that does the right thing). Document your rules in the comments to tagger-test. You should tell what each rule is and give a specific example of the kind of problem it will fix. Label the rules in your comments and code as U-1, U-2, U-3, ...

Provide at least 5 manually created rules that correct errors made by the most frequent tag mode (0) that do not involve unknown words. Document these rules in the comments to tagger-test. You should tell what each rule is and give a specific example of the kind of problem it will fix. Label the rules in your comments and code as E-1, E-2, E-3 ... 

tagger-eval should compare the output of tagger-test to the gold standard key data (pos-key.txt) and report overall tagging accuracy and also provide information equivalent to what you find in a confusion matrix in the following format : 

PREDICT_TAG KEY_TAG : count 

PREDICT_TAG is what your system says the tag should be, KEY_TAG says what the correct gold standard key tag really is according to pos-key.txt, and count is the number of times a word that should have a KEY_TAG is tagged (incorrectly) as PREDICT_TAG.  List these one per line, and sort on PREDICT_TAG and then KEY_TAG in ascending alphabetic order (A B C D ...). Do not display 0 count values.  

For correct classifications report (for example)

DET DET : 10,000 

This means that your system (tagger-test) tagged 10,000 word tokens as determiners (DET) that really are determiners. 

For incorrect classifications report (for example)

NN VBD : 5

This means that your system (tagger-test) predicted that 5 word tokens were nouns (NN) when they were really past tense verbs (VBD).  

You should run your system in both mode 0 and mode 1 as follows. Note that these examples are Linux specific, and should be adapted to different platforms. However, you should make sure to have just one tagger-test program that has two different modes (controlled by a command line option) rather than having two separate programs (eg tagger-test-1 and tagger-test-2).

# learn probabilities from the training data

tagger-train pos-train.txt > tagger-train-prob.txt 

# tag the test data using the most frequent tag mode (0)

tagger-test 0 tagger-train-prob.txt pos-test.txt > pos-test-0.txt

# evaluate the results of most frequent tag mode (0)

tagger-eval pos-test-key.txt pos-test-0.txt > pos-test-0-eval.txt

# tag the test data using the most frequent tag + enhancements mode (1)

tagger-test 1 tagger-train-prob.txt pos-test.txt > pos-test-1.txt

# evaluate the results of most frequent ag mode (1) 

tagger-eval pos-test-key.txt post-test-1.txt > pos-test-1-eval.txt

Note that your overall accuracy for mode (1) must be an improvement upon mode (0). If it is not there is a problem with your rules and you should rethink them. 

Submit a single pdf file that consists of the following files (in this order)

tagger-train (source code)

tagger-test (source code)

tagger-eval (source code)

tagger-train-probs.txt (not the whole file, just the smallest 750 p(tag|word))

post-test-0-eval.txt (most frequent tag results)

pos-test-1-eval.txt (most frequent tag + enhancement results)

For your tagger-train-probs.txt output, the output you submit should be sorted in ascending order (where the smallest probabilities are listed first) and then your output should include the probability, tag, and word (in that order), one probability per line. The exact formatting of this information is up to you, but please make sure to sort your output and only provide 750 lines.  You can prepare this output file after running your program (e.g., using Linux command line tools, a small utility program you write but do not need to submit, etc.) 

Remember that each program must be documented with an introductory comment and detailed comment, and please make sure the algorithm overview in the introductory comment includes a numbered list of steps for your algorithm, and that these numbers are referred to in your detailed comments. Make sure that in tagger-train you provide the overall accuracy for both mode 0 and mode 1 in your introductory comment. 

In terms of functionality these are the things I will be looking for :

(1 point) mode 0 accuracy >= 90.00 %

(1 point) 5 rules to improve uknown word handling (U rules)

(1 point) 5 rules to improve known word errors (E rules)

(1 point) mode 1 accuracy is greater than mode 0 accuracy by at least 1%. If your mode 0 accuracy is 90.00% then this means your mode 1 accuracy should be >= 91.00%.

(1 point) tagger-eval program provides accuracy and confusion matrix info in the format described above. 

Please do not use any pre-existing code that is specific to NLP for this assignment. You may however re-use code from your own previous assignments. If you do, please clearly indicate where and how you are using your previous code (in the detailed source code comments).
