using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace OpenBCMS
{
    /// <summary>
    /// SQLite数据库操作类
    /// </summary>
    public class DalSQLite
    {
        /// <summary>
        /// 软件启动数据检测
        /// </summary>
        /// <returns></returns>
        public static bool CheckDb()
        {
            bool result = false;
            if (File.Exists(BaseDbSQLiteClass.DbFilePath) == true)//数据文件存在
            {
                if (GetSoftName() == DalDataConfig.SoftName)
                {
                    result = true;
                }
                else
                {
                    RepairDB();
                }

            }
            else//数据文件不存在
            {
                if ((System.Windows.MessageBox.Show("新的软件安装、是否创建新的数据文件？", "数据检测", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes))//
                {
                    result = BaseDbSQLiteClass.CreateDB("SQLiteDB.sql");
                }
            }
            return result;
        }

        private static void RepairDB()
        {
            try
            {
                if (MessageBox.Show("删除现有数据库并新建数据结构吗？", "数据库异常提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (File.Exists(BaseDbSQLiteClass.DbFilePath) == true)
                    {
                        File.Delete(BaseDbSQLiteClass.DbFilePath);
                    }
                    BaseDbSQLiteClass.CreateDB("SQLiteDB.sql");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 返回软件名
        /// </summary>
        /// <param name="connString">连接字符串</param>
        /// <returns>版本号</returns>
        public static string GetSoftName()
        {
            object obj = BaseDbSQLiteClass.GetSingle("SELECT SoftName FROM sys_config");
            if (obj != null)
                return obj.ToString();
            else
                return string.Empty;
        }

        /// <summary>
        /// 返回应用程序对应数据库的数据结构版本号
        /// </summary>
        /// <param name="connString">连接字符串</param>
        /// <returns>版本号</returns>
        public static bool GetVersion(ref string version)
        {
            bool result = false;
            double ver = 0.0;
            try
            {
                object obj = BaseDbSQLiteClass.GetSingle("SELECT DbVersion FROM sys_config");
                if (obj != null)
                {
                    if (double.TryParse(obj.ToString(), out ver) == true)
                    {
                        if (ver > 0)
                        {
                            if (ver == DalDataConfig.DbVerion)
                            {
                                version = obj.ToString();
                                result = true;
                            }
                            else if (ver > DalDataConfig.DbVerion)
                            {
                                version = obj.ToString();
                                result = true;
                                MessageBox.Show("当前数据库版本高于软件要求版本，请检查软件、数据正确性！");
                            }
                            else
                            {
                                if (MessageBox.Show("当前数据库版本低于软件要求版本，是否升级数据库结构？", "数据库异常", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                                {
                                    if (BaseDbSQLiteClass.UpdateDB(obj.ToString()) == true)
                                    {
                                        System.Environment.Exit(0);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            return result;
        }

        /// <summary>
        /// 返回数据集合
        /// </summary>
        /// <returns>IList</returns>
        public static IList<T> GetIList<T>(string sqlString)
        {
            return BaseDataTable.DataTableToIList<T>(BaseDbSQLiteClass.GetDataTable(sqlString));
        }

        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="sqlString">sql语句</param>
        /// <returns>bool</returns>
        public static bool ExecuteSql(string sqlString)
        {
            bool result = false;
            try
            {
                if (BaseDbSQLiteClass.ExecuteSql(sqlString) == 1)
                {
                    return true;
                }
                else
                {
                    result = false;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            return result;
        }

        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="sqlString">sql语句</param>
        /// <returns>bool</returns>
        public static bool Insert(string sqlString)
        {
            bool result = false;
            try
            {
                if (BaseDbSQLiteClass.ExecuteSql(sqlString) > 0)
                {
                    MessageBox.Show(DalPrompt.SaveOK);
                    return true;
                }
                else
                {
                    MessageBox.Show(DalPrompt.SaveFail);
                    result = false;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            return result;
        }

        /// <summary>
        /// 批量插入数据
        /// </summary>
        /// <param name="l">sql语句集合</param>
        /// <returns>bool</returns>
        public static bool Insert(List<string> l)
        {
            bool result = false;
            try
            {
                if (BaseDbSQLiteClass.ExecuteSqlTran(l) > 0)
                {
                    MessageBox.Show(DalPrompt.SaveOK);
                    result = true;
                }
                else
                {
                    MessageBox.Show(DalPrompt.SaveFail);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            return result;
        }

        /// <summary>
        /// 批量插入数据
        /// </summary>
        /// <param name="l">sql语句集合</param>
        /// <returns>bool</returns>
        public static bool Import(List<string> l)
        {
            bool result = false;
            try
            {
                if (BaseDbSQLiteClass.ExecuteSqlTran(l) > 0)
                {
                    MessageBox.Show(DalPrompt.ImportOK);
                    result = true;
                }
                else
                {
                    MessageBox.Show(DalPrompt.ImportFail);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            return result;
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="sqlString">sql语句</param>
        /// <returns>bool</returns>
        public static bool Save(string sqlString)
        {
            bool result = false;
            try
            {
                if (BaseDbSQLiteClass.ExecuteSql(sqlString) == 1)
                {
                    MessageBox.Show(DalPrompt.SaveOK);
                    result = true;
                }
                else
                {
                    MessageBox.Show(DalPrompt.SaveFail);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            return result;
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <param name="sqlString">sql语句</param>
        /// <returns>bool</returns>
        public static bool Update(string sqlString)
        {
            bool result = false;
            try
            {
                if (BaseDbSQLiteClass.ExecuteSql(sqlString) == 1)
                {
                    MessageBox.Show(DalPrompt.UpdateOK);
                    result = true;
                }
                else
                {
                    MessageBox.Show(DalPrompt.UpdateFail);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            return result;
        }

        /// <summary>
        /// 批量更新数据
        /// </summary>
        /// <param name="l">sql语句集合</param>
        /// <returns>bool</returns>
        public static bool Update(List<string> l)
        {
            bool result = false;
            if (l != null)
            {
                try
                {
                    if (BaseDbSQLiteClass.ExecuteSqlTran(l) > 0)
                    {
                        MessageBox.Show(DalPrompt.UpdateOK);
                        result = true;
                    }
                    else
                    {
                        MessageBox.Show("更新失败！");
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.ToString());
                }
            }
            return result;
        }

        /// <summary>
        /// 收款（批量SQL语句）
        /// </summary>
        /// <param name="l">sql语句集合</param>
        /// <returns>bool</returns>
        public static bool Pay(List<string> l)
        {
            bool result = false;
            if (l != null)
            {
                try
                {
                    if (BaseDbSQLiteClass.ExecuteSqlTran(l) > 0)
                    {
                        MessageBox.Show(DalPrompt.UpdateOK);
                        result = true;
                    }
                    else
                    {
                        MessageBox.Show("更新失败！");
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.ToString());
                }
            }
            return result;
        }

        /// <summary>
        /// 结束（批量SQL语句）
        /// </summary>
        /// <param name="l">sql语句集合</param>
        /// <returns>bool</returns>
        public static bool Complete(List<string> l)
        {
            bool result = false;
            if (l != null)
            {
                try
                {
                    if (BaseDbSQLiteClass.ExecuteSqlTran(l) > 0)
                    {
                        MessageBox.Show("结束成功！");
                        result = true;
                    }
                    else
                    {
                        MessageBox.Show("结束失败！");
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.ToString());
                }
            }
            return result;
        }

        /// <summary>
        /// 删除标记
        /// </summary>
        /// <param name="sqlStr">sql语句</param>
        /// <returns>bool</returns>
        public static bool DeleteMark(string sqlStr)
        {
            bool result = false;
            try
            {
                if (BaseDbSQLiteClass.ExecuteSql(sqlStr) > 0)
                {
                    MessageBox.Show("删除成功！");
                    result = true;
                }
                else
                {
                    MessageBox.Show("删除失败！");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            return result;
        }



        /// <summary>
        /// /// <summary>
        /// 追加查询条件拼接SQL语句
        /// </summary>
        /// <typeparam name="T">数据对象</typeparam>
        /// <param name="s">被拼接的数据对象实例</param>
        /// <param name="sqlString">被追加的SQL字符串</param>
        public static void AppendConditionString<T>(T s, StringBuilder sqlString)
        {
            Type t = s.GetType();//获得该类的Type
            //再用Type.GetProperties获得PropertyInfo[],然后就可以用foreach 遍历了
            foreach (PropertyInfo pi in t.GetProperties())
            {
                string name = pi.Name;//获得属性的名字,后面就可以根据名字判断来进行些自己想要的操作
                if (pi.PropertyType == typeof(int))
                {
                    int num = Convert.ToInt32(pi.GetValue(s, null));
                    if (num != 0)
                    {
                        if (num > 0)
                        {
                            sqlString.Append(" AND " + name + " = " + num + " ");
                        }
                    }
                }
                else if (pi.PropertyType == typeof(string))
                {
                    string str = Convert.ToString(pi.GetValue(s, null));
                    if (str != null)
                    {
                        if (str != "")
                        {
                            string n = pi.Name.ToString();
                            sqlString.Append(" AND " + n + " LIKE '%" + str + "%' ");
                        }
                    }
                }
                else if (pi.PropertyType == typeof(Guid))//属性类型GUID
                {
                    Guid g = (Guid)(pi.GetValue(s, null));//获取条件值
                    if (g != (new Guid()))
                    {
                        sqlString.Append(" AND " + name + " = '" + g + "' ");//增加SQL条件语句：与条件值相等
                    }
                }
            }
        }

        /// <summary>
        /// 追加查询条件拼接SQL语句(FROM 表名——别名=a)
        /// </summary>
        /// <typeparam name="T">数据对象</typeparam>
        /// <param name="s">被拼接的数据对象实例</param>
        /// <param name="sqlString">被追加的SQL语句</param>
        public static void AppendAliasString<T>(T s, StringBuilder sqlString, string alias)
        {
            Type t = s.GetType();//获得该类的Type
            foreach (PropertyInfo pi in t.GetProperties()) //遍历Type.GetProperties获得的PropertyInfo[]
            {
                string name = pi.Name;//获取属性名  
                                      //根据属性类型条件判断是否拼接SQL语句
                if (pi.PropertyType == typeof(int))
                {
                    int num = Convert.ToInt32(pi.GetValue(s, null));
                    if (num != 0)
                    {
                        if (num > 0)
                        {
                            sqlString.Append(" AND " + alias + "." + name + " = " + num + " ");
                        }
                    }
                }
                else if (pi.PropertyType == typeof(string))//属性类型字符串
                {
                    string str = Convert.ToString(pi.GetValue(s, null));//获取条件值
                    if (str != null)
                    {
                        if (str != "")
                        {
                            sqlString.Append(" AND " + alias + "." + name + " LIKE '%" + str + "%' ");//增加SQL条件语句：包含条件字符
                        }
                    }
                }
                else if (pi.PropertyType == typeof(Guid))//属性类型GUID
                {
                    Guid g = (Guid)(pi.GetValue(s, null));//获取条件值
                    if (g != (new Guid()))
                    {
                        sqlString.Append(" AND " + alias + "." + name + " = '" + g + "' ");//增加SQL条件语句：与条件值相等
                    }
                }
            }
        }

        /// <summary>
        /// 备份当前数据库文件到所选路径（命名为年月日时分秒.bak）
        /// </summary>
        /// <param name="filePath">备份文件绝对路径</param>
        /// <returns></returns>
        public static bool Backup(string filePath)
        {
            bool result = false;
            string dbPath = BaseDbSQLiteClass.DbFilePath;//数据文件绝对路径
            try
            {
                if (File.Exists(dbPath) == true)//有数据文件
                {
                    if (File.Exists(filePath) == false)//绝对路径不存在备份文件，可以直接备份
                    {
                        File.Copy(dbPath, filePath);
                        System.Windows.MessageBox.Show(DalPrompt.DbBackupOK + @filePath);
                        result = true;
                    }
                    else//已有同名备份文件存在
                    {
                        if ((System.Windows.MessageBox.Show("备份文件已存在，是否覆盖？", "数据备份", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes))//
                        {
                            File.Delete(filePath);//删除已存在的备份文件
                            File.Copy(dbPath, filePath);
                            System.Windows.MessageBox.Show(DalPrompt.DbBackupOK + @filePath);
                            result = true;
                        }
                    }
                }
                else//无数据文件
                {
                    MessageBox.Show("不存在需要备份的数据文件！");

                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            return result;
        }

        /// <summary>
        /// 还原所选择的备份文件到当前数据库
        /// </summary>
        /// <param name="dbPath"></param>
        /// <returns></returns>
        public static bool Restore(string filePath)
        {
            bool result = false;
            string dbPath = BaseDbSQLiteClass.DbFilePath;//数据文件绝对路径
            try
            {
                if (File.Exists(filePath) == true)//有备份文件
                {
                    if (File.Exists(dbPath))//有现有数据文件存在
                    {
                        if ((System.Windows.MessageBox.Show("备份数据将会覆盖现有数据，是否继续？", "数据还原", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes))//
                        {

                            File.Delete(dbPath);//删除已存在的备份文件
                            File.Copy(filePath, dbPath);
                            System.Windows.MessageBox.Show(DalPrompt.DbRestoreOK);
                            result = true;
                        }
                    }
                    else//无现有数据文件
                    {
                        File.Copy(filePath, dbPath);
                        System.Windows.MessageBox.Show(DalPrompt.DbRestoreOK);
                        result = true;
                    }
                }
                else//无数据文件
                {
                    MessageBox.Show("请选择一个需要恢复的数据备份文件！");

                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
            return result;
        }
    }
}
