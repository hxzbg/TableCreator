using System;
using System.IO;
using System.Text;
using ExcelDataReader;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public enum ExceFieldType
{
	None = 0,
	INTEGER,
	LONG,
	REAL,
	TEXT,
	Count
}

public class ExcelHeaderItem
{
	public int fieldid;
	public string fieldname;
	public ExceFieldType fieldtype;
}

public class ExcelParser
{
	string[] _fieldNames = null;
	int[] _fieldTypeStatus = null;

	List<string[]> _contentList = null;

	ExcelHeaderItem[] _excelheader;
	public ExcelHeaderItem[] ExcelHeader
	{
		get
		{
			return _excelheader;
		}
	}

	string _filename = null;
	public string FileName
	{
		get
		{
			return _filename;
		}
	}

	int _fieldCount = 0;
	public int FieldCount
	{
		get
		{
			return _fieldCount;
		}
	}

	int _rowCount = 0;
	public int RowCount
	{
		get
		{
			return _rowCount;
		}
	}

	void PushFieldType(int field, ExceFieldType type)
	{
		int index = field * (int)ExceFieldType.Count + (int)type;
		_fieldTypeStatus[index] = _fieldTypeStatus[index] + 1;
	}

	public static Dictionary<string, ExcelHeaderItem[]> ParseExcelHeaderFromFbs(string fbspath)
	{
		//从文本读取并解析
		Dictionary<string, ExcelHeaderItem[]> dict = new Dictionary<string, ExcelHeaderItem[]>();
		if (string.IsNullOrEmpty(fbspath) == false && File.Exists(fbspath))
		{
			string[] lines = File.ReadAllLines(fbspath);
			for(int i = 0; i < lines.Length; i ++)
			{
				string[] parts = lines[i].Split(',');
				if(parts == null || parts.Length < 3)
				{
					continue;
				}

				string name = parts[0];
				int fieldCount = (parts.Length - 1) / 2;
				ExcelHeaderItem[] items = new ExcelHeaderItem[fieldCount];
				for (int j = 0; j < fieldCount; j ++)
				{
					ExcelHeaderItem item = new ExcelHeaderItem();
					item.fieldname = parts[j * 2 + 1];
					item.fieldid = j;
					switch(parts[j * 2 + 2])
					{
						case "INTEGER":
							item.fieldtype = ExceFieldType.INTEGER;
							break;

						case "LONG":
							item.fieldtype = ExceFieldType.LONG;
							break;

						case "REAL":
							item.fieldtype = ExceFieldType.REAL;
							break;

						default:
							item.fieldtype = ExceFieldType.TEXT;
							break;
					}
					items[j] = item;
				}
				dict[name] = items;
			}
		}
		return dict;
	}

	public static void SaveExcelHeaderToFbs(string fbspath, Dictionary<string, ExcelHeaderItem[]> dict)
	{
		StringBuilder path = new StringBuilder();
		foreach (var item in dict)
		{
			path.Append(item.Key);
			ExcelHeaderItem[] items = item.Value;
			for(int i = 0; i < items.Length; i ++)
			{
				path.Append(",");
				ExcelHeaderItem temp = items[i];
				path.Append(temp.fieldname);
				path.Append(",");
				path.Append(temp.fieldtype);
			}
			path.AppendLine();
		}
		File.WriteAllText(fbspath, path.ToString());
	}

	static string GetString(IExcelDataReader excelReader, int index)
	{
		if(index >= 0)
		{
			object obj = excelReader.GetValue(index);
			if (obj == null || obj.GetType() == typeof(System.DBNull))
			{
				return string.Empty;
			}
			return obj.ToString();
		}
		return "";
	}

	//适用于特定场合，仅简单检查特征，未对数据有效性作校验。
	public static unsafe ExceFieldType GetStringType(string value)
	{
		if(string.IsNullOrEmpty(value))
		{
			return ExceFieldType.None;
		}

		int digit = -1;	//首次出现数字的位置;
		int zero = -1;  //首次出现0的位置
		int dot = -1;   //点的位置
		int index = 0;

		value = value.Trim();
		int length = value.Length;
		fixed(char* ptr = value)
		{
			char c = '\0';
			while((c = *(ptr + index)) != 0)
			{
				switch(c)
				{
					case '0':
						{
							if(zero < 0)
							{
								zero = index;
							}

							if (digit < 0)
							{
								digit = index;
							}
						}
						break;

					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
						{
							if(digit < 0)
							{
								digit = index;
							}
						}
						break;

					case '-':
						{
							if(index != 0)
							{
								return ExceFieldType.TEXT;
							}
						}
						break;

					case '.':
						{
							if(index == 0 || dot > 0)
							{
								return ExceFieldType.TEXT;
							}
							dot = index;
						}
						break;

					default:
						return ExceFieldType.TEXT;
				}
				index++;
			}
		}

		if(digit >= 0 && index >= length)
		{
			if(dot == 0 || dot == (index - 1) || digit < 0)	//第一位是小数点，最后一位是小数点，没有找到数字
			{
				return ExceFieldType.TEXT;
			}

			if(dot > 0)
			{
				return ExceFieldType.REAL;
			}

			long l = 0;
			long.TryParse(value, out l);
			if(l >= int.MaxValue)
			{
				return ExceFieldType.LONG;
			}
			return ExceFieldType.INTEGER;
		}
		return ExceFieldType.TEXT;
	}

