using Microsoft.Data.SqlClient;
using System.Data;

namespace Smart
{
    static class DB
    {
        private static string ConnectionString = @"Data Source=Localhost;Initial Catalog=Smart;Integrated Security=True;TrustServerCertificate=True";

        #region Выбор
        public static List<object[]>? SelectAll(string TableName)
        {
            // Подключение к БД
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                // Проверка подключения
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                    if (connection.State != ConnectionState.Open)
                        throw new Exception("Не удаётся подключиться к БД");
                }

                // Создание запроса
                string zapros = $"SELECT * FROM {TableName}";
                using (SqlCommand command = new SqlCommand(zapros, connection))
                {
                    // Обработка запроса
                    SqlDataReader reader = command.ExecuteReader();
                    List<object[]>? res = new List<object[]>();
                    object[] columns;
                    while (reader.Read())
                    {
                        columns = new object[reader.FieldCount];
                        reader.GetValues(columns);
                        res.Add(columns);
                    }

                    // Вывод результата
                    connection.Close();
                    return res;
                }
            }
        }
        public static List<object[]>? SelectWhere(string TableName, string KeyName, string KeyValue)
        {
            // Подключение к БД
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                // Проверка подключения
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                    if (connection.State != ConnectionState.Open)
                        throw new Exception("Не удаётся подключиться к БД");
                }

                // Создание запроса
                string zapros = $"SELECT * FROM \"{TableName}\" WHERE \"{KeyName}\" = '{KeyValue}'";
                using (SqlCommand command = new SqlCommand(zapros, connection))
                {
                    // Обработка запроса
                    SqlDataReader reader = command.ExecuteReader();
                    List<object[]>? res = new List<object[]>();
                    object[] columns;
                    while (reader.Read())
                    {
                        columns = new object[reader.FieldCount];
                        reader.GetValues(columns);
                        res.Add(columns);
                    }

                    // Вывод результата
                    connection.Close();
                    return res;
                }
            }
        }
        public static List<object[]>? SelectContains(string TableName, string KeyName, string KeyValue)
        {
            // Подключение к БД
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                // Проверка подключения
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                    if (connection.State != ConnectionState.Open)
                        throw new Exception("Не удаётся подключиться к БД");
                }

                // Создание запроса
                string zapros = $"SELECT * FROM \"{TableName}\" WHERE CONTAINS(\"{KeyName}\", '{KeyValue}')";

                // Обработка запроса
                using (SqlCommand command = new SqlCommand(zapros, connection))
                {
                    SqlDataReader reader = command.ExecuteReader();
                    List<object[]>? res = new List<object[]>();
                    object[] columns;
                    while (reader.Read())
                    {
                        columns = new object[reader.FieldCount];
                        reader.GetValues(columns);
                        res.Add(columns);
                    }

                    // Вывод результата
                    connection.Close();
                    return res;
                }
            }
        }
        #endregion

        #region Вставка
        public static bool Insert(string TableName, string[] ColumnsNames, string[] ColumnsValues, out int? returnID)
        {
            returnID = null;
            // Подключение к БД
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                // Проверка подключения
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                    if (connection.State != ConnectionState.Open)
                        throw new Exception("Не удаётся подключиться к БД");
                }

                // Создание запроса
                string query = $"INSERT INTO \"{TableName}\" (";
                foreach (string Name in ColumnsNames)
                    query += $"\"{Name}\",";
                query = query.Remove(query.Length - 1) + ") VALUES (";
                foreach (string Val in ColumnsValues)
                    query += $"'{Val.Replace("'", "")}',";
                query = query.Remove(query.Length - 1) + ");";
                query += "SELECT SCOPE_IDENTITY();"; // Вернуть getID

                // Обработка запроса
                using (var cmd = new SqlCommand(query, connection))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read() && !reader.IsDBNull(0))
                        returnID = reader.GetInt32(0);
                    connection.Close();
                    return returnID == null ? false : true; // Вывод результата
                }
            }
        }
        public static bool Insert(string TableName, string[] ColumnsValues, out int? returnID)
        {
            returnID = null;
            // Подключение к БД
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                // Проверка подключения
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                    if (connection.State != ConnectionState.Open)
                        throw new Exception("Не удаётся подключиться к БД");
                }
                // Создание запроса
                string query = $"INSERT INTO \"{TableName}\" VALUES (";
                foreach (string col in ColumnsValues)
                    query += $"'{col.Replace("'", "")}',";
                query = query.Remove(query.Length - 1) + ");";
                query += "SELECT SCOPE_IDENTITY();"; // Вернуть getID

                // Обработка запроса
                using (var cmd = new SqlCommand(query, connection))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read() && !reader.IsDBNull(0))
                        returnID = int.Parse(reader.GetDecimal(0).ToString());
                    connection.Close();
                    return returnID == null ? false : true; // Вывод результата
                }
            }
        }
        #endregion

        #region Обновление
        public static bool Replace(string TableName, string KeyName, string KeyValue, string[] ColumnsNames, string[] ColumnsValues)
        {
            // Проверка
            if (ColumnsNames.Length != ColumnsValues.Length)
                throw new Exception("Количество столбцов не совпадает с количество значений");

            // Подключение к БД
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                // Проверка подключения
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                    if (connection.State != ConnectionState.Open)
                        throw new Exception("Не удаётся подключиться к БД");
                }

                // Создание запроса
                string zapros = $"UPDATE {TableName} SET ";
                for (int i = 0; i < ColumnsNames.Length; ++i)
                {
                    zapros += $"[{ColumnsNames[i]}] = '{ColumnsValues[i]}',";
                }
                zapros = zapros.Remove(zapros.Length - 1) + $" WHERE [{KeyName}] = '{KeyValue}'";

                // Обработка запроса
                using (var cmd = new SqlCommand(zapros, connection))
                {
                    int count = cmd.ExecuteNonQuery();
                    connection.Close();
                    return count < 1 ? false : true; // Вывод результата
                }
            }
        }
        public static bool Replace(string TableName, string KeyName, string KeyValue, string ColumnName, string ColumnValue)
        {
            // Подключение к БД
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                // Проверка подключения
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                    if (connection.State != ConnectionState.Open)
                        throw new Exception("Не удаётся подключиться к БД");
                }

                // Создание запроса
                string zapros = $"UPDATE {TableName} SET [{ColumnName}] = '{ColumnValue}' WHERE [{KeyName}] = '{KeyValue}'";

                // Обработка запроса
                using (var cmd = new SqlCommand(zapros, connection))
                {
                    int count = cmd.ExecuteNonQuery();
                    connection.Close();
                    return count < 1 ? false : true; // Вывод результата
                }
            }
        }
        #endregion

        #region Удаление
        public static bool Delete(string TableName, string KeyName, string KeyValue)
        {
            // Подключение к БД
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                // Проверка подключения
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                    if (connection.State != ConnectionState.Open)
                        throw new Exception("Не удаётся подключиться к БД");
                }

                // Создание запроса
                string zapros = $"DELETE FROM \"{TableName}\" WHERE \"{KeyName}\" = '{KeyValue}'";

                // Обработка запроса
                using (var cmd = new SqlCommand(zapros, connection))
                {
                    int count = cmd.ExecuteNonQuery();
                    connection.Close();
                    return count < 1 ? false : true; // Вывод результата
                }
            }
        }
        #endregion
    }
}