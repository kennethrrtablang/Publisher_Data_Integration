using System;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Publisher_Data_Operations.Extensions
{
    /// <summary>
    /// Simplify SqlClient calls
    /// </summary>
    public static class SqlExtensions
    {

        //https://stackoverflow.com/questions/13451085/exception-when-addwithvalue-parameter-is-null
        public static SqlParameter AddWithNullableValue(this SqlParameterCollection collection, string parameterName, object value)
        {
            if (value is null)
                return collection.AddWithValue(parameterName, DBNull.Value);
            else
                return collection.AddWithValue(parameterName, value);
        }

        /// <summary>
        /// Map provided columns in the DataTable to the same columns in the bulk copy - case sensitive
        /// </summary>
        /// <param name="sbc"></param>
        /// <param name="dt"></param>
        public static void AutoMapColumns(this SqlBulkCopy sbc, DataTable dt)
        {
            foreach (DataColumn column in dt.Columns)
                sbc.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }
    }
}