	public ExcelParser(string excelPath, Dictionary<string, ExcelHeaderItem[]> headersdict, FileAccess access = FileAccess.Read)
	{
		string extension = Path.GetExtension(excelPath);
		if (string.IsNullOrEmpty(extension) == false)
		{
			extension = extension.ToLower();
		}

		if(extension != ".xls" && extension != ".xlsx")
		{
			return;
		}

		_filename = Path.GetFileNameWithoutExtension(excelPath);
		using (FileStream excelFile = File.Open(excelPath, FileMode.Open, access, FileShare.Read))
		{
			if (headersdict != null)
			{
				headersdict.TryGetValue(_filename, out _excelheader);
			}

			IExcelDataReader excelReader = extension == ".xls" ? ExcelReaderFactory.CreateBinaryReader(excelFile) : ExcelReaderFactory.CreateOpenXmlReader(excelFile);

			//读标题
			if (excelReader.Read())
			{
				List<int> fieldids = new List<int>();
				List<string> fieldlist = new List<string>();
				for (int i = 0; i < excelReader.FieldCount; i++)
				{
					string fieldname = GetString(excelReader, i).Trim();
					fieldlist.Add(fieldname);
					fieldids.Add(i);
				}

				if(_excelheader == null)
				{
					for(int i = fieldlist.Count - 1; i >= 0; i --)
					{
						if(string.IsNullOrEmpty(fieldlist[i]))
						{
							fieldlist.RemoveAt(i);
							fieldids.RemoveAt(i);
						}
					}

					_fieldNames = fieldlist.ToArray();
					_fieldCount = _fieldNames.Length;
					_excelheader = new ExcelHeaderItem[_fieldCount];
					for (int i = 0; i < _fieldCount; i++)
					{
						ExcelHeaderItem item = new ExcelHeaderItem();
						item.fieldid = i;
						item.fieldname = _fieldNames[i];
						item.fieldtype = ExceFieldType.None;
						_excelheader[i] = item;
					}
				}
				else
				{
					List<int> list2 = new List<int>();
					List<string> list1 = new List<string>();
					for (int i = 0; i < _excelheader.Length; i ++)
					{
						ExcelHeaderItem item = _excelheader[i];
						int pos = fieldlist.IndexOf(item.fieldname);
						list1.Add(item.fieldname);
						list2.Add(pos);
						if (pos < 0)
						{
							Console.WriteLine(string.Format("没有找到字段 : {0}", item.fieldname));
						}
					}

					fieldids = list2;
					fieldlist = list1;
					_fieldNames = fieldlist.ToArray();
					_fieldCount = _fieldNames.Length;
				}

				//检查数据类型
				int index = 0;
				_rowCount = excelReader.RowCount - 1;
				_contentList = new List<string[]>(_fieldCount);
				_fieldTypeStatus = new int[_fieldCount * (int)ExceFieldType.Count];
				while (excelReader.Read())
				{
					index++;
					string[] contents = new string[_fieldCount];
					for (int i = 0; i < _fieldCount; i++)
					{
						contents[i] = GetString(excelReader, fieldids[i]);
						if(_excelheader[i].fieldtype == ExceFieldType.None)
						{
							PushFieldType(i, GetStringType(contents[i]));
						}
						else
						{
							PushFieldType(i, _excelheader[i].fieldtype);
						}
					}
					_contentList.Add(contents);
				}
			}
			excelFile.Dispose();
			excelReader.Dispose();

			for (int i = 0; i < FieldCount; i++)
			{
				_excelheader[i].fieldtype = GetFieldType(i);
			}

			if (headersdict != null)
			{
				headersdict[_filename] = _excelheader;
			}
		}
	}

	public string GetFieldName(int index)
	{
		return _fieldNames != null && index >= 0 && index < _fieldNames.Length ? _fieldNames[index] : string.Empty;
	}

	public long GetLong(int row, int field)
	{
		long var = 0;
		string str = GetString(row, field);
		if(string.IsNullOrEmpty(str) == false && long.TryParse(str, out var) == false)
		{
			Console.WriteLine(string.Format("{0}.{1}：{2}, 转换为整数失败", row, field, str));
		}
		return var;
	}

	public float GetSingle(int row, int field)
	{
		float var = 0;
		string str = GetString(row, field);
		if (string.IsNullOrEmpty(str) == false && float.TryParse(str, out var) == false)
		{
			Console.WriteLine(string.Format("{0}.{1}：{2}, 转换为浮点数失败", row, field, str));
		}
		return var;
	}

	public string GetString(int row, int field)
	{
		return row >= 0 && row < _contentList.Count && field >= 0 && field < _fieldCount ? _contentList[row][field] : string.Empty;
	}

	public ExceFieldType GetFieldType(int field)
	{
		int start = field * (int)ExceFieldType.Count;
		ExceFieldType fieldType = ExceFieldType.TEXT;
		for(int index = (int)ExceFieldType.Count - 1; index >= 1; index --)
		{
			if(_fieldTypeStatus[start + index] > 0)
			{
				fieldType = (ExceFieldType)index;
				break;
			}
		}
		return fieldType;
	}

	public void Dispose()
	{
		if(_contentList != null)
		{
			_contentList.Clear();
			_contentList = null;
		}
	}
}
