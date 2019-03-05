# UrduTessTrainingText
A simple CLI to generate training text for Tesseract 4
The purpose of this Console program is to generate training text for Tesseract 4 OCR for Urdu language.
The input consists of a word list which is already created from a dictionary and a list of text files 
to generate training lines from. Each word will be searched three times to get the lines for each entry.
The output list is populated on first come first serve basis. Hence if a word is present in alphabetically
first occuring file, it will not be re-searched again in subsequent files.
