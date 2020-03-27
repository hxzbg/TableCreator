using System;
using System.Text;
using FlatBuffers;
using System.Collections.Generic;

public class FlatbufferDataStore : IFlatbufferObject
{
	private Table __p;
	public Table table { get { return __p; } }
	public ByteBuffer ByteBuffer { get { return __p.bb; } }
	public void __init(int _i, ByteBuffer _bb) { __p.bb_pos = _i; __p.bb = _bb; }
	FlatbufferDataStore __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

	public bool AppendChild(FlatbufferDataStore item, int index)
	{
		if(this == item)
		{
			throw new System.Exception();
		}
		if(index < 0 || index >= Length)
		{
			return false;
		}
		int offset = __p.__offset(4);
		if(offset == 0)
		{
			return false;
		}
		item.__assign(__p.__indirect(__p.__vector(offset) + index * 4), __p.bb);
		return true;
	}

	public static FlatbufferDataStore CreateFlatbufferDataStore(ByteBuffer _bb) { return CreateFlatbufferDataStore(_bb, new FlatbufferDataStore()); }
	public static FlatbufferDataStore CreateFlatbufferDataStore(ByteBuffer _bb, FlatbufferDataStore obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }

	public int Length
	{
		get
		{
			int o = __p.__offset(4);
			return o != 0 ? __p.__vector_len(o) : 0;
		}
	}

	public int GetIntValue(int index)
	{
		int o = __p.__offset((1 + index) * 2);
		return o != 0 ? __p.bb.GetInt(o + __p.bb_pos) : 0;
	}

	public float GetFloatValue(int index)
	{
		int o = __p.__offset((1 + index) * 2);
		return o != 0 ? __p.bb.GetFloat(o + __p.bb_pos) : 0;
	}

	public long GetLongValue(int index)
	{
		int o = __p.__offset((1 + index) * 2);
		return o != 0 ? __p.bb.GetLong(o + __p.bb_pos) : 0;
	}

	public string GetStringValue(int index)
	{
		int o = __p.__offset((1 + index) * 2);
		return table.__string(table.__vector(o));
	}
}

public abstract class DataStoreItem
{
	public abstract void Parse(int position, FlatbufferDataStore item);

	public abstract void Dispose();
}

public class DataStoreSet
{
	protected int m_fieldsCount = 0;
	protected string m_assetPath = null;
	protected ByteBuffer m_buffer = null;
	FlatbufferDataStore m_structItem = null;
	FlatbufferDataStore m_structList = null;
	protected List<DataStoreItem> m_list = null;
	protected List<DataStoreItem>[] m_mainKeys = null;
	protected System.Func<DataStoreItem> m_creator = null;
	protected Comparison<DataStoreItem>[] m_comparison = null;
    public Comparison<DataStoreItem>[] __Comparsions
    {
        get
        {
			return m_comparison;
        }
        set
        {
			m_comparison = value;
        }
    }

	protected string[] m_fieldNames = null;
    public string[] __FieldNames
    {
        get
        {
			return m_fieldNames;
        }
        set
        {
			m_fieldNames = value;
        }
    }

	/*:1,int
     * 2,long
     * 3,float
     * 4,string
    */
	protected int[] m_fieldAttributes = null;
    public int[] __FieldAttributes
    {
        get
        {
			return m_fieldAttributes;
        }
        set
        {
			m_fieldAttributes = value;
        }
    }
	static System.Func<string, ByteBuffer> ByteBufferLoader = null;

	protected bool __Init()
	{
		if (m_list != null)
		{
			return false;
		}

		if (m_buffer != null)
		{
			m_buffer.Dispose();
			m_buffer = null;
		}

		m_buffer = ByteBufferLoader(m_assetPath);
		if (m_buffer == null)
		{
			m_list = new List<DataStoreItem>();
			return false;
		}

		m_structItem = new FlatbufferDataStore();
		m_mainKeys = new List<DataStoreItem>[m_fieldsCount];
		m_structList = FlatbufferDataStore.CreateFlatbufferDataStore(m_buffer);
		m_list = new List<DataStoreItem>(m_structList.Length);
		for (int index = 0; index < m_structList.Length; index++)
		{
			m_structList.AppendChild(m_structItem, index);
			DataStoreItem item = m_creator();
			item.Parse(index, m_structItem);
			m_list.Add(item);
		}
		return true;
		//DataStoreItem.OnPostLoaded(SqliteStat1.Dispose);
	}

