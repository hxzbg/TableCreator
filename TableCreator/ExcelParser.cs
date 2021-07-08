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
    DOUBLE,
    TEXT,
    Count
}

public class ExcelHeaderItem
{
    public int fieldid;
    public string fieldname;
    public int[] indexs;
    public ExceFieldType fieldtype;
}

public class ExcelParser
{
    static Dictionary<string, ExceFieldType> m_typeNames = new Dictionary<string, ExceFieldType> {
        {"string", ExceFieldType.TEXT},
        {"double", ExceFieldType.DOUBLE },
        { "integer", ExceFieldType.LONG},
    };

    public static ExceFieldType ParseFieldType(string typename)
    {
        ExceFieldType result = ExceFieldType.None;
        if(m_typeNames.TryGetValue(typename, out result) == false)
        {
            Console.WriteLine("解析类型失败:{0}",typename);
        }
        return result;
    }

    protected List<string[]> _contentList = new List<string[]>();

    protected ExcelHeaderItem[] _excelheader;
    public ExcelHeaderItem[] ExcelHeader
    {
        get
        {
            return _excelheader;
        }
    }

    protected string _filename = null;
    public string FileName
    {
        get
        {
            return _filename;
        }
    }

    public int FieldCount
    {
        get
        {
            return _excelheader != null ? _excelheader.Length : 0;
        }
    }

    ExcelHeaderItem GetHeaderItem(int index)
    {
        return _excelheader != null && index >= 0 && index < _excelheader.Length ? _excelheader[index] : null;
    }

    public string GetFieldName(int field)
    {
        ExcelHeaderItem header = GetHeaderItem(field);
        return header != null ? header.fieldname : string.Empty;
    }

    public ExceFieldType GetFieldType(int field)
    {
        ExcelHeaderItem header = GetHeaderItem(field);
        return header != null ? header.fieldtype : ExceFieldType.None;
    }

    public void SetFiledIndexs(int field, int[] indexs)
    {
        ExcelHeaderItem header = GetHeaderItem(field);
        if(header != null)
        {
            header.indexs = indexs;
        }
    }

    public int[] GetFieldIndexs(int field)
    {
        ExcelHeaderItem header = GetHeaderItem(field);
        return header != null ? header.indexs : null;
    }

    public int RowCount
    {
        get
        {
            return _contentList != null ? _contentList.Count : 0;
        }
    }

    public string GetString(int row, int field)
    {
        return row >= 0 && row < _contentList.Count && field >= 0 && field < FieldCount ? _contentList[row][field] : string.Empty;
    }

    public long GetLong(int row, int field)
    {
        long var = 0;
        string str = GetString(row, field);
        if (string.IsNullOrEmpty(str) == false && long.TryParse(str, out var) == false)
        {
            Console.WriteLine(string.Format("{0}.{1}：{2}, 转换为整数失败", row + 1, GetFieldName(field), str));
        }
        return var;
    }

    public float GetSingle(int row, int field)
    {
        float var = 0;
        string str = GetString(row, field);
        if (string.IsNullOrEmpty(str) == false && float.TryParse(str, out var) == false)
        {
            Console.WriteLine(string.Format("{0}.{1}：{2}, 转换为浮点数失败", row + 1, GetFieldName(field), str));
        }
        return var;
    }

    public double GetDouble(int row, int field)
    {
        double var = 0;
        string str = GetString(row, field);
        if (string.IsNullOrEmpty(str) == false && double.TryParse(str, out var) == false)
        {
            Console.WriteLine(string.Format("{0}.{1}：{2}, 转换为双精度数失败", row + 1, GetFieldName(field), str));
        }
        return var;
    }

    public static ExcelParser Create(string path)
    {
        ExcelParser parser = null;
        string extension = Path.GetExtension(path).ToLower();
        switch(extension)
        {
            case ".xls":
            case ".xlsx":
                parser = new XlsParser(path);
                break;

            case ".txt":
            case ".csv":
                parser = new CSVParser(path);
                break;
        }
        return parser;
    }

    public void Dispose()
    {
        if (_contentList != null)
        {
            _contentList.Clear();
            _contentList = null;
        }
    }
}
