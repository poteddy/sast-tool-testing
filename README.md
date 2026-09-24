# sast-tool-testing
THe intention of this app is to aggregate the scanned results of the NIST Juliet test cases for different programming languages

It reads the export files from the scanners and identifies the relationship and counts of the scans per cwe. 


Process

1. Input results of scans (Parse)
2. Aggregate Counts
3. Identifiy Confidence Score that the scanner found the Intended CWE per the filenames of the files scanned ( See "Reading Juliet Test Names") based on the MITRE CWE TOpology and relationship framework.
3. Report visually


Current Supported Reports
Veracode detailed xml
Snyk sarif file
Semgrep

Current Supported NITS tests
https://samate.nist.gov/SARD/test-suites/112

Reading juliet test names

in C++ 
Juliet naming convention:
[CWE being demonstrated]
    CWE122_Heap_Based_Buffer_Overflow

[source of bad data]
    CWE129_rand

[control flow variant]
    21

[bad sink]
    bad

The important distinction is:

CWE-122 = the ground-truth vulnerability being demonstrated.
CWE-129 = the source weakness used to trigger the vulnerability.
rand = merely how the bad index value is generated.