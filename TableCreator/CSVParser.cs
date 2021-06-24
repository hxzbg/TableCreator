﻿using System;
using System.IO;
using ExcelDataReader;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class CSVParser : ExcelParser
{
    public CSVParser(string excelPath)
    {
        string extension = Path.GetExtension(excelPath);
        if (string.IsNullOrEmpty(extension) == false)
        {
            extension = extension.ToLower();
        }

        if (extension != ".txt" && extension != ".vis")
        {
            return;
        }

        _filename = Path.GetFileNameWithoutExtension(excelPath);
        ByteReader reader = ByteReader.Open(excelPath);
		if(reader != null)
        {
            List<string> header = new List<string>();
            reader.ReadCSV(header);

			List<string> types = new List<string>();
			reader.ReadCSV(types);

			int index = 0;
            List<ExcelHeaderItem> excelheaderlist = new List<ExcelHeaderItem>();
			for (int i = 0; i < header.Count && i < types.Count; i++)
            {
                string fieldName = header[i].Trim();
                if (string.IsNullOrEmpty(fieldName))
                {
                    continue;
                }

				ExceFieldType fieldType = ParseFieldType(types[i]);
				if (fieldType != ExceFieldType.None)
                {
                    ExcelHeaderItem fielditem = new ExcelHeaderItem();
                    fielditem.fieldid = index++;
                    fielditem.fieldname = fieldName;
                    fielditem.fieldtype = fieldType;
					excelheaderlist.Add(fielditem);
				}
			}
			_excelheader = excelheaderlist.ToArray();

			List<string> comment = new List<string>();
			reader.ReadCSV(comment);

			List<string> item = new List<string>();
			while (true)
            {
                if(reader.ReadCSV(item) == false)
                {
					break;
				}
				_contentList.Add(item.ToArray());
            }
			reader.Dispose();
		}
    }
}
