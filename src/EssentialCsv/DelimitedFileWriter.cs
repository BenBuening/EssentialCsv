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
using System.Collections;
using System.IO;

namespace EssentialCsv
{
    public class DelimitedFileWriter : IDisposable
    {

    #region Member Variables

        private delegate void WriteDelegate(IList fields);

        private string Delimiter { get; set; }
        private string FilePath { get; set; }
        private StreamWriter Writer { get; set; }
        private WriteDelegate WriteInternal { get; set; }
        private bool IsDisposed { get; set; }

        private readonly char[] _specialChars = new char[] { ',', '"', '\r', '\n' };

    #endregion

    #region Private Methods

        private void FirstWrite(IList fields)
        {
            this.Writer = new StreamWriter(this.FilePath);
            this.WriteInternal = SubsequentWrite;
            SubsequentWrite(fields);
        }

        private void SubsequentWrite(IList fields)
        {
            if (fields == null) throw new ArgumentException("Parameter 'fields' may not be null");
            this.Writer.WriteLine(BuildLine(fields));
        }

        private string PrepareFieldForCsv(object input)
        {
            string field = input == null ? string.Empty : input.ToString();
            if (field.IndexOfAny(_specialChars) >= 0)
                field = '"' + field.Replace("\"", "\"\"") + '"';
            return field;
        }

        private string BuildLine(IList fields)
        {
            string[] preparedFields = new string[fields.Count];
            for (int i = 0; i < fields.Count; i++)
                preparedFields[i] = PrepareFieldForCsv(fields[i]);
            return string.Join(this.Delimiter, preparedFields);
        }

    #endregion

    #region Public Methods

        public DelimitedFileWriter(string filePath) : this(filePath, ',') { }

        public DelimitedFileWriter(string filePath, char delimiter)
        {
            this.WriteInternal = FirstWrite;
            this.FilePath = filePath;
            this.Delimiter = (_specialChars[0] = delimiter).ToString();
        }

        public void Write(IList fields)
        {
            if (this.IsDisposed)
                throw new ObjectDisposedException(nameof(DelimitedFileWriter));

            WriteInternal(fields);
        }

        public void Dispose()
        {
            if (this.Writer != null)
            {
                this.Writer.Dispose();
                this.Writer = null;
            }
            this.WriteInternal = null;
        }

    #endregion

    }
}
