using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class FormatOperatorChecker
{
	int _offset = 1;
	ExcelParser _parser = null;
	Dictionary<string, bool>[] _formats = null;
	public FormatOperatorChecker(ExcelParser parser, int offset = 1)
	{
		_parser = parser;
		_offset = offset;
	}

	static System.Func<string, int, int> OneCharChecker = delegate (string line, int pos)
	{
		if (line.Length <= pos)
		{
			return 0;
		}

		switch (line[pos])
		{
			case 'd':
			case 'o':
			case 'x':
			case 'X':
			case 'u':
			case 'f':
			case 'c':
			case 's':
				return 1;

			default:
				return 0;
		}
	};

	static System.Func<string, int, int> TwoCharChecker = delegate (string line, int pos)   //lu
	{
		if (line.Length <= pos + 1)
		{
			return 0;
		}

		char c0 = line[pos];
		char c1 = line[pos + 1];
		return c0 == 'l' && c1 == 'u' ? 2 : 0;
	};

	static System.Func<string, int, int> ThreeCharChecker = delegate (string line, int pos)   //lu
	{
		if (line.Length <= pos + 2)
		{
			return 0;
		}

		char c0 = line[pos];
		char c1 = line[pos + 1];
		char c2 = line[pos + 2];
		return c0 == 'l' && c1 == 'l' && c2 == 'u' ? 3 : 0;
	};

	static System.Func<string, int, int>[] _FormatCheckers = { ThreeCharChecker, TwoCharChecker, OneCharChecker };

	public static void ParseFormaters(string line, System.Action<int, int> parser)
	{
		if(string.IsNullOrEmpty(line) == false && parser != null)
		{
			for (int index = 0; index < line.Length; index++)
			{
                /*
				if (line[index] == '%')
				{
					index++;
					for (int i = 0; i < _FormatCheckers.Length; i++)
					{
						int result = _FormatCheckers[i](line, index);
						if (result > 0)
						{
							parser(index - 1, result);
						}
					}
				}
				else */if (line[index] == '{' && (index + 2) < line.Length && line[index + 2] == '}')
				{
					parser(index, 1);
					index += 2;
				}
			}
		}
	}

	void ExtraFormaters(string line, Dictionary<string, bool> formaters)
	{
		formaters.Clear();
		for(int index = 0; index < line.Length; index ++)
		{
            /*
			if(line[index] == '%')
			{
				index++;
				for(int i = 0; i < _FormatCheckers.Length; i ++)
				{
					int result = _FormatCheckers[i](line, index);
					if(result > 0)
					{
						formaters[line.Substring(index - 1, result + 1)] = true;
					}
				}
			}
			else */if(line[index] == '{' && (index + 2) < line.Length && line[index + 2] == '}')
			{
				formaters[line.Substring(index, 3)] = true;
				index += 2;
			}
		}
	}

	List<int> _unMatchFileds = new List<int>();
	void AppendFormats(StringBuilder stringBuilder, Dictionary<string, bool>[] formats, int row, int field)
	{
		Dictionary<string, bool> list = formats[field];
		stringBuilder.AppendFormat("\t\t{0}:", _parser.GetFieldName(field));
		stringBuilder.AppendLine(_parser.GetString(row, field));
		stringBuilder.Append("\t\tFormat:");
		if(list.Count > 0)
		{
            Dictionary<string, bool>.Enumerator iter = list.GetEnumerator();
            while(iter.MoveNext())
            {
                stringBuilder.Append(iter.Current.Key);
                stringBuilder.Append(",");
            }
			stringBuilder.Remove(stringBuilder.Length - 1, 1);
		}
		stringBuilder.AppendLine();
	}

	bool CheckRow(int row, StringBuilder stringBuilder)
	{
		_unMatchFileds.Clear();
		for (int index = _offset; index < _parser.FieldCount; index ++)
		{
			_formats[index].Clear();
			string line = _parser.GetString(row, index);
			if (string.IsNullOrEmpty(line))
			{
				continue;
			}

			ExtraFormaters(line, _formats[index]);
		}

		Dictionary<string, bool> src = _formats[_offset];
		for (int index = _offset + 1; index < _formats.Length; index++)
		{
			string line = _parser.GetString(row, index);
			if(string.IsNullOrEmpty(line))
			{
				continue;
			}

			bool unMatch = false;
			Dictionary<string, bool> list = _formats[index];
			if (list.Count != src.Count)
			{
				unMatch = true;
			}
			else
			{
                Dictionary<string, bool>.Enumerator iter = src.GetEnumerator();
                while(iter.MoveNext())
                {
                    if(!list.ContainsKey(iter.Current.Key))
                    {
                        unMatch = true;
                        break;
                    }
                }
			}

			if (unMatch)
			{
				_unMatchFileds.Add(index);
			}
		}

		if(_unMatchFileds.Count > 0)
		{
			stringBuilder.AppendFormat("{0}行出错:\n", row + 1);
			AppendFormats(stringBuilder, _formats, row, _offset);
			for (int index = 0; index < _unMatchFileds.Count; index++)
			{
				AppendFormats(stringBuilder, _formats, row, _unMatchFileds[index]);
			}
			return false;
		}
		return true;
	}

	public string Run()
	{
		if(_parser == null || _parser.FieldCount <= 0)
		{
			return "";
		}

		_formats = new Dictionary<string, bool>[_parser.FieldCount];
		for(int index = 0; index < _formats.Length; index ++)
		{
			_formats[index] = new Dictionary<string, bool>();
		}

		bool hasError = false;
		StringBuilder stringBuilder = new StringBuilder();
		for(int index = 0; index < _parser.RowCount; index ++)
		{
			hasError |= CheckRow(index, stringBuilder);
		}
		return hasError ? stringBuilder.ToString() : "";
	}
}