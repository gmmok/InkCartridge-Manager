using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;


namespace OpenBCMS
{
    /// <summary>
    /// SQLite数据库访问基础类
    /// </summary>
    public class BaseDbSQLiteClass
    {
        //数据库连接字符串
        public static string DbFileName = "CartridgeDB.db";
        public static string DbFilePath = DbFileName;
        //public static string DbFilePath = Path.Combine(BaseFileClass.AppPath, DbFileName);
        ////public static string DbFilePath = @"D:\R&D\WpfMYSQL\OpenBCMS\my.db";
        public static string connString = @"data source=" + DbFilePath + ";version=3;BinaryGUID=False;";

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="sqlString">计算查询结果语句</param>
        /// <returns>查询结果（object）</returns>
        public static object GetSingle(string sqlString)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connString))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(sqlString, conn))
                {
                    try
                    {
                        conn.Open();
                        object obj = cmd.ExecuteScalar();
                        if ((Object.Equals(obj, null)) || (Object.Equals(obj, System.DBNull.Value)))
                        {
                            return null;
                        }
                        else
                        {
                            return obj;
                        }
                    }
                    catch //(Exception ex)
                    {
                        //MessageBox.Show(ex.ToString());
                        conn.Close();
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// 执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="sqlString">计算查询结果语句</param>
        /// <param name="connString">连接字符串</param>
        /// <returns>查询结果（object）</returns>
        public static object GetSingle(string sqlString, string connString)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connString))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(sqlString, conn))
                {
                    try
                    {
                        conn.Open();
                        object obj = cmd.ExecuteScalar();
                        if ((Object.Equals(obj, null)) || (Object.Equals(obj, System.DBNull.Value)))
                        {
                            return null;
                        }
                        else
                        {
                            return obj;
                        }
                    }
                    catch //(Exception ex)
                    {
                        //MessageBox.Show(ex.ToString());
                        conn.Close();
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// 执行SQL语句返回DataTable
        /// </summary>
        /// <param name="sqlString"></param>
        /// <returns>DataTable</returns>
        public static DataTable GetDataTable(string sqlString)
        {
            DataTable dt = new DataTable();
            try
            {
                SQLiteConnection conn = new SQLiteConnection(connString); //创建连接   
                SQLiteCommand cmd = conn.CreateCommand();//创建SqlCommand对象          
                cmd.CommandText = sqlString;
                conn.Open();  //打开连接         
                SQLiteDataAdapter ad = new SQLiteDataAdapter(cmd);
                ad.Fill(dt);
                conn.Close();
            }
            catch //(Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                return null;
            }
            return dt;
        }

        /// <summary>
        /// 执行SQL语句
        /// </summary>
        /// <returns>受影响的行数</returns>
        /// <param name="queryString">SQL命令字符串</param>
        public static int ExecuteSql(string sqlString)
        {
            try
            {
                SQLiteConnection conn = new SQLiteConnection(connString);    //创建连接
                SQLiteCommand cmd = conn.CreateCommand();          //创建SqlCommand对象
                cmd.CommandText = sqlString;
                conn.Open();                            //打开连接
                int i = cmd.ExecuteNonQuery();
                conn.Close();
                return i;
            }
            catch //(Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                return 0;
            }
        }

        /// <summary>   
        /// 执行多条SQL语句，实现数据库事务，返回受影响的行。   
        /// </summary>   
        /// <param name="SQLStringList">多条SQL语句</param>        
        public static int ExecuteSqlTran(List<string> listString)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connString))
            {
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand();
                cmd.Connection = conn;
                SQLiteTransaction tx = conn.BeginTransaction();
                cmd.Transaction = tx;
                int x = -1;
                try
                {
                    if (listString != null)
                    {
                        for (int n = 0; n < listString.Count; n++)
                        {
                            string strsql = listString[n].ToString();
                            if (strsql.Trim().Length > 1)
                            {
                                cmd.CommandText = strsql;
                                x = cmd.ExecuteNonQuery();
                                if (x <= 0)//如果x<=0就说明该Sql语句没有成功执行，那么就让它回滚，回滚至没有更新数据之前
                                {
                                    tx.Rollback();//让事务回滚  }
                                    break;
                                }
                            }
                        }
                        tx.Commit();
                    }
                }
                catch //(Exception ex)
                {
                    x = -1;
                    tx.Rollback();
                }
                finally
                {
                    if (conn.State == ConnectionState.Open)
                    {
                        //cmd = null;
                        conn.Close();//关闭连接
                    }
                }
                return x;
            }
        }

        /// <summary>
        /// 创建数据库文件
        /// </summary>
        /// <param name="dbPath">数据库文件绝对路径</param>
        /// <returns>新建成功，返回true，否则返回false</returns>
        public static bool CreateDB(string sqlFileName)
        {
            bool result = false;
            try
            {
                if (File.Exists(DbFilePath) == false)
                {
                    SQLiteConnection.CreateFile(DbFileName);
                    string sqlPath = Path.Combine(BaseDirClass.AppPath, sqlFileName);
                    string sql = BaseFileClass.FileToString(sqlPath);
                    if (ExecuteSql(sql) > 0)
                    {
                        System.Windows.MessageBox.Show("数据库创建成功！");
                        result = true;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("数据库创建失败！");
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("数据库文件已存在！");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            return result;
        }

        /// <summary>
        /// 升级数据库结构
        /// </summary>
        /// <param name="dbPath">数据库文件绝对路径</param>
        /// <returns>新建成功，返回true，否则返回false</returns>
        public static bool UpdateDB(string ver)
        {
            bool result = false;
            string fileName = GetUpdFileName(ver);
            try
            {
                string updateFilePath = Path.Combine(BaseDirClass.AppPath, fileName);
                string sql = BaseFileClass.FileToString(updateFilePath);
                if (File.Exists(DbFilePath) == true)
                {
                    if (File.Exists(updateFilePath) == true)
                    {
                        if (ExecuteSql(sql) > 0)
                        {
                            System.Windows.MessageBox.Show("数据库升级成功！请重新启动软件！");

                            result = true;
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("数据库升级失败！");
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("升级文件不存在！");
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show(ver + "升级" + DalDataConfig.DbVerion + "的文件不存在！");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            return result;
        }

        /// <summary>
        /// 返回升级文件名
        /// </summary>
        /// <param name="ver">数据库中现在的数据结构版本号</param>
        /// <returns>string</returns>
        static string GetUpdFileName(string ver)
        {
            string result = string.Empty;
            string[] dirPaths = BaseDirClass.GetDirFiles(BaseDirClass.AppPath, "*.upd");
            if (dirPaths.Length > 0)
            {
                foreach (string tmp in dirPaths)
                {
                    string s = BaseFileClass.GetFileName(tmp, false).Substring(0, 6);
                    if (s == ver)
                    {
                        result = tmp;
                        break;
                    }
                }
            }
            return result;
        }
    }
}
