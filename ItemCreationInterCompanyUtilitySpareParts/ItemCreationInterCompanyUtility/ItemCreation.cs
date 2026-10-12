using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using SAPbobsCOM;
using ExcelDataReader;
using ItemCreationInterCompanyUtilitySpareParts;
using System.Net.Mail;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace ItemCreationInterCompanyUtility
{
    public partial class FrmCompanySelection : Form
    {

        #region Variable Declaration

        static string gdbName = string.Empty;
        string sql = string.Empty;
        bool message = false;
        DataTable grdCmpdt = null;
        string oPath = string.Empty;
        public static SAPbobsCOM.Company oCompany = null;
        public bool flag = false;

        // In-memory item cache: no DB round trip when the user picks a code or name
        private readonly Dictionary<string, string> codeToName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> nameToCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Manufacturer name -> FirmCode (replaces the ValueMember hack)
        private readonly Dictionary<string, int> manufNameToCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Guards
        private bool _syncing = false;   // stops Item Code <-> Item Name ping-pong
        private bool _busy = false;      // stops double clicks while work is running

        private static readonly object _logLock = new object();
        private static DataSet _settingsCache = null;

        DataTableCollection DtCollect;
        WaitFormFunc waitForm = new WaitFormFunc();

        #endregion

        #region Helper types

        private class ItemInput
        {
            public string ItemCode = "";
            public string ItemName = "";
            public string ItemGroup = "";
            public string ValuationMethod = "";
            public string SubCategory = "";
            public string ItemCategory = "";
            public string ModelYear = "";
            public string HsnCode = "";
            public string InvUoM = "";
            public string SalUoM = "";
            public string PurUoM = "";
            public string ItemStatus = "";
            public int? ManufacturerCode;
            public double? DealerPrice;
            public double? MrpPrice;
        }

        private class CompanyTarget
        {
            public string DataBase = "";
            public string ServerIP = "";
            public string DBServer = "";
            public string DBType = "";
            public string InvItm = "";
            public string SalItm = "";
            public string PurItm = "";
        }

        private class ProcessResult
        {
            public Dictionary<string, List<KeyValuePair<string, string>>> Success =
                new Dictionary<string, List<KeyValuePair<string, string>>>();
            public List<string> Errors = new List<string>();
        }

        private class StockCheckResult
        {
            public int Row;
            public DataTable Data;
        }

        #endregion

        #region HANA SQL Connection String

        private static string gSQLConnectionString
        {
            get
            {
                string gConString = "Data Source=" + clsConnection.sqlServer
                                  + ";Initial Catalog=" + gdbName
                                  + ";Persist Security Info=False;User ID=" + clsConnection.sqlUser
                                  + ";pwd=" + clsConnection.sqlPassword;
                return gConString;
            }
        }

        #endregion

        public FrmCompanySelection()
        {
            InitializeComponent();
            cmdMulItmCreat.SelectedIndex = 1;
        }

        #region Small utilities

        /// <summary>Escapes a value for use inside a single-quoted SQL literal.</summary>
        private static string Q(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }

        private static string Cell(DataGridView grid, string column, int row)
        {
            if (!grid.Columns.Contains(column))
                throw new Exception("Column '" + column + "' was not found.");
            object v = grid[column, row].Value;
            return (v == null || v == DBNull.Value) ? string.Empty : v.ToString().Trim();
        }

        private static double? ParsePrice(string text, string label)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            double d;
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out d) ||
                double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                return d;
            throw new Exception(label + " '" + text + "' is not a valid number.");
        }

        private static void ReleaseCom(object o)
        {
            try
            {
                if (o != null && Marshal.IsComObject(o))
                    Marshal.ReleaseComObject(o);
            }
            catch { }
        }

        /// <summary>
        /// Runs work on a background STA thread. SAP DI API is COM, so all DI API
        /// calls for one run stay on this single thread while the UI stays responsive.
        /// </summary>
        private static Task<T> RunOnStaThread<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            var thread = new Thread(() =>
            {
                try { tcs.SetResult(func()); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            return tcs.Task;
        }

        #endregion

        #region Form Load

        private void FrmCompanySelection_Load(object sender, EventArgs e)
        {
            try
            {
                // 1. Items: ONE query, cached in dictionaries, bulk-added to the combos
                sql = "select \"ItemCode\", \"ItemName\" from OITM order by \"ItemCode\"";
                DataTable dtItems = SqlHelper.Hana_ADOConnection(sql);

                codeToName.Clear();
                nameToCode.Clear();
                foreach (DataRow r in dtItems.Rows)
                {
                    string code = r["ItemCode"].ToString();
                    string name = r["ItemName"].ToString();
                    codeToName[code] = name;
                    if (!nameToCode.ContainsKey(name))   // duplicate names: keep the first code
                        nameToCode[name] = code;
                }

                cmbItmCd.BeginUpdate();
                cmbItmCd.Items.Clear();
                cmbItmCd.Items.AddRange(codeToName.Keys.ToArray<object>());
                cmbItmCd.EndUpdate();

                cmbItmNm.BeginUpdate();
                cmbItmNm.Items.Clear();
                cmbItmNm.Items.AddRange(nameToCode.Keys.OrderBy(n => n).ToArray<object>());
                cmbItmNm.EndUpdate();

                // 2. Item groups
                sql = @"SELECT DISTINCT T1.""ItmsGrpNam"" 
                        FROM ""OITB"" T1 
                        INNER JOIN ""OITM"" T0 ON T1.""ItmsGrpCod"" = T0.""ItmsGrpCod"" 
                        WHERE T0.""U_ItemCategory"" = 'Spare Parts'";
                DataTable dtGroups = SqlHelper.Hana_ADOConnection(sql);
                cmbItmGrp.BeginUpdate();
                foreach (DataRow drow in dtGroups.Rows)
                    cmbItmGrp.Items.Add(drow[0].ToString());
                cmbItmGrp.EndUpdate();

                cmbValMtd.Text = "Serial/Batch";
                fillgrid();

                // 3. Manufacturers (name -> code kept in a dictionary)
                DataTable dtManuf = SqlHelper.Hana_ADOConnection("select \"FirmCode\",\"FirmName\" from OMRC");
                manufNameToCode.Clear();
                cmbManuf.BeginUpdate();
                foreach (DataRow row in dtManuf.Rows)
                {
                    string name = row[1].ToString();
                    int code;
                    if (int.TryParse(row[0].ToString(), out code) && !manufNameToCode.ContainsKey(name))
                        manufNameToCode[name] = code;
                    cmbManuf.Items.Add(name);
                }
                cmbManuf.EndUpdate();

                txtModlYear.Enabled = false;
                cmbItmCd.Enabled = true;
                cmbItmNm.Enabled = true;
                cmbItmGrp.Enabled = false;
                cmbValMtd.Enabled = false;
                txtDlrPrc.Enabled = false;
                txtMRPPrc.Enabled = false;
                txtInvUoM.Enabled = false;
                txtSalUoM.Enabled = false;
                txtPurUoM.Enabled = false;
                cmbManuf.Enabled = false;
                button1.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void fillgrid()
        {
            sql = "select \"U_ServerIP\" as \"DBServer\", \"U_LiServerIP\" as \"ServerIP\",\"U_DBType\" as \"DBType\",\"U_cmpName\" as \"Company\" ,\"U_dbName\" as \"DataBase\" from \"@INTRCOMPITEMCR\" ";
            grdCmpdt = SqlHelper.Hana_ADOConnection(sql);

            dataGridView1.DataSource = grdCmpdt;
            dataGridView1.Columns["Company"].ReadOnly = true;
            dataGridView1.Columns["DataBase"].ReadOnly = true;
            dataGridView1.AllowUserToAddRows = false;

            DataGridViewComboBoxColumn colInv = new DataGridViewComboBoxColumn();
            colInv.HeaderText = "Inventory Item";
            colInv.Name = "InvItm";
            colInv.Items.Add("YES");
            colInv.Items.Add("NO");
            dataGridView1.Columns.Insert(5, colInv);

            DataGridViewComboBoxColumn colSal = new DataGridViewComboBoxColumn();
            colSal.HeaderText = "Sales Item";
            colSal.Name = "SalItm";
            colSal.Items.Add("YES");
            colSal.Items.Add("NO");
            dataGridView1.Columns.Insert(6, colSal);

            DataGridViewComboBoxColumn colPur = new DataGridViewComboBoxColumn();
            colPur.HeaderText = "Purchase Item";
            colPur.Name = "PurItm";
            colPur.Items.Add("YES");
            colPur.Items.Add("NO");
            dataGridView1.Columns.Insert(7, colPur);

            DataGridViewComboBoxColumn colFound = new DataGridViewComboBoxColumn();
            colFound.HeaderText = "Record Found";
            colFound.Name = "RecFound";
            colFound.Items.Add("YES");
            colFound.Items.Add("NO");
            dataGridView1.Columns.Insert(8, colFound);

            DataGridViewTextBoxColumn colStock = new DataGridViewTextBoxColumn();
            colStock.HeaderText = "Current Stock";
            colStock.Name = "CurStock";
            colStock.ReadOnly = true;
            colStock.DefaultCellStyle.Format = "N0";
            dataGridView1.Columns.Insert(9, colStock);

            DataGridViewCheckBoxColumn colSel = new DataGridViewCheckBoxColumn();
            colSel.HeaderText = "Selected";
            colSel.Width = 60;
            colSel.Name = "Selected";
            dataGridView1.Columns.Insert(10, colSel);

            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                dataGridView1["InvItm", i].Value = "YES";
                dataGridView1["SalItm", i].Value = "YES";
                dataGridView1["PurItm", i].Value = "YES";
                dataGridView1["Selected", i].Value = 1;
            }
        }

        #endregion

        #region Create / Update button

        private async void button1_Click(object sender, EventArgs e)
        {
            if (_busy) return;
            if (!validation()) return;

            message = false;
            if (!isCompanySelected())
            {
                if (!message)
                    MessageBox.Show("Please select at least one company", "Error", MessageBoxButtons.OK);
                return;
            }

            if (!(cmbCat.Text == "Spare Parts" || cmbCat.Text == ""))
                return;

            // Read everything from the UI on the UI thread, then hand plain data to the worker
            List<CompanyTarget> companies;
            List<ItemInput> items;
            try
            {
                companies = GetSelectedCompanies();
                items = cmdMulItmCreat.Text == "Yes"
                    ? ReadBulkItems()
                    : new List<ItemInput> { ReadSingleItem() };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (items.Count == 0)
            {
                MessageBox.Show("There are no items to process.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _busy = true;
            Control btn = sender as Control;
            if (btn != null) btn.Enabled = false;
            UseWaitCursor = true;
            waitForm.Show(this);

            ProcessResult result = null;
            try
            {
                result = await RunOnStaThread(() => ProcessItems(companies, items));
            }
            catch (Exception ex)
            {
                WriteLogFile("PROCESS ERROR: " + ex);
                MessageBox.Show(ex.Message);
                return;
            }
            finally
            {
                waitForm.Close();
                UseWaitCursor = false;
                if (btn != null) btn.Enabled = true;
                _busy = false;
            }

            int successCount = result.Success.Sum(x => x.Value.Count);
            flag = successCount > 0 && result.Errors.Count == 0;

            if (successCount > 0)
            {
                DataSet dsEmail = LoadCompanySetting();
                await Task.Run(() => SendSummaryEmail(result.Success, result.Errors, dsEmail));
            }

            if (result.Errors.Count == 0)
            {
                MessageBox.Show("Operation completed successfully!", this.Text, MessageBoxButtons.OK);
            }
            else
            {
                string errText = string.Join(Environment.NewLine, result.Errors.Take(15));
                if (result.Errors.Count > 15)
                    errText += Environment.NewLine + "... and " + (result.Errors.Count - 15) + " more (see log file).";
                MessageBox.Show(
                    "Operation completed with errors.\n\nSucceeded: " + successCount +
                    "\nFailed: " + result.Errors.Count + "\n\n" + errText,
                    this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private List<CompanyTarget> GetSelectedCompanies()
        {
            var list = new List<CompanyTarget>();
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                object sel = dataGridView1["Selected", i].Value;
                if (sel == null || Convert.ToInt32(sel) <= 0) continue;

                list.Add(new CompanyTarget
                {
                    DataBase = Cell(dataGridView1, "DataBase", i),
                    ServerIP = Cell(dataGridView1, "ServerIP", i),
                    DBServer = Cell(dataGridView1, "DBServer", i),
                    DBType = Cell(dataGridView1, "DBType", i),
                    InvItm = Cell(dataGridView1, "InvItm", i),
                    SalItm = Cell(dataGridView1, "SalItm", i),
                    PurItm = Cell(dataGridView1, "PurItm", i)
                });
            }
            return list;
        }

        private ItemInput ReadSingleItem()
        {
            int manufCode;
            return new ItemInput
            {
                ItemCode = cmbItmCd.Text.Trim(),
                ItemName = cmbItmNm.Text.Trim(),
                ItemGroup = cmbItmGrp.Text.Trim(),
                ValuationMethod = cmbValMtd.Text.Trim(),
                SubCategory = cmbSubCat.ValueMember,
                ItemCategory = cmbCat.Text.Trim(),
                ModelYear = txtModlYear.Text,
                HsnCode = txtHSNCode.Text.Trim(),
                InvUoM = txtInvUoM.Text.Trim(),
                SalUoM = txtSalUoM.Text.Trim(),
                PurUoM = txtPurUoM.Text.Trim(),
                ItemStatus = cmbItmStatus.Text.Trim(),
                ManufacturerCode = manufNameToCode.TryGetValue(cmbManuf.Text.Trim(), out manufCode) ? (int?)manufCode : null,
                DealerPrice = ParsePrice(txtDlrPrc.Text, "Dealer price"),
                MrpPrice = ParsePrice(txtMRPPrc.Text, "MRP price")
            };
        }

        private List<ItemInput> ReadBulkItems()
        {
            var list = new List<ItemInput>();
            for (int ii = 0; ii < dataGridView2.Rows.Count; ii++)
            {
                if (dataGridView2.Rows[ii].IsNewRow) continue;

                string code = Cell(dataGridView2, "ItemCode", ii);
                if (code == "") continue;   // skip blank Excel rows

                int manuf;
                string manufText = Cell(dataGridView2, "Manufacturer", ii);
                string rowLabel = "Row " + (ii + 1) + " (" + code + ")";

                list.Add(new ItemInput
                {
                    ItemCode = code,
                    ItemName = Cell(dataGridView2, "ItemDescription", ii),
                    ItemGroup = Cell(dataGridView2, "ItemGrp", ii),
                    ValuationMethod = Cell(dataGridView2, "ValuationMth", ii),
                    SubCategory = Cell(dataGridView2, "Category", ii),
                    ItemCategory = "Spare Parts",
                    ModelYear = Cell(dataGridView2, "ModelYear", ii),
                    HsnCode = Cell(dataGridView2, "HSNCode", ii),
                    InvUoM = Cell(dataGridView2, "InventoryUOM", ii),
                    SalUoM = Cell(dataGridView2, "SalesUOM", ii),
                    PurUoM = Cell(dataGridView2, "PurchaseUOM", ii),
                    ItemStatus = Cell(dataGridView2, "ItemStatus", ii),
                    ManufacturerCode = int.TryParse(manufText, out manuf) ? (int?)manuf : null,
                    DealerPrice = ParsePrice(Cell(dataGridView2, "DealerPrice", ii), rowLabel + " dealer price"),
                    MrpPrice = ParsePrice(Cell(dataGridView2, "MRPPrice", ii), rowLabel + " MRP price")
                });
            }
            return list;
        }

        #endregion

        #region Background processing (DI API)

        private ProcessResult ProcessItems(List<CompanyTarget> companies, List<ItemInput> items)
        {
            var result = new ProcessResult();

            foreach (CompanyTarget company in companies)
            {
                if (!Connect(company.DataBase, company.ServerIP, company.DBServer))
                {
                    result.Errors.Add("Could not connect to company " + company.DataBase);
                    continue;
                }

                try
                {
                    if (!result.Success.ContainsKey(company.DataBase))
                        result.Success.Add(company.DataBase, new List<KeyValuePair<string, string>>());

                    foreach (ItemInput item in items)
                    {
                        string error;
                        try
                        {
                            if (SaveItem(company, item, out error))
                            {
                                result.Success[company.DataBase].Add(new KeyValuePair<string, string>(item.ItemCode, item.ItemName));
                                WriteLogFile("Item saved successfully ItemCode:" + item.ItemCode + ", Company:" + company.DataBase);
                            }
                            else
                            {
                                result.Errors.Add(company.DataBase + " / " + item.ItemCode + ": " + error);
                                WriteLogFile("Error in Item Add/Update ItemCode:" + item.ItemCode + ", Company:" + company.DataBase + " & error is: " + error);
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add(company.DataBase + " / " + item.ItemCode + ": " + ex.Message);
                            WriteLogFile("Exception ItemCode:" + item.ItemCode + ", Company:" + company.DataBase + " : " + ex);
                        }
                    }
                }
                finally
                {
                    DisconnectCompany();
                }
            }

            return result;
        }

        private bool SaveItem(CompanyTarget company, ItemInput item, out string error)
        {
            error = null;
            SAPbobsCOM.Items oItem = null;
            try
            {
                oItem = (SAPbobsCOM.Items)oCompany.GetBusinessObject(BoObjectTypes.oItems);
                bool exists = oItem.GetByKey(item.ItemCode);

                if (!exists)
                {
                    int groupCode;
                    if (!TryGetItemGroupCode(item.ItemGroup, out groupCode))
                    {
                        error = "Item group '" + item.ItemGroup + "' not found in this company.";
                        return false;
                    }
                    oItem.ItemCode = item.ItemCode;
                    oItem.ItemsGroupCode = groupCode;
                }

                ApplyItemFields(oItem, item, company);

                int retval = exists ? oItem.Update() : oItem.Add();
                if (retval != 0)
                {
                    int errCode;
                    string errMsg;
                    oCompany.GetLastError(out errCode, out errMsg);
                    error = errCode + " - " + errMsg;
                    return false;
                }
            }
            finally
            {
                ReleaseCom(oItem);
            }

            UpdatePriceList(item.ItemCode, item.DealerPrice, item.MrpPrice);
            return true;
        }

        private void ApplyItemFields(SAPbobsCOM.Items oItem, ItemInput item, CompanyTarget company)
        {
            oItem.ItemName = item.ItemName;
            oItem.InventoryUOM = item.InvUoM;
            oItem.SalesUnit = item.SalUoM;
            oItem.PurchaseUnit = item.PurUoM;
            oItem.ForeignName = item.HsnCode;

            if (company.DBType == "SHOWROOM")
            {
                switch (item.ValuationMethod)
                {
                    case "FIFO":
                        oItem.CostAccountingMethod = BoInventorySystem.bis_FIFO;
                        break;
                    case "Standard":
                        oItem.CostAccountingMethod = BoInventorySystem.bis_Standard;
                        break;
                    case "Moving Average":
                        oItem.CostAccountingMethod = BoInventorySystem.bis_MovingAverage;
                        break;
                }
                if (!string.IsNullOrEmpty(item.SubCategory))
                    oItem.UserFields.Fields.Item("U_Categeory").Value = item.SubCategory;
            }
            else
            {
                oItem.UserFields.Fields.Item("U_Categeory").Value = "2";
            }

            if (item.ManufacturerCode.HasValue)
                oItem.Manufacturer = item.ManufacturerCode.Value;

            oItem.UserFields.Fields.Item("U_ItemCategory").Value = item.ItemCategory;
            oItem.UserFields.Fields.Item("U_ModelYear").Value = item.ModelYear;

            oItem.InventoryItem = company.InvItm == "YES" ? BoYesNoEnum.tYES : BoYesNoEnum.tNO;
            oItem.SalesItem = company.SalItm == "YES" ? BoYesNoEnum.tYES : BoYesNoEnum.tNO;
            oItem.PurchaseItem = company.PurItm == "YES" ? BoYesNoEnum.tYES : BoYesNoEnum.tNO;

            ApplyItemStatus(oItem, item.ItemStatus);
        }

        private static void ApplyItemStatus(SAPbobsCOM.Items oItem, string status)
        {
            if (status == "In Active")
            {
                oItem.Valid = BoYesNoEnum.tNO;
                oItem.ValidTo = DateTime.Today.AddDays(-1);          // active until yesterday

                oItem.Frozen = BoYesNoEnum.tYES;
                oItem.FrozenFrom = DateTime.Today;                   // inactive from today
                oItem.FrozenTo = new DateTime(2099, 12, 31);
            }
            else if (status == "Active")
            {
                oItem.Frozen = BoYesNoEnum.tNO;
                oItem.FrozenFrom = DateTime.MinValue;
                oItem.FrozenTo = DateTime.MinValue;

                oItem.Valid = BoYesNoEnum.tYES;                      // active forever
                oItem.ValidFrom = DateTime.MinValue;
                oItem.ValidTo = DateTime.MinValue;
            }
        }

        private bool TryGetItemGroupCode(string groupName, out int groupCode)
        {
            groupCode = 0;
            SAPbobsCOM.Recordset rs = null;
            try
            {
                rs = (SAPbobsCOM.Recordset)oCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery("select \"ItmsGrpCod\" from OITB where \"ItmsGrpNam\" = '" + Q(groupName) + "'");
                if (rs.RecordCount == 0) return false;
                return int.TryParse(rs.Fields.Item("ItmsGrpCod").Value.ToString(), out groupCode);
            }
            finally
            {
                ReleaseCom(rs);
            }
        }

        #endregion

        #region Update Price List

        /// <summary>Price list line 0 = Dealer price, line 1 = MRP (same as before).</summary>
        private void UpdatePriceList(string itemCode, double? dealerPrice, double? mrpPrice)
        {
            if (!dealerPrice.HasValue && !mrpPrice.HasValue) return;

            SAPbobsCOM.Items items = null;
            try
            {
                items = (SAPbobsCOM.Items)oCompany.GetBusinessObject(BoObjectTypes.oItems);
                if (!items.GetByKey(itemCode))
                {
                    WriteLogFile("Error in PriceList updation: item " + itemCode + " not found after save.");
                    return;
                }

                if (dealerPrice.HasValue)
                {
                    items.PriceList.SetCurrentLine(0);
                    items.PriceList.Price = dealerPrice.Value;
                }
                if (mrpPrice.HasValue && items.PriceList.Count > 1)
                {
                    items.PriceList.SetCurrentLine(1);
                    items.PriceList.Price = mrpPrice.Value;
                }

                if (items.Update() != 0)
                {
                    int errCode;
                    string errMsg;
                    oCompany.GetLastError(out errCode, out errMsg);
                    WriteLogFile("Error in PriceList updation ItemCode:" + itemCode + " : " + errMsg);
                }
            }
            finally
            {
                ReleaseCom(items);
            }
        }

        #endregion

        #region Connect / Disconnect

        private bool Connect(string CompanyDb, string _ServerName, string _DBServer)
        {
            DisconnectCompany();   // never leave a previous session (and licence) open
            oCompany = new SAPbobsCOM.Company();

            try
            {
                DataSet ds = LoadCompanySetting();
                DataRow cfg = ds.Tables[0].Rows[0];

                string UserID = (_ServerName == "sap.laxmigroup.com.np")
                    ? cfg["SourceUserID"].ToString().Trim()
                    : cfg["DesUserID"].ToString().Trim();

                oCompany.CompanyDB = CompanyDb;
                oCompany.DbUserName = UserID;
                oCompany.DbPassword = cfg["Password"].ToString().Trim();
                oCompany.LicenseServer = _ServerName.Trim();
                oCompany.Server = _DBServer.Trim();
                oCompany.DbServerType = BoDataServerTypes.dst_HANADB;
                oCompany.UserName = cfg["SapUser"].ToString().Trim();
                oCompany.Password = cfg["SapPassword"].ToString().Trim();

                if (oCompany.Connect() == 0)
                    return true;

                int ErrCode;
                string ErrMsg;
                oCompany.GetLastError(out ErrCode, out ErrMsg);
                WriteLogFile("Error in connecting to company:" + CompanyDb + " & error is: " + ErrMsg);
                return false;
            }
            catch (Exception ex)
            {
                WriteLogFile("Exception connecting to company:" + CompanyDb + " : " + ex.Message);
                return false;
            }
        }

        private void DisconnectCompany()
        {
            if (oCompany == null) return;
            try
            {
                if (oCompany.Connected) oCompany.Disconnect();
            }
            catch { }
            finally
            {
                ReleaseCom(oCompany);
                oCompany = null;
            }
        }

        #endregion

        #region LOAD COMPANY SETTINGS

        public static DataSet LoadCompanySetting()
        {
            // Read AppConfig.xml once instead of on every connect
            if (_settingsCache == null)
            {
                DataSet dataSet = new DataSet();
                dataSet.ReadXml(Path.Combine(Application.StartupPath, "AppConfig.xml"));
                _settingsCache = dataSet;
            }
            return _settingsCache;
        }

        #endregion

        #region WriteLogFile

        private void WriteLogFile(string Message)
        {
            try
            {
                string dir = Path.Combine(Application.StartupPath, "Log");
                string date = DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Year.ToString();
                string file = Path.Combine(dir, "ItemCreationLog_" + date + ".txt");

                lock (_logLock)   // called from the background thread too
                {
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.AppendAllText(file, Message + " " + DateTime.Now + Environment.NewLine);
                }
            }
            catch
            {
                // logging must never crash the process
            }
        }

        #endregion

        #region FilePath

        private void FilePath()
        {
            string dir = Path.Combine(Application.StartupPath, "Log");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string date = DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Year.ToString();
            oPath = Path.Combine(dir, "ItemCreationInterCompanyUtility" + date + ".txt");
        }

        #endregion

        #region isCompanySelected

        private bool isCompanySelected()
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                object sel = dataGridView1["Selected", i].Value;
                if (sel != null && Convert.ToInt32(sel) > 0)
                    return true;
            }
            return false;
        }

        #endregion

        #region Fetch item details + check in every company

        private async void button1_Click_1(object sender, EventArgs e)
        {
            if (_busy) return;

            string itemCode = cmbItmCd.Text.Trim();
            if (itemCode == "")
            {
                MessageBox.Show("Please select or enter an item code.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _busy = true;
            Control btn = sender as Control;
            if (btn != null) btn.Enabled = false;
            UseWaitCursor = true;

            try
            {
                string headerSql = @"SELECT T0.""ItemCode"", T0.""ItemName"", T1.""ItmsGrpNam"", T0.""EvalSystem"", T0.""U_ItemCategory"", T0.""FrgnName"",
                        T0.""InvntryUom"", T0.""SalUnitMsr"", T0.""BuyUnitMsr"", T2.""FirmName"", T0.""U_ModelYear"",
                        (SELECT ""Price"" FROM ""ITM1"" WHERE ""ItemCode"" = T0.""ItemCode"" AND ""PriceList"" = 1) AS ""DPrice"", 
                        (SELECT ""Price"" FROM ""ITM1"" WHERE ""ItemCode"" = T0.""ItemCode"" AND ""PriceList"" = 2) AS ""MPrice"",
                        T0.""validFor"", T0.""frozenFor""
                        FROM OITM T0 
                        INNER JOIN OITB T1 ON T1.""ItmsGrpCod"" = T0.""ItmsGrpCod"" 
                        LEFT OUTER JOIN OMRC T2 ON T2.""FirmCode"" = T0.""FirmCode"" 
                        WHERE T0.""ItemCode"" = '" + Q(itemCode) + "'";

                DataTable header = await Task.Run(() => SqlHelper.Hana_ADOConnection(headerSql));
                FillHeaderFields(header);

                // Check the item in every company database IN PARALLEL, off the UI thread
                string sqlQryISP = "select \"InvntItem\", \"SellItem\", \"PrchseItem\", \"OnHand\" from OITM where \"ItemCode\" = '" + Q(itemCode) + "'";

                var tasks = new List<Task<StockCheckResult>>();
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    int row = i;
                    string db = Cell(dataGridView1, "DataBase", i);
                    string server = Cell(dataGridView1, "ServerIP", i);

                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            return new StockCheckResult { Row = row, Data = SqlHelper.Hana_ADOConnection3(sqlQryISP, db, server) };
                        }
                        catch (Exception ex)
                        {
                            WriteLogFile("Item check failed in " + db + " (" + server + "): " + ex.Message);
                            return new StockCheckResult { Row = row, Data = null };
                        }
                    }));
                }

                StockCheckResult[] results = await Task.WhenAll(tasks);

                foreach (StockCheckResult r in results)
                {
                    int i = r.Row;
                    if (r.Data != null && r.Data.Rows.Count > 0)
                    {
                        DataRow row = r.Data.Rows[0];
                        dataGridView1["InvItm", i].Value = row["InvntItem"].ToString() == "Y" ? "YES" : "NO";
                        dataGridView1["SalItm", i].Value = row["SellItem"].ToString() == "Y" ? "YES" : "NO";
                        dataGridView1["PurItm", i].Value = row["PrchseItem"].ToString() == "Y" ? "YES" : "NO";
                        dataGridView1["RecFound", i].Value = "YES";
                        dataGridView1["CurStock", i].Value = row["OnHand"].ToString();
                    }
                    else
                    {
                        dataGridView1["RecFound", i].Value = "NO";
                        dataGridView1["InvItm", i].Value = "NO";
                        dataGridView1["SalItm", i].Value = "NO";
                        dataGridView1["PurItm", i].Value = "NO";
                        dataGridView1["CurStock", i].Value = "0";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                if (btn != null) btn.Enabled = true;
                _busy = false;
            }
        }

        private void FillHeaderFields(DataTable header)
        {
            _syncing = true;   // setting cmbItmNm.Text must not rewrite the item code
            try
            {
                if (header != null && header.Rows.Count > 0)
                {
                    DataRow row = header.Rows[0];

                    cmbItmNm.Text = row["ItemName"].ToString();
                    cmbItmGrp.Text = row["ItmsGrpNam"].ToString();

                    switch (row["EvalSystem"].ToString())
                    {
                        case "F": cmbValMtd.Text = "FIFO"; break;
                        case "S": cmbValMtd.Text = "Standard"; break;
                        case "B": cmbValMtd.Text = "Serial/Batch"; break;
                        default: cmbValMtd.Text = "Moving Average"; break;
                    }

                    txtDlrPrc.Text = row["DPrice"].ToString();
                    txtMRPPrc.Text = row["MPrice"].ToString();
                    txtInvUoM.Text = row["InvntryUom"].ToString();
                    txtSalUoM.Text = row["SalUnitMsr"].ToString();
                    txtPurUoM.Text = row["BuyUnitMsr"].ToString();
                    txtHSNCode.Text = row["FrgnName"].ToString();
                    cmbManuf.Text = row["FirmName"].ToString();

                    if (row["validFor"].ToString() == "Y")
                        cmbItmStatus.Text = "Active";
                    else if (row["frozenFor"].ToString() == "Y")
                        cmbItmStatus.Text = "In Active";
                }
                else
                {
                    // New item: clear the detail fields but keep the typed Item Code and Category
                    cmbItmNm.Text = "";
                    cmbItmGrp.Text = "";
                    cmbValMtd.Text = "";
                    txtDlrPrc.Text = "";
                    txtMRPPrc.Text = "";
                    txtInvUoM.Text = "";
                    txtSalUoM.Text = "";
                    txtPurUoM.Text = "";
                    txtHSNCode.Text = "";
                    cmbManuf.Text = "";
                }
            }
            finally
            {
                _syncing = false;
            }
        }

        #endregion

        #region Item Code <-> Item Name sync (no DB calls, no loop)

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)   // Item Code
        {
            if (_syncing) return;
            _syncing = true;
            try
            {
                string name;
                if (codeToName.TryGetValue(cmbItmCd.Text, out name))
                    cmbItmNm.Text = name;
            }
            finally
            {
                _syncing = false;
            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)   // Item Name
        {
            if (_syncing) return;
            _syncing = true;
            try
            {
                string code;
                if (nameToCode.TryGetValue(cmbItmNm.Text, out code))
                    cmbItmCd.Text = code;
            }
            finally
            {
                _syncing = false;
            }
        }

        #endregion

        #region Validation

        private bool validation()
        {
            try
            {
                if (cmdMulItmCreat.Text != "No")
                    return true;

                if (string.IsNullOrWhiteSpace(cmbItmCd.Text)) return Fail("Item code can't be left blank.");
                if (string.IsNullOrWhiteSpace(cmbItmNm.Text)) return Fail("Item Description can't be left blank.");
                if (string.IsNullOrWhiteSpace(cmbItmGrp.Text)) return Fail("Item group can't be left blank.");
                if (string.IsNullOrWhiteSpace(cmbValMtd.Text)) return Fail("Valuation method can't be left blank.");
                if (string.IsNullOrWhiteSpace(cmbCat.Text)) return Fail("Item Category can't be left blank.");
                if (string.IsNullOrWhiteSpace(txtInvUoM.Text)) return Fail("Inventory UoM can't be left blank.");
                if (string.IsNullOrWhiteSpace(txtSalUoM.Text)) return Fail("Sales UoM can't be left blank.");
                if (string.IsNullOrWhiteSpace(txtPurUoM.Text)) return Fail("Purchase UoM can't be left blank.");

                ParsePrice(txtDlrPrc.Text, "Dealer price");   // throws with a clear message if invalid
                ParsePrice(txtMRPPrc.Text, "MRP price");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error :" + ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool Fail(string msg)
        {
            MessageBox.Show(msg, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        #endregion

        #region Other UI events

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)   // Item Category
        {
            bool isSpare = cmbCat.Text == "Spare Parts";

            txtModlYear.Enabled = isSpare;
            cmbItmCd.Enabled = true;
            cmbItmNm.Enabled = true;
            cmbItmGrp.Enabled = isSpare;
            cmbValMtd.Enabled = isSpare;
            txtDlrPrc.Enabled = isSpare;
            txtMRPPrc.Enabled = isSpare;
            txtInvUoM.Enabled = isSpare;
            txtSalUoM.Enabled = isSpare;
            txtPurUoM.Enabled = isSpare;
            cmbManuf.Enabled = isSpare;
            cmbSubCat.Enabled = isSpare;
            cmbItmStatus.Enabled = isSpare;

            if (!isSpare)
            {
                button1.Visible = true;
                _syncing = true;
                try
                {
                    txtModlYear.Text = "";
                    cmbItmCd.Text = "";
                    cmbItmNm.Text = "";
                    cmbSubCat.Text = "";
                    cmbItmGrp.Text = "";
                    cmbValMtd.Text = "";
                    txtDlrPrc.Text = "";
                    txtMRPPrc.Text = "";
                    txtInvUoM.Text = "";
                    txtSalUoM.Text = "";
                    txtPurUoM.Text = "";
                    cmbManuf.Text = "";
                    cmbItmStatus.Text = "";
                }
                finally
                {
                    _syncing = false;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)   // Clear
        {
            button1.Visible = true;
            _syncing = true;
            try
            {
                txtModlYear.Text = "";
                cmbItmCd.Text = "";
                cmbItmNm.Text = "";
                cmbSubCat.Text = "";
                cmbItmGrp.Text = "";
                cmbValMtd.Text = "";
                txtDlrPrc.Text = "";
                txtMRPPrc.Text = "";
                txtInvUoM.Text = "";
                txtSalUoM.Text = "";
                txtPurUoM.Text = "";
                cmbManuf.Text = "";
            }
            finally
            {
                _syncing = false;
            }
        }

        private void comboBox8_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Manufacturer code is now looked up from manufNameToCode when saving.
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog { Filter = "Excel files|*.xlsx;*.xls|All files|*.*" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    txtFilePath.Text = dlg.FileName;
                    using (var stream = File.Open(dlg.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        DataSet ds = reader.AsDataSet(new ExcelDataSetConfiguration
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration { UseHeaderRow = true }
                        });
                        DtCollect = ds.Tables;
                        if (DtCollect.Count > 0)
                            dataGridView2.DataSource = DtCollect[0];   // first sheet
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not read the file: " + ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void cmdMulItmCreat_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool multi = cmdMulItmCreat.Text == "Yes";
            dataGridView2.Visible = multi;
            btnBrowse.Visible = multi;
            txtFilePath.Visible = multi;
            lblFile.Visible = multi;
        }

        private void btnLogOut_Click(object sender, EventArgs e)
        {
            this.Hide();
            Program.usr.Show();
        }

        private void dataGridView2_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int count = 0; count <= dataGridView2.Rows.Count - 2; count++)
                dataGridView2.Rows[count].Cells[0].Value = (count + 1).ToString();
        }

        private void FrmCompanySelection_FormClosed(object sender, FormClosedEventArgs e)
        {
            DisconnectCompany();
            Application.Exit();
        }

        #endregion

        #region GRID EVENTS

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Password" && e.Value != null)
                e.Value = new string('*', e.Value.ToString().Length);
        }

        private void dataGridView1_CellValidated(object sender, DataGridViewCellEventArgs e) { }

        #endregion

        #region Empty designer-wired handlers (kept so the Designer still compiles)

        private void label1_Click(object sender, EventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void comboBox12_SelectedIndexChanged(object sender, EventArgs e) { }
        private void lblModel_Click(object sender, EventArgs e) { }
        private void lblVarient_Click(object sender, EventArgs e) { }
        private void txtDlrPrc_TextChanged(object sender, EventArgs e) { }
        private void cmbModelYear_ValueChanged(object sender, EventArgs e) { }
        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e) { }

        #endregion

        #region Summary Email

        private void SendSummaryEmail(Dictionary<string, List<KeyValuePair<string, string>>> log,
                                      List<string> errors,
                                      DataSet settingsDs)
        {
            try
            {
                if (log.Count == 0) return;

                DataRow settings = settingsDs.Tables["CompanyDetails"].Rows[0];
                string smtpServer = settings["SmtpServer"].ToString();
                int smtpPort = Convert.ToInt32(settings["SmtpPort"]);
                string senderEmail = settings["SenderEmail"].ToString();
                string senderPwd = settings["SenderPassword"].ToString();
                string recipientSetting = settings["RecipientEmails"].ToString();

                StringBuilder sb = new StringBuilder();
                sb.Append("<html><body style='font-family: Arial, sans-serif;'>");
                sb.Append("<h2 style='color: #2E86C1;'>SAP Item Master Add/Update Notification</h2>");
                sb.Append("<p>The following items were successfully created/updated in the respective databases:</p>");
                sb.Append("<table border='1' cellpadding='8' style='border-collapse: collapse; width: 100%; font-size: 12px;'>");
                sb.Append("<tr style='background-color: #D6EAF8;'><th>Company Name</th><th>Item Codes</th><th>Item Names</th></tr>");

                foreach (var entry in log)
                {
                    if (entry.Value.Count == 0) continue;

                    string allCodes = string.Join(", ", entry.Value.Select(x => WebUtility.HtmlEncode(x.Key)));
                    string allNames = string.Join(", ", entry.Value.Select(x => WebUtility.HtmlEncode(x.Value)));

                    sb.Append("<tr>");
                    sb.Append("<td style='font-weight: bold; background-color: #F9FBFC;'>" + WebUtility.HtmlEncode(entry.Key) + "</td>");
                    sb.Append("<td>" + allCodes + "</td>");
                    sb.Append("<td>" + allNames + "</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table>");

                if (errors != null && errors.Count > 0)
                {
                    sb.Append("<h3 style='color: #C0392B;'>Failed</h3><ul style='font-size: 12px;'>");
                    foreach (string err in errors)
                        sb.Append("<li>" + WebUtility.HtmlEncode(err) + "</li>");
                    sb.Append("</ul>");
                }

                sb.Append("<p style='font-size: 11px; color: grey;'>Timestamp: " + DateTime.Now.ToString("g") + "</p>");
                sb.Append("</body></html>");

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(senderEmail);
                    mail.Subject = "SAP Notification: Item Master Processed";
                    mail.Body = sb.ToString();
                    mail.IsBodyHtml = true;

                    foreach (string email in recipientSetting.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                        mail.To.Add(email.Trim());

                    using (SmtpClient smtp = new SmtpClient(smtpServer, smtpPort))
                    {
                        smtp.Credentials = new NetworkCredential(senderEmail, senderPwd);
                        smtp.EnableSsl = true;
                        smtp.Timeout = 30000;
                        smtp.Send(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLogFile("EMAIL ERROR: " + ex.Message);
            }
        }

        #endregion
    }
}
