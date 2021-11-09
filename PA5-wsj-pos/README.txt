This data is taken from the POS tagged portion of the Penn 
Treebank WSJ corpus. 

pos-test.txt is the contents of all files in directory 23. 
pos-train.txt is from all other directories (00-22, and 24). 

pos-test.txt and pos-key.txt should be identical except
that pos-key should have no POS tags.

wc output :

  56824    56824   490954 pos-key.txt
   56824    56824   307906 pos-test.txt
 1232377  1232377 10688633 pos-train.txt
 1346025  1346025 11487493 total

Note that / within a word is escaped via a /. This can be
confused as a POS if you are not careful.  For example

1/2 

would appear in test as 
1\/2

and in train or key as
1\/2/CD

Note that the POS tag is CD and not 2/CD

run 

grep '\\//' pos-train.txt 
grep '\\//' pos-key.txt 

to see more examples. If you notice any irregularities in this data please
let me know. 