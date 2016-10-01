using CRUDpanel.Filters;
using CRUDpanel.Models;
using CRUDpanel.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace CRUDpanel.Controllers
{
    public class HomeController : BaseController
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;

        public ActionResult Index()
        {
            return View();
        }


        public JsonResult GetTables()
        {
            string sqlQuery = @"SELECT s.name schemaname, o.name tablename, i.name identitycolname
                                FROM sys.schemas s
                                JOIN sys.sysobjects o ON o.uid = s.schema_id
                                LEFT JOIN sys.identity_columns i ON o.id = i.object_id
                                WHERE o.xtype = 'U' AND o.name != 'sysdiagrams'
                                ORDER BY s.name, o.name";

            List<Table> tables = new List<Table>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand(sqlQuery, connection))
                {
                    sqlCommand.CommandType = CommandType.Text;

                    connection.Open();
                    using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlDataReader.Read())
                        {
                            tables.Add(new Table
                            {
                                Schema = sqlDataReader.GetValue(0).ToString(),
                                Name = sqlDataReader.GetValue(1).ToString(),
                                IdentityColumn = sqlDataReader.GetValue(2).ToString()
                            });
                        }
                    }
                    connection.Close();
                }
            }

            return Json(tables);
        }


        public JsonResult GetColumns(Table table)
        {
            string sqlQuery = string.Format(@"SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE, COLUMN_DEFAULT 
                                              FROM INFORMATION_SCHEMA.COLUMNS 
                                              WHERE TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}'",
                                              table.Schema, table.Name);

            List<Column> columns = new List<Column>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand(sqlQuery, connection))
                {
                    sqlCommand.CommandType = CommandType.Text;

                    connection.Open();
                    using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlDataReader.Read())
                        {
                            columns.Add(new Column
                            {
                                Name = sqlDataReader.GetValue(0).ToString(),
                                DataType = sqlDataReader.GetValue(1).ToString(),
                                MaxLength = sqlDataReader.IsDBNull(2) ? null : (int?)sqlDataReader.GetInt32(2),
                                IsNullable = sqlDataReader.GetValue(3).ToString() == "NO" ? false : true,
                                DefaultValue = sqlDataReader.IsDBNull(4) ? null : sqlDataReader.GetValue(4).ToString().Substring(2, sqlDataReader.GetValue(4).ToString().Length - 4)
                            });
                        }
                    }
                    connection.Close();
                }
            }

            return Json(columns);
        }


        public JsonResult GetRows(Table table, int columnsCount, bool isDependant, int? identityValue)
        {            
            string sqlQuery = isDependant && identityValue.HasValue ? string.Format("SELECT * FROM {0}.{1} WHERE [{2}] = {3}", table.Schema, table.Name, table.ReferencingColumn == null ?
                GetTableIdentityColumn(table) :
                table.ReferencingColumn, identityValue) : string.Format("SELECT * FROM {0}.{1}", table.Schema, table.Name);

            List<Row> rows = new List<Row>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand(sqlQuery, connection))
                {
                    sqlCommand.CommandType = CommandType.Text;

                    connection.Open();
                    using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlDataReader.Read())
                        {
                            Row row = new Row();
                            row.Cells = new List<string>();

                            for (int i = 0; i < columnsCount; i++)
                            {
                                row.Cells.Add(sqlDataReader.IsDBNull(i) ? null : sqlDataReader.GetValue(i).ToString());
                            }

                            rows.Add(row);
                        }
                    }
                    connection.Close();
                }
            }

            return Json(rows);
        }


        public JsonResult SaveRecord(string action, Table table, List<Cell> oldCells, List<Cell> newCells)
        {
            StringBuilder sqlExpression = new StringBuilder();
            StringBuilder sqlFilter = new StringBuilder();

            //cells sql friendly format generation
            oldCells.ForEach(oc =>
            {
                switch (oc.Column.DataType)
                {
                    case "float":
                    case "decimal":
                    case "numeric":
                        {
                            if (oc.Value == string.Empty || oc.Value == null)
                                oc.Value = null;
                            else
                                oc.Value = GenerateSqlFloatingNumberFormat(oc.Value);
                            break;
                        }
                    case "datetime":
                        {
                            if (oc.Value == string.Empty || oc.Value == null)
                                oc.Value = null;
                            else
                                oc.Value = GenerateSqlDateTimeFormat(oc.Value);
                            break;
                        }
                    case "date":
                        {
                            if (oc.Value == string.Empty || oc.Value == null)
                                oc.Value = null;
                            else
                                oc.Value = GenerateSqlDateFormat(oc.Value);
                            break;
                        }                        
                }
            });
            newCells.ForEach(nc =>
            {
                switch (nc.Column.DataType)
                {
                    case "float":
                    case "decimal":
                    case "numeric":
                        {
                            if (nc.Value == string.Empty || nc.Value == null)
                                nc.Value = null;
                            else
                                nc.Value = GenerateSqlFloatingNumberFormat(nc.Value);
                            break;
                        }
                    case "datetime":
                        {
                            if (nc.Value == string.Empty || nc.Value == null)
                                nc.Value = null;
                            else
                                nc.Value = GenerateSqlDateTimeFormat(nc.Value);
                            break;
                        }
                    case "date":
                        {
                            if (nc.Value == string.Empty || nc.Value == null)
                                nc.Value = null;
                            else
                                nc.Value = GenerateSqlDateFormat(nc.Value);
                            break;
                        }                  
                }
            });
            //---


            string tablePrimaryKeyColumn = GetTablePrimaryKeyColumn(table);

            switch (action)
            {
                case "INSERT INTO":
                    for (int i = table.IdentityColumn == null ? 0 : 1; i < newCells.Count - 1; i++)
                    {
                        sqlExpression.Append(newCells[i].Column.IsNullable && newCells[i].Value == string.Empty || newCells[i].Value == null || newCells[i].Value == "null" ? "NULL, " : string.Format("N'{0}', ", newCells[i].Value));
                    }
                    sqlExpression.Append(newCells[newCells.Count - 1].Column.IsNullable && newCells[newCells.Count - 1].Value == string.Empty || newCells[newCells.Count - 1].Value == null || newCells[newCells.Count - 1].Value == "null" ? "NULL" : string.Format("N'{0}'", newCells[newCells.Count - 1].Value));
                    break;

                case "UPDATE":                    
                    for (int i = table.IdentityColumn == null ? 0 : 1; i < newCells.Count - 1; i++)
                    {
                        sqlExpression.Append(string.Format(newCells[i].Value == string.Empty || newCells[i].Value == null || newCells[i].Value == "null" ? "[{0}]=NULL, " : "[{0}]=N'{1}', ", newCells[i].Column.Name, newCells[i].Value));
                        if(tablePrimaryKeyColumn == string.Empty)
                        {
                            sqlFilter.Append(oldCells[i].Value == null ? string.Format("[{0}] IS NULL AND ", oldCells[i].Column.Name) : string.Format("[{0}]=N'{1}' AND ", oldCells[i].Column.Name, oldCells[i].Value));
                        }
                    }
                    sqlExpression.Append(string.Format(newCells[newCells.Count - 1].Value == string.Empty || newCells[newCells.Count - 1].Value == null || newCells[newCells.Count - 1].Value == "null" ? "[{0}]=NULL" : "[{0}]=N'{1}'", newCells[newCells.Count - 1].Column.Name, newCells[newCells.Count - 1].Value));
                    if(tablePrimaryKeyColumn == string.Empty)
                    {
                        sqlFilter.Append(oldCells[newCells.Count - 1].Value == null ? string.Format("[{0}] IS NULL", oldCells[newCells.Count - 1].Column.Name) : string.Format("[{0}]=N'{1}'", oldCells[newCells.Count - 1].Column.Name, oldCells[newCells.Count - 1].Value));
                    }
                    else
                    {
                        sqlFilter.Append(string.Format("[{0}]=N'{1}'", tablePrimaryKeyColumn, GetTablePrimaryKeyValue(tablePrimaryKeyColumn, oldCells)));
                    }
                    break;

                case "DELETE FROM":
                    for (int i = 0; i < newCells.Count - 1; i++)
                    {
                        if(tablePrimaryKeyColumn == string.Empty)
                        {
                            sqlFilter.Append(string.Format(newCells[i].Value == null ? "[{0}] IS NULL AND " : "[{0}]=N'{1}' AND ", newCells[i].Column.Name, newCells[i].Value));
                        }
                    }
                    if(tablePrimaryKeyColumn == string.Empty)
                    {
                        sqlFilter.Append(string.Format(newCells[newCells.Count - 1].Value == null ? "[{0}] IS NULL" : "[{0}]=N'{1}'", newCells[newCells.Count - 1].Column.Name, newCells[newCells.Count - 1].Value));
                    }
                    else
                    {
                        sqlFilter.Append(string.Format("[{0}]=N'{1}'", tablePrimaryKeyColumn, GetTablePrimaryKeyValue(tablePrimaryKeyColumn, oldCells)));
                    }
                    break;
            }

            string sqlQuery = action == "INSERT INTO" ? string.Format("{0} {1}.{2} VALUES ({3})", action, table.Schema, table.Name, sqlExpression.ToString())
                : action == "UPDATE" ? string.Format("{0} {1}.{2} SET {3} WHERE {4}", action, table.Schema, table.Name, sqlExpression.ToString(), sqlFilter.ToString())
                : string.Format("{0} {1}.{2} WHERE {3}", action, table.Schema, table.Name, sqlFilter.ToString());

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand(sqlQuery, connection))
                {
                    sqlCommand.CommandType = CommandType.Text;
                    connection.Open();
                    SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                    connection.Close();

                    return Json(sqlDataReader.RecordsAffected > 0);
                }
            }
        }


        string GenerateSqlDateTimeFormat(string param)
        {
            if (string.IsNullOrEmpty(param))
                return string.Empty;
            else
                return Convert.ToDateTime(param).ToString("yyyy-MM-dd HH:mm:ss:fff");
        }


        string GenerateSqlDateFormat(string param)
        {
            if (string.IsNullOrEmpty(param))
                return string.Empty;
            else
                return Convert.ToDateTime(param).ToString("yyyy-MM-dd");
        }

        string GenerateSqlFloatingNumberFormat(string param)
        {
            if (string.IsNullOrEmpty(param))
                return string.Empty;
            else
                return param.Replace(',', '.');
        }


        public JsonResult GetTableDependantTables(Table table)
        {
            string sqlQuery = @"SELECT cu.TABLE_SCHEMA AS schemaName, cu.TABLE_NAME AS referencingTable, cu.COLUMN_NAME AS referencingColumn
                                FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS c
                                INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cu
                                ON cu.CONSTRAINT_NAME = c.CONSTRAINT_NAME
                                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                                ON ku.CONSTRAINT_NAME = c.UNIQUE_CONSTRAINT_NAME
                                WHERE ku.TABLE_SCHEMA = '" + table.Schema + "' AND ku.TABLE_NAME = '" + table.Name + "'";

            List<Table> tables = new List<Table>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand(sqlQuery, connection))
                {
                    sqlCommand.CommandType = CommandType.Text;

                    connection.Open();
                    using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlDataReader.Read())
                        {
                            tables.Add(new Table
                            {
                                Schema = sqlDataReader.GetValue(0).ToString(),
                                Name = sqlDataReader.GetValue(1).ToString(),
                                ReferencingColumn = sqlDataReader.GetValue(2).ToString()
                            });
                        }
                    }
                    connection.Close();
                }
            }

            return Json(tables.OrderBy(t => t.Schema).ThenBy(t => t.Name));
        }


        public JsonResult GetTableReferencedTable(Table table)
        {
            string sqlQuery = @"SELECT referencedSchemaName = OBJECT_SCHEMA_NAME(referenced_object_id), 
                                referencedTable = OBJECT_NAME(referenced_object_id),
                                referencedColumn = pc.name 
                                FROM sys.foreign_key_columns AS pt
                                INNER JOIN sys.columns AS pc
                                ON pt.parent_object_id = pc.[object_id]
                                AND pt.parent_column_id = pc.column_id
                                INNER JOIN sys.columns AS rc
                                ON pt.referenced_column_id = rc.column_id
                                AND pt.referenced_object_id = rc.[object_id]
                                WHERE OBJECT_SCHEMA_NAME(parent_object_id) = '" + table.Schema + "' AND OBJECT_NAME(parent_object_id) = '" + table.Name + "'";

            List<Table> tables = new List<Table>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand(sqlQuery, connection))
                {
                    sqlCommand.CommandType = CommandType.Text;

                    connection.Open();
                    using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlDataReader.Read())
                        {
                            tables.Add(new Table
                            {
                                Schema = sqlDataReader.GetValue(0).ToString(),
                                Name = sqlDataReader.GetValue(1).ToString(),
                                ReferencedColumn = sqlDataReader.GetValue(2).ToString()
                            });
                        }
                    }
                    connection.Close();
                }
            }

            return Json(tables.OrderBy(t => t.Schema).ThenBy(t => t.Name));
        }


        string GetTablePrimaryKeyColumn(Table table)
        {
            string sqlQuery = string.Format(@"SELECT COLUMN_NAME
                                              FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                                              WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                                              AND TABLE_SCHEMA = '{0}' AND TABLE_NAME = '{1}'",
                                              table.Schema, table.Name);

            List<Table> tables = new List<Table>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand(sqlQuery, connection))
                {
                    sqlCommand.CommandType = CommandType.Text;

                    connection.Open();
                    using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlDataReader.Read())
                        {
                            tables.Add(new Table
                            {
                                PrimaryKeyColumn = sqlDataReader.GetValue(0).ToString()
                            });
                        }
                    }
                    connection.Close();
                }
            }

            return tables.Count == 0 ? string.Empty : tables.First().PrimaryKeyColumn;
        }


        string GetTableIdentityColumn(Table table)
        {
            string sqlQuery = string.Format(@"SELECT i.name identitycolname
                                              FROM sys.schemas s
                                              JOIN sys.sysobjects o ON o.uid = s.schema_id
                                              LEFT JOIN sys.identity_columns i ON o.id = i.object_id
                                              WHERE o.xtype = 'U' AND s.name = '{0}' AND o.name = '{1}'
                                              ORDER BY s.name, o.name",
                                              table.Schema, table.Name);

            List<Table> tables = new List<Table>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand(sqlQuery, connection))
                {
                    sqlCommand.CommandType = CommandType.Text;

                    connection.Open();
                    using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                    {
                        while (sqlDataReader.Read())
                        {
                            tables.Add(new Table
                            {
                                IdentityColumn = sqlDataReader.GetValue(0).ToString()
                            });
                        }
                    }
                    connection.Close();
                }
            }

            return tables.Count == 0 ? string.Empty : tables.First().IdentityColumn;
        }


        string GetTablePrimaryKeyValue(string tablePrimaryKeyColumn, List<Cell> cells)
        {
            string result = string.Empty;

            foreach(Cell cell in cells)
            {
                if(tablePrimaryKeyColumn == cell.Column.Name)
                {
                    result = cell.Value;
                }
            }

            return result;
        }
    }
}