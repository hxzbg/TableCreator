using System;
using System.Text;
using FlatBuffers;
using System.Collections.Generic;

public class FlatbufferDataStore : IFlatbufferObject
{
	private Table __p;
	public int bb_pos { get { return __p.bb_pos; } set { __p.bb_pos = value; } }
	public Table table { get { return __p; } }
	public ByteBuffer ByteBuffer { get { return __p.bb; } }
	public void __init(int _i, ByteBuffer _bb) { __p.bb_pos = _i; __p.bb = _bb; }
	FlatbufferDataStore __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

	public bool AppendChild(FlatbufferDataStore item, int index, int subid = 0)
	{
		if(this == item)
		{
			throw new System.Exception();
		}
		if(index < 0)
		{
			return false;
		}
		int offset = __p.__offset(4 + subid * 2);
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

	public int[] GetInt32Array()
	{
		int arrayLength = Length;
		int o = __p.__offset(4);
		int v = __p.__vector(o);
		int[] array = new int[arrayLength];
		for(int i = 0; i < arrayLength; i ++)
        {
			array[i] = __p.bb.GetInt(v + i * 4);

		}
		return array;
	}

	public int GetInt32(int index)
	{
		int o = __p.__offset((2 + index) * 2);
		return o != 0 ? __p.bb.GetInt(o + __p.bb_pos) : 0;
	}

	public float GetSingle(int index)
	{
		int o = __p.__offset((2 + index) * 2);
		return o != 0 ? __p.bb.GetFloat(o + __p.bb_pos) : 0;
	}

	public long GetInt64(int index)
	{
		int o = __p.__offset((2 + index) * 2);
		return o != 0 ? __p.bb.GetLong(o + __p.bb_pos) : 0;
	}

	public string GetString(int index)
	{
		int o = __p.__offset((1 + index) * 2);
		return table.__string(table.__vector(o));
	}
}

internal enum DataStoreItemValueType
{
	None = 0,
	Int32,
	Int64,
	Single,
	String,
}

interface DataStoreItemValue
{
	DataStoreItemValueType GetValueType();
	int GetInt32();
	float GetSingle();
	long GetInt64();
	string GetString();
}

class DataStoreItemInt32Value : DataStoreItemValue
{
	int _value = 0;
	public DataStoreItemInt32Value(int v)
	{
		_value = v;
	}

	public DataStoreItemValueType GetValueType() {return DataStoreItemValueType.Int32;}
	public int GetInt32(){return _value;}
	public long GetInt64() { return _value; }
	public float GetSingle() {throw new System.NotImplementedException();}
	public string GetString() {throw new System.NotImplementedException();}
}

class DataStoreItemInt64Value : DataStoreItemValue
{
	long _value = 0;
	public DataStoreItemInt64Value(long v)
	{
		_value = v;
	}

	public DataStoreItemValueType GetValueType() {return DataStoreItemValueType.Int32;}
	public int GetInt32(){throw new System.NotImplementedException();}
	public long GetInt64() {return _value;}
	public float GetSingle() {throw new System.NotImplementedException();}
	public string GetString() {throw new System.NotImplementedException();}
}

class DataStoreItemSingleValue : DataStoreItemValue
{
	float _value = 0;
	public DataStoreItemSingleValue(float v)
	{
		_value = v;
	}

	public DataStoreItemValueType GetValueType() {return DataStoreItemValueType.Single;}
	public int GetInt32(){throw new System.NotImplementedException();}
	public long GetInt64() {throw new System.NotImplementedException();}
	public float GetSingle() {return _value;}
	public string GetString() {throw new System.NotImplementedException();}
}

class DataStoreItemStringValue : DataStoreItemValue
{
	string _value = null;
	public DataStoreItemStringValue(string v)
	{
		_value = v;
	}

