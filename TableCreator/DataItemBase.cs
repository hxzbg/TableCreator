using System;
using System.IO;
using FlatBuffers;
using System.Collections.Generic;

public enum IteratorStatus
{
	CONTINUE = 0,
	BREAK,
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

	public static int BinarySearch<T, TV>(List<T> list, System.Func<TV, TV, int> comparison, System.Func<T, TV> get_value, TV value) where T : DataItemBase
	{
		int result = -1;
		if(list != null && list.Count > 0)
		{
			int low = 0;
			int high = list.Count - 1;
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

		if(result > 0)
		{
			while(comparison(get_value(list[result - 1]), value) == 0)
			{
				result -= 1;
			}
		}

		return result;
	}

	public static ByteBuffer Load(string path)
	{
		return null;
	}

	public static void OnPostLoaded(System.Action action)
	{
		if (action == null)
		{
			return;
		}
		_dispose += action;
	}

	public static void Dispose()
	{
		try
		{
			if (_dispose != null)
			{
				_dispose();
			}
		}
		catch (System.Exception e)
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

	public T Value
	{
		get { return _list != null && _position >= 0 && _position < _list.Count ? _list[_position] : null; }
	}

	public static Query<T> Create(List<T> list)
	{
		Query<T> query = new Query<T>();
		query._list = list;
		query._position = list.Count;
		return query;
	}

	public bool Step()
	{
		if (_list != null && _list.Count > 0 && _position >= 0)
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
		return _position >= 0;
	}

	public void Dispose()
	{
		_list = null;
	}
}

public class Query<T, TV> : System.IDisposable where T : DataItemBase
{
	int _position;
	List<T> _list;
	TV _value = default(TV);
	System.Func<T, TV> _get_value = null;
	System.Func<TV, TV, int> _comparison = null;

	public T Value
	{
		get { return _list != null && _position >= 0 && _position < _list.Count ? _list[_position] : null; }
	}

	public static Query<T, TV> Create(List<T> list, TV value, System.Func<TV, TV, int> comparison, System.Func<T, TV> get_value)
	{
		Query<T, TV> query = new Query<T, TV>();
		query._list = list;
		query._value = value;
		query._comparison = comparison;
		query._get_value = get_value;
		query._position = list.Count;
		return query;
	}

	public bool Step()
	{
		if (_list != null && _list.Count > 0 && _position >= 0)
		{
			if (_position >= _list.Count)
			{
				_position = DataItemBase.BinarySearch<T, TV>(_list, _comparison, _get_value, _value);
				if (_position >= 0)
				{
					for (int index = _position; index < _list.Count; index++)
					{
						T item = _list[index];
						if (_comparison(_get_value(item), _value) != 0)
						{
							break;
						}
						else
						{
							_position = index;
						}
					}
				}
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
		return _position >= 0;
	}

	public void Dispose()
	{
		_list = null;
		_get_value = null;
		_comparison = null;
	}
}