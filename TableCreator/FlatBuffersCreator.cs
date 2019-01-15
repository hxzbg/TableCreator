﻿using System;
using System.IO;
using FlatBuffers;
using System.Collections.Generic;
using System.Text.RegularExpressions;

class FlatBuffersCreator
{
	ExcelParser _excel = null;
	Dictionary<string, string> _dict = null;
	public FlatBuffersCreator(ExcelParser parser, Dictionary<string, string> dict)
	{
		_dict = dict;
		_excel = parser;
	}

	class StringUnit
	{
		static string[] _empty = new string[0];
		public string _in;
		public string _out;
		public int _out_offset;
		public string _outkey;
		public int _outkey_offset;
		public string[] _out_pars;
		public int[] _out_pars_offset_array;
		public StringUnit()
		{
			_out_pars = _empty;
		}
	}

	static int _lastkeyIndex = 0;
	static StringUnit SplitString(string input, Dictionary<string, string> user_dict)
	{
		string keyWord = "";
		StringUnit unit = new StringUnit();
		foreach(char ch in input)
		{
			if(ch > '~')
			{
				MatchCollection collection = Regex.Matches(input, "(\\d+(\\.\\d+)?)(?![^[]*\\])");
				if (collection.Count > 0)
				{
					unit._out_pars = new string[collection.Count];
					for (int i = 0; i < collection.Count; i++)
					{
						unit._out_pars[i] = collection[i].Value;
						keyWord += input.Substring(i == 0 ? 0 : collection[i - 1].Index + collection[i - 1].Length, (i == 0 ? collection[0].Index : collection[i].Index - collection[i - 1].Index - collection[i - 1].Length)) + "{" + (char)(i + 'A') + "}";
					}
					Match lastMatch = collection[collection.Count - 1];
					int lastEnd = lastMatch.Index + lastMatch.Length;
					if (lastEnd < input.Length)
					{
						keyWord += input.Substring(lastEnd);
					}
				}
				else
				{
					keyWord = input;
				}

				string key_index = null;
				if (user_dict.TryGetValue(keyWord, out key_index) == false)
				{
					key_index = string.Format("__ak_{0}", _lastkeyIndex++);
					while (user_dict.ContainsValue(key_index))
					{
						key_index = string.Format("__ak_{0}", _lastkeyIndex++);
					}
					user_dict[keyWord] = key_index;
				}
				unit._in = input;
				unit._out = keyWord;
				unit._outkey = key_index;
				return unit;
			}
		}

		unit._out = input;
		unit._outkey = input;
		return unit;
	}

	ulong makeKey(int field, int row)
	{
		ulong key = (ulong)(row + 1);
		return (key << 16) + (ulong)field;
	}

	public FlatBufferBuilder CreateFlatBufferBuilder()
	{
		if(_excel == null || _excel.RowCount <= 0)
		{
			return null;
		}

		FlatBufferBuilder builder = new FlatBufferBuilder(1);
		//提前收集所有字符串
		Dictionary<ulong, StringUnit> unitResult = new Dictionary<ulong, StringUnit>();
		Dictionary<string, int> dict = new Dictionary<string, int>();
		for(int index = 0; index < _excel.FieldCount; index ++)
		{
			if(_excel.GetFieldType(index) == ExceFieldType.TEXT)
			{
				for(int i = 0; i < _excel.RowCount; i ++)
				{
					string key = _excel.GetString(i, index);
					if(string.IsNullOrEmpty(key) || key.StartsWith("//"))
					{
						key = "";
					}

					int offset = 0;
					ulong grid_key = makeKey(index, i);
					StringUnit unit = SplitString(key, _dict);
					string str = unit._outkey;
					if (dict.TryGetValue(str, out offset) == false)
					{
						offset = builder.CreateString(str).Value;
						dict[str] = offset;
					}
					unit._outkey_offset = offset;

					unit._out_pars_offset_array = new int[unit._out_pars.Length];
					for (int j = 0; j < unit._out_pars.Length; j ++)
					{
						str = unit._out_pars[j];
						if (dict.TryGetValue(str, out offset) == false)
						{
							offset = builder.CreateString(str).Value;
							dict[str] = offset;
						}
						unit._out_pars_offset_array[j] = offset;
					}

					builder.StartVector(4, 1 + unit._out_pars_offset_array.Length, 4);
					for (int j = unit._out_pars_offset_array.Length - 1; j >= 0; j--)
					{
						builder.AddOffset(unit._out_pars_offset_array[j]);
					}
					builder.AddOffset(unit._outkey_offset);
					unit._out_offset = builder.EndVector().Value;
					unitResult[grid_key] = unit;
				}
			}
		}

		//压栈数据
		int[] items = new int[_excel.RowCount];
		for(int i = 0; i < _excel.RowCount; i ++)
		{
			builder.StartObject(_excel.FieldCount);
			for(int j = 0; j < _excel.FieldCount; j ++)
			{
				switch(_excel.GetFieldType(j))
				{
					case ExceFieldType.INTEGER:
						{
							builder.AddInt(j, _excel.GetInt(i, j), 0);
						}
						break;

					case ExceFieldType.REAL:
						{
							builder.AddFloat(j, _excel.GetSingle(i, j), 0.0f);
						}
						break;

					case ExceFieldType.TEXT:
						{
							ulong key = makeKey(j, i);
							StringUnit unit = unitResult[key];
							builder.AddOffset(j, unit._out_offset, 0);
						}
						break;
				}
			}
			items[i] = builder.EndObject();
		}

		//打包数据
		builder.StartVector(4, items.Length, 4);
		for (int index = items.Length - 1; index >= 0; index--)
		{
			builder.AddOffset(items[index]);
		}
		int vector = builder.EndVector().Value;

		builder.StartObject(1);
		builder.AddOffset(0, vector, 0);
		builder.Finish(builder.EndObject());
		
		return builder;
	}

	public string SaveFlatBuffer(FlatBufferBuilder builder, string path)
	{
		if(builder == null)
		{
			return "";
		}

		if(string.IsNullOrEmpty(path) == false && Directory.Exists(path) == false)
		{
			Directory.CreateDirectory(path);
		}
		path = Path.Combine(path, _excel.FileName + ".bytes");
		MemoryStream ms = builder.DataBuffer.ToMemoryStream(builder.DataBuffer.Position, builder.Offset);
		File.WriteAllBytes(path, ms.ToArray());
		return path;
	}
}
