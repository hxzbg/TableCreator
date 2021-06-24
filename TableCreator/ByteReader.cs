//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2018 Tasharen Entertainment Inc
//-------------------------------------------------

using System.Text;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// MemoryStream.ReadLine has an interesting oddity: it doesn't always advance the stream's position by the correct amount:
/// http://social.msdn.microsoft.com/Forums/en-AU/Vsexpressvcs/thread/b8f7837b-e396-494e-88e1-30547fcf385f
/// Solution? Custom line reader with the added benefit of not having to use streams at all.
/// </summary>

public class ByteReader
{
	StreamReader mStream;

	public ByteReader (StreamReader stream) { mStream = stream; }

	/// <summary>
	/// Read the contents of the specified file and return a Byte Reader to work with.
	/// </summary>

	static public ByteReader Open (string path)
	{
        FileStream fs = File.OpenRead(path);
        if (fs != null)
        {
            return new ByteReader(new StreamReader(fs));
        }
        return null;
	}

	/// <summary>
	/// Whether the buffer is readable.
	/// </summary>

	public bool canRead { get { return (mStream != null && mStream.EndOfStream == false); } }

	/// <summary>
	/// Read a single line from the buffer.
	/// </summary>

	/// <summary>
	/// Read a single line from the buffer.
	/// </summary>

	public string ReadLine () { return ReadLine(true); }

	/// <summary>
	/// Read a single line from the buffer.
	/// </summary>

	public string ReadLine (bool skipEmptyLines)
	{
		string line = mStream.ReadLine();
		while(skipEmptyLines && string.IsNullOrEmpty(line) && mStream.EndOfStream == false)
        {
			line = mStream.ReadLine();
        }
		return skipEmptyLines && string.IsNullOrEmpty(line) ? null : line;
	}

	/// <summary>
	/// Assume that the entire file is a collection of key/value pairs.
	/// </summary>

	public Dictionary<string, string> ReadDictionary ()
	{
		Dictionary<string, string> dict = new Dictionary<string, string>();
		char[] separator = new char[] { '=' };

		while (canRead)
		{
			string line = ReadLine();
			if (line == null) break;
			if (line.StartsWith("//")) continue;

#if UNITY_FLASH
			string[] split = line.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
#else
			string[] split = line.Split(separator, 2, System.StringSplitOptions.RemoveEmptyEntries);
#endif

			if (split.Length == 2)
			{
				string key = split[0].Trim();
				string val = split[1].Trim().Replace("\\n", "\n");
				dict[key] = val;
			}
		}
		return dict;
	}

	/// <summary>
	/// Read a single line of Comma-Separated Values from the file.
	/// </summary>

	public bool ReadCSV (List<string> list)
	{
		list.Clear();
		string line = "";
		bool insideQuotes = false;
		int wordStart = 0;

		while (canRead)
		{
			if (insideQuotes)
			{
				string s = ReadLine(false);
				if (s == null) return false;
				s = s.Replace("\\n", "\n");
				line += "\n" + s;
			}
			else
			{
				line = ReadLine(true);
				if (line == null) return false;
				line = line.Replace("\\n", "\n");
				wordStart = 0;
			}

			for (int i = wordStart, imax = line.Length; i < imax; ++i)
			{
				char ch = line[i];

				if (ch == ',' || ch == '\t')
				{
					if (!insideQuotes)
					{
						list.Add(line.Substring(wordStart, i - wordStart));
						wordStart = i + 1;
					}
				}
				else if (ch == '"')
				{
					if (insideQuotes)
					{
						if (i + 1 >= imax)
						{
							list.Add(line.Substring(wordStart, i - wordStart).Replace("\"\"", "\""));
							return true;
						}

						char ch_next = line[i + 1];
						if (ch_next != '"')
						{
							list.Add(line.Substring(wordStart, i - wordStart).Replace("\"\"", "\""));
							insideQuotes = false;

							if (ch_next == ',' || ch_next == '\t')
							{
								++i;
								wordStart = i + 1;
							}
						}
						else ++i;
					}
					else
					{
						wordStart = i + 1;
						insideQuotes = true;
					}
				}
			}

			if (wordStart < line.Length)
			{
				if (insideQuotes) continue;
				list.Add(line.Substring(wordStart, line.Length - wordStart));
			}
			else list.Add("");
			return true;
		}
		return false;
	}

	public void Dispose()
    {
		if(mStream != null)
        {
			mStream.Dispose();
			mStream = null;
        }
    }
}
