/*
 * Copyright (c) 2017 Benjamin Buening
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * */

using System;
using System.Collections.Generic;
using System.IO;

namespace EssentialCsv
{
    public sealed class DelimitedFileReader : IDisposable
    {

    #region Member Variables

        private delegate bool ReadDelegate(List<string> fieldStorage);

        private bool StripNullChars { get; set; }
        private bool IsDisposed { get; set; }
        private string NullChar { get; set; }
        private StreamReader Reader { get; set; }
        private ReadDelegate ReadInternal { get; set; }
        private char Delimiter { get; set; }
        private string FilePath { get; set; }

    #endregion

    #region Private Methods

        private bool FirstRead(List<string> fieldStorage)
        {
            if (!File.Exists(this.FilePath)) throw new ArgumentException("The specified file does not exist");
            this.Reader = new StreamReader(this.FilePath);
            this.ReadInternal = SubsequentRead;
            return SubsequentRead(fieldStorage);
        }

        private bool SubsequentRead(List<string> fieldStorage)
        {
            if (fieldStorage == null) throw new ArgumentException("Parameter 'fieldStorage' may not be null");

            string line = FetchRecordLine();

            if (line == null)
                return false;
            else
            {
                int currentIndex = 0;
                int readLength = line.IndexOf(this.Delimiter, currentIndex);

                while (readLength >= 0)
                {
                    string record = line.Substring(currentIndex, readLength);
                    currentIndex += readLength;

                    // if there are an odd number of quotes, read to next comma & append. repeat as necessary
                    while (CountDelimiter(record, '"') % 2 > 0)
                    {
                        readLength = line.IndexOf(this.Delimiter, currentIndex + 1) - currentIndex;
                        if (readLength < 0)
                        {
                            record += line.Substring(currentIndex);
                            currentIndex = line.Length;
                        }
                        else
                        {
                            record += line.Substring(currentIndex, readLength);
                            currentIndex += readLength;
                        }
                    }

                    // Remove leading and trailing spaces, quotes, and commas. Then, store the field
                    fieldStorage.Add(ReformatField(record));

                    // go on to parsing the next field from the line until there are no more
                    readLength = currentIndex < line.Length ? line.IndexOf(this.Delimiter, currentIndex + 1) - currentIndex : -1;
                }

                // don't forget the final record
                if (currentIndex < line.Length) fieldStorage.Add(ReformatField(line.Substring(currentIndex)));

                return true;
            }
        }

        private string FetchRecordLine()
        {
            string line;
            do
            {
                line = this.Reader.ReadLine();
                if (line == null) return null;
            } while (line.Trim(new[] { ' ' }).Length == 0);

            int delimiterCount = CountDelimiter(line, '"');
            while (delimiterCount % 2 > 0)
            {   // if the line has an odd number of quotes, read the next line (odd means the line break is inside a quoted field)
                string temp = this.Reader.ReadLine();
                if (temp == null) throw new InvalidFileFormatException("Invalid CSV file format most likely caused by mismatched on non-escaped quotes.");
                delimiterCount += CountDelimiter(temp, '"');
                line += Environment.NewLine + temp;
            }
            
            return this.StripNullChars ? line.Replace(this.NullChar, string.Empty) : line;
        }

        private int CountDelimiter(string sourceString, char delimiter)
        {
            int count = 0;

            foreach (char c in sourceString)
                if (c == delimiter) count++;

            return count;
        }

        private string ReformatField(string sourceString)
        {
            // remove leading and trailing spaces and commas
            sourceString = sourceString.Trim(new char[] { ' ', this.Delimiter });

            // still have to remove leading and trailing quotes
            if (sourceString.Contains("\""))
                if (sourceString.Length >= 2 && sourceString[0] == '\"' && sourceString[sourceString.Length - 1] == '\"')
                {
                    sourceString = sourceString.Substring(1, sourceString.Length - 2);
                    AssertQuotesValidity(sourceString);
                    sourceString = sourceString.Replace("\"\"", "\"");
                }
                else
                    throw new InvalidFileFormatException("Invalid CSV file format caused by quotes in a non-quoted field.");

            return sourceString;
        }

        private void AssertQuotesValidity(string value)
        {
            if (value.Length > 1)
                for (int i = 0; i < value.Length - 1; i++)
                    if (value[i] == '"' && value[++i] != '"')
                        throw new InvalidFileFormatException("Invalid CSV file format caused by non-escaped quotes inside a quoted field.");
        }


        private static List<List<string>> ReadAllRecords(DelimitedFileReader reader)
        {
            List<List<string>> results = new List<List<string>>();
            List<string> record = new List<string>();
            while (reader.Read(record))
            {
                results.Add(record);
                record = new List<string>();
            }
            return results;
        }

    #endregion

    #region Public Methods

        public DelimitedFileReader(string filePath) : this(filePath, ',') { }

        public DelimitedFileReader(string filePath, char delimiter) : this(filePath, delimiter, true) { }

        public DelimitedFileReader(string filePath, bool stripNullChars) : this(filePath, ',', stripNullChars) { }

        public DelimitedFileReader(string filePath, char delimiter, bool stripNullChars)
        {
            this.NullChar = '\0'.ToString();
            this.ReadInternal = FirstRead;
            this.FilePath = filePath;
            this.Delimiter = delimiter;
            this.StripNullChars = stripNullChars;
        }

        public bool Read(List<string> fieldStorage)
        {
            if (this.IsDisposed)
                throw new ObjectDisposedException(nameof(DelimitedFileReader));

            return ReadInternal(fieldStorage);
        }

        public void Dispose()
        {
            if (this.Reader != null)
            {
                this.Reader.Dispose();
                this.Reader = null;
            }
            this.ReadInternal = null;
            this.IsDisposed = true;
        }



        public static List<List<string>> ReadAllRecords(string filePath)
        {
            using (var reader = new DelimitedFileReader(filePath))
                return ReadAllRecords(reader);
        }

        public static List<List<string>> ReadAllRecords(string filePath, char delimiter)
        {
            using (var reader = new DelimitedFileReader(filePath, delimiter))
                return ReadAllRecords(reader);
        }

        public static List<List<string>> ReadAllRecords(string filePath, bool stripNullChars)
        {
            using (var reader = new DelimitedFileReader(filePath, stripNullChars))
                return ReadAllRecords(reader);
        }

        public static List<List<string>> ReadAllRecords(string filePath, char delimiter, bool stripNullChars)
        {
            using (var reader = new DelimitedFileReader(filePath, delimiter, stripNullChars))
                return ReadAllRecords(reader);
        }

    #endregion

    }
}
