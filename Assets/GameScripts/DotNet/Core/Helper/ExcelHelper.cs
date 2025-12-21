#if TENGINE_NET
using ClosedXML.Excel;

namespace TEngine.Helper;

public static class ExcelHelper
{
    public static XLWorkbook LoadExcel(string name)
    {
        return new XLWorkbook(name);
    }
    
    public static string GetCellValue(this IXLWorksheet sheet, int row, int column)
    {
        var cell = sheet.Cell(row, column);
            
        try
        {
            if (cell.Value.IsBlank)
            {
                return "";
            }

            string s = cell.GetString();
                
            return s.Trim();
        }
        catch (Exception e)
        {
            throw new Exception($"Rows {row} Columns {column} Content {cell.Value} {e}");
        }
    }
}
#endif