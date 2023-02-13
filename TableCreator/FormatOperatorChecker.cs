using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class FormatOperatorChecker
{
	int _offset = 1;
	ExcelParser _parser = null;
	Dictionary<string, int>[] _formats = null;
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

    static void FindFormat(string line, System.Action<int, int, int ,string> handler)
    {
        if(string.IsNullOrEmpty(line))
        {
            return;
        }

        int startindex = 0;
        while(startindex >= 0)
        {
            int start = line.IndexOf('{', startindex);
            startindex = start;
            if (start >= 0)
            {
                startindex++;
                int end_pos = line.IndexOf('}', start);
                if (end_pos > start)
                {
                    string test = line.Substring(start, end_pos - start + 1);
                    int symbol = test.IndexOf(':');
                    if (symbol < 0)
                    {
                        //没有找到 ':'
                        int status = 0;
                        for(int i = start + 1; i < end_pos; i ++)
                        {
                            if(line[i] >= '0' && line[i] <= '9')
                            {
                                status |= 1;
                            }
                            else if((line[i] >= 'a' && line[i] <= 'z') || (line[i] >= 'A' && line[i] <= 'Z'))
                            {
                                status |= 2;
                            }
                        }

                        if(status == 1)
                        {
                            int id = 0;
                            string number = test.Substring(1, test.Length - 2);
                            if(int.TryParse(number, out id))
                            {
                                handler(start, end_pos, id, test);
                            }
                        }
                        else if(status == 2)
                        {
                            handler(start, end_pos, -1, test);
                        }
                    }
                    else if (symbol > 0)
                    {
                        int status = 0;
                        for (int i = 1; i < symbol; i++)
                        {
                            if (test[i] >= '0' && test[i] <= '9')
                            {
                                status |= 1;
                            }
                            else if ((test[i] >= 'a' && test[i] <= 'z') || (test[i] >= 'A' && test[i] <= 'Z'))
                            {
                                status |= 2;
                            }
                        }

                        if (status == 1)
                        {
                            int id = 0;
                            string number = test.Substring(1, symbol - 1);
                            if (int.TryParse(number, out id))
                            {
                                handler(start, end_pos, id, test);
                            }
                        }
                        else if (status == 2)
                        {
                            handler(start, end_pos, -1, test);
                        }
                    }
                }
            }
        }
    }

	void ExtraFormaters(string line, Dictionary<string, int> formaters)
	{
		formaters.Clear();
        FindFormat(line, delegate (int start, int end, int id, string format)
        {
            formaters[format] = id;
        });
	}

	List<int> _unMatchFileds = new List<int>();
	void AppendFormats(StringBuilder stringBuilder, Dictionary<string, int>[] formats, int row, int field)
	{
		Dictionary<string, int> list = formats[field];
		stringBuilder.AppendFormat("\t\t{0}:", _parser.GetFieldName(field));
		stringBuilder.AppendLine(_parser.GetString(row, field));
		stringBuilder.Append("\t\tFormat:");
		if(list.Count > 0)
		{
            Dictionary<string, int>.Enumerator iter = list.GetEnumerator();
            while(iter.MoveNext())
            {
                stringBuilder.Append(iter.Current.Key);
                stringBuilder.Append(",");
            }
			stringBuilder.Remove(stringBuilder.Length - 1, 1);
		}
		stringBuilder.AppendLine();
	}

    Dictionary<int, bool> _formatIndexs = new Dictionary<int, bool>();
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

		Dictionary<string, int> src = _formats[_offset];
		for (int index = _offset + 1; index < _formats.Length; index++)
		{
			string line = _parser.GetString(row, index);
			if(string.IsNullOrEmpty(line))
			{
				continue;
			}

			bool unMatch = false;
			Dictionary<string, int> list = _formats[index];
			if (list.Count != src.Count)
			{
				unMatch = true;
			}
			else
			{
                Dictionary<string, int>.Enumerator iter = src.GetEnumerator();
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

        if (_unMatchFileds.Count > 0)
		{
			stringBuilder.AppendFormat("{0}行出错:\n", row + 1);
			AppendFormats(stringBuilder, _formats, row, _offset);
			for (int index = 0; index < _unMatchFileds.Count; index++)
			{
				AppendFormats(stringBuilder, _formats, row, _unMatchFileds[index]);
			}
			return false;
		}
        else if (src.Count > 0)
        {
            //检查format顺序是否正确
            int maxindex = -1;
            _formatIndexs.Clear();
            Dictionary<string, int>.Enumerator iter = src.GetEnumerator();
            while (iter.MoveNext())
            {
                int v = iter.Current.Value;
                if(v >= 0)
                {
                    _formatIndexs[v] = true;
                }

                if (v > maxindex)
                {
                    maxindex = v;
                }
            }

            if(maxindex + 1 != _formatIndexs.Count)
            {
                stringBuilder.AppendFormat("{0}行出错:\n", row + 1);
                AppendFormats(stringBuilder, _formats, row, _offset);
            }
        }
        return true;
	}

	public string Run()
	{
		if(_parser == null || _parser.FieldCount <= 0)
		{
			return "";
		}

		_formats = new Dictionary<string, int>[_parser.FieldCount];
		for(int index = 0; index < _formats.Length; index ++)
		{
			_formats[index] = new Dictionary<string, int>();
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