namespace ItemCreationInterCompanyUtility.Parts
{
    partial class FrmCompanySelection
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.cmdNext = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.LblItmCode = new System.Windows.Forms.Label();
            this.LblItmDesc = new System.Windows.Forms.Label();
            this.lblValMthd = new System.Windows.Forms.Label();
            this.lblItmGrp = new System.Windows.Forms.Label();
            this.lblItmCat = new System.Windows.Forms.Label();
            this.cmbCat = new System.Windows.Forms.ComboBox();
            this.LblInvUoM = new System.Windows.Forms.Label();
            this.LblSalUoM = new System.Windows.Forms.Label();
            this.LblPurUom = new System.Windows.Forms.Label();
            this.cmbItmGrp = new System.Windows.Forms.ComboBox();
            this.cmbValMtd = new System.Windows.Forms.ComboBox();
            this.txtInvUoM = new System.Windows.Forms.TextBox();
            this.txtSalUoM = new System.Windows.Forms.TextBox();
            this.txtPurUoM = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.cmbItmCd = new System.Windows.Forms.ComboBox();
            this.cmbItmNm = new System.Windows.Forms.ComboBox();
            this.txtDlrPrc = new System.Windows.Forms.TextBox();
            this.LblDelPrice = new System.Windows.Forms.Label();
            this.txtMRPPrc = new System.Windows.Forms.TextBox();
            this.LblMRPPrice = new System.Windows.Forms.Label();
            this.LblManfc = new System.Windows.Forms.Label();
            this.cmbManuf = new System.Windows.Forms.ComboBox();
            this.LblMdlYer = new System.Windows.Forms.Label();
            this.cmbSubCat = new System.Windows.Forms.ComboBox();
            this.lblItmSubCat = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.cmbItmStatus = new System.Windows.Forms.ComboBox();
            this.LblItmStatus = new System.Windows.Forms.Label();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.txtFilePath = new System.Windows.Forms.TextBox();
            this.lblFile = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cmdMulItmCreat = new System.Windows.Forms.ComboBox();
            this.txtModlYear = new System.Windows.Forms.DateTimePicker();
            this.btnLogOut = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.txtHSNCode = new System.Windows.Forms.TextBox();
            this.SnNo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ItemCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ItemDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ItemGrp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ValuationMth = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DealerPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MRPPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.InventoryUOM = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SalesUOM = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PurchaseUOM = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Category = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ModelYear = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Manufacturer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ItemStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(2, 192);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 62;
            this.dataGridView1.Size = new System.Drawing.Size(1052, 184);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            this.dataGridView1.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dataGridView1_CellFormatting);
            this.dataGridView1.CellValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellValidated);
            // 
            // cmdNext
            // 
            this.cmdNext.Location = new System.Drawing.Point(984, 597);
            this.cmdNext.Name = "cmdNext";
            this.cmdNext.Size = new System.Drawing.Size(69, 24);
            this.cmdNext.TabIndex = 1;
            this.cmdNext.Text = "&Process";
            this.cmdNext.UseVisualStyleBackColor = true;
            this.cmdNext.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Location = new System.Drawing.Point(6, 172);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(200, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Please select database For Item Creation";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // LblItmCode
            // 
            this.LblItmCode.AutoSize = true;
            this.LblItmCode.Location = new System.Drawing.Point(259, 74);
            this.LblItmCode.Name = "LblItmCode";
            this.LblItmCode.Size = new System.Drawing.Size(55, 13);
            this.LblItmCode.TabIndex = 3;
            this.LblItmCode.Text = "Item Code";
            // 
            // LblItmDesc
            // 
            this.LblItmDesc.AutoSize = true;
            this.LblItmDesc.Location = new System.Drawing.Point(259, 95);
            this.LblItmDesc.Name = "LblItmDesc";
            this.LblItmDesc.Size = new System.Drawing.Size(83, 13);
            this.LblItmDesc.TabIndex = 5;
            this.LblItmDesc.Text = "Item Description";
            // 
            // lblValMthd
            // 
            this.lblValMthd.AutoSize = true;
            this.lblValMthd.Location = new System.Drawing.Point(12, 139);
            this.lblValMthd.Name = "lblValMthd";
            this.lblValMthd.Size = new System.Drawing.Size(90, 13);
            this.lblValMthd.TabIndex = 9;
            this.lblValMthd.Text = "Valuation Method";
            // 
            // lblItmGrp
            // 
            this.lblItmGrp.AutoSize = true;
            this.lblItmGrp.Location = new System.Drawing.Point(12, 117);
            this.lblItmGrp.Name = "lblItmGrp";
            this.lblItmGrp.Size = new System.Drawing.Size(59, 13);
            this.lblItmGrp.TabIndex = 11;
            this.lblItmGrp.Text = "Item Group";
            // 
            // lblItmCat
            // 
            this.lblItmCat.AutoSize = true;
            this.lblItmCat.Location = new System.Drawing.Point(11, 73);
            this.lblItmCat.Name = "lblItmCat";
            this.lblItmCat.Size = new System.Drawing.Size(72, 13);
            this.lblItmCat.TabIndex = 15;
            this.lblItmCat.Text = "Item Category";
            // 
            // cmbCat
            // 
            this.cmbCat.FormattingEnabled = true;
            this.cmbCat.Items.AddRange(new object[] {
            "Spare Parts",
            "Find"});
            this.cmbCat.Location = new System.Drawing.Point(109, 69);
            this.cmbCat.Name = "cmbCat";
            this.cmbCat.Size = new System.Drawing.Size(125, 21);
            this.cmbCat.TabIndex = 16;
            this.cmbCat.SelectedIndexChanged += new System.EventHandler(this.comboBox2_SelectedIndexChanged);
            // 
            // LblInvUoM
            // 
            this.LblInvUoM.AutoSize = true;
            this.LblInvUoM.Location = new System.Drawing.Point(590, 71);
            this.LblInvUoM.Name = "LblInvUoM";
            this.LblInvUoM.Size = new System.Drawing.Size(77, 13);
            this.LblInvUoM.TabIndex = 23;
            this.LblInvUoM.Text = "Inventory UoM";
            // 
            // LblSalUoM
            // 
            this.LblSalUoM.AutoSize = true;
            this.LblSalUoM.Location = new System.Drawing.Point(590, 94);
            this.LblSalUoM.Name = "LblSalUoM";
            this.LblSalUoM.Size = new System.Drawing.Size(59, 13);
            this.LblSalUoM.TabIndex = 25;
            this.LblSalUoM.Text = "Sales UoM";
            // 
            // LblPurUom
            // 
            this.LblPurUom.AutoSize = true;
            this.LblPurUom.Location = new System.Drawing.Point(590, 115);
            this.LblPurUom.Name = "LblPurUom";
            this.LblPurUom.Size = new System.Drawing.Size(78, 13);
            this.LblPurUom.TabIndex = 27;
            this.LblPurUom.Text = "Purchase UoM";
            // 
            // cmbItmGrp
            // 
            this.cmbItmGrp.FormattingEnabled = true;
            this.cmbItmGrp.Location = new System.Drawing.Point(109, 115);
            this.cmbItmGrp.Name = "cmbItmGrp";
            this.cmbItmGrp.Size = new System.Drawing.Size(125, 21);
            this.cmbItmGrp.TabIndex = 29;
            // 
            // cmbValMtd
            // 
            this.cmbValMtd.FormattingEnabled = true;
            this.cmbValMtd.Items.AddRange(new object[] {
            "FIFO",
            "Serial/Batch"});
            this.cmbValMtd.Location = new System.Drawing.Point(109, 138);
            this.cmbValMtd.Name = "cmbValMtd";
            this.cmbValMtd.Size = new System.Drawing.Size(125, 21);
            this.cmbValMtd.TabIndex = 30;
            // 
            // txtInvUoM
            // 
            this.txtInvUoM.Location = new System.Drawing.Point(674, 68);
            this.txtInvUoM.Name = "txtInvUoM";
            this.txtInvUoM.Size = new System.Drawing.Size(125, 20);
            this.txtInvUoM.TabIndex = 32;
            // 
            // txtSalUoM
            // 
            this.txtSalUoM.Location = new System.Drawing.Point(674, 90);
            this.txtSalUoM.Name = "txtSalUoM";
            this.txtSalUoM.Size = new System.Drawing.Size(125, 20);
            this.txtSalUoM.TabIndex = 33;
            // 
            // txtPurUoM
            // 
            this.txtPurUoM.Location = new System.Drawing.Point(674, 112);
            this.txtPurUoM.Name = "txtPurUoM";
            this.txtPurUoM.Size = new System.Drawing.Size(125, 20);
            this.txtPurUoM.TabIndex = 34;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(909, 597);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(69, 24);
            this.button1.TabIndex = 35;
            this.button1.Text = "Find";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // cmbItmCd
            // 
            this.cmbItmCd.Enabled = false;
            this.cmbItmCd.FormattingEnabled = true;
            this.cmbItmCd.Location = new System.Drawing.Point(347, 68);
            this.cmbItmCd.Name = "cmbItmCd";
            this.cmbItmCd.Size = new System.Drawing.Size(213, 21);
            this.cmbItmCd.TabIndex = 36;
            this.cmbItmCd.SelectedIndexChanged += new System.EventHandler(this.comboBox3_SelectedIndexChanged);
            // 
            // cmbItmNm
            // 
            this.cmbItmNm.Enabled = false;
            this.cmbItmNm.FormattingEnabled = true;
            this.cmbItmNm.Location = new System.Drawing.Point(347, 91);
            this.cmbItmNm.Name = "cmbItmNm";
            this.cmbItmNm.Size = new System.Drawing.Size(213, 21);
            this.cmbItmNm.TabIndex = 37;
            this.cmbItmNm.SelectedIndexChanged += new System.EventHandler(this.comboBox4_SelectedIndexChanged);
            // 
            // txtDlrPrc
            // 
            this.txtDlrPrc.Location = new System.Drawing.Point(347, 114);
            this.txtDlrPrc.Name = "txtDlrPrc";
            this.txtDlrPrc.Size = new System.Drawing.Size(213, 20);
            this.txtDlrPrc.TabIndex = 45;
            this.txtDlrPrc.TextChanged += new System.EventHandler(this.txtDlrPrc_TextChanged);
            // 
            // LblDelPrice
            // 
            this.LblDelPrice.AutoSize = true;
            this.LblDelPrice.Location = new System.Drawing.Point(258, 117);
            this.LblDelPrice.Name = "LblDelPrice";
            this.LblDelPrice.Size = new System.Drawing.Size(65, 13);
            this.LblDelPrice.TabIndex = 44;
            this.LblDelPrice.Text = "Dealer Price";
            // 
            // txtMRPPrc
            // 
            this.txtMRPPrc.Location = new System.Drawing.Point(347, 136);
            this.txtMRPPrc.Name = "txtMRPPrc";
            this.txtMRPPrc.Size = new System.Drawing.Size(213, 20);
            this.txtMRPPrc.TabIndex = 47;
            // 
            // LblMRPPrice
            // 
            this.LblMRPPrice.AutoSize = true;
            this.LblMRPPrice.Location = new System.Drawing.Point(258, 140);
            this.LblMRPPrice.Name = "LblMRPPrice";
            this.LblMRPPrice.Size = new System.Drawing.Size(58, 13);
            this.LblMRPPrice.TabIndex = 46;
            this.LblMRPPrice.Text = "MRP Price";
            // 
            // LblManfc
            // 
            this.LblManfc.AutoSize = true;
            this.LblManfc.Location = new System.Drawing.Point(819, 92);
            this.LblManfc.Name = "LblManfc";
            this.LblManfc.Size = new System.Drawing.Size(70, 13);
            this.LblManfc.TabIndex = 48;
            this.LblManfc.Text = "Manufacturer";
            // 
            // cmbManuf
            // 
            this.cmbManuf.FormattingEnabled = true;
            this.cmbManuf.Location = new System.Drawing.Point(929, 90);
            this.cmbManuf.Name = "cmbManuf";
            this.cmbManuf.Size = new System.Drawing.Size(125, 21);
            this.cmbManuf.TabIndex = 49;
            this.cmbManuf.SelectedIndexChanged += new System.EventHandler(this.comboBox8_SelectedIndexChanged);
            // 
            // LblMdlYer
            // 
            this.LblMdlYer.AutoSize = true;
            this.LblMdlYer.Location = new System.Drawing.Point(817, 71);
            this.LblMdlYer.Name = "LblMdlYer";
            this.LblMdlYer.Size = new System.Drawing.Size(61, 13);
            this.LblMdlYer.TabIndex = 52;
            this.LblMdlYer.Text = "Model Year";
            // 
            // cmbSubCat
            // 
            this.cmbSubCat.Enabled = false;
            this.cmbSubCat.FormattingEnabled = true;
            this.cmbSubCat.Location = new System.Drawing.Point(109, 92);
            this.cmbSubCat.Name = "cmbSubCat";
            this.cmbSubCat.Size = new System.Drawing.Size(125, 21);
            this.cmbSubCat.TabIndex = 55;
            this.cmbSubCat.SelectedIndexChanged += new System.EventHandler(this.comboBox12_SelectedIndexChanged);
            // 
            // lblItmSubCat
            // 
            this.lblItmSubCat.AutoSize = true;
            this.lblItmSubCat.Location = new System.Drawing.Point(11, 94);
            this.lblItmSubCat.Name = "lblItmSubCat";
            this.lblItmSubCat.Size = new System.Drawing.Size(94, 13);
            this.lblItmSubCat.TabIndex = 54;
            this.lblItmSubCat.Text = "Item Sub Category";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(2, 597);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(69, 24);
            this.button2.TabIndex = 59;
            this.button2.Text = "Refresh";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // cmbItmStatus
            // 
            this.cmbItmStatus.Enabled = false;
            this.cmbItmStatus.FormattingEnabled = true;
            this.cmbItmStatus.Items.AddRange(new object[] {
            "Active",
            "In Active"});
            this.cmbItmStatus.Location = new System.Drawing.Point(674, 134);
            this.cmbItmStatus.Name = "cmbItmStatus";
            this.cmbItmStatus.Size = new System.Drawing.Size(125, 21);
            this.cmbItmStatus.TabIndex = 61;
            // 
            // LblItmStatus
            // 
            this.LblItmStatus.AutoSize = true;
            this.LblItmStatus.Location = new System.Drawing.Point(590, 136);
            this.LblItmStatus.Name = "LblItmStatus";
            this.LblItmStatus.Size = new System.Drawing.Size(60, 13);
            this.LblItmStatus.TabIndex = 60;
            this.LblItmStatus.Text = "Item Status";
            // 
            // dataGridView2
            // 
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.SnNo,
            this.ItemCode,
            this.ItemDescription,
            this.ItemGrp,
            this.ValuationMth,
            this.DealerPrice,
            this.MRPPrice,
            this.InventoryUOM,
            this.SalesUOM,
            this.PurchaseUOM,
            this.Category,
            this.ModelYear,
            this.Manufacturer,
            this.ItemStatus});
            this.dataGridView2.Location = new System.Drawing.Point(2, 382);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.RowHeadersWidth = 62;
            this.dataGridView2.Size = new System.Drawing.Size(1052, 209);
            this.dataGridView2.TabIndex = 62;
            this.dataGridView2.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView2_CellContentClick);
            this.dataGridView2.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dataGridView2_RowsAdded);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(802, 159);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(35, 20);
            this.btnBrowse.TabIndex = 65;
            this.btnBrowse.Text = ".......";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // txtFilePath
            // 
            this.txtFilePath.Location = new System.Drawing.Point(347, 158);
            this.txtFilePath.Name = "txtFilePath";
            this.txtFilePath.Size = new System.Drawing.Size(453, 20);
            this.txtFilePath.TabIndex = 64;
            // 
            // lblFile
            // 
            this.lblFile.AutoSize = true;
            this.lblFile.Location = new System.Drawing.Point(258, 161);
            this.lblFile.Name = "lblFile";
            this.lblFile.Size = new System.Drawing.Size(54, 13);
            this.lblFile.TabIndex = 63;
            this.lblFile.Text = "File Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(819, 119);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 13);
            this.label2.TabIndex = 66;
            this.label2.Text = "Multiple Item Creation";
            // 
            // cmdMulItmCreat
            // 
            this.cmdMulItmCreat.FormattingEnabled = true;
            this.cmdMulItmCreat.Items.AddRange(new object[] {
            "Yes",
            "No"});
            this.cmdMulItmCreat.Location = new System.Drawing.Point(929, 113);
            this.cmdMulItmCreat.Name = "cmdMulItmCreat";
            this.cmdMulItmCreat.Size = new System.Drawing.Size(125, 21);
            this.cmdMulItmCreat.TabIndex = 67;
            this.cmdMulItmCreat.SelectedIndexChanged += new System.EventHandler(this.cmdMulItmCreat_SelectedIndexChanged);
            // 
            // txtModlYear
            // 
            this.txtModlYear.CustomFormat = "dd-MM-yyy";
            this.txtModlYear.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.txtModlYear.Location = new System.Drawing.Point(929, 67);
            this.txtModlYear.Name = "txtModlYear";
            this.txtModlYear.ShowUpDown = true;
            this.txtModlYear.Size = new System.Drawing.Size(125, 20);
            this.txtModlYear.TabIndex = 68;
            this.txtModlYear.ValueChanged += new System.EventHandler(this.cmbModelYear_ValueChanged);
            // 
            // btnLogOut
            // 
            this.btnLogOut.Location = new System.Drawing.Point(92, 597);
            this.btnLogOut.Name = "btnLogOut";
            this.btnLogOut.Size = new System.Drawing.Size(69, 24);
            this.btnLogOut.TabIndex = 69;
            this.btnLogOut.Text = "LogOut";
            this.btnLogOut.UseVisualStyleBackColor = true;
            this.btnLogOut.Click += new System.EventHandler(this.btnLogOut_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(819, 139);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 70;
            this.label3.Text = "HSN Code";
            // 
            // txtHSNCode
            // 
            this.txtHSNCode.Location = new System.Drawing.Point(929, 138);
            this.txtHSNCode.Name = "txtHSNCode";
            this.txtHSNCode.Size = new System.Drawing.Size(125, 20);
            this.txtHSNCode.TabIndex = 71;
            // 
            // SnNo
            // 
            this.SnNo.HeaderText = "Sn No.";
            this.SnNo.MinimumWidth = 8;
            this.SnNo.Name = "SnNo";
            this.SnNo.Width = 150;
            // 
            // ItemCode
            // 
            this.ItemCode.DataPropertyName = "Item No.";
            this.ItemCode.HeaderText = "Item Code";
            this.ItemCode.MinimumWidth = 8;
            this.ItemCode.Name = "ItemCode";
            this.ItemCode.Width = 150;
            // 
            // ItemDescription
            // 
            this.ItemDescription.DataPropertyName = "Item Description";
            this.ItemDescription.HeaderText = "Item Description";
            this.ItemDescription.MinimumWidth = 8;
            this.ItemDescription.Name = "ItemDescription";
            this.ItemDescription.Width = 150;
            // 
            // ItemGrp
            // 
            this.ItemGrp.DataPropertyName = "Item Group";
            this.ItemGrp.HeaderText = "Item Group";
            this.ItemGrp.MinimumWidth = 8;
            this.ItemGrp.Name = "ItemGrp";
            this.ItemGrp.Width = 150;
            // 
            // ValuationMth
            // 
            this.ValuationMth.DataPropertyName = "Valuation Method";
            this.ValuationMth.HeaderText = "Valuation Method";
            this.ValuationMth.MinimumWidth = 8;
            this.ValuationMth.Name = "ValuationMth";
            this.ValuationMth.Width = 150;
            // 
            // DealerPrice
            // 
            this.DealerPrice.DataPropertyName = "Dealer Price";
            this.DealerPrice.HeaderText = "Dealer Price";
            this.DealerPrice.MinimumWidth = 8;
            this.DealerPrice.Name = "DealerPrice";
            this.DealerPrice.Width = 150;
            // 
            // MRPPrice
            // 
            this.MRPPrice.DataPropertyName = "MRP Price";
            this.MRPPrice.HeaderText = "MRP Price";
            this.MRPPrice.MinimumWidth = 8;
            this.MRPPrice.Name = "MRPPrice";
            this.MRPPrice.Width = 150;
            // 
            // InventoryUOM
            // 
            this.InventoryUOM.DataPropertyName = "Inventory UOM";
            this.InventoryUOM.HeaderText = "Inventory UOM";
            this.InventoryUOM.MinimumWidth = 8;
            this.InventoryUOM.Name = "InventoryUOM";
            this.InventoryUOM.Width = 150;
            // 
            // SalesUOM
            // 
            this.SalesUOM.DataPropertyName = "Sales UOM";
            this.SalesUOM.HeaderText = "Sales UOM";
            this.SalesUOM.MinimumWidth = 8;
            this.SalesUOM.Name = "SalesUOM";
            this.SalesUOM.Width = 150;
            // 
            // PurchaseUOM
            // 
            this.PurchaseUOM.DataPropertyName = "Purchasing UoM";
            this.PurchaseUOM.HeaderText = "Purchase UOM";
            this.PurchaseUOM.MinimumWidth = 8;
            this.PurchaseUOM.Name = "PurchaseUOM";
            this.PurchaseUOM.Width = 150;
            // 
            // Category
            // 
            this.Category.DataPropertyName = "Category";
            this.Category.HeaderText = "Category";
            this.Category.MinimumWidth = 8;
            this.Category.Name = "Category";
            this.Category.Width = 150;
            // 
            // ModelYear
            // 
            this.ModelYear.DataPropertyName = "Model Year";
            this.ModelYear.HeaderText = "Model Year";
            this.ModelYear.MinimumWidth = 8;
            this.ModelYear.Name = "ModelYear";
            this.ModelYear.Width = 150;
            // 
            // Manufacturer
            // 
            this.Manufacturer.DataPropertyName = "Manufacturer";
            this.Manufacturer.HeaderText = "Manufacturer";
            this.Manufacturer.MinimumWidth = 8;
            this.Manufacturer.Name = "Manufacturer";
            this.Manufacturer.Width = 150;
            // 
            // ItemStatus
            // 
            this.ItemStatus.DataPropertyName = "Item Status";
            this.ItemStatus.HeaderText = "Item Status";
            this.ItemStatus.MinimumWidth = 8;
            this.ItemStatus.Name = "ItemStatus";
            this.ItemStatus.Width = 150;
            // 
            // FrmCompanySelection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1066, 631);
            this.Controls.Add(this.txtHSNCode);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnLogOut);
            this.Controls.Add(this.txtModlYear);
            this.Controls.Add(this.cmdMulItmCreat);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtFilePath);
            this.Controls.Add(this.lblFile);
            this.Controls.Add(this.dataGridView2);
            this.Controls.Add(this.cmbItmStatus);
            this.Controls.Add(this.LblItmStatus);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.cmbSubCat);
            this.Controls.Add(this.lblItmSubCat);
            this.Controls.Add(this.LblMdlYer);
            this.Controls.Add(this.cmbManuf);
            this.Controls.Add(this.LblManfc);
            this.Controls.Add(this.txtMRPPrc);
            this.Controls.Add(this.LblMRPPrice);
            this.Controls.Add(this.txtDlrPrc);
            this.Controls.Add(this.LblDelPrice);
            this.Controls.Add(this.cmbItmNm);
            this.Controls.Add(this.cmbItmCd);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.txtPurUoM);
            this.Controls.Add(this.txtSalUoM);
            this.Controls.Add(this.txtInvUoM);
            this.Controls.Add(this.cmbValMtd);
            this.Controls.Add(this.cmbItmGrp);
            this.Controls.Add(this.LblPurUom);
            this.Controls.Add(this.LblSalUoM);
            this.Controls.Add(this.LblInvUoM);
            this.Controls.Add(this.cmbCat);
            this.Controls.Add(this.lblItmCat);
            this.Controls.Add(this.lblItmGrp);
            this.Controls.Add(this.lblValMthd);
            this.Controls.Add(this.LblItmDesc);
            this.Controls.Add(this.LblItmCode);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmdNext);
            this.Controls.Add(this.dataGridView1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FrmCompanySelection";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Item Master Creation";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmCompanySelection_FormClosed);
            this.Load += new System.EventHandler(this.FrmCompanySelection_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button cmdNext;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label LblItmCode;
        private System.Windows.Forms.Label LblItmDesc;
        private System.Windows.Forms.Label lblValMthd;
        private System.Windows.Forms.Label lblItmGrp;
        private System.Windows.Forms.Label lblItmCat;
        private System.Windows.Forms.ComboBox cmbCat;
        private System.Windows.Forms.Label LblInvUoM;
        private System.Windows.Forms.Label LblSalUoM;
        private System.Windows.Forms.Label LblPurUom;
        private System.Windows.Forms.ComboBox cmbItmGrp;
        private System.Windows.Forms.ComboBox cmbValMtd;
        private System.Windows.Forms.TextBox txtInvUoM;
        private System.Windows.Forms.TextBox txtSalUoM;
        private System.Windows.Forms.TextBox txtPurUoM;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ComboBox cmbItmCd;
        private System.Windows.Forms.ComboBox cmbItmNm;
        private System.Windows.Forms.TextBox txtDlrPrc;
        private System.Windows.Forms.Label LblDelPrice;
        private System.Windows.Forms.TextBox txtMRPPrc;
        private System.Windows.Forms.Label LblMRPPrice;
        private System.Windows.Forms.Label LblManfc;
        private System.Windows.Forms.ComboBox cmbManuf;
        private System.Windows.Forms.Label LblMdlYer;
        private System.Windows.Forms.ComboBox cmbSubCat;
        private System.Windows.Forms.Label lblItmSubCat;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ComboBox cmbItmStatus;
        private System.Windows.Forms.Label LblItmStatus;
        private System.Windows.Forms.DataGridView dataGridView2;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.TextBox txtFilePath;
        private System.Windows.Forms.Label lblFile;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmdMulItmCreat;
        private System.Windows.Forms.DateTimePicker txtModlYear;
        private System.Windows.Forms.Button btnLogOut;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtHSNCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn SnNo;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemDescription;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemGrp;
        private System.Windows.Forms.DataGridViewTextBoxColumn ValuationMth;
        private System.Windows.Forms.DataGridViewTextBoxColumn DealerPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn MRPPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn InventoryUOM;
        private System.Windows.Forms.DataGridViewTextBoxColumn SalesUOM;
        private System.Windows.Forms.DataGridViewTextBoxColumn PurchaseUOM;
        private System.Windows.Forms.DataGridViewTextBoxColumn Category;
        private System.Windows.Forms.DataGridViewTextBoxColumn ModelYear;
        private System.Windows.Forms.DataGridViewTextBoxColumn Manufacturer;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemStatus;
    }
}