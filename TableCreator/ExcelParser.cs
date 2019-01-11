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

	static bool IsMatch(string input, string pattern)
	{
		Match match = Regex.Match(input, pattern);
		return match != null && match.Success && match.Length == input.Length;
	}

	static ExceFieldType GetStringType(string value)
	{
		if(string.IsNullOrEmpty(value))
		{
			return ExceFieldType.None;
		}
		else if(value == "0")
		{
			return ExceFieldType.INTEGER;
		}

		if (IsMatch(value, "[0-9]\\d*[.][0-9]*$"))
		{
			return ExceFieldType.REAL;
		}
		else if (IsMatch(value, "[1-9]\\d*$"))
		{
			return ExceFieldType.INTEGER;
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