    protected void __BuildString(ref string str, DataStoreItem item, int offset)
	{
        if(str != null || item == null)
        {
			return;
        }
		m_structList.AppendChild(m_structItem, m_list.IndexOf(item));
		DataStoreHelper.__BuildString(ref str, m_structItem.table, offset);
	}

    protected void __BuildKeyByField(int field)
    {
		__Init();
		if (m_list != null && m_mainKeys[field] == null)
		{
			List<DataStoreItem> list = new List<DataStoreItem>(m_list.Count);
			list.AddRange(m_list);
			list.Sort(__Comparsions[field]);
			m_mainKeys[field] = list;
		}
	}

    protected int __GetLength()
    {
		__Init();
		return m_list != null ? m_list.Count : 0;
    }

	protected DataStoreItem __FindMax(int field, System.Func<DataStoreItem, bool> checker = null)
	{
		__BuildKeyByField(field);
		List<DataStoreItem> list = m_mainKeys[field];
        if(list == null || list.Count <= 0)
        {
			return null;
        }

		if (checker == null)
		{
			return list[list.Count - 1];
		}

		for (int index = list.Count - 1; index >= 0; index--)
		{
			DataStoreItem item = list[index];
			if (item == null || checker(item) == false)
			{
				continue;
			}
			return item;
		}
		return null;
	}

	protected QueryDataStore __Search(System.Func<DataStoreItem, bool> filter = null)
	{
		__Init();
		return QueryDataStore.Create(m_list, filter);
	}

	protected QueryDataStore<TV> __Search<TV>(int field, System.Func<TV, TV, int> comparison, System.Func<DataStoreItem, TV> get_value, TV value, System.Func<DataStoreItem, bool> filter = null)
	{
		__BuildKeyByField(field);
		return QueryDataStore<TV>.Create(m_mainKeys[field], value, comparison, get_value, filter);
	}

	protected void Disposed()
    {
		if (m_buffer != null)
		{
			m_buffer.Dispose();
			m_buffer = null;
		}

		if (m_list != null)
		{
			for (int index = 0; index < m_list.Count; index++)
			{
				DataStoreItem item = m_list[index];
				item.Dispose();
			}
			m_list = null;
		}

		if (m_mainKeys != null)
		{
			for (int index = 0; index < m_mainKeys.Length; index++)
			{
				List<DataStoreItem> list = m_mainKeys[index];
				if (list != null)
				{
					list.Clear();
				}
				m_mainKeys[index] = null;
			}
			m_mainKeys = null;
		}
	}
}

public class DataStoreHelper
{
	public static System.Action<IntPtr> __FreePointer = null;
	public static System.Func<string, string> __FindStringByKey = null;

	public static List<DataStoreItem> ParseDatas(ByteBuffer buffer, System.Func<DataStoreItem> creator, System.Action<DataStoreItem> poster, System.Func<ByteBuffer, List<DataStoreItem>> parser)
	{
		if(buffer == null)
		{
			return null;
		}
		FlatbufferDataStore root = FlatbufferDataStore.CreateFlatbufferDataStore(buffer);

		int itemCount = root.Length;
		List<DataStoreItem> list = new List<DataStoreItem>(itemCount);
		FlatbufferDataStore sub = new FlatbufferDataStore();
		for(int index = 0; index < itemCount; index ++)
		{
			root.AppendChild(sub, index);

		}

		return list;
	}

	public static void Free(IntPtr pointer)
	{
		__FreePointer(pointer);
	}

	readonly public static System.Func<int, int, int> __CompareInt = delegate (int a, int b)
	{
		if (a == b)
		{
			return 0;
		}
		return a > b ? 1 : -1;
	};

	readonly public static System.Func<long, long, int> __CompareLong = delegate (long a, long b)
	{
		if (a == b)
		{
			return 0;
		}
		return a > b ? 1 : -1;
	};

	readonly public static System.Func<float, float, int> __CompareSingle = delegate (float a, float b)
	{
		if (a == b)
		{
			return 0;
		}
		return a > b ? 1 : -1;
	};

	readonly public static System.Func<string, string, int> __CompareString = delegate (string a, string b)
	{
		return a.CompareTo(b);
	};

	public static System.Func<string, ByteBuffer> LoadByteBuffer = null;

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
		if (str != null)
		{
			return;
		}