	public DataStoreItemValueType GetValueType() {return DataStoreItemValueType.String;}
	public int GetInt32(){throw new System.NotImplementedException();}
	public long GetInt64() {throw new System.NotImplementedException();}
	public float GetSingle() {throw new System.NotImplementedException();}
	public string GetString()
	{
		return _value;
	}
}

public abstract class DataStoreItem
{
	int m_bb_pos = 0;
	int m_position = 0;
	DataStoreSet m_DataStoreSet;
	DataStoreItemValue[] m_values;
	static int _CurrentComparsionField = 0;
	static DataStoreItemValueType _CurrentValueType = DataStoreItemValueType.None;
	static Comparison<DataStoreItem> __Comparsion = delegate (DataStoreItem a, DataStoreItem b)
	{
		int result = 0;
		DataStoreItemValue va = a.GetValue(_CurrentComparsionField);
		DataStoreItemValue vb = b.GetValue(_CurrentComparsionField);
		switch (_CurrentValueType)
		{
			case DataStoreItemValueType.Int32:
				{
					result = DataStoreHelper.__CompareInt32(va.GetInt32(), vb.GetInt32());
				}
				break;

			case DataStoreItemValueType.Int64:
				{
					result = DataStoreHelper.__CompareInt64(va.GetInt64(), vb.GetInt64());
				}
				break;

			case DataStoreItemValueType.Single:
				{
					result = DataStoreHelper.__CompareSingle(va.GetSingle(), vb.GetSingle());
				}
				break;

			case DataStoreItemValueType.String:
				{
					result = DataStoreHelper.__CompareString(va.GetString(), vb.GetString());
				}
				break;

			default:
				throw new System.Exception();
		}
		return result;
	};

	internal static Comparison<DataStoreItem> GetComparsion(int field, DataStoreItemValueType type)
	{
		_CurrentValueType = type;
		_CurrentComparsionField = field;
		return __Comparsion;
	}

	internal DataStoreItemValue GetValue(int index)
	{
		if(m_values == null)
		{
			return null;
		}

		if(m_values[index] == null)
		{
			FlatbufferDataStore buff = m_DataStoreSet.FlatBuffer;
			buff.bb_pos = m_bb_pos;
			switch (m_DataStoreSet.__FieldAttributes[index])
			{
				case (int)DataStoreItemValueType.Int32:
					m_values[index] = new DataStoreItemInt32Value(buff.GetInt32(index));
					break;

				case (int)DataStoreItemValueType.Int64:
					m_values[index] = new DataStoreItemInt64Value(buff.GetInt64(index));
					break;

				case (int)DataStoreItemValueType.Single:
					m_values[index] = new DataStoreItemSingleValue(buff.GetSingle(index));
					break;

				case (int)DataStoreItemValueType.String:
					{
						string v = null;
						DataStoreHelper.__BuildString(ref v, buff.table, 4 + index * 2);
						m_values[index] = new DataStoreItemStringValue(v);
					}
					break;

				default:
					throw new System.Exception();
			}
		}
		return m_values[index];
	}

	public int GetInt32(int position)
	{
		DataStoreItemValue value = GetValue(position);
		return value != null ? value.GetInt32() : 0;
	}

	public float GetSingle(int position)
	{
		DataStoreItemValue value = GetValue(position);
		return value != null ? value.GetSingle() : 0;
	}

	public long GetInt64(int position)
	{
		DataStoreItemValue value = GetValue(position);
		return value != null ? value.GetInt64() : 0;
	}

	public string GetString(int position)
	{
		DataStoreItemValue value = GetValue(position);
		return value != null ? value.GetString() : "";
	}

	public virtual void Dispose()
	{
		if(m_values == null)
		{
			return;
		}
		for(int index = 0; index < m_values.Length; index ++)
		{
			m_values[index] = null;
		}
		m_values = null;
	}

	public void Parse(DataStoreSet dataSet, int position)
	{
		m_position = position;
		m_DataStoreSet = dataSet;
		m_bb_pos = dataSet.FlatBuffer.bb_pos;
		int count = dataSet.__FieldAttributes != null ? dataSet.__FieldAttributes.Length : 0;
		m_values = new DataStoreItemValue[count];
	}
}

public class DataStoreSet
{
	protected Type __TypeForItem = null;
	protected string __AssetPath = null;
	protected ByteBuffer m_buffer = null;

	protected List<int[]> m_mainKeys = null;
	protected List<DataStoreItem> m_list = null;
	protected FlatbufferDataStore m_structList = null;
	protected FlatbufferDataStore m_indexCache = new FlatbufferDataStore();
	protected FlatbufferDataStore m_structItem = new FlatbufferDataStore();

	internal FlatbufferDataStore FlatBuffer
	{
		get
		{
			return m_structItem;
		}
	}

