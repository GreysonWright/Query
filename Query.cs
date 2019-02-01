using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Query
{
    public class Query
    {
        private string connectionString;
        private string queryString;
        private List<SqlParameter> parameters;

        public Query(string queryString, string connectionString)
        {
            this.connectionString = connectionString;
            this.queryString = queryString;
            parameters = new List<SqlParameter>();
        }

        public void AddParameter(string parameterName, object value)
        {
            object paramVal = value ?? DBNull.Value;
            var parameter = new SqlParameter(parameterName, paramVal);
            parameters.Add(parameter);
        }

        public void AddParameters<T>(T obj)
        {
            var validParameters = GetParametersFromQueryString();
            var objType = typeof(T);
            foreach (var parameter in validParameters)
            {
                var property = objType.GetProperty(parameter, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    var propertyValue = property.GetValue(obj);
                    var parameterName = string.Format("@{0}", property.Name);
                    AddParameter(parameterName, propertyValue);
                }
            }
        }

        private List<string> GetParametersFromQueryString()
        {
            var tokenizedQueryString = TokenizeQueryString();
            var parameters = new List<string>();
            for (int i = 1; i < tokenizedQueryString.Length; i++)
            {
                var token = tokenizedQueryString[i];
                if (!string.IsNullOrWhiteSpace(token))
                {
                    var parameter = SanatizeString(token);
                    parameters.Add(parameter);
                }
            }
            return parameters;
        }

        private string[] TokenizeQueryString()
        {
            char[] delimeters = { '@', ','};
            var splitQueryString = queryString.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
            return splitQueryString;
        }

        private string SanatizeString(string str)
        {
            char[] trimChars = {' ', ';'};
            var sanatizedString = str.Trim(trimChars);
            return sanatizedString;
        }

        public DataTable Execute()
        {
            var resultTable = new DataTable();
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(queryString, connection))
            {
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }

                connection.Open();
                var reader = command.ExecuteReader();
                resultTable.Load(reader);
                reader.Close();
            }
            return resultTable;
        }

        public object ExecuteScalar()
        {
            object result = null;
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(queryString, connection))
            {
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }

                connection.Open();
                result = command.ExecuteScalar();
            }
            return result;
        }
    }
}