		string key = __GetString(table, offset, 0);
		if (string.IsNullOrEmpty(key) == false)
		{
			__BuildString(ref str, key, __GetStringArgs(table, offset, 1));
		}
		else
		{
			str = "";
		}
	}

	public static void __BuildString(ref string str, string key, string[] args)
	{
		if (str == null)
		{
			if (key.StartsWith("__"))
			{
				str = __FindStringByKey(key);
				if (args != null && args.Length > 0 && string.IsNullOrEmpty(str) == false)
				{
					_builder.Remove(0, _builder.Length);

					bool rebuild = false;
					unsafe
					{
						fixed (char* ptr = str)
						{
							int chunklen = 0;
							int length = str.Length;
							for (int index = 0; index < length; index++)
							{
								char c = *(ptr + index);
								if (c == '}')
								{
									if (index >= 2 && *(ptr + index - 2) == '{')
									{
										int offset = 0;
										c = *(ptr + index - 1);
										if (c <= 'Z')
										{
											offset = c - 'A';
										}
										else if (c <= 'z')
										{
											offset = c - 'a' + 26;
										}
										if (offset >= 0 && offset < args.Length)
										{
											rebuild = true;
											_builder.Append(str, index - chunklen, chunklen - 2);
											if (string.IsNullOrEmpty(args[offset]) == false)
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

	public static int BinarySearch<TV>(List<DataStoreItem> list, System.Func<TV, TV, int> comparison, System.Func<DataStoreItem, TV> get_value, TV value)
	{
		int result = -1;
		if (list != null && list.Count > 0)
		{
			int low = 0;
			int high = list.Count - 1;
			if (comparison(get_value(list[low]), value) > 0)
			{
				return -1;
			}
			else if (high > low && (comparison(get_value(list[high]), value) < 0))
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
}

public class QueryDataStore: System.IDisposable
{
	int _position;
	List<DataStoreItem> _list;
	System.Func<DataStoreItem, bool> _filter = null;
	LinkedListNode<QueryDataStore> _node = null;
	static LinkedList<QueryDataStore> _inactiveList = new LinkedList<QueryDataStore>();

	public DataStoreItem Value
	{
		get { return _position >= 0 && _position < _list.Count ? _list[_position] : null; }
	}

	public static QueryDataStore Create(List<DataStoreItem> list, System.Func<DataStoreItem, bool> checker = null)
	{
		QueryDataStore query = null;
		if (_inactiveList.Count > 0)
		{
			query = _inactiveList.Last.Value;
			_inactiveList.Remove(query._node);
		}
		else
		{
			query = new QueryDataStore();
			query._node = new LinkedListNode<QueryDataStore>(query);
		}

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
		if (_node.List == null)
		{
			_inactiveList.AddFirst(_node);
		}
	}

	public void Release()
	{
		Dispose();
	}

	public int ValueCount
	{
		get
		{
			int count = 0;
			int pos = _position;
			_position = _list.Count;
			while (Step())
			{
				count++;
			}
			_position = pos;
			return count;
		}
	}
}

public class QueryDataStore<TV> : System.IDisposable
{
	int _position;
	List<DataStoreItem> _list;
	TV _value = default(TV);
	System.Func<DataStoreItem, bool> _filter = null;
	System.Func<DataStoreItem, TV> _get_value = null;
	System.Func<TV, TV, int> _comparison = null;
	LinkedListNode<QueryDataStore<TV>> _node = null;
	static LinkedList<QueryDataStore<TV>> _inactiveList = new LinkedList<QueryDataStore<TV>>();

	public DataStoreItem Value
	{
		get { return _position >= 0 && _position < _list.Count ? _list[_position] : null; }
	}

	public static QueryDataStore<TV> Create(List<DataStoreItem> list, TV value, System.Func<TV, TV, int> comparison, System.Func<DataStoreItem, TV> get_value, System.Func<DataStoreItem, bool> checker = null)
	{
		QueryDataStore<TV> query = null;
		if (_inactiveList.Count > 0)
		{
			query = _inactiveList.Last.Value;
			_inactiveList.Remove(query._node);
		}
		else
		{
			query = new QueryDataStore<TV>();
			query._node = new LinkedListNode<QueryDataStore<TV>>(query);
		}

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
		if (_list == null || _list.Count <= 0)
		{
			return false;
		}

		if (_position >= 0)
		{
			do
			{
				if (_position >= _list.Count)
				{
					_position = DataStoreHelper.BinarySearch<TV>(_list, _comparison, _get_value, _value);
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
						DataStoreItem item = _list[_position];
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
		if (_node.List == null)
		{
			_inactiveList.AddFirst(_node);
		}
	}

	public void Release()
	{
		Dispose();
	}

	public int ValueCount
	{
		get
		{
			int count = 0;
			int pos = _position;
			_position = _list.Count;
			while (Step())
			{
				count++;
			}
			_position = pos;
			return count;
		}
	}
}