using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Publisher_Data_Operations.Helper;


namespace Publisher_Data_Operations.Extensions
{
    public class DBConnection
    {
        //internal SqlConnection sqlCon = null;
        //internal const string metaData = "res://*/Entities.PDIDatabaseFirst.csdl|res://*/Entities.PDIDatabaseFirst.ssdl|res://*/Entities.PDIDatabaseFirst.msl"; //The EF Metadata required for connections
        public SqlTransaction Transaction { get; set; }
        public string LastError { get; private set; }
        private Object ConObject = null;


        private string Database = null;
        private string Server = null;
        private string User = null;
        private string Password = null;

        private static int MaxRetryCount = 5;
        private static int RetryWaitMS = 5000;

        public string GetServer { 
            get {
                if (Server is null)
                    return null;
                if (Server.Contains("tcp:"))
                    return Server.Substring(0, Server.IndexOf('.')).Replace("tcp:", "");
                return Server;
            }   
        }
        public string GetDatabase { get => Database; }

        public DBConnection(object connectionObject)
        {
            ConObject = connectionObject;
            SetupConnection();
        }

        public static string TestSQLConnectionString(string sqlCon, string sql)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(sqlCon);
                using (SqlConnection dbCon = new SqlConnection(builder.ConnectionString))
                {
                    dbCon.Open();
                    SqlCommand cmd = new SqlCommand(sql, dbCon); // $"pdi_File_Receipt_Log Contains {cmd.ExecuteScalar()} rows";
                    cmd.ExecuteScalar();
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
            return null;
        }

        private SqlConnection SetupConnection()
        {
            SqlConnection sqlCon = new SqlConnection();
            //if (sqlCon is null || sqlCon.ConnectionString is null || sqlCon.ConnectionString.Equals(string.Empty))
            //{
            if (ConObject is null)
                return sqlCon;
            if (ConObject.GetType() == typeof(string))
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConObject.ToString());
                builder.PersistSecurityInfo = false; // the connection will fail for EF if this parameter is false (default) - may not be necessary as the password won't pass from SSIS if this isn't already true
                builder.ConnectTimeout = 300;
                builder.ConnectRetryCount = 20;
                builder.ConnectRetryInterval = 15;

                Server = builder.DataSource;
                Database = builder.InitialCatalog;
                User = builder.UserID;
                Password = builder.Password;

                sqlCon = new SqlConnection(builder.ConnectionString);
            }
            else if (ConObject.GetType() == typeof(SqlConnection))
                sqlCon = (SqlConnection)ConObject;
            else if (ConObject.GetType() == typeof(DBConnection))
                sqlCon = ((DBConnection)ConObject).GetSqlConnection();
            else
                throw new ArgumentException("Unknown connection object provided to DBConnection.");

