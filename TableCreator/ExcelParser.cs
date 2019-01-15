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
	REAL,
	TEXT,
}

public class ExcelParser
{
	string[] _fieldNames = null;
	ExceFieldType[] _fieldTypes = null;

	List<string[]> _contentList = null;

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

	static string GetString(IExcelDataReader excelReader, int index)
	{
		object obj = excelReader.GetValue(index);
		if(obj == null || obj.GetType() == typeof(System.DBNull))
		{
			return string.Empty;
		}
		return obj.ToString();
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
			return dot > 0 ? ExceFieldType.REAL : ExceFieldType.INTEGER;
		}
		return ExceFieldType.TEXT;
	}

	public ExcelParser(string excelPath)
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
		using (FileStream excelFile = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			IExcelDataReader excelReader = extension == ".xls" ? ExcelReaderFactory.CreateBinaryReader(excelFile) : ExcelReaderFactory.CreateOpenXmlReader(excelFile);

			//读标题
			if (excelReader.Read())
			{
				List<int> fieldids = new List<int>();
				List<string> fieldlist = new List<string>();
				for (int i = 0; i < excelReader.FieldCount; i++)
				{
					string fieldname = GetString(excelReader, i).Trim();
					if (string.IsNullOrEmpty(fieldname) == false)
					{
						fieldlist.Add(fieldname);
						fieldids.Add(i);
					}
				}
				_fieldNames = fieldlist.ToArray();
				_fieldCount = _fieldNames.Length;

				//检查数据类型
				int index = 0;
				_rowCount = excelReader.RowCount - 1;
				_contentList = new List<string[]>(_fieldCount);
				_fieldTypes = new ExceFieldType[_fieldCount];
				while (excelReader.Read())
				{
					index++;
					string[] contents = new string[_fieldCount];
					for (int i = 0; i < _fieldCount; i++)
					{
						contents[i] = GetString(excelReader, fieldids[i]);
						ExceFieldType contentType = GetStringType(contents[i]);
						if (_fieldTypes[i] < contentType)
						{
							_fieldTypes[i] = contentType;
						}
					}
					_contentList.Add(contents);
				}
			}
			excelFile.Dispose();
			excelReader.Dispose();
		}
	}

	public string GetFieldName(int index)
	{
		return _fieldNames != null && index >= 0 && index < _fieldNames.Length ? _fieldNames[index] : string.Empty;
	}

	public int GetInt(int row, int field)
	{
		int var = 0;
		int.TryParse(GetString(row, field), out var);
		return var;
	}

	public float GetSingle(int row, int field)
	{
		float var = 0;
		float.TryParse(GetString(row, field), out var);
		return var;
	}

	public string GetString(int row, int field)
	{
		return row >= 0 && row < _contentList.Count && field >= 0 && field < _fieldCount ? _contentList[row][field] : string.Empty;
	}

	public ExceFieldType GetFieldType(int field)
	{
		return _fieldTypes != null && field >= 0 && field < _fieldTypes.Length ? _fieldTypes[field] : ExceFieldType.None;
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
