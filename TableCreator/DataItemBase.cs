﻿using System;
using System.Text;
using FlatBuffers;
using System.Collections.Generic;

public class DataItemPaser
{
	string _name;
	public string Name
	{
		get
		{
			return _name;
		}
	}

	string[] _fieldNames;
	public string[] FieldNames
	{
		get
		{
			return _fieldNames;
		}
	}

	string[] _fieldTypes;
	public string[] FieldTypes
	{
		get
		{
			return _fieldTypes;
		}
	}

	int _columnsCount;
	public int ColumnsCount
	{
		get
		{
			return _columnsCount;
		}
	}

	public DataItemPaser(string name, string[] filedNames, string[] fieldTypes, int columnsCount, System.Func<int, string[]> fieldParser)
	{
		_name = name;
		_fieldNames = filedNames;
		_fieldTypes = fieldTypes;
		_columnsCount = columnsCount;
		_fieldParser = fieldParser;
	}

	System.Func<int, string[]> _fieldParser = null;
	public System.Func<int, string[]> FieldParser
	{
		get
		{
			return _fieldParser;
		}
	}

	static List<System.Func<DataItemPaser>> _parserList = new List<System.Func<DataItemPaser>>();
	public static List<System.Func<DataItemPaser>> ParserList
	{
		get
		{
			return _parserList;
		}
	}

	public static void PushDataItemParser(System.Func<DataItemPaser> parser)
	{
		if(parser != null && _parserList.Contains(parser) == false)
		{
			_parserList.Add(parser);
		}
	}
}

public class DataItemBase
{
	static System.Action _dispose = null;

	public static int CompareInt(int a, int b)
	{
		if(a == b)
		{
			return 0;
		}
		return a > b ? 1 : -1;
	}

	public static int CompareLong(long a, long b)
	{
		if (a == b)
		{
			return 0;
		}
		return a > b ? 1 : -1;
	}

	public static int CompareSingle(float a, float b)
	{
		if (a == b)
		{
			return 0;
		}
		return a > b ? 1 : -1;
	}

	public static int CompareString(string a, string b)
	{
		return a.CompareTo(b);
	}

	public static string __GetString(Table table, int vtableOffset, int start)
	{
		int o = table.__offset(vtableOffset);
		return table.__string(table.__vector(o) + start * 4);
	}

	public static string[] __GetStringArgs(Table table, int vtableOffset, int start)
	{
		string[] args = null;
		int offset = table.__offset(vtableOffset);
		int length = table.__vector_len(offset);
		if (length > start)
		{
			int o = table.__vector(offset);
			args = new string[length - start];
			for (int i = start; i < length; i++)
			{
				args[i - start] = table.__string(o + i * 4);
			}
		}
		return args;
	}

	static StringBuilder _builder = new StringBuilder();
	public static void __BuildString(ref string str, Table table, int offset)
	{
		if(str != null)
		{
			return;
		}

		string key = DataItemBase.__GetString(table, offset, 0);
		if(string.IsNullOrEmpty(key) == false)
		{
			__BuildString(ref str, key, DataItemBase.__GetStringArgs(table, offset, 1));
		}
		else
		{
			str = "";
		}
	}

	public static void __BuildString(ref string str, string key, string[] args)
	{
		if(str == null)
		{
			if(key.StartsWith("__"))
			{
				//str = Localization.Get(key);
				if(args != null && args.Length > 0 && string.IsNullOrEmpty(str) == false)
				{
					_builder.Remove(0, _builder.Length);

					bool rebuild = false;
					unsafe
					{
						fixed(char* ptr = str)
						{
							int chunklen = 0;
							int length = str.Length;
							for(int index = 0; index < length; index ++)
							{
								char c = *(ptr + index);
								if (c == '}')
								{
									if(index >= 2 && *(ptr + index - 2) == '{')
									{
										int offset = *(ptr + index - 1) - 'A';
										if(offset >= 0 && offset < args.Length)
										{
											rebuild = true;
											_builder.Append(str, index - chunklen, chunklen - 2);
											if(string.IsNullOrEmpty(args[offset]) == false)
											{
												_builder.Append(args[offset]);
											}
											chunklen = 0;
											continue;
										}
									}
								}
								chunklen++;
							}

							if (rebuild)
							{
								if (chunklen > 0)
								{
									_builder.Append(str, length - chunklen, chunklen);
								}

								str = _builder.ToString();
							}
						}
					}					
				}
			}
			else
			{
				str = key;
			}
		}
	}

