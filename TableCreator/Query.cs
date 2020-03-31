using System;
using System.Collections.Generic;

#if UNITY_EDITOR
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
		if (parser != null && _parserList.Contains(parser) == false)
		{
			_parserList.Add(parser);
		}
	}
}
#endif

public class Query<T> : System.IDisposable where T : DataStoreItem
{
	QueryDataStore _query = null;

	public T Value
	{
		get { return _query != null ? _query.Value as T : null; }
	}

	public static Query<T> Create(QueryDataStore src)
	{
		Query<T> query = new Query<T>();
		query._query = src;
		return query;
	}

	public bool Step()
	{
		return _query != null && _query.Step();
	}

	public void Dispose()
	{
		if(_query != null)
        {
			_query.Dispose();
			_query = null;
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
			return _query != null ? _query.ValueCount : 0;
		}
	}
}

public class Query<T, TV> : System.IDisposable where T : DataStoreItem
{
	QueryDataStore<TV> _query = null;

	public T Value
	{
		get { return _query != null ? _query.Value as T : null; }
	}

	public static Query<T, TV> Create(QueryDataStore<TV> src)
	{
		Query<T, TV> query = new Query<T, TV>();
		query._query = src;
		return query;
	}

	public bool Step()
	{
		return _query != null && _query.Step();
	}

	public void Dispose()
	{
		if (_query != null)
		{
			_query.Dispose();
			_query = null;
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
			return _query != null ? _query.ValueCount : 0;
		}
	}
}

public class QueryInt<T> : Query<T, int> where T : DataStoreItem
{

}

public class QueryLong<T> : Query<T, long> where T : DataStoreItem
{

}

public class QuerySingle<T> : Query<T, float> where T : DataStoreItem
{

}