	protected virtual DataStoreItem CreateDataStoreItem()
	{
		return (DataStoreItem)System.Activator.CreateInstance(__TypeForItem);
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

	static System.Action<IntPtr> _FreeFlatBuffer = delegate (IntPtr ptr)
	{
		//AssetBundleParser.free_pointer(ptr);
	};

	static FlatBuffers.ByteBuffer LoadFlatBuffer(string asset)
	{
		int offset = 0;
		string abpath = null;
		FlatBuffers.ByteBuffer byteBuffer = null;
		/*
		string assetpath = string.Format("Assets/Defenders/Resource/db/{0}.bytes", asset);
		if (GTResourceManager.FindAssetBundleFile(assetpath, out abpath, out offset))
		{
			int length = 0;
			IntPtr pointer = new IntPtr(0);
			abpath = System.IO.Path.GetFullPath(abpath);
			if (AssetBundleParser.assetbundle_loadtextasset(asset, abpath, offset, ref pointer, ref length))
			{
				byteBuffer = new FlatBuffers.ByteBuffer(pointer, length, _FreeFlatBuffer);
			}
		}
#if UNITY_EDITOR
		if(byteBuffer == null)
		{
			UnityEngine.TextAsset textasset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(assetpath);
			if (textasset != null)
			{
				byteBuffer = new FlatBuffers.ByteBuffer(textasset.bytes);
			}
		}

#endif
		*/
		return byteBuffer;
	}

	protected static System.Action m_disposeAll = null;
	public static System.Func<string, ByteBuffer> ByteBufferLoader = LoadFlatBuffer;

	protected virtual bool __Init()
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

		m_buffer = ByteBufferLoader(__AssetPath);
		if (m_buffer == null)
		{
			m_list = new List<DataStoreItem>();
			return false;
		}

		m_structItem = new FlatbufferDataStore();
		m_structList = FlatbufferDataStore.CreateFlatbufferDataStore(m_buffer);
		m_list = new List<DataStoreItem>(m_structList.Length);
		if(!m_structList.AppendChild(m_indexCache, 0, 1))
        {
			throw new System.Exception();
        }
		int indexSize = m_indexCache.Length;
		m_mainKeys = new List<int[]>(indexSize);
		for (int index = 0; index < indexSize; index++)
		{
			m_mainKeys.Add(null);
		}

		for (int index = 0; index < m_structList.Length; index++)
		{
			DataStoreItem item = CreateDataStoreItem();
			m_structList.AppendChild(m_structItem, index);
			item.Parse(this, index);
			m_list.Add(item);
		}
		m_disposeAll += __Disposed;
		return true;
	}

	static FlatbufferDataStore m_indexsArray = new FlatbufferDataStore();
	protected virtual void __BuildKeyByField(int field)
    {
		__Init();
		if (m_list != null && m_mainKeys[field] == null)
		{
			if(m_indexCache.AppendChild(m_indexsArray, field, 0))
			{
				m_mainKeys[field] = m_indexsArray.GetInt32Array();
			}
		}
	}

    protected int __GetLength()
    {
		__Init();
		return m_list != null ? m_list.Count : 0;
    }

    protected DataStoreItem __GetItem(int index)
    {
		__Init();
		return index >= 0 && index < m_list.Count ? m_list[index] : null;
    }

	protected DataStoreItem __FindMax(int field, System.Func<DataStoreItem, bool> checker = null)
	{
		__BuildKeyByField(field);
		int[] keys = m_mainKeys[field];
        if(keys == null || keys.Length <= 0)
        {
			return null;
        }

		int count = keys.Length;
		if (checker == null)
		{
			return m_list[keys[count - 1]];
		}

		for (int index = count - 1; index >= 0; index--)
		{
			DataStoreItem item = m_list[keys[index]];
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

	protected virtual QueryDataStoreInt32 __SearchInt32(int field, int value, System.Func<DataStoreItem, bool> filter = null)
	{
		__BuildKeyByField(field);
		return (QueryDataStoreInt32)QueryDataStoreInt32.Create(m_list, m_mainKeys[field], field, value, filter);
	}

	protected virtual QueryDataStoreInt64 __SearchInt64(int field, long value, System.Func<DataStoreItem, bool> filter = null)
	{
		__BuildKeyByField(field);
		return (QueryDataStoreInt64)QueryDataStoreInt64.Create(m_list, m_mainKeys[field], field, value, filter);
	}

	protected virtual QueryDataStoreSingle __SearchSingle(int field, float value, System.Func<DataStoreItem, bool> filter = null)
	{
		__BuildKeyByField(field);
		return (QueryDataStoreSingle)QueryDataStoreSingle.Create(m_list, m_mainKeys[field], field, value, filter);
	}

	protected void __Disposed()
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
			m_mainKeys.Clear();
			m_mainKeys = null;
		}
	}

    public static void DisposeAll()
    {
        if(m_disposeAll != null)
        {
			m_disposeAll();
        }
    }
}

public class DataStoreHelper
{
	static string FindStringByKey(string key)
	{
		//return Localization.Get(key);
		return "";
	}
	
	public static System.Func<string, string> __FindStringByKey = FindStringByKey;

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

	readonly public static System.Func<int, int, int> __CompareInt32 = delegate (int a, int b)
	{
		if (a == b)
		{
			return 0;
		}
		return a > b ? 1 : -1;
	};