	public static int BinarySearch<T, TV>(List<T> list, System.Func<TV, TV, int> comparison, System.Func<T, TV> get_value, TV value) where T : DataItemBase
	{
		int result = -1;
		if(list != null && list.Count > 0)
		{
			int low = 0;
			int high = list.Count - 1;
			if(comparison(get_value(list[low]), value) > 0)
			{
				return -1;
			}
			else if(high > low && (comparison(get_value(list[high]), value) < 0))
			{
				return -1;
			}

			while (low <= high)
			{
				int middle = (low + high) / 2;
				int status = comparison(get_value(list[middle]), value);
				if (status == 0)
				{
					result = middle;
					break;
				}
				else if (status < 0)
				{
					low = middle + 1;
				}
				else
				{
					high = middle - 1;
				}
			}
		}

		while (result > 0 && comparison(get_value(list[result - 1]), value) == 0)
		{
			result -= 1;
		}
		return result;
	}

	public static T FindMax<T>(List<T> list, System.Func<T, bool> checker = null) where T : DataItemBase
	{
		if (list != null && list.Count > 0)
		{
			if(checker == null)
			{
				return list[list.Count - 1];
			}

			for(int index = list.Count - 1; index >= 0; index --)
			{
				T item = list[index];
				if(item == null || checker(item) == false)
				{
					continue;
				}
				return item;
			}
		}
		return null;
	}

	public static ByteBuffer Load(string asset)
	{
		return null;
	}

	public static void OnPostLoaded(System.Action action)
	{
		if(action == null)
		{
			return;
		}
		_dispose += action;
	}

	public static void DisposeAll()
	{
		try
		{
			if(_dispose != null)
			{
				_dispose();
			}
		}
		catch(System.Exception e)
		{
			Console.WriteLine(e.ToString());
		}
		finally
		{
			_dispose = null;
		}
	}
}

public class Query<T> : System.IDisposable where T : DataItemBase
{
	int _position;
	List<T> _list;
	System.Func<T, bool> _filter = null;

	public T Value
	{
		get { return _position >= 0 && _position < _list.Count ? _list[_position] : null; }
	}

	public static Query<T> Create(List<T> list, System.Func<T, bool> checker = null)
	{
		Query<T> query = new Query<T>();
		query._list = list;
		query._filter = checker;
		query._position = list.Count;
		return query;
	}

	public bool Step()
	{
		if (_list.Count > 0 && _position >= 0)
		{
			do
			{
				if (_position >= _list.Count)
				{
					_position = 0;
				}
				else
				{
					_position++;
					if (_position >= _list.Count)
					{
						_position = -1;
					}
				}
			}
			while (_position >= 0 && _filter != null && _filter(_list[_position]) == false);
		}
		return _position >= 0;
	}

	public void Dispose()
	{
		_list = null;
	}

	public void Release()
	{
		Dispose();
	}
}

public class Query<T, TV> : System.IDisposable where T : DataItemBase
{
	int _position;
	List<T> _list;
	TV _value = default(TV);
	System.Func<T, bool> _filter = null;
	System.Func<T, TV> _get_value = null;
	System.Func<TV, TV, int> _comparison = null;

	public T Value
	{
		get { return _position >= 0 && _position < _list.Count ? _list[_position] : null; }
	}

	public static Query<T, TV> Create(List<T>list, TV value, System.Func<TV, TV, int> comparison, System.Func<T, TV> get_value, System.Func<T, bool> checker = null)
	{
		Query<T, TV> query = new Query<T, TV>();
		query._list = list;
		query._value = value;
		query._filter = checker;
		query._comparison = comparison;
		query._get_value = get_value;
		query._position = list.Count;
		return query;
	}

	public bool Step()
	{
		if(_list.Count <= 0)
		{
			return false;
		}

		if (_position >= 0)
		{
			do
			{
				if (_position >= _list.Count)
				{
					_position = DataItemBase.BinarySearch<T, TV>(_list, _comparison, _get_value, _value);
				}
				else
				{
					_position++;
					if (_position >= _list.Count)
					{
						_position = -1;
					}
					else
					{
						T item = _list[_position];
						if (_comparison(_get_value(item), _value) != 0)
						{
							_position = -1;
						}
					}
				}
			}
			while (_position >= 0 && _filter != null && _filter(_list[_position]) == false);
		}
		return _position >= 0;
	}

	public void Dispose()
	{
		_list = null;
		_get_value = null;
		_comparison = null;
	}

	public void Release()
	{
		Dispose();
	}
}