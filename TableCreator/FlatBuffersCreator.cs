using System;
using System.IO;
using FlatBuffers;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

class FlatBuffersCreator
{
	ExcelParser _excel = null;
	string excelHeader
	{
		get
		{
			//生成并返回表头
			return "";
		}
	}
	Dictionary<string, string> _dict = null;
	public FlatBuffersCreator(ExcelParser parser, Dictionary<string, string> dict)
	{
		_dict = dict;
		_excel = parser;
	}

	public class StringUnit
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

	static public bool IsHex(char ch)
	{
		return (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
	}

	static Dictionary<string, int> _lastkeyIndex = new Dictionary<string, int>();
	public static StringUnit SplitString(string input, string keyname, Dictionary<string, string> user_dict)
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
					List<string> list = new List<string>();
					for (int i = 0; i < collection.Count; i++)
					{
						//判断是否为{0}之类的形式
						string add = null;
						Match match = collection[i];
						if(match.Index > 0 && (match.Index + match.Length) < input.Length && input[match.Index - 1] == '{' && input[match.Index + match.Length] == '}')
						{
							add = match.Value;
						}
						else
						{
							char cKey = (char)(list.Count + 'A');
							if (cKey > 'Z')
							{
								cKey = (char)('a' + (cKey - 'Z'));
							}
							list.Add(match.Value);
							add = "{" + cKey + "}";
						}
						keyWord += input.Substring(i == 0 ? 0 : collection[i - 1].Index + collection[i - 1].Length, (i == 0 ? collection[0].Index : collection[i].Index - collection[i - 1].Index - collection[i - 1].Length)) + add;
					}
					unit._out_pars = list.ToArray();
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

				int key_len = keyWord.Length;
				int index = keyWord.IndexOf('[');
				while(index >= 0)
				{
					int endIndex = keyWord.IndexOf(']', index);
					if(endIndex < 0)
					{
						break;
					}
					int offset = endIndex - index;
					if (offset == 7 || offset == 9)
					{
						bool isHexColor = true;
						for(int j = 1; j <= offset - 1; j ++)
						{
							if(IsHex(keyWord[index + j]) == false)
							{
								isHexColor = false;
								break;
							}
						}

						if(isHexColor)
						{
							string hexColor = keyWord.Substring(index, offset + 1);
							keyWord = keyWord.Replace(hexColor, hexColor.ToUpper());
						}
					}
					index = keyWord.IndexOf('[', index + 1);
				}

				string key_index = null;
				Dictionary<string, string>.Enumerator em = user_dict.GetEnumerator();
				while(em.MoveNext())
				{
					KeyValuePair<string, string> pair = em.Current;
					if(pair.Value == keyWord && pair.Key.StartsWith("__"))
					{
						key_index = pair.Key;
						break;
					}
				}

				int keyid = 0;
				_lastkeyIndex.TryGetValue(keyname, out keyid);
				if (string.IsNullOrEmpty(key_index))
				{
					do
					{
						key_index = string.Format("__{0}_{1}", keyname, keyid++);
					}
					while (user_dict.ContainsKey(key_index));
					user_dict[key_index] = keyWord;
				}
				_lastkeyIndex[keyname] = keyid;

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

	class ExcelFieldValues
	{
		public int m_position;
		public int m_int32;
		public float m_single;
		public double m_double;
		public long m_int64;
		public string m_string;

		public ExcelFieldValues(int position)
		{
			m_position = position;
		}
	}

	int AppendIndexs(FlatBufferBuilder builder)
    {
		//生成索引数据
		int[] indexArray = new int[_excel.FieldCount];
		//为排序做准备
		List<ExcelFieldValues> filedValues = new List<ExcelFieldValues>(_excel.RowCount);
		{
			for (int i = 0; i < _excel.RowCount; i++)
			{
				filedValues.Add(new ExcelFieldValues(i));
			}
		}

		int elementSize = _excel.RowCount >= ushort.MaxValue ? 4 : 2;
		for (int j = 0; j < _excel.FieldCount; j++)
		{
			ExceFieldType fieldType = _excel.GetFieldType(j);
			for (int i = 0; i < _excel.RowCount; i++)
			{
				ExcelFieldValues item = filedValues[i];
				item.m_position = i;
				switch (fieldType)
				{
					case ExceFieldType.INTEGER:
						{
							item.m_int32 = (int)_excel.GetLong(i, j);
						}
						break;

					case ExceFieldType.LONG:
						{
							item.m_int64 = _excel.GetLong(i, j);
						}
						break;

					case ExceFieldType.REAL:
						{
							item.m_single = _excel.GetSingle(i, j);
						}
						break;

					case ExceFieldType.DOUBLE:
                        {
							item.m_double = _excel.GetDouble(i, j);
						}
						break;

					case ExceFieldType.TEXT:
						{
							item.m_string = _excel.GetString(i, j);
						}
						break;
				}
			}

			filedValues.Sort(delegate (ExcelFieldValues a, ExcelFieldValues b)
			{
				int result = 0;
				switch (fieldType)
				{
					case ExceFieldType.INTEGER:
						{
							result = DataStoreHelper.__CompareInt32(a.m_int32, b.m_int32);
						}
						break;

					case ExceFieldType.LONG:
						{
							result = DataStoreHelper.__CompareInt64(a.m_int64, b.m_int64);
						}
						break;

					case ExceFieldType.REAL:
						{
							result = DataStoreHelper.__CompareSingle(a.m_single, b.m_single);
						}
						break;

					case ExceFieldType.DOUBLE:
						{
							result = DataStoreHelper.__CompareDouble(a.m_double, b.m_double);
						}
						break;

					case ExceFieldType.TEXT:
						{
							result = DataStoreHelper.__CompareString(a.m_string, b.m_string);
						}
						break;
				}

				if (result == 0 && a.m_position != b.m_position)
				{
					result = DataStoreHelper.__CompareInt32(a.m_position, b.m_position);
				}
				return result;
			});

			/*
			bool writelog = false;
			if (writelog)
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < filedValues.Count; i++)
				{
					switch (fieldType)
					{
						case ExceFieldType.INTEGER:
							{
								sb.AppendFormat("{0},{1},{2}\n", filedValues[i].m_position, i, filedValues[i].m_int32);
							}
							break;

						case ExceFieldType.LONG:
							{
								sb.AppendFormat("{0},{1},{2}\n", filedValues[i].m_position, i, filedValues[i].m_int64);
							}
							break;

						case ExceFieldType.REAL:
							{
								sb.AppendFormat("{0},{1},{2}\n", filedValues[i].m_position, i, filedValues[i].m_single);
							}
							break;
					}
				}
				File.WriteAllText(@"Y:\Documents\Work\log\log.csv", sb.ToString());
			}
			*/

			builder.StartVector(elementSize, filedValues.Count, elementSize);
            int[] indexs = new int[_excel.RowCount];
            for(int i = 0; i < indexs.Length; i ++)
            {
                indexs[i] = filedValues[i].m_position;
            }
            _excel.SetFiledIndexs(j, indexs);

			for (int i = _excel.RowCount - 1; i >= 0; i--)
			{
				ExcelFieldValues item = filedValues[i];
				if (elementSize == 4)
				{
					builder.AddInt(item.m_position);
				}
				else
				{
					builder.AddUshort((ushort)item.m_position);
				}
			}
			indexArray[j] = builder.EndVector().Value;

			builder.StartObject(1);
			builder.AddOffset(0, indexArray[j], 0);
			indexArray[j] = builder.EndObject();
		}

		builder.StartVector(4, indexArray.Length, 4);
		for (int i = indexArray.Length - 1; i >= 0; i--)
		{
			builder.AddOffset(indexArray[i]);
		}
		int indexObject = builder.EndVector().Value;
		builder.StartObject(1);
		builder.AddOffset(0, indexObject, 0);
		indexObject = builder.EndObject();
		return indexObject;
	}

	public FlatBufferBuilder CreateFlatBufferBuilder()
	{
		if(_excel == null || _excel.RowCount <= 0)
		{
			return null;
		}

		string excelname = _excel.FileName;
		FlatBufferBuilder builder = new FlatBufferBuilder(1);
		/*
		builder.Finish(AppendIndexs(builder));
		int[] int32Array = null;
		FlatbufferDataStore test = FlatbufferDataStore.CreateFlatbufferDataStore(builder.DataBuffer);
		FlatbufferDataStore cc = new FlatbufferDataStore();
		for(int i = 0; i < test.Length; i ++)
        {
			test.AppendChild(cc, i, 0);
			int32Array = cc.GetInt32Array();
		}
		*/
	
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
					StringUnit unit = SplitString(key, excelname, _dict);
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
							builder.AddInt(j, (int)_excel.GetLong(i, j), 0);
						}
						break;

					case ExceFieldType.LONG:
						{
							builder.AddLong(j, _excel.GetLong(i, j), 0);
						}
						break;

					case ExceFieldType.REAL:
						{
							builder.AddFloat(j, _excel.GetSingle(i, j), 0.0f);
						}
						break;

                    case ExceFieldType.DOUBLE:
                        {
                            builder.AddDouble(j, _excel.GetDouble(i, j), 0.0f);
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
		int indexs = AppendIndexs(builder);
		builder.StartObject(1);
		builder.AddOffset(0, indexs, 0);
		indexs = builder.EndObject();

		builder.StartObject(2);
		builder.AddOffset(0, vector, 0);
		builder.AddOffset(1, indexs, 0);
		
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
