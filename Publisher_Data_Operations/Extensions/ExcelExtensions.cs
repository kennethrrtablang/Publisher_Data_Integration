using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Aspose.Cells;

namespace Publisher_Data_Operations.Extensions
{
    public static class ExcelExtensions
    {
        public static bool IsOptionalWorksheet(this Excel.Worksheet ws)
        {
            for (int i = 0; i < ws.Comments.Count; i++)
            {
                if (ws.Comments[i].Note.IndexOf("Optional", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }
    }
}
