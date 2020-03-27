using System;
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