            return sqlCon;
        }
       
       public SqlConnection ChangeDB(string dbName)
        {
            if (Server != null && Server.Length > 0 && dbName != null && dbName.Length > 0)
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
                builder.DataSource = Server;
                builder.InitialCatalog = dbName;
                builder.UserID = User;
                builder.Password = Password;
                return new SqlConnection(builder.ToString());
            }
            return null;
        }

        //will return open sql connection
        public SqlConnection GetSqlConnection(string dbName = null)
        {
            int retryCount = 1;
            SqlConnection sqlCon;
            if (dbName is null)
                sqlCon = SetupConnection();
            else
                sqlCon = ChangeDB(dbName);

            if (sqlCon is null)
            {
                Logger.AddError(null, $"Connection for Database was unable to be initialized - check connection string");
                return null;
            } 
               

            while (sqlCon.State != ConnectionState.Open && retryCount <= MaxRetryCount)
            {
                if (sqlCon.State != ConnectionState.Open) 
                {
                    try
                    {
                        sqlCon.Open();
                    }
                    catch (SqlException sqlE)
                    {
                        Logger.AddError(null, $"SQL Exception error on Open retry {retryCount}: {sqlE.Message}");
                        System.Threading.Thread.Sleep(RetryWaitMS * retryCount); // increase the delay for each retry - most likely resource overload
                        retryCount++;
                    }
                    catch (Exception e)
                    {
                        Logger.AddError(null, $"Exception opening SQL connection on retry {retryCount}: {e.Message}");
                        if (e.HResult == -2146233079)
                            retryCount = MaxRetryCount + 1;
                        else
                        {
                            System.Threading.Thread.Sleep(RetryWaitMS * retryCount); // increase the delay for each retry - most likely resource overload
                            retryCount++;
                        }
                       
                    }
                }
            }

            if (sqlCon.State == ConnectionState.Open) 
                return sqlCon;
            else
            {
                Logger.AddError(null, $"Connection for {Database} in unexpected state {sqlCon.State}");
                return null;
            }
                

        }

        public ConnectionState Close()
        {
            SqlConnection sqlCon;
            if (ConObject.GetType() == typeof(SqlConnection))
            {
                sqlCon = (SqlConnection)ConObject;
                if (sqlCon.State == ConnectionState.Open)
                    sqlCon.Close();

                return sqlCon.State;
            }
            return ConnectionState.Broken; 
        }

        public void DisposeConnection()
        {
            if (ConObject.GetType() == typeof(SqlConnection))
                ((SqlConnection)ConObject).Dispose();
        }
            
        //private void StateChangeHandler(object mySender, StateChangeEventArgs myEvent)
        //{
        //    Console.WriteLine($"mySqlConnection State has changed from {myEvent.OriginalState} to {myEvent.CurrentState}");
        //    conState = myEvent.CurrentState;
        //}

        public bool LoadDataTable(string sql, Dictionary<string, object> parameters, DataTable dt, bool isStoredProcedure = false, string dbName = null)
        {
            int retryCount = 1;
            while (retryCount < MaxRetryCount) // if we encounter SQL errors - retry up to MaxCount - TODO: Limit the SQL errors to retry?
            {
                using (SqlConnection localCon = GetSqlConnection(dbName))
                {
                    using (SqlCommand cmd = new SqlCommand(sql, localCon))
                    {
                        if (parameters != null && parameters.Count > 0)
                            foreach (KeyValuePair<string, object> kvp in parameters)
                                cmd.Parameters.AddWithNullableValue(kvp.Key, kvp.Value);

                        if (isStoredProcedure)
                            cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Transaction = Transaction;
                        try
                        {
                            dt.Load(cmd.ExecuteReader());
                            return true;
                        }
                        catch (SqlException sqlE)
                        {
                            LastError = "Execute SQL Error: " + sqlE.Message;
                            Logger.AddError(null, LastError);
                            System.Threading.Thread.Sleep(RetryWaitMS * retryCount); // increase the delay for each retry - most likely resource overload
                            retryCount++;
                        }
                        catch (Exception e)
                        {
                            LastError = $"Could not load data for table {dt.TableName}  Error: {e.Message}";
                            Logger.AddError(null, LastError);
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        public bool ExecuteNonQuery(string sql, out int rows, Dictionary<string, object> parameters = null, int cmdTimeout = -1, bool isStoredProc = false, bool closeCon = true)
        {
            
            int retryCount = 1;
            rows = -1;
            while (retryCount < MaxRetryCount) // if we encounter SQL errors - retry up to MaxCount - TODO: Limit the SQL errors to retry?
            {
                SqlConnection localCon = GetSqlConnection();
                
                using (SqlCommand cmd = new SqlCommand(sql, localCon))
                {
                    if (parameters != null && parameters.Count > 0)
                        foreach (KeyValuePair<string, object> kvp in parameters)
                            cmd.Parameters.AddWithValue(kvp.Key, kvp.Value);

                    if (cmdTimeout >= 0)
                        cmd.CommandTimeout = cmdTimeout;

                    if (isStoredProc)
                        cmd.CommandType = CommandType.StoredProcedure;

                    try
                    {
                        cmd.Transaction = Transaction;
                        rows = cmd.ExecuteNonQuery();
                        LastError = null;
                        return true;
                    }
                    catch (SqlException sqlE)
                    {
                        LastError = "Execute SQL Error: " + sqlE.Message;
                        System.Threading.Thread.Sleep(RetryWaitMS * retryCount); // increase the delay for each retry - most likely resource overload
                        retryCount++;
                    }
                    catch (Exception e)
                    {
                        LastError = "Error: " + e.Message;
                        return false;
                    }
                    finally
                    {
                        if (closeCon)
                            localCon.Dispose();
                    }
                }
            }
            return false;
        }

        public object ExecuteScalar(string sql, Dictionary<string, object> parameters = null, int cmdTimeout = -1, bool isStoredProc = false)
        {
            int retryCount = 1;
            while (retryCount < MaxRetryCount) // if we encounter SQL errors - retry up to MaxCount - TODO: Limit the SQL errors to retry?
            {

                using (SqlConnection localCon = GetSqlConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(sql, localCon))
                    {
                        if (parameters != null && parameters.Count > 0)
                            foreach (KeyValuePair<string, object> kvp in parameters)
                                cmd.Parameters.AddWithNullableValue(kvp.Key, kvp.Value);

                        if (cmdTimeout >= 0)
                            cmd.CommandTimeout = cmdTimeout;

                        if (isStoredProc)
                            cmd.CommandType = CommandType.StoredProcedure;

                        try
                        {
                            cmd.Transaction = Transaction;
                            return cmd.ExecuteScalar();
                        }
                        catch (SqlException sqlE)
                        {
                            LastError = "Execute SQL Error: " + sqlE.Message;
                            System.Threading.Thread.Sleep(RetryWaitMS * retryCount); // increase the delay for each retry - most likely resource overload TODO: switch to Microsoft's TransientFaultHandling
                            retryCount++;
                        }
                        catch (Exception e)
                        {
                            LastError = "Execute SQL Error: " + e.Message;
                            return false;
                        }

                    }
                }
            }
            return false;
        }

        public bool UpdateDataTable(string sql, Dictionary<string, object> parameters, DataTable dtUpdate)
        {
            int retryCount = 1;
            while (retryCount < MaxRetryCount) // if we encounter SQL errors - retry up to MaxCount - TODO: Limit the SQL errors to retry?
            {
                using (SqlConnection localCon = GetSqlConnection())
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(sql, localCon))
                    {

                        if (parameters != null && parameters.Count > 0)
                            foreach (KeyValuePair<string, object> kvp in parameters)
                                da.SelectCommand.Parameters.AddWithValue(kvp.Key, kvp.Value);

                        da.SelectCommand.Transaction = Transaction;
                        da.UpdateBatchSize = 0;
                        SqlCommandBuilder scb = new SqlCommandBuilder(da);
                        da.InsertCommand = scb.GetInsertCommand(true);
                        da.UpdateCommand = scb.GetUpdateCommand(true);
                        da.DeleteCommand = scb.GetDeleteCommand(true);
                        try
                        {
                            da.Update(dtUpdate);
                            dtUpdate.AcceptChanges();
                            return true;
                        }
                        catch (SqlException sqlE)
                        {
                            LastError = "Execute SQL Error: " + sqlE.Message;
                            System.Threading.Thread.Sleep(RetryWaitMS * retryCount); // increase the delay for each retry - most likely resource overload
                            retryCount++;
                        }
                        catch (Exception e)
                        {
                            LastError = $"DataAdapter Update error on table {dtUpdate.TableName}: {e.Message}";
                            return false;
                        }
                    }
                } 
            }
            return false;
        }

        public bool BulkCopy(string destTable, DataTable dt, bool addedOnly = false, bool closeCon = true)
        {
            if (dt is null)
                return false;

            int retryCount = 1;
            while (retryCount < MaxRetryCount) // if we encounter SQL errors - retry up to MaxCount - TODO: Limit the SQL errors to retry?
            {
                SqlConnection localCon = GetSqlConnection();

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(localCon))
                {
                    bulkCopy.DestinationTableName = destTable;
                    bulkCopy.AutoMapColumns(dt);
                    try
                    {
                        // Write from the source to the destination.
                        if (addedOnly)
                            bulkCopy.WriteToServer(dt, DataRowState.Added); // only bulk copy the newly added rows
                        else
                            bulkCopy.WriteToServer(dt);

                        dt.AcceptChanges(); // accept all the changes in case we use the table more than once
                        if (closeCon && localCon != null)
                            localCon.Dispose();

                        return true;
                    }
                    catch (SqlException sqlE)
                    {
                        LastError = "BulkCopy SQL Error: " + sqlE.Message;
                        System.Threading.Thread.Sleep(RetryWaitMS * retryCount); // increase the delay for each retry - most likely resource overload
                        retryCount++;
                    }
                    catch (Exception ex)
                    {
                        LastError = $"{dt.TableName} BulkCopy Failed - Error: {ex.Message}";
                        System.Threading.Thread.Sleep(RetryWaitMS * retryCount); // increase the delay for each retry - most likely resource overload
                        retryCount++;
                    }
                    finally
                    {
                        if (closeCon)
                            localCon.Dispose();
                    }
                }
                if (closeCon && localCon != null)
                    localCon.Dispose();
            }

            return false;
        }

        /// <summary>
        /// Convert a regular SQL connection string to one usable by Entity Framework
        /// </summary>
        /// <param name="meta">The meta data required for EF (optional)</param>
        /// <returns></returns>
        //public string GetEntityConnectionString(string meta = metaData)
        //{
        //    //The Generic.connection is a regular SQL connection and needs to be modified to work with Entity Framework

        //    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(sqlCon.ConnectionString)
        //    {
        //        MultipleActiveResultSets = true //Entity Framework is using multiple active connection sets - make sure it's set to true on the connection
        //    };
        //    EntityConnectionStringBuilder entityBuilder = new EntityConnectionStringBuilder
        //    {
        //        Metadata = meta,
        //        Provider = "System.Data.SqlClient",
        //        ProviderConnectionString = builder.ToString()
        //    };
        //    return entityBuilder.ToString();
        //}
        /*
        private static void ExecuteSqlTransaction(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = connection.CreateCommand();
                SqlTransaction transaction;

                // Start a local transaction.
                transaction = connection.BeginTransaction("SampleTransaction");

                // Must assign both transaction object and connection
                // to Command object for a pending local transaction
                command.Connection = connection;
                command.Transaction = transaction;

                try
                {
                    command.CommandText =
                        "Insert into Region (RegionID, RegionDescription) VALUES (100, 'Description')";
                    command.ExecuteNonQuery();
                    command.CommandText =
                        "Insert into Region (RegionID, RegionDescription) VALUES (101, 'Description')";
                    command.ExecuteNonQuery();

                    // Attempt to commit the transaction.
                    transaction.Commit();
                    Console.WriteLine("Both records are written to database.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Commit Exception Type: {0}", ex.GetType());
                    Console.WriteLine("  Message: {0}", ex.Message);

                    // Attempt to roll back the transaction.
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception ex2)
                    {
                        // This catch block will handle any errors that may have occurred
                        // on the server that would cause the rollback to fail, such as
                        // a closed connection.
                        Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
                        Console.WriteLine("  Message: {0}", ex2.Message);
                    }
                }
            }
        }*/

    }

}
