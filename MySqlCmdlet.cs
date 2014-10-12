using System;
using System.Data;
using System.Management.Automation;
using MySql.Data.MySqlClient;

namespace InvokeMySqlCmd
{
    [Cmdlet("Invoke", "MySqlcmd")]
    public class MySqlCmdlet : Cmdlet
    {
        public MySqlCmdlet()
        {
            Server = "localhost";
            QueryTimeout = 30;
        }

        [Parameter(Mandatory = true)]
        public string Query { get; set; }

        [Parameter(Mandatory = true)]
        public string Username { get; set; }

        [Parameter(Mandatory = true)]
        public string Password { get; set; }

        [Parameter(Mandatory = false)]
        public int QueryTimeout { get; set; }

        [Parameter(Mandatory = false)]
        public string InputFile { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Scalar { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter NonQuery { get; set; }

        [Parameter(Mandatory = false)]
        public string Database { get; set; }

        [Parameter(Mandatory = false)]
        public string Server { get; set; }

        protected override void BeginProcessing()
        {
            if (!string.IsNullOrEmpty(Query) && !string.IsNullOrEmpty(InputFile))
            {
                throw new ArgumentException("You cannot specify both Query and InputFile.");
            }
            if (string.IsNullOrEmpty(Query) && string.IsNullOrEmpty(InputFile))
            {
                throw new ArgumentException("You must specify a Query or an InputFile containing a valid query to run.");
            }
            WriteDebug("Input validated.");
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            using (var connection = new MySqlConnection(CreateConnectionString()))
            {
                connection.Open();
                WriteDebug("Connection opened.");
                var dataTable = new DataTable();
                var command = GetCommand(connection);

                if (Scalar)
                {
                    WriteDebug("Running scalar query...");
                    var result = command.ExecuteScalar();
                    WriteDebug("Query complete. Retrieved scalar result:" + result.ToString() );
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
    }
}
