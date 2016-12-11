using System;
using System.Data;
using System.Management.Automation;
using System.Transactions;
using MySql.Data.MySqlClient;

namespace InvokeMySqlCmd
{
    [Cmdlet("Invoke", "MySqlcmd")]
    public class MySqlCmdlet : PSCmdlet // derive from PSCmdlet to gain access to CurrentPSTransaction property
    {
        public MySqlCmdlet()
        {
            Server = "localhost";
            QueryTimeout = 30;
        }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string Query { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Username { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Password { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public int QueryTimeout { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public string InputFile { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter Scalar { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public SwitchParameter NonQuery { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public string Database { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public string Server { get; set; }
        
        [Parameter(Mandatory = false)]
        public SwitchParameter UseTransaction { get; set; }

        protected override void ProcessRecord()
        {
            //TODO: replace this with parameter sets and attribute validation.
            if (!string.IsNullOrEmpty(Query) && !string.IsNullOrEmpty(InputFile))
            {
                throw new ArgumentException("You cannot specify both Query and InputFile.");
            }
            if (string.IsNullOrEmpty(Query) && string.IsNullOrEmpty(InputFile))
            {
                throw new ArgumentException("You must specify a Query or an InputFile containing a valid query to run.");
            }
            WriteDebug("Input validated.");

            // we need to scope the use of the PS Transaction, if one is supplied and 
            //  the user wants the cmdlet to participate in the existing transaction
            IDisposable txnScope = new NullDisposible();
            if (UseTransaction.IsPresent && null != CurrentPSTransaction)
            {
                txnScope = CurrentPSTransaction;
            }

            using (txnScope)
            {
                using (var connection = new MySqlConnection(CreateConnectionString()))
                {
                    connection.Open();

                    if (null != CurrentPSTransaction)
                    {
                        // enlist the current transaction 
                        //  note that a user can create a new cross-cmdlet transaction scope
                        //  using start-transaction
                        //  in order to allow the DB provider to properly manage the transactional
                        //  resources, we need to enlist THAT transaction, which is available in 
                        //  Transaction.Current
                        connection.EnlistTransaction(Transaction.Current);
                    }

                    WriteDebug("Connection opened.");
                    var dataTable = new DataTable();
                    var command = GetCommand(connection);

                    if (Scalar)
                    {
                        WriteDebug("Running scalar query...");
                        var result = command.ExecuteScalar();
                        WriteDebug("Query complete. Retrieved scalar result:" + result.ToString());
                        WriteObject(result);
                    }
                    else if (NonQuery)
                    {
                        WriteDebug("Running NonQuery query...");
                        var result = command.ExecuteNonQuery();
                        WriteDebug("NonQuery query complete. " + result + " rows affected.");
                        WriteObject(result);
                    }
                    else
                    {
                        WriteDebug("Running query....");
                        using (var adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                            WriteDebug("Query complete. Returned " + dataTable.Rows.Count + " rows.");
                            var resultSet = GetDataRowArrayFromTable(dataTable);
                            WriteObject(resultSet);
                        }
                    }
                }
                base.ProcessRecord();
            }
        }

        private DataRow[] GetDataRowArrayFromTable(DataTable dataTable)
        {
            var resultSet = new DataRow[dataTable.Rows.Count];
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                resultSet[i] = dataTable.Rows[i];
            }
            return resultSet;
        }

        private string CreateConnectionString()
        {
            var formatString = "Data Source={0};Initial Catalog={1};User ID={2};Password={3}";
            var connectionString = string.Format(formatString, Server, Database, Username, Password);
            WriteDebug("Using the following connection string: " + connectionString);
            return connectionString;
        }

        private MySqlCommand GetCommand(MySqlConnection connection)
        {
            var sql = Query;
            if (!string.IsNullOrEmpty(InputFile))
            {
                WriteDebug("Using query in file " + InputFile);
                sql = new System.IO.StreamReader(InputFile).ReadToEnd();
            }
            var command = new MySqlCommand(sql, connection);
            command.CommandTimeout = QueryTimeout;

            return command;
        }

        class NullDisposible : IDisposable
        {
            public void Dispose()
            {                
            }
        }
    }
}
