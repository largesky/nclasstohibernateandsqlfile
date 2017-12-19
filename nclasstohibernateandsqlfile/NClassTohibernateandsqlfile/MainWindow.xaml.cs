using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace NClassTohibernateandsqlfile
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private string dllname = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
                if (ofd.ShowDialog().Value == false)
                {
                    return;
                }
                Assembly a = Assembly.LoadFrom(ofd.FileName);
                dllname = a.GetName().Name;
                this.tv1.ItemsSource = a.GetTypes().OrderBy(obj => obj.FullName).Select(obj => new TreeViewNode { IsChecked = false, IsClass = true, SubNodes = new List<TreeViewNode>(), Title = obj.FullName, Type = obj });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private XDocument GetDefaultDocument()
        {
            XDocument xdoc = XDocument.Parse("<hibernate-mapping xmlns=\"urn:nhibernate-mapping-2.2\"/>");
            return xdoc;
        }

        private void btnGen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.cbbDBType.SelectedIndex == 0)
                {
                    this.GenMsSqlFile();
                }
                else
                {
                    this.GenMySqlFile();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void tv1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                var tn = e.NewValue as TreeViewNode;
                if (tn == null)
                {
                    return;
                }
                if (this.cbbDBType.SelectedIndex == 0)
                {
                    this.GenMsSql(tn);
                }
                else
                {
                    this.GenMySql(tn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



        #region MS SQL 

        private Dictionary<string, string> GetMsSqlMap(Type t)
        {
            var ptcs = new Dictionary<string, string>();
            var properties = t.GetProperties();
            foreach (var p in properties)
            {
                string type = "";
                var pc = Type.GetTypeCode(p.PropertyType);
                if (pc == TypeCode.Object)
                {

                }
                else if (pc == TypeCode.Single)
                {
                    type = "float";
                }
                else if (pc == TypeCode.DateTime)
                {
                    type = "datetime";
                }
                else if (pc == TypeCode.Boolean)
                {
                    type = "bit";
                }
                else if (pc == TypeCode.String)
                {
                    type = "nvarchar";
                }
                else if (pc == TypeCode.Int16 || pc == TypeCode.Int32 || pc == TypeCode.UInt16 || pc == TypeCode.UInt32)
                {
                    type = "int";
                }
                else if (pc == TypeCode.UInt64 || pc == TypeCode.Int64)
                {
                    type = "bigint";
                }
                else
                {
                    throw new Exception("类型：" + t.FullName + "字段：" + p.Name + "类型：" + pc + "无法转换");
                }

                if (string.IsNullOrWhiteSpace(type))
                {
                    continue;
                }

                ptcs.Add(p.Name, type);


            }
            return ptcs;
        }

        private void GenMsSql(TreeViewNode tn)
        {
            var t = tn.Type;
            var xdoc = GetDefaultDocument();
            xdoc.Root.SetAttributeValue("assembly", dllname);
            xdoc.Root.SetAttributeValue("namespace", t.Namespace);
            var xe = new XElement("class", new XAttribute("name", t.Name), new XAttribute("table", t.Name), new XAttribute("lazy", "false"));

            var properties = GetMsSqlMap(t);
            foreach (var p in properties)
            {
                if (p.Key != "Id")
                {
                    var xp = new XElement("property", new XAttribute("name", p.Key),
                        new XElement("column", new XAttribute("column", p.Key), new XAttribute("sql-type", p.Value), new XAttribute("not-null", "true")));
                    xe.Add(xp);
                }
                else
                {
                    var xp = new XElement("id", new XAttribute("name", p.Key), new XAttribute("column", "Id"),
                        new XElement("generator", new XAttribute("class", "identity")));
                    xe.Add(xp);
                }
            }
            xdoc.Root.Add(xe);
            this.tbNHibernate.Text = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + xdoc.ToString();
        }

        private void GenMsSqlFile()
        {
            string sqlAll = "";
            string dir = this.tbDir.Text.Trim();
            var selected = this.tv1.Items.OfType<TreeViewNode>().Where(obj => obj.IsChecked).ToList();
            if (selected.Count < 1)
            {
                throw new Exception("没有选择数据");
            }

            foreach (var t in selected.Select(obj => obj.Type).ToArray())
            {
                string sqlFields = "";
                var xdoc = GetDefaultDocument();
                xdoc.Root.SetAttributeValue("assembly", dllname);
                xdoc.Root.SetAttributeValue("namespace", t.Namespace);
                var xe = new XElement("class", new XAttribute("name", t.Name), new XAttribute("table", t.Name), new XAttribute("lazy", "false"));
                var ptcs = GetMsSqlMap(t);
                foreach (var p in ptcs.ToArray())
                {
                    if (p.Key == "Id")
                    {
                        sqlFields += "[Id] [bigint] IDENTITY(1,1) NOT NULL," + Environment.NewLine;
                    }
                    else if (p.Value == "nvarchar")
                    {
                        sqlFields += "[" + p.Key + "] [" + p.Value + "](500)  NOT NULL," + Environment.NewLine;
                    }
                    else
                    {
                        sqlFields += "[" + p.Key + "] [" + p.Value + "] NOT NULL," + Environment.NewLine;
                    }


                    if (p.Key != "Id")
                    {
                        var xp = new XElement("property", new XAttribute("name", p.Key),
                            new XElement("column", new XAttribute("name", p.Key), new XAttribute("sql-type", p.Value), new XAttribute("not-null", "true")));
                        xe.Add(xp);
                    }
                    else
                    {
                        var xp = new XElement("id", new XAttribute("name", p.Key), new XAttribute("column", "Id"),
                            new XElement("generator", new XAttribute("class", "identity")));
                        xe.Add(xp);
                    }

                }

                string sql = "CREATE TABLE[dbo].[" + t.Name + "](" + Environment.NewLine + sqlFields + Environment.NewLine + " CONSTRAINT [PK_" + t.Name + "] PRIMARY KEY CLUSTERED ([Id] ASC)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]) ON[PRIMARY]";
                sqlAll += sql + Environment.NewLine;

                xdoc.Root.Add(xe);
                //HBM FILES
                string filePath = dir + "\\" + t.Name + ".hbm.xml";
                File.WriteAllText(filePath, "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + xdoc.ToString().Replace("xmlns=\"\"", ""));
            }
            File.WriteAllText(dir + "\\SQL.sql", sqlAll);
            MessageBox.Show("已生成数据");
        }

        #endregion


        #region MY SQL 

        private Dictionary<string, string> GetMySqlMap(Type t)
        {
            var ptcs = new Dictionary<string, string>();
            var properties = t.GetProperties();
            foreach (var p in properties)
            {
                string type = "";
                var pc = Type.GetTypeCode(p.PropertyType);
                if (pc == TypeCode.Object)
                {

                }
                else if (pc == TypeCode.Single || pc == TypeCode.Double)
                {
                    type = "float";
                }
                else if (pc == TypeCode.DateTime)
                {
                    type = "datetime";
                }
                else if (pc == TypeCode.Boolean)
                {
                    type = "boolean";
                }
                else if (pc == TypeCode.String)
                {
                    type = "string";
                }
                else if (pc == TypeCode.Int16 || pc == TypeCode.Int32 || pc == TypeCode.UInt16 || pc == TypeCode.UInt32)
                {
                    type = "int";
                }
                else if (pc == TypeCode.UInt64 || pc == TypeCode.Int64)
                {
                    type = "bigint(20)";
                }
                else
                {
                    throw new Exception("类型：" + t.FullName + "字段：" + p.Name + "类型：" + pc + "无法转换");
                }

                if (string.IsNullOrWhiteSpace(type))
                {
                    continue;
                }

                ptcs.Add(p.Name, type);


            }
            return ptcs;
        }

        private void GenMySql(TreeViewNode tn)
        {
            var t = tn.Type;
            var xdoc = GetDefaultDocument();
            xdoc.Root.SetAttributeValue("assembly", dllname);
            xdoc.Root.SetAttributeValue("namespace", t.Namespace);
            var xe = new XElement("class", new XAttribute("name", t.Name), new XAttribute("table", t.Name.ToLower()), new XAttribute("lazy", "false"));

            var properties = GetMySqlMap(t);
            foreach (var p in properties)
            {
                if (p.Key != "Id")
                {
                    var xp = new XElement("property", new XAttribute("name", p.Key),
                        new XElement("column", new XAttribute("column", p.Key), new XAttribute("sql-type", p.Value), new XAttribute("not-null", "true")));
                    xe.Add(xp);
                }
                else
                {
                    var xp = new XElement("id", new XAttribute("name", p.Key), new XAttribute("column", "Id"),
                        new XElement("generator", new XAttribute("class", "identity")));
                    xe.Add(xp);
                }
            }
            xdoc.Root.Add(xe);
            this.tbNHibernate.Text = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + xdoc.ToString();
        }

        private void GenMySqlFile()
        {
            string sqlAll = "";
            string dir = this.tbDir.Text.Trim();
            var selected = this.tv1.Items.OfType<TreeViewNode>().Where(obj => obj.IsChecked).ToList();
            if (selected.Count < 1)
            {
                throw new Exception("没有选择数据");
            }

            foreach (var t in selected.Select(obj => obj.Type).ToArray())
            {
                string sqlFields = "";
                var xdoc = GetDefaultDocument();
                xdoc.Root.SetAttributeValue("assembly", dllname);
                xdoc.Root.SetAttributeValue("namespace", t.Namespace);
                var xe = new XElement("class", new XAttribute("name", t.Name), new XAttribute("table", t.Name.ToLower()), new XAttribute("lazy", "false"));
                var ptcs = GetMySqlMap(t);
                foreach (var p in ptcs.ToArray())
                {
                    if (p.Key == "Id")
                    {
                        sqlFields += "`Id` bigint(20)  NOT NULL AUTO_INCREMENT," + Environment.NewLine;
                    }
                    else if (p.Value == "string")
                    {
                        sqlFields += "`" + p.Key + "` " + "varchar (500)  NOT NULL," + Environment.NewLine;
                    }
                    else
                    {
                        sqlFields += "`" + p.Key + "` " + p.Value + " NOT NULL," + Environment.NewLine;
                    }


                    if (p.Key != "Id")
                    {
                        var xp = new XElement("property", new XAttribute("name", p.Key),
                            new XElement("column", new XAttribute("name", p.Key), new XAttribute("sql-type", p.Value), new XAttribute("not-null", "true")));
                        xe.Add(xp);
                    }
                    else
                    {
                        var xp = new XElement("id", new XAttribute("name", p.Key), new XAttribute("column", "Id"),
                            new XElement("generator", new XAttribute("class", "identity")));
                        xe.Add(xp);
                    }
                }

                string sql = "CREATE TABLE `" + t.Name.ToLower() + "`(" + Environment.NewLine + sqlFields + Environment.NewLine + "   PRIMARY KEY (`Id`) " + Environment.NewLine + ") ENGINE = InnoDB AUTO_INCREMENT = 1 DEFAULT CHARSET = utf8";
                sqlAll += sql + ";" + Environment.NewLine;

                xdoc.Root.Add(xe);
                //HBM FILES
                string filePath = dir + "\\" + t.Name + ".hbm.xml";
                File.WriteAllText(filePath, "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + xdoc.ToString().Replace("xmlns=\"\"", ""));
            }
            File.WriteAllText(dir + "\\SQL.sql", sqlAll);
            MessageBox.Show("已生成数据");
        }

        #endregion

    }
}
