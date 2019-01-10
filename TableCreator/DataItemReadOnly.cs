using System;
using System.IO;
using FlatBuffers;
using System.Collections.Generic;

public class DataItemReadOnly
{
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

	public static int BinarySearch<T, TV>(List<T> list, System.Func<TV, TV, int> comparison, System.Func<T, TV> get_value, TV value) where T : DataItemReadOnly
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
}