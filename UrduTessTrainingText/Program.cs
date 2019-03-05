using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Collections.Generic;
using System.Linq;

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
            string textFiles = @"text files directory";
            string output = @"output for concordance lines";
            List<string> words = new List<string>();
            //word list and reverse for easy removal of redundant items later
            words.AddRange(File.ReadAllLines(output + "WordList.txt"));
            words.Reverse();
            //final ouput will be stored here
            Dictionary<string, concLines> concordanceLinesDict = new Dictionary<string, concLines>();
            //Looped through all available text files
            foreach (string file in Directory.GetFiles(textFiles, "*.txt", SearchOption.AllDirectories))
            {
                //Remove new lines and create single line text
                string text = Regex.Replace(File.ReadAllText(file), @"[\r\n]+", " ");
                text = Regex.Replace(text, " {2,}", " ");
                Console.WriteLine(file);
                if (text != "")
                {
                    //Reverse loop through words list
                    for (int j = words.Count - 1; j >= 0; j--)
                    {
                        string word = words[j];
                        Console.WriteLine(file + "\t" + words.Count + "\t" + j);
                        if (!concordanceLinesDict.ContainsKey(word))
                        {
                            if (text.Contains(word))
                            {
                                Console.WriteLine("Empty dictionary, regex matched");
                                concLines tmpConcLines = new concLines();
                                //Index where to start looking for word occurrence in the text
                                int startIndex = 0;
                                //Only loop through three times
                                for (int i = 0; i < 3; i++)
                                {
                                    //Get the concordance which consists of the word and 10
                                    //space separated chunks on each side
                                    string m = getConcordance(word, text, startIndex, 10);
                                    if (m != "")
                                    {
                                        tmpConcLines.lines.Add(m);
                                        tmpConcLines.count = tmpConcLines.count + 1;
                                        //Update the index to avoid repetition
                                        startIndex = text.IndexOf(m) + m.Length;
                                        if (tmpConcLines.count > 2)
                                        {
                                            //If already three occurrences are completed remove the item
                                            //from words list to reduce loping burden and break the sub- for loop
                                            words.RemoveAt(j);
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                //Add the object to final output dictionary
                                concordanceLinesDict.Add(word, tmpConcLines);
                                Console.WriteLine(word + "\t" + tmpConcLines.count);
                            }
                            else
                            {
                                concordanceLinesDict.Add(word, new concLines());
                                Console.WriteLine(word + "\t" + "empty entry created");
                            }
                        }
                        //If the dictionary was half empty the same process as above will be repeated
                        else
                        {
                            if (concordanceLinesDict[word].count < 2)
                            {
                                if (text.Contains(word))
                                {
                                    Console.WriteLine("Half filled dictionary, regex matched");
                                    int startIndex = 0;
                                    concLines tmpConcLines = concordanceLinesDict[word];
                                    for (int i = 0; i < 3; i++)
                                    {
                                        string m = getConcordance(word, text, startIndex, 10);
                                        if (m != "")
                                        {
                                            tmpConcLines.lines.Add(m);
                                            tmpConcLines.count = tmpConcLines.count + 1;
                                            startIndex = text.IndexOf(m) + m.Length;
                                            if (tmpConcLines.count > 2)
                                            {
                                                words.RemoveAt(j);
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    concordanceLinesDict[word] = tmpConcLines;
                                    Console.WriteLine(word + "\t" + tmpConcLines.count);
                                }
                            }
                        }
                    }
                }

            }
            //Write down words which are not found in the given list of text files
            StreamWriter sw = new StreamWriter(output + "NotFound.txt");
            foreach (string word in words)
            {
                if (concordanceLinesDict[word].count == 0)
                {
                    sw.WriteLine(word);
                }
            }
            sw.Close();
            //Write the concordance lines generated using the main method
            concordanceLinesDict.WriteAllLines(output + "ConcordanceLines.txt");

        }
        //An extension method to write all lines in the custom dictionary
    public static void WriteAllLines(this Dictionary<string, concLines> list, string path)
    {
     	StreamWriter sw = new StreamWriter(path);
        foreach(KeyValuePair<string, concLines> kvp in list)
        {
        	foreach(string line in kvp.Value.lines)
        	{
        		sw.WriteLine(line.Trim());
        	}
        }
        sw.Close();
     }
        //Get the concordance lines
     public static string getConcordance(string word, string content, int startIndex, int numberOfSpaces)
     {
        //Slice the string from start index
     	string content1 = content.Substring(startIndex);
        //find out the invoices of 10th space on both sides of the given word
     	int sliceIndexLeft = getSliceIndexLeft(content1, word, numberOfSpaces);
     	int sliceIndexRight = getSliceIndexRight(content1, word, numberOfSpaces);
        //The length of final line
     	int length = sliceIndexRight - sliceIndexLeft;
     	string concordance = "";
        //If left index or left index was zero, it will throw an error. A simple safeguard to avoid this
     	if(length > 0)
     	{
     		concordance = content1.Substring(sliceIndexLeft, length);
     	}
     	return concordance.Trim(' ');
     }
     //Reverse loop through to find 10 spaces to the left
     public static int getSliceIndexLeft(string content, string word, int numberOfSpaces)
     {
     	int sliceIndex = 0;
     	int spaceCount = 0;
     	for(int i=content.IndexOf(word);i>0;i--)
     	{
     		if(content[i] == ' ')
     		{
     			spaceCount++;
     		}
     		if(spaceCount > numberOfSpaces)
     		{
     			sliceIndex = i;
     			break;
     		}
     	}
     	return sliceIndex;
     }
        //Look through to the right 10 spaces to find out the index
     public static int getSliceIndexRight(string content, string word, int numberOfSpaces)
     {
     	int sliceIndex = 0;
     	int spaceCount = 0;
     	int startIterationRight = content.IndexOf(word)+word.Length;
     	for(int i=startIterationRight;i<content.Length;i++)
     	{
     		if(content[i] == ' ')
     		{
     			spaceCount++;
     		}
     		if(spaceCount > numberOfSpaces)
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
     	public List<string> lines {get; set;}
     	public int count {get; set;}
     	public concLines()
     	{
     		lines = new List<string>();
     	}
     }
    }
}
