using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class LuaCodeBuilder
{
    ExcelParser _parser = null;

    public LuaCodeBuilder(ExcelParser parser)
	{
        _parser = parser;
    }

    public string Build(string output)
    {
        StringBuilder file = new StringBuilder();
        file.Append(@"local fields =
{
");
        int fieldCount = _parser.FieldCount;
        int lastfieldid = fieldCount - 1;
        for(int i = 0; i < fieldCount; i ++)
        {
            file.AppendFormat("\t[\"{0}\"]={1}", _parser.GetFieldName(i), i + 1);
            file.Append(i == lastfieldid ? "\n" : ",\n");
        }
        ExcelHeaderItem[] headers = _parser.ExcelHeader;
        file.Append("}\n\n");

        file.Append(@"local contents =
{
");
        int lastrowid = _parser.RowCount - 1;
        for (int i = 0; i < _parser.RowCount; i ++)
        {
            file.AppendFormat("\t[{0}]=", i + 1);
            file.Append("{");
            for(int j = 0; j < fieldCount; j ++)
            {
                string v = _parser.GetString(i, j);
                if(_parser.GetFieldType(j) == ExceFieldType.TEXT)
                {
                    file.AppendFormat("\"{0}\"", v);
                }
                else
                {
                    if(string.IsNullOrEmpty(v))
                    {
                        v = "0";
                    }
                    file.Append(v);
                }

                if(j < lastfieldid)
                {
                    file.Append(",");
                }
            }
            file.Append(i == lastrowid ? "}\n" : "},\n");
        }
        file.Append("}\n\n");

        file.Append(@"local indexs =
{
");
        for (int i = 0; i < fieldCount; i++)
        {
            file.AppendFormat("\t[\"{0}\"]=", _parser.GetFieldName(i), i + 1);
            file.Append("{");
            int[] indexs = _parser.GetFieldIndexs(i);
            if(indexs != null)
            {
                int indexsLength = indexs.Length;
                int lastindex = indexsLength - 1;
                for(int j = 0; j < indexsLength; j ++)
                {
                    file.AppendFormat(j < lastindex ? "{0}," : "{0}", indexs[j]);
                }
            }
            file.Append(i == lastfieldid ? "}\n" : "},\n");
        }
        file.Append("}\n\n");

        file.Append(@"local inited = {}
local exceldata = {__fields=fields,__indexs=indexs}
local child_metatable = 
{
	__index=function(input,key)
		local fieldid = fields[key]
		if fieldid == nil then
			return nil
		end
		return input[fieldid]
	end,
	__newindex=function()
		assert(false)
	end}

setmetatable(exceldata,
{
	__index=function(input, key)
		local child = contents[key]
		if child ~= nil and inited[key] ~= true then
			setmetatable(child, child_metatable)
			inited[key] = true
		end
		return child
	end,
	__newindex=function() assert(false) end}
)

return exceldata");

        if (string.IsNullOrEmpty(output) == false && Directory.Exists(output) == false)
        {
            Directory.CreateDirectory(output);
        }
        output = Path.Combine(output, _parser.FileName + ".bytes");
        File.WriteAllText(output, file.ToString());
        return output;
    }
}