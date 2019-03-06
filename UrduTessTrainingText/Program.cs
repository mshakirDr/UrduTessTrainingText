using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using Ganss.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UrduTessTrainingText
{
    /// <summary>
    /// The purpose of this Console program is to generate training text for Tesseract 4 OCR for Urdu language.
    /// The input consists of A word list which is already created from a dictionary And a list of text files 
    /// to generate training lines from. Each word will be searched three times to get the lines for each entry.
    /// The output list is populated on first come first serve basis, Hence if a word is present in alphabetically
    /// first file, it will not be researched again in subsequent files.
    /// </summary>
    public static class Program
    {
        static void Main(string[] args)
        {

            string textFiles = @"D:\Corpus Related\UrduCorpus\Complete\OCR\";
            string output = @"D:\Corpus Related\UrduOCR\";
            int exampleCount = 5;
            int spaceCount = 25;
            List<string> words = new List<string>();
            //word list for search
            words.AddRange(File.ReadAllLines(output + "WordList.txt"));
            //final ouput will be stored here
            Dictionary<string, concLines> concordanceLinesDict = populateDict(words);
            //Looped through all available text files
            foreach (string file in Directory.GetFiles(textFiles, "*.txt", SearchOption.AllDirectories))
            {
                //Remove new lines and create single line text
                string text = Regex.Replace(File.ReadAllText(file), @"[\r\n]+", " ");
                text = Regex.Replace(text, " {2,}", " ");
                //build AhoCorasick tree
                var ac = new AhoCorasick(words);
                //words that are there in text string
                var results = ac.Search(text).ToList();
                //words to be removed at the end of each text file
                List<string> toRemove = new List<string>();
                if (text != "")
                {
                    //loop through resulting strings
                    //for (int j = 0; j < results.Count; j++)
                    int j = 0;
                    Parallel.ForEach(results,
                        new ParallelOptions { MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 1.0)) },
                    (result) =>
                    {
                        string word = result.Word;
                        Console.WriteLine(file + "\t" + word + "\t" + results.Count + "\t" + j + "\t" + words.Count);
                        if (concordanceLinesDict.ContainsKey(word) && concordanceLinesDict[word].count < exampleCount)
                        {
                            {
                                concLines tmpConcLines = concordanceLinesDict[word];
                                string m = getConcordance(word, text, result.Index, spaceCount);
                                if (m != "")
                                {
                                    tmpConcLines.lines.Add(m);
                                    tmpConcLines.count = tmpConcLines.count + 1;
                                    if (tmpConcLines.count >= exampleCount)
                                    {
                                        //add word for removal at the end of loop
                                        toRemove.Add(word);
                                    }
                                }
                                concordanceLinesDict[word] = tmpConcLines;
                                Console.WriteLine(word + "\t" + tmpConcLines.count);
                            }
                        }
                        j++;
                    });
                }
                //remove all completed strings
                words.RemoveAll(item => toRemove.Contains(item));
            }
            //Write down words which are not found in the given list of text files
            StreamWriter sw = new StreamWriter(output + "NotFound.txt");
            foreach (KeyValuePair<string, concLines> kvp in concordanceLinesDict)
            {
                if (kvp.Value.count == 0)
                {
                    sw.WriteLine(kvp.Key);
                }
            }
            sw.Close();
            //Write the concordance lines generated using the main method
            concordanceLinesDict.WriteAllLines(output + "ConcordanceLines.txt");

        }
        public static void dummyMethodForParallelLooping(string file, WordMatch result, int  resultsCount, int wordsCount)
        {
            
        }
        //populate final output dict with words and prospect list objects to be filled in laters
        public static Dictionary<string, concLines> populateDict(List<string> list)
        {
            Dictionary<string, concLines> toReturn = new Dictionary<string, concLines>();
            foreach(string word in list)
            {
                if(!toReturn.ContainsKey(word))
                {
                    toReturn.Add(word, new concLines());
                }
            }
            return toReturn;
        }
        //An extension method to write all lines in the custom dictionary
        public static void WriteAllLines(this Dictionary<string, concLines> list, string path)
        {
            StreamWriter sw = new StreamWriter(path);
            foreach (KeyValuePair<string, concLines> kvp in list)
            {
                foreach (string line in kvp.Value.lines)
                {
                    sw.WriteLine(line.Trim());
                }
            }
            sw.Close();
        }
        //Get the concordance lines
        //Get the concordance lines
        public static string getConcordance(string word, string content, int startIndex, int numberOfSpaces)
        {
            //Slice the string from start index
            //find out the invoices of 10th space on both sides of the given word
            int sliceIndexLeft = getSliceIndexLeft(content, word, startIndex, numberOfSpaces);
            int sliceIndexRight = getSliceIndexRight(content, word, startIndex, numberOfSpaces);
            //The length of final line
            int length = sliceIndexRight - sliceIndexLeft;
            string concordance = "";
            //If left index or left index was zero, it will throw an error. A simple safeguard to avoid this
            if (length > 0)
            {
                concordance = content.Substring(sliceIndexLeft, length);
            }
            return concordance.Trim(' ');
        }
        //Reverse loop through to find 10 spaces to the left
        public static int getSliceIndexLeft(string content, string word, int startIndex, int numberOfSpaces)
        {
            int sliceIndex = 0;
            int spaceCount = 0;
            for (int i = startIndex; i > 0; i--)
            {
                if (content[i] == ' ')
                {
                    spaceCount++;
                }
                if (spaceCount > numberOfSpaces)
                {
                    sliceIndex = i;
                    break;
                }
            }
            return sliceIndex;
        }
        //Look through to the right 10 spaces to find out the index
        public static int getSliceIndexRight(string content, string word, int startIndex, int numberOfSpaces)
        {
            int sliceIndex = 0;
            int spaceCount = 0;
            int startIterationRight = startIndex + word.Length;
            for (int i = startIterationRight; i < content.Length; i++)
            {
                if (content[i] == ' ')
                {
                    spaceCount++;
                }
                if (spaceCount > numberOfSpaces)
                {
                    sliceIndex = i;
                    break;
                }
            }
            return sliceIndex;
        }
        //A custom object subclass to store concordance lines and their count
        public class concLines
        {
            public List<string> lines { get; set; }
            public int count { get; set; }
            public concLines()
            {
                lines = new List<string>();
            }
        }
    }
}