using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//using Microsoft.SqlServer.Management.Smo;
//using Microsoft.SqlServer.Management.Common;
using System.Data.SqlClient;
using System.IO;
  
namespace ItemCreationInterCompanyUtility
{
    public partial class FrmLogin : Form
    {
        string oPath = string.Empty;

        public FrmLogin()
        {
            InitializeComponent();
        }

        #region Control Events
        private void btnNext_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked == true)
            {
                FrmCompanySelection obj = new FrmCompanySelection();
                obj.Show();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();  
        }
        #endregion

        #region From Events
        private void FrmLogin_Load(object sender, EventArgs e)
        {
           
        }
        #endregion

        #region Private Methods

        private bool validation()
        {
            try
            {
                //if (string.IsNullOrEmpty(cmbServerType.Text.ToString()))
                //{
                //    MessageBox.Show("Please select the server type.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    return false;
                //}
                //else if (string.IsNullOrEmpty(cmbServer.Text.ToString()))
                //{
                //    MessageBox.Show("Please enter the server.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    return false;
                //}
                //else if (string.IsNullOrEmpty(this.txtSqlUser.Text.Trim()))
                //{
                //    MessageBox.Show("Please enter the userid.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    return false;
                //}
                //else if (string.IsNullOrEmpty(this.txtSqlPwd.Text.Trim()))
                //{
                //    MessageBox.Show("Please enter the password.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    return false;
                //}
                //else if (string.IsNullOrEmpty(this.txtSAPLicense.Text.ToString()))
                //{
                //    MessageBox.Show("Please enter SAP License Server.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    return false;
                //}
                //else
                 return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error :" + ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool WriteFile()
        {
            //SqlConnection cn = null;
            //try
            //{
            //    cn = new SqlConnection("server=" + this.cmbServer.Text.Trim() + ";database=master;uid=" + this.txtSqlUser.Text.Trim() + ";pwd=" + this.txtSqlPwd.Text.Trim());
            //    cn.Open();
            //    if (File.Exists(Application.StartupPath + @"\DBLogin.ini") == true)
            //    {
            //        File.Delete(Application.StartupPath + @"\DBLogin.ini");
            //    }
            //    StreamWriter sw = new StreamWriter(Application.StartupPath + @"\DBLogin.ini", true);
            //    sw.WriteLine(clsEncrypt.Encrypt(this.cmbServerType.Text.Trim()));
            //    sw.WriteLine(clsEncrypt.Encrypt(this.cmbServer.Text.Trim()));
            //    sw.WriteLine(clsEncrypt.Encrypt(this.txtSqlUser.Text.Trim()));
            //    sw.WriteLine(clsEncrypt.Encrypt(this.txtSqlPwd.Text.Trim()));
            //    sw.WriteLine(clsEncrypt.Encrypt(this.txtSAPLicense.Text.Trim()));
            //    sw.Close();
            //    if (cn != null)
            //    {
            //        if (cn.State == ConnectionState.Open)
            //            cn.Close();
            //        cn.Dispose();
            //        cn = null;
            //    }
            //    return true; 
            //}
            //catch (SqlException ex)
            //{
            //    MessageBox.Show(ex.Number + "-" + ex.Message, "SQL Server Error");
               return false;
            //}
        }

        private void ReadFile()
        {
            //try
            //{
            //    string[] Str = new string[5];
            //    string oPath = Application.StartupPath + @"\DBLogin.ini";
            //    StreamReader sr = new StreamReader(oPath);
            //    int i = 0;
            //    string input = null;
            //    while ((input = sr.ReadLine()) != null)
            //    {
            //        Str[i] = clsEncrypt.Decrypt(input);
            //        i++;
            //    }
            //    sr.Close();
            //    this.cmbServerType.Text = Str[0];
            //    this.cmbServer.Text = Str[1];
            //    this.txtSqlUser.Text = Str[2];
            //    this.txtSqlPwd.Text = Str[3];
            //    this.txtSAPLicense.Text = Str[4];
            //}
            //catch (Exception ex)
            //{
            //    throw ex;
            //}
        }
        #endregion

        private void cmbServerType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void WriteLogFile(string Message)
        {
            try
            {
                FileStream fs = File.Open(oPath, FileMode.Append, FileAccess.Write);
                StreamWriter sr = new StreamWriter(fs);
                sr.WriteLine(Message);
                sr.Flush();
                sr.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void FilePath()
        {
            try
            {
                if (!Directory.Exists(System.Windows.Forms.Application.StartupPath + "\\Log"))
                    Directory.CreateDirectory(System.Windows.Forms.Application.StartupPath + "\\Log");
                string date = DateTime.Now.Date.Day.ToString() + DateTime.Now.Date.Month.ToString() + DateTime.Now.Date.Year.ToString();
                oPath = System.Windows.Forms.Application.StartupPath + "\\Log\\ItemCreationInterCompanyUtility" + date + ".txt";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void cmbServer_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