	readonly public static System.Func<long, long, int> __CompareInt64 = delegate (long a, long b)
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

	public static int BinarySearch<TV>(List<DataStoreItem> list, int[] keys, System.Func<TV, TV, int> comparison, System.Func<DataStoreItem, TV> get_value, TV value)
	{
		int result = -1;
		if (list != null && list.Count > 0 && keys != null && keys.Length > 0)
		{
			int low = 0;
			int high = list.Count - 1;
			if (comparison(get_value(list[keys[low]]), value) > 0)
			{
				return -1;
			}
			else if (high > low && (comparison(get_value(list[keys[high]]), value) < 0))
			{
				return -1;
			}

			while (low <= high)
			{
				int middle = (low + high) / 2;
				int status = comparison(get_value(list[keys[middle]]), value);
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

		while (result > 0 && comparison(get_value(list[keys[result - 1]]), value) == 0)
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

	DataStoreItem GetValue(int position)
	{
		return position >= 0 && position < _list.Count ? _list[position] : null; 
	}

	public DataStoreItem Value
	{
		get
		{
			return GetValue(_position);
		}
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
			while (_position >= 0 && _filter != null && _filter(Value) == false);
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
	protected int[] _keys;
	protected int _position;
	protected List<DataStoreItem> _list;
	protected TV _value = default(TV);
	protected System.Func<DataStoreItem, bool> _filter = null;
	protected System.Func<DataStoreItem, TV> _get_value = null;
	protected System.Func<TV, TV, int> _comparison = null;

	DataStoreItem GetValue(int position)
	{
		return position >= 0 && position < _list.Count ? _list[_keys[position]] : null;
	}

	public DataStoreItem Value
	{
		get
		{
			return GetValue(_position);
		}
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
					_position = DataStoreHelper.BinarySearch<TV>(_list, _keys, _comparison, _get_value, _value);
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
						DataStoreItem item = Value;
						if (_comparison(_get_value(item), _value) != 0)
						{
							_position = -1;
						}
					}
				}
			}
			while (_position >= 0 && _filter != null && _filter(Value) == false);
		}
		return _position >= 0;
	}

	public void Dispose()
	{
		_list = null;
		_keys = null;
		_get_value = null;
		_comparison = null;
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

public class QueryDataStoreInt32 : QueryDataStore<int>
{
	int _field = 0;
	int get_value(DataStoreItem item)
	{
		return item.GetValue(_field).GetInt32();
	}

	public static QueryDataStoreInt32 Create(List<DataStoreItem> list, int[] keys, int field, int value, System.Func<DataStoreItem, bool> checker = null)
	{
		QueryDataStoreInt32 query = new QueryDataStoreInt32();
		query._field = field;
		query._list = list;
		query._keys = keys;
		query._value = value;
		query._filter = checker;
		query._comparison = DataStoreHelper.__CompareInt32;
		query._get_value = query.get_value;
		query._position = list.Count;
		return query;
	}

	public void Reset(int value)
	{
		_value = value;
		_position = _list.Count;
	}
}

public class QueryDataStoreInt64 : QueryDataStore<long>
{
	int _field = 0;
	long get_value(DataStoreItem item)
	{
		return item.GetValue(_field).GetInt64();
	}

	public static QueryDataStoreInt64 Create(List<DataStoreItem> list, int[] keys, int field, long value, System.Func<DataStoreItem, bool> checker = null)
	{
		QueryDataStoreInt64 query = new QueryDataStoreInt64();
		query._field = field;
		query._list = list;
		query._keys = keys;
		query._value = value;
		query._filter = checker;
		query._comparison = DataStoreHelper.__CompareInt64;
		query._get_value = query.get_value;
		query._position = list.Count;
		return query;
	}

	public void Reset(int value)
	{
		_value = value;
		_position = _list.Count;
	}
}

public class QueryDataStoreSingle : QueryDataStore<float>
{
	int _field = 0;
	float get_value(DataStoreItem item)
	{
		return item.GetValue(_field).GetSingle();
	}

	public static QueryDataStoreSingle Create(List<DataStoreItem> list, int[] keys, int field, float value, System.Func<DataStoreItem, bool> checker = null)
	{
		QueryDataStoreSingle query = new QueryDataStoreSingle();
		query._list = list;
		query._keys = keys;
		query._value = value;
		query._filter = checker;
		query._comparison = DataStoreHelper.__CompareSingle;
		query._get_value = query.get_value;
		query._position = list.Count;
		return query;
	}

	public void Reset(int value)
	{
		_value = value;
		_position = _list.Count;
	}
}