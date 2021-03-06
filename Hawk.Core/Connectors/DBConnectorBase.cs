﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows.Controls.WpfPropertyGrid.Controls;
using System.Windows.Input;
using Hawk.Core.Utils;
using Hawk.Core.Utils.MVVM;
using Hawk.Core.Utils.Plugins;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Hawk.Core.Connectors
{
    /// <summary>
    ///     列信息
    /// </summary>
    public class ColumnInfo : PropertyChangeNotifier, IDictionarySerializable
    {
        #region Constructors and Destructors

        public ColumnInfo(string name)
        {
            Name = name;
            Importance = 1;
        }

        public ColumnInfo()
        {
        }

        #endregion

        #region Properties

        private string dataType;

        [DisplayName("类型")]
        [PropertyOrder(1)]
        [Description("该数据的类型")]
        public string DataType
        {
            get { return dataType; }
            set
            {
                if (dataType != value)
                {
                    dataType = value;
                    OnPropertyChanged("DataType");
                }
            }
        }

        [DisplayName("权重")]
        [PropertyOrder(3)]
        public double Importance { get; set; }

        [DisplayName("主键")]
        [PropertyOrder(2)]
        public bool IsKey { get; set; }


        [DisplayName("描述")]
        [PropertyOrder(4)]
        public string Description { get; set; }

        /// <summary>
        ///     启用虚拟化，则该值在需要时被动态计算
        /// </summary>
        [DisplayName("虚拟值")]
        public bool IsVirtual { get; set; }

        [DisplayName("名称")]
        [PropertyOrder(0)]
        public string Name { get; set; }
        [DisplayName("可空")]
        [PropertyOrder(0)]
        public bool CanNull { get; set; }

        #endregion

        #region Implemented Interfaces

        #region IDictionarySerializable

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            Importance = docu.Set("Importance", Importance);
            Name = docu.Set("Name", Name);
            DataType = docu.Set("DataType", DataType);
            IsKey = docu.Set("IsKey", IsKey);
            IsVirtual = docu.Set("IsVirtual", IsVirtual);
            IsVirtual = docu.Set("CanNull", CanNull);
            Description = docu.Set("Desc", Description);
        }

        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = new FreeDocument();
            dict.Add("Importance", Importance);
            dict.Add("Name", Name);
            dict.Add("DataType", DataType);
            dict.Add("IsKey", IsKey);
            dict.Add("IsVirtual", IsVirtual);
            dict.Add("Desc", Description);
            return dict;
        }

        #endregion

        #endregion
    }

    /// <summary>
    ///     数据表信息
    /// </summary>
    public class TableInfo : IDictionarySerializable
    {
        public TableInfo(string name, IDataBaseConnector connector)
        {
            Name = name;
            Connector = connector;
            ColumnInfos=new List<ColumnInfo>();
        }


        public TableInfo()
        {
            ColumnInfos=new List<ColumnInfo>();
        }

        [DisplayName("列特性")]
        public List<ColumnInfo> ColumnInfos { get; set; }

        [DisplayName("表大小")]
        public int Size { get; set; }

        [DisplayName("名称")]
        public string Name { get; set; }

        [DisplayName("描述")]
        public string Description { get; set; }

        [Browsable(false)]
        public IDataBaseConnector Connector { get; set; }

        public FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = new FreeDocument();
            dict.Add("Name", Name);
            dict.Add("Size", Size);
            dict.Add("Description", Description);
            dict.Children = new List<FreeDocument>();
            dict.Children = ColumnInfos.Select(d => d.DictSerialize()).ToList();
            return dict;
        }

        public void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            Name = docu.Set("Name", Name);
            Size = docu.Set("Size", Size);

            Description = docu.Set("Description", Description);
            var doc = docu as FreeDocument;
            if (doc != null && doc.Children != null)
            {


                foreach (FreeDocument item in doc.Children)
                {
                    var Column = new ColumnInfo();
                    Column.DictDeserialize(item);
                    ColumnInfos.Add(Column);
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public interface IEnumerableProvider<T>
    {
        IEnumerable<T> GetEnumerable(string tableName, Type type = null);

        bool CanSkip(string tableName);
    }

    public enum DBSearchStrategy
    {
     
        Contains,
        Match,
        Like,
        /// <summary>
        /// 首字母匹配
        /// </summary>
        Initials, 
    }
    public abstract class DBConnectorBase : PropertyChangeNotifier, IDataBaseConnector, IDictionarySerializable
    {
        #region Constructors and Destructors

        protected DBConnectorBase()
        {
            IsUseable = false;


            TableNames = new ExtendSelector<TableInfo>();
            AutoConnect = false;
        }

        protected virtual string Insert(IFreeDocument data, string dbTableName)
        {
            FreeDocument item = data.DictSerialize(Scenario.Database);
            var sb = new StringBuilder();
            foreach (var o in item)
            {
                string value;
                if (o.Value is DateTime)
                {
                    value = ((DateTime) o.Value).ToString("s");
                }
                else
                {
                    value = o.Value.ToString();
                }
                sb.Append($"'{value}',");
            }
            sb.Remove(sb.Length - 1, 1);
            string sql = string.Format("INSERT INTO {0} VALUES({1})", dbTableName, sb);
            return sql;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
                return TypeName;
            return Name;
        }


        public void SetObjects(IFreeDocument item, object[] value, string[] titles = null)
        {
            int index = 0;
            var values = new Dictionary<string, object>();
            if (item is IFreeDocument)
            {
                if (titles == null)
                {
                    foreach (object o in value)
                    {
                        values.Add(string.Format("Row{0}", index), value[index]);
                        index++;
                    }
                }
                else
                {
                    foreach (object o in value)
                    {
                        values.Add(titles[index], value[index]);
                        index++;
                    }
                }
            }
            else
            {
                foreach (string o in item.DictSerialize().Keys.OrderBy(d => d))
                {
                    values.Add(o, value[index]);
                    index++;
                }
            }

            item.DictDeserialize(values);
            ;
        }

        #endregion

        #region Properties

        private bool _IsUseable;
        private string name;

        [Category("参数设置")]
        [DisplayName("操作表名")]
        public ExtendSelector<TableInfo> TableNames { get; set; }

        [DisplayName("服务器地址")]
        [Category("1.连接管理")]
        [PropertyOrder(2)]
        public string Server { get; set; }

        [DisplayName("用户名")]
        [Category("1.连接管理")]
        [PropertyOrder(3)]
        public string UserName { get; set; }

        [DisplayName("密码")]
        [Category("1.连接管理")]
        [PropertyOrder(4)]
      //  [PropertyEditor("PasswordEditor")]
        public string Password { get; set; }

        [Category("参数设置")]
        [DisplayName("数据库类型")]
        public string TypeName
        {
            get
            {
                XFrmWorkAttribute item = AttributeHelper.GetCustomAttribute(GetType());
                if (item == null)
                {
                    return GetType().ToString();
                }
                return item.Name;
            }
        }

        public virtual void CreateDataBase(string dbname)
        {
            ExecuteNonQuery(string.Format("CREATE DATABASE {0}", dbname));
        }

        public virtual List<IFreeDocument> QueryEntities(string querySQL, out int count,
            string tablename = null, Type type = null)
        {
            count = 0;
            return new List<IFreeDocument>();
        }

        [Browsable(false)]
        public virtual string ConnectionString { get; set; }


        [Category("1.连接管理")]
        [DisplayName("数据库名称")]
        [PropertyOrder(5)]
        public string DBName { get; set; }


        public virtual bool CreateTable(IFreeDocument example, string name)
        {
            FreeDocument txt = example.DictSerialize(Scenario.Database);
            var sb = new StringBuilder();
            foreach (var o in txt)
            {
                sb.Append(o.Key);
                sb.AppendFormat(" {0},", DataTypeConverter.ToType(o.Value));
            }
            sb.Remove(sb.Length - 1, 1);
            string sql = $"CREATE TABLE {GetTableName(name)} ({sb})";
            ExecuteNonQuery(sql);
            RefreshTableNames();
            return true;
        }


        [Category("1.连接管理")]
        [PropertyOrder(4)]
        [DisplayName("连接状态")]
        public bool IsUseable
        {
            get { return _IsUseable; }
            protected set
            {
                if (_IsUseable != value)
                {
                    _IsUseable = value;
                    OnPropertyChanged("IsUseable");
                }
            }
        }


        public virtual List<IFreeDocument> TryFindEntities(string tableName, IDictionary<string, object> search
           , Type type = null, int count = -1, DBSearchStrategy searchStrategy = DBSearchStrategy.Contains)
        {
         
            return new List<IFreeDocument>();
        }

        [Category("1.连接管理")]
        [PropertyOrder(1)]
        [DisplayName("连接名称")]
        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        #endregion

        #region Implemented Interfaces

        #region IDataBaseConnector

        [Category("1.连接管理")]
        [PropertyOrder(5)]
        [DisplayName("自动连接")]
        public bool AutoConnect { get; set; }

        public virtual void BatchInsert(IEnumerable<IFreeDocument> source, string dbTableName)
        {
            throw new NotImplementedException();
        }

        public virtual bool CloseDB()
        {
            IsUseable = false;
            return true;
        }

        public virtual bool ConnectDB()
        {
            IsUseable = true;
            return true;
        }

        public virtual void DropTable(string tableName)
        {
            try
            {
                string sql = string.Format("DROP TABLE   {0}", GetTableName(tableName));
                ExecuteNonQuery(sql);
                RefreshTableNames();
            }
            catch (Exception ex)
            {
            }
        }


        public virtual IEnumerable<IFreeDocument> GetEntities(string tableName, Type type, int mount = -1,
            int skip = 0)
        {
            string sql = null;
            if (mount == 0)
            {
                sql = string.Format("Select * from {0}", GetTableName(tableName));
            }
            else
            {
                sql = string.Format("Select * from {0} LIMIT {1} OFFSET {2}", tableName, mount, skip);
            }


            DataTable data = GetDataTable(sql);
            return Table2Data(data, type);
        }


        public virtual List<TableInfo> RefreshTableNames()
        {
            DataTable items = GetDataTable("show tables");
            List<TableInfo> res =
                (from DataRow dr in items.Rows select new TableInfo(dr.ItemArray[0].ToString(), this)).ToList();
            TableNames.SetSource(res);
            return res;
        }

        public virtual void SaveOrUpdateEntity(
            IFreeDocument updateItem, string tableName,  IDictionary<string, object> keys,EntityExecuteType executeType=EntityExecuteType.InsertOrUpdate)
        {
            var sb = new StringBuilder();
            FreeDocument data = updateItem.DictSerialize(Scenario.Database);

            if (data.Count >= 1)
            {
                foreach (var val in data)
                {
                    sb.Append(String.Format(" {0} = '{1}',", val.Key, val.Value));
                }

                sb = sb.Remove(sb.Length - 1, 1);
            }

            try
            {
                ExecuteNonQuery(String.Format("update {0} set {1} where {2};", GetTableName(tableName), sb, ToString()));
            }

            catch
            {
            }
        }

        protected virtual void ConnectStringToOtherInfos()
        {
            try
            {
                var sqlConnBuilder = new SqlConnectionStringBuilder(ConnectionString);
                UserName = sqlConnBuilder.UserID;
                Password = sqlConnBuilder.Password;
                Server = sqlConnBuilder.DataSource;
            }
            catch (Exception)
            {
            }
        }

        protected virtual string GetConnectString()
        {
            var sqlConnBuilder = new SqlConnectionStringBuilder
            {
                DataSource = Server,
                UserID = UserName,
                Password = Password,
                InitialCatalog = DBName
            };
            return sqlConnBuilder.ConnectionString;
        }

        protected List<IFreeDocument> Table2Data(DataTable data, Type type)
        {
            var result = new List<IFreeDocument>();
            string[] titles = (from object column in data.Columns select column.ToString()).ToArray();
            foreach (DataRow dr in data.Rows)
            {
                var data2 = Activator.CreateInstance(type) as IFreeDocument;


                SetObjects(data2, dr.ItemArray, titles);
                result.Add(data2);
            }
            return result;
        }

        protected virtual string GetTableName(string tableName)
        {
            return tableName.Replace(" ", "");
        }

        protected virtual DataTable GetDataTable(string sql)
        {
            return new DataTable();
        }

        protected virtual int ExecuteNonQuery(string sql)
        {
            return 0;
        }

        #endregion

        #endregion

        [DisplayName("执行")]
        [PropertyOrder(20)]
        public ReadOnlyCollection<ICommand> Commands
        {
            get
            {
                return CommandBuilder.GetCommands(
                    this,
                    new[]
                    {
                        new Command("连接数据库", obj =>
                        {
                              ConnectDB();
                            if (IsUseable)
                            {
                                RefreshTableNames();
                            }
                        }, obj => IsUseable == false),
                        new Command("关闭连接", obj => CloseDB(), obj => IsUseable),
                        new Command("创建新库", obj => CreateDataBase(DBName), obj => string.IsNullOrEmpty(DBName) == false)
                    });
            }
        }

        public virtual FreeDocument DictSerialize(Scenario scenario = Scenario.Database)
        {
            var dict = new FreeDocument();
            dict.Add("DBName", DBName);
            dict.Add("Name", Name);

            dict.Add("TypeName",this.GetType().Name );
            dict.Add("ConnectString", ConnectionString);
            dict.Add("AutoConnect", AutoConnect);
            return dict;
        }


        public virtual void DictDeserialize(IDictionary<string, object> docu, Scenario scenario = Scenario.Database)
        {
            DBName = docu.Set("DBName", DBName);
            Name = docu.Set("Name", Name);
            AutoConnect = docu.Set("AutoConnect", AutoConnect);
            ConnectionString = docu.Set("ConnectString", ConnectionString);

            ConnectStringToOtherInfos();
        }
    }
}