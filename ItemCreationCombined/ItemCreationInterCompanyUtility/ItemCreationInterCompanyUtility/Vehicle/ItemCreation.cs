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
using ExcelDataReader;
using ItemCreationInterCompanyUtility;
using System.Net.Mail;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace ItemCreationInterCompanyUtility.Vehicle
{
    public partial class FrmCompanySelection : Form
    {

        #region Variable Declaration

        static string gdbName = string.Empty;
        string sql = string.Empty;
        bool message = false;
        DataTable grdCmpdt = null;
        string oPath = string.Empty;
        private ServiceLayerClient _sl = null;                  // current Service Layer session (one company at a time)
        private Dictionary<string, string> _udfTypes = null;    // OITM user field types of the connected company
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
            public string Displacement = "";
            public string SerialManaged = "";   // "YES" / "NO" / "" (leave unchanged)
            public int? ManufacturerCode;
            public double? DealerPrice;
            public double? MrpPrice;

            public string Colour = "";
            public string Model = "";
            public string Variant = "";
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
            this.Text = "Item Master Creation - Vehicle";
            ApplyModernTheme();   // called here (not in the Designer) so regenerating the Designer cannot lose it
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

                // 2. Item groups (vehicle models only)
                sql = "select \"ItmsGrpNam\" from OITB where \"U_Category\" = 'Vehicle' order by \"ItmsGrpNam\"";
                DataTable dtGroups = SqlHelper.Hana_ADOConnection(sql);
                cmbItmGrp.BeginUpdate();
                foreach (DataRow drow in dtGroups.Rows)
                    cmbItmGrp.Items.Add(drow[0].ToString());
                cmbItmGrp.EndUpdate();

                // 2b. Colours (models are loaded when an item group is picked, variants when a model is picked)
                DataTable dtColor = SqlHelper.Hana_ADOConnection("select \"Code\",\"Name\" from \"@COLOR\"");
                cmbColorCd.BeginUpdate();
                cmbColorNm.BeginUpdate();
                foreach (DataRow drow in dtColor.Rows)
                {
                    cmbColorCd.Items.Add(drow[0].ToString());
                    cmbColorNm.Items.Add(drow[1].ToString());
                }
                cmbColorCd.EndUpdate();
                cmbColorNm.EndUpdate();

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

                SetVehicleFieldsEnabled(false);
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

            // SqlHelper swallows query errors and hands back the previous result, so check the shape.
            // If this base company's @INTRCOMPITEMCR has no U_LiServerIP field yet, fall back to the
            // old vehicle layout: U_ServerIP holds the host only, HANA port 30015 is appended.
            if (grdCmpdt == null || !grdCmpdt.Columns.Contains("DBServer") || !grdCmpdt.Columns.Contains("DataBase"))
            {
                sql = "select \"U_ServerIP\" || ':30015' as \"DBServer\", \"U_ServerIP\" as \"ServerIP\",\"U_DBType\" as \"DBType\",\"U_cmpName\" as \"Company\" ,\"U_dbName\" as \"DataBase\" from \"@INTRCOMPITEMCR\" ";
                grdCmpdt = SqlHelper.Hana_ADOConnection(sql);
                if (grdCmpdt == null || !grdCmpdt.Columns.Contains("DBServer") || !grdCmpdt.Columns.Contains("DataBase"))
                    throw new Exception("Could not read the company list from @INTRCOMPITEMCR.");
            }

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
                // Vehicles are stocked/sold/purchased in showroom companies only
                string yesNo = Cell(dataGridView1, "DBType", i) == "SHOWROOM" ? "YES" : "NO";
                dataGridView1["InvItm", i].Value = yesNo;
                dataGridView1["SalItm", i].Value = yesNo;
                dataGridView1["PurItm", i].Value = yesNo;
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

            if (!(cmbCat.Text == "Vehicle" || cmbCat.Text == ""))
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
                result = await Task.Run(() => ProcessItems(companies, items));
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
                Model = cmbModelCd.Text.Trim(),
                Variant = cmbVarCd.Text.Trim(),
                Colour = cmbColorCd.Text.Trim(),
                ModelYear = txtModlYear.Text,
                HsnCode = txtHSNCode.Text.Trim(),
                InvUoM = txtInvUoM.Text.Trim(),
                SalUoM = txtSalUoM.Text.Trim(),
                PurUoM = txtPurUoM.Text.Trim(),
                ItemStatus = cmbItmStatus.Text.Trim(),
                Displacement = cmbDspMnt.Text.Trim(),
                SerialManaged = cmbSerlMng.Text.Trim().ToUpperInvariant(),
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
                    ItemCategory = "Vehicle",
                    ModelYear = Cell(dataGridView2, "ModelYear", ii),
                    HsnCode = Cell(dataGridView2, "HSNCode", ii),
                    InvUoM = Cell(dataGridView2, "InventoryUOM", ii),
                    SalUoM = Cell(dataGridView2, "SalesUOM", ii),
                    PurUoM = Cell(dataGridView2, "PurchaseUOM", ii),
                    ItemStatus = Cell(dataGridView2, "ItemStatus", ii),
                    Displacement = Cell(dataGridView2, "Displacement", ii),
                    SerialManaged = Cell(dataGridView2, "SerialManage", ii).ToUpperInvariant(),
                    ManufacturerCode = int.TryParse(manufText, out manuf) ? (int?)manuf : null,
                    DealerPrice = ParsePrice(Cell(dataGridView2, "DealerPrice", ii), rowLabel + " dealer price"),
                    MrpPrice = ParsePrice(Cell(dataGridView2, "MRPPrice", ii), rowLabel + " MRP price")
                });
            }
            return list;
        }

        #endregion

        #region Background processing (Service Layer)

        private ProcessResult ProcessItems(List<CompanyTarget> companies, List<ItemInput> items)
        {
            var result = new ProcessResult();

            foreach (CompanyTarget company in companies)
            {
                string connectError;
                if (!Connect(company.DataBase, company.ServerIP, out connectError))
                {
                    result.Errors.Add("Could not connect to company " + company.DataBase + ": " + connectError);
                    DisconnectCompany();
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
            try
            {
                bool exists = _sl.ItemExists(item.ItemCode);
                Dictionary<string, object> payload = BuildItemPayload(item, company);

                if (exists)
                {
                    _sl.UpdateItem(item.ItemCode, payload);
                }
                else
                {
                    int? groupCode = _sl.GetItemGroupCode(item.ItemGroup);
                    if (!groupCode.HasValue)
                    {
                        error = "Item group '" + item.ItemGroup + "' not found in this company.";
                        return false;
                    }
                    payload["ItemCode"] = item.ItemCode;
                    payload["ItemsGroupCode"] = groupCode.Value;
                    _sl.AddItem(payload);
                }
            }
            catch (ServiceLayerException ex)
            {
                error = ex.Message;
                return false;
            }

            UpdatePriceList(item.ItemCode, item.DealerPrice, item.MrpPrice);
            return true;
        }

        /// <summary>Item fields as a Service Layer "Items" body (same fields the DI API version wrote).</summary>
        private Dictionary<string, object> BuildItemPayload(ItemInput item, CompanyTarget company)
        {
            var p = new Dictionary<string, object>();

            p["ItemName"] = item.ItemName;
            p["InventoryUOM"] = item.InvUoM;
            p["SalesUnit"] = item.SalUoM;
            p["PurchaseUnit"] = item.PurUoM;
            if (!string.IsNullOrEmpty(item.HsnCode))
                p["ForeignName"] = item.HsnCode;

            if (company.DBType == "SHOWROOM")
            {
                switch (item.ValuationMethod)
                {
                    case "FIFO":
                        p["CostAccountingMethod"] = "bis_FIFO";
                        break;
                    case "Standard":
                        p["CostAccountingMethod"] = "bis_Standard";
                        break;
                    case "Moving Average":
                        p["CostAccountingMethod"] = "bis_MovingAverage";
                        break;
                }

                if (item.SerialManaged == "YES")
                    p["ManageSerialNumbers"] = "tYES";
                else if (item.SerialManaged == "NO")
                    p["ManageSerialNumbers"] = "tNO";

                if (!string.IsNullOrEmpty(item.SubCategory))
                    p["U_Categeory"] = UdfValue("Categeory", item.SubCategory);

                p["U_Colour"] = UdfValue("Colour", item.Colour);
                p["U_Variant"] = UdfValue("Variant", item.Variant);
                p["U_Model"] = UdfValue("Model", item.Model);
            }
            else
            {
                p["U_Categeory"] = UdfValue("Categeory", "9");   // vehicle, non-showroom companies
            }

            if (!string.IsNullOrEmpty(item.Displacement))
                p["U_Displacement"] = UdfValue("Displacement", item.Displacement);

            if (item.ManufacturerCode.HasValue)
                p["Manufacturer"] = item.ManufacturerCode.Value;

            p["U_ItemCategory"] = UdfValue("ItemCategory", item.ItemCategory);
            p["U_ModelYear"] = UdfValue("ModelYear", item.ModelYear);

            p["InventoryItem"] = company.InvItm == "YES" ? "tYES" : "tNO";
            p["SalesItem"] = company.SalItm == "YES" ? "tYES" : "tNO";
            p["PurchaseItem"] = company.PurItm == "YES" ? "tYES" : "tNO";

            ApplyItemStatus(p, item.ItemStatus);
            return p;
        }

        private static void ApplyItemStatus(Dictionary<string, object> p, string status)
        {
            const string fmt = "yyyy-MM-dd";
            if (status == "In Active")
            {
                p["Valid"] = "tNO";
                p["ValidTo"] = DateTime.Today.AddDays(-1).ToString(fmt, CultureInfo.InvariantCulture);   // active until yesterday

                p["Frozen"] = "tYES";
                p["FrozenFrom"] = DateTime.Today.ToString(fmt, CultureInfo.InvariantCulture);            // inactive from today
                p["FrozenTo"] = "2099-12-31";
            }
            else if (status == "Active")
            {
                p["Frozen"] = "tNO";
                p["FrozenFrom"] = null;
                p["FrozenTo"] = null;

                p["Valid"] = "tYES";                                 // active forever
                p["ValidFrom"] = null;
                p["ValidTo"] = null;
            }
        }

        /// <summary>
        /// Service Layer is strict about types (the DI API converted text for us), so a numeric
        /// user field gets a number and a date user field gets yyyy-MM-dd.
        /// </summary>
        private object UdfValue(string fieldName, string value)
        {
            string type;
            if (value == null || _udfTypes == null || !_udfTypes.TryGetValue(fieldName, out type))
                return value;

            string v = value.Trim();
            switch (type)
            {
                case "db_Numeric":
                    int n;
                    if (v == "") return null;
                    if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) return n;
                    return value;
                case "db_Float":
                    double d;
                    if (v == "") return null;
                    if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
                    return value;
                case "db_Date":
                    DateTime dt;
                    if (v == "") return null;
                    if (DateTime.TryParseExact(v, new[] { "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt) ||
                        DateTime.TryParse(v, out dt))
                        return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    return value;
                default:
                    return value;
            }
        }

        #endregion

        #region Update Price List

        /// <summary>Price list 1 = Dealer price, price list 2 = MRP (same as before). A price error is logged, the item still counts as saved.</summary>
        private void UpdatePriceList(string itemCode, double? dealerPrice, double? mrpPrice)
        {
            if (!dealerPrice.HasValue && !mrpPrice.HasValue) return;

            var prices = new List<Dictionary<string, object>>();
            if (dealerPrice.HasValue)
                prices.Add(new Dictionary<string, object> { { "PriceList", 1 }, { "Price", dealerPrice.Value } });
            if (mrpPrice.HasValue)
                prices.Add(new Dictionary<string, object> { { "PriceList", 2 }, { "Price", mrpPrice.Value } });

            try
            {
                _sl.UpdateItem(itemCode, new Dictionary<string, object> { { "ItemPrices", prices } });
            }
            catch (Exception ex)
            {
                WriteLogFile("Error in PriceList updation ItemCode:" + itemCode + " : " + ex.Message);
            }
        }

        #endregion

        #region Connect / Disconnect (Service Layer)

        private static string Setting(DataRow cfg, string name, string fallback)
        {
            if (!cfg.Table.Columns.Contains(name)) return fallback;
            string v = cfg[name].ToString().Trim();
            return v == "" ? fallback : v;
        }

        /// <summary>Logs in to the company through Service Layer. The session is closed again by DisconnectCompany().</summary>
        private bool Connect(string companyDb, string serverName, out string error)
        {
            error = null;
            DisconnectCompany();   // never leave a previous session (and licence) open

            try
            {
                DataRow cfg = LoadCompanySetting().Tables[0].Rows[0];

                // {server} is replaced by the company's server from @INTRCOMPITEMCR
                string url = Setting(cfg, "ServiceLayerUrl", "https://{server}:50000/b1s/v1/")
                    .Replace("{server}", (serverName ?? "").Trim());
                bool ignoreCert = Setting(cfg, "ServiceLayerIgnoreCertErrors", "true").ToLowerInvariant() != "false";

                _sl = new ServiceLayerClient(url, ignoreCert);
                _sl.Login(companyDb, cfg["SapUser"].ToString().Trim(), cfg["SapPassword"].ToString().Trim());
                WriteLogFile("Connected to company DB:" + companyDb + " (" + _sl.BaseUrl + ")");

                try
                {
                    _udfTypes = _sl.GetItemUserFieldTypes("Categeory", "Displacement", "ItemCategory", "ModelYear", "Colour", "Variant", "Model");
                }
                catch (Exception ex)
                {
                    _udfTypes = null;   // values are then sent as text
                    WriteLogFile("Could not read user field types in " + companyDb + " : " + ex.Message);
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                WriteLogFile("Error in connecting to company:" + companyDb + " & error is: " + ex.Message);
                return false;
            }
        }

        /// <summary>Logs out of the current company. Called after every company and when the form closes.</summary>
        private void DisconnectCompany()
        {
            ServiceLayerClient sl = _sl;
            _sl = null;
            _udfTypes = null;
            if (sl == null) return;

            bool wasLoggedIn = sl.IsLoggedIn;
            string db = sl.CompanyDB;
            string logoutError = sl.Logout();
            if (!wasLoggedIn) return;

            if (logoutError == null)
                WriteLogFile("Disconnected from company DB:" + db);
            else
                WriteLogFile("Error in disconnecting from company:" + db + " & error is: " + logoutError);
        }

        #endregion

        #region LOAD COMPANY SETTINGS

        public static DataSet LoadCompanySetting()
        {
            // Common settings + the section of the module chosen at login (cached in AppSettings)
            return AppSettings.Load();
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
                        T0.""U_Displacement"", T0.""ManSerNum"",
                        (SELECT ""Price"" FROM ""ITM1"" WHERE ""ItemCode"" = T0.""ItemCode"" AND ""PriceList"" = 1) AS ""DPrice"", 
                        (SELECT ""Price"" FROM ""ITM1"" WHERE ""ItemCode"" = T0.""ItemCode"" AND ""PriceList"" = 2) AS ""MPrice"",
                        T0.""validFor"", T0.""frozenFor""
                        FROM OITM T0 
                        INNER JOIN OITB T1 ON T1.""ItmsGrpCod"" = T0.""ItmsGrpCod"" 
                        LEFT OUTER JOIN OMRC T2 ON T2.""FirmCode"" = T0.""FirmCode"" 
                        WHERE T0.""ItemCode"" = '" + Q(itemCode) + "'";

                DataTable header = await Task.Run(() => SqlHelper.Hana_ADOConnection(headerSql));
                if (header != null && !header.Columns.Contains("ItmsGrpNam"))
                    throw new Exception("Could not read the item from the base company.");
                FillHeaderFields(header);

                // Check the item in every company database, off the UI thread.
                // One company at a time: SqlHelper keeps its connection/result in static fields,
                // so parallel calls could hand one company's result to another.
                string sqlQryISP = "select \"InvntItem\", \"SellItem\", \"PrchseItem\", \"OnHand\" from OITM where \"ItemCode\" = '" + Q(itemCode) + "'";

                var targets = new List<KeyValuePair<string, string>>();
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    targets.Add(new KeyValuePair<string, string>(Cell(dataGridView1, "DataBase", i), Cell(dataGridView1, "ServerIP", i)));

                List<StockCheckResult> results = await Task.Run(() =>
                {
                    var list = new List<StockCheckResult>();
                    DataTable last = header;
                    for (int row = 0; row < targets.Count; row++)
                    {
                        DataTable data = null;
                        try
                        {
                            data = SqlHelper.Hana_ADOConnection3(sqlQryISP, targets[row].Key, targets[row].Value);
                            // A failed query returns the previous table instance: treat that as "no answer"
                            if (ReferenceEquals(data, last) || (data != null && !data.Columns.Contains("OnHand")))
                            {
                                WriteLogFile("Item check failed in " + targets[row].Key + " (" + targets[row].Value + ")");
                                data = null;
                            }
                            else
                            {
                                last = data;
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteLogFile("Item check failed in " + targets[row].Key + " (" + targets[row].Value + "): " + ex.Message);
                            data = null;
                        }
                        list.Add(new StockCheckResult { Row = row, Data = data });
                    }
                    return list;
                });

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
                    cmbDspMnt.Text = row["U_Displacement"].ToString();
                    cmbSerlMng.Text = row["ManSerNum"].ToString() == "Y" ? "YES" : "NO";

                    DateTime modelYear;
                    string my = row["U_ModelYear"].ToString().Trim();
                    if (my.Length >= 8 &&
                        (DateTime.TryParseExact(my, new[] { "dd-MM-yyyy", "d-M-yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out modelYear) ||
                         DateTime.TryParse(my, out modelYear)) &&
                        modelYear >= txtModlYear.MinDate && modelYear <= txtModlYear.MaxDate)
                        txtModlYear.Value = modelYear;

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
                    cmbDspMnt.Text = "";
                    cmbSerlMng.Text = "";
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
                if (cmbCat.Text == "Vehicle" && string.IsNullOrWhiteSpace(cmbDspMnt.Text)) return Fail("Displacement can't be left blank.");
                if (cmbCat.Text == "Vehicle" && string.IsNullOrWhiteSpace(cmbSerlMng.Text)) return Fail("Serial manage can't be left blank.");
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

        private void SetVehicleFieldsEnabled(bool enabled)
        {
            cmbModelCd.Enabled = enabled;
            cmbModelNm.Enabled = enabled;
            cmbVarCd.Enabled = enabled;
            cmbVarNm.Enabled = enabled;
            cmbColorCd.Enabled = enabled;
            cmbColorNm.Enabled = enabled;
            cmbDspMnt.Enabled = enabled;
            cmbSerlMng.Enabled = enabled;
        }

        private void ClearItemFields()
        {
            _syncing = true;
            try
            {
                cmbModelCd.Text = "";
                cmbModelNm.Text = "";
                cmbVarCd.Text = "";
                cmbVarNm.Text = "";
                cmbColorCd.Text = "";
                cmbColorNm.Text = "";
                cmbSerlMng.Text = "";
                cmbDspMnt.Text = "";
                cmbItmCd.Text = "";
                cmbItmNm.Text = "";
                cmbSubCat.Text = "";
                cmbSubCat.ValueMember = "";
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
            finally
            {
                _syncing = false;
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)   // Item Category
        {
            bool isVehicle = cmbCat.Text == "Vehicle";

            // Vehicle: the item code/name are built from Model-Variant-Colour, so they are not typed.
            // Find: only the item code/name are open, everything else is filled by the Find button.
            SetVehicleFieldsEnabled(isVehicle);
            cmbItmCd.Enabled = !isVehicle;
            cmbItmNm.Enabled = !isVehicle;
            txtModlYear.Enabled = isVehicle;
            cmbItmGrp.Enabled = isVehicle;
            cmbValMtd.Enabled = isVehicle;
            txtDlrPrc.Enabled = isVehicle;
            txtMRPPrc.Enabled = isVehicle;
            txtInvUoM.Enabled = isVehicle;
            txtSalUoM.Enabled = isVehicle;
            txtPurUoM.Enabled = isVehicle;
            cmbManuf.Enabled = isVehicle;
            cmbItmStatus.Enabled = isVehicle;
            button1.Visible = !isVehicle;   // Find

            if (!isVehicle)
            {
                ClearItemFields();
                cmbItmStatus.Text = "";
            }
        }

        private void button2_Click(object sender, EventArgs e)   // Refresh / clear
        {
            button1.Visible = true;
            ClearItemFields();
        }

        #endregion

        #region Model / Variant / Colour -> Item Code and Item Name

        // Picking Model, then Variant, then Colour builds the item code and name as
        // Model-Variant-Colour. _syncing stops the Item Code/Name combos (and the paired
        // code/name combos) from reacting while the fields are being filled.

        private void comboBox9_SelectedIndexChanged(object sender, EventArgs e)   // Item Group -> models
        {
            DataTable dtModel = SqlHelper.Hana_ADOConnection(
                "select \"Code\",\"Name\" from \"@MODEL\" where \"Name\" = '" + Q(cmbItmGrp.Text) + "' ");

            cmbModelCd.Items.Clear();
            cmbModelNm.Items.Clear();
            if (dtModel == null || !dtModel.Columns.Contains("Code")) return;
            foreach (DataRow drow in dtModel.Rows)
            {
                cmbModelCd.Items.Add(drow[0].ToString());
                cmbModelNm.Items.Add(drow[1].ToString());
            }
        }

        private void LoadVariants(string modelCode)
        {
            cmbVarCd.Items.Clear();
            cmbVarNm.Items.Clear();
            DataTable dtVariant = SqlHelper.Hana_ADOConnection(
                "select \"U_VarCode\",\"U_VarName\" from \"@VARIANT\" where \"U_ModelCd\" = '" + Q(modelCode) + "' ");
            if (dtVariant == null || !dtVariant.Columns.Contains("U_VarCode")) return;
            foreach (DataRow drow in dtVariant.Rows)
            {
                cmbVarCd.Items.Add(drow[0].ToString());
                cmbVarNm.Items.Add(drow[1].ToString());
            }
        }

        private void cmbModelCd_SelectedIndexChanged(object sender, EventArgs e)   // Model code
        {
            if (_syncing) return;
            _syncing = true;
            try
            {
                LoadVariants(cmbModelCd.Text);

                DataTable dtModel = SqlHelper.Hana_ADOConnection(
                    "select \"Code\",\"Name\" from \"@MODEL\" where \"Code\" = '" + Q(cmbModelCd.Text) + "' ");
                if (dtModel != null && dtModel.Columns.Contains("Name"))
                    foreach (DataRow row in dtModel.Rows)
                        cmbModelNm.Text = row["Name"].ToString();

                cmbItmCd.Text = cmbModelCd.Text;
                cmbItmNm.Text = cmbModelNm.Text;
            }
            finally
            {
                _syncing = false;
            }
        }

        private void comboBox13_SelectedIndexChanged(object sender, EventArgs e)   // Model name
        {
            if (_syncing) return;
            _syncing = true;
            try
            {
                DataTable dtModel = SqlHelper.Hana_ADOConnection(
                    "select \"Code\",\"Name\" from \"@MODEL\" where \"Name\" = '" + Q(cmbModelNm.Text) + "' ");
                if (dtModel != null && dtModel.Columns.Contains("Code"))
                    foreach (DataRow row in dtModel.Rows)
                        cmbModelCd.Text = row["Code"].ToString();

                LoadVariants(cmbModelCd.Text);

                cmbItmCd.Text = cmbModelCd.Text;
                cmbItmNm.Text = cmbModelNm.Text;
            }
            finally
            {
                _syncing = false;
            }
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)   // Variant code
        {
            if (_syncing) return;
            _syncing = true;
            try
            {
                DataTable dtVariant = SqlHelper.Hana_ADOConnection(
                    "select \"U_VarCode\",\"U_VarName\" from \"@VARIANT\" where \"U_VarCode\" = '" + Q(cmbVarCd.Text) + "' ");
                if (dtVariant != null && dtVariant.Columns.Contains("U_VarName"))
                    foreach (DataRow row in dtVariant.Rows)
                        cmbVarNm.Text = row["U_VarName"].ToString();

                cmbItmCd.Text = cmbModelCd.Text + "-" + cmbVarCd.Text;
                cmbItmNm.Text = cmbModelNm.Text + "-" + cmbVarNm.Text;
            }
            finally
            {
                _syncing = false;
            }
        }

        private void comboBox14_SelectedIndexChanged(object sender, EventArgs e)   // Variant name
        {
            if (_syncing) return;
            _syncing = true;
            try
            {
                DataTable dtVariant = SqlHelper.Hana_ADOConnection(
                    "select \"U_VarCode\",\"U_VarName\" from \"@VARIANT\" where \"U_VarName\" = '" + Q(cmbVarNm.Text) + "' ");
                if (dtVariant != null && dtVariant.Columns.Contains("U_VarCode"))
                    foreach (DataRow row in dtVariant.Rows)
                        cmbVarCd.Text = row["U_VarCode"].ToString();

                cmbItmCd.Text = cmbModelCd.Text + "-" + cmbVarCd.Text;
                cmbItmNm.Text = cmbModelNm.Text + "-" + cmbVarNm.Text;
            }
            finally
            {
                _syncing = false;
            }
        }

        private void ApplyColour(string whereColumn, string value)
        {
            DataTable dtColor = SqlHelper.Hana_ADOConnection(
                "select \"Code\",\"Name\",\"U_Categeory\",\"U_CategeoryName\" from \"@COLOR\" where \"" + whereColumn + "\" = '" + Q(value) + "' ");
            if (dtColor != null && dtColor.Columns.Contains("U_Categeory"))
            {
                foreach (DataRow row in dtColor.Rows)
                {
                    cmbColorCd.Text = row["Code"].ToString();
                    cmbColorNm.Text = row["Name"].ToString();
                    // Sub category comes from the colour: code is kept in ValueMember, name is shown
                    cmbSubCat.DisplayMember = row["U_CategeoryName"].ToString();
                    cmbSubCat.ValueMember = row["U_Categeory"].ToString();
                    cmbSubCat.Text = row["U_CategeoryName"].ToString();
                }
            }

            cmbItmCd.Text = cmbModelCd.Text + "-" + cmbVarCd.Text + "-" + cmbColorCd.Text;
            cmbItmNm.Text = cmbModelNm.Text + "-" + cmbVarNm.Text + "-" + cmbColorNm.Text;
        }

        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)   // Colour code
        {
            if (_syncing) return;
            _syncing = true;
            try { ApplyColour("Code", cmbColorCd.Text); }
            finally { _syncing = false; }
        }

        private void comboBox15_SelectedIndexChanged(object sender, EventArgs e)   // Colour name
        {
            if (_syncing) return;
            _syncing = true;
            try { ApplyColour("Name", cmbColorNm.Text); }
            finally { _syncing = false; }
        }

        #endregion

        #region Other UI events (continued)

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
                DataColumnCollection cfgCols = settings.Table.Columns;
                if (!cfgCols.Contains("SmtpServer") || !cfgCols.Contains("RecipientEmails") ||
                    string.IsNullOrWhiteSpace(settings["SmtpServer"].ToString()) ||
                    string.IsNullOrWhiteSpace(settings["RecipientEmails"].ToString()))
                {
                    WriteLogFile("Summary email skipped: SmtpServer / RecipientEmails not set in AppConfig.xml");
                    return;
                }
                string smtpServer = settings["SmtpServer"].ToString();
                int smtpPort = Convert.ToInt32(settings["SmtpPort"]);
                string senderEmail = settings["SenderEmail"].ToString();
                string senderPwd = settings["SenderPassword"].ToString();
                string recipientSetting = settings["RecipientEmails"].ToString();

                StringBuilder sb = new StringBuilder();
                sb.Append("<html><body style='font-family: Arial, sans-serif;'>");
                sb.Append("<h2 style='color: #2E86C1;'>SAP Vehicle Item Master Add/Update Notification</h2>");
                sb.Append("<p>The following vehicle items were successfully created/updated in the respective databases:</p>");
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
                    mail.Subject = "SAP Notification: Vehicle Item Master Processed";
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
