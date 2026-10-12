using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Sap.Data.Hana;

namespace ItemCreationInterCompanyUtility
{
    public partial class UserLogin : Form
    {
        public UserLogin()
        {
            InitializeComponent();
            WireUpUiEvents();
        }

        #region Variable Declaration

        static string gdbName = string.Empty;

        // Brand palette (Hyundai navy / Laxmi orange)
        private static readonly Color Navy = Color.FromArgb(0, 44, 95);
        private static readonly Color NavyHover = Color.FromArgb(0, 62, 130);
        private static readonly Color Orange = Color.FromArgb(245, 130, 32);
        private static readonly Color LineIdle = Color.FromArgb(200, 205, 214);

        #endregion

        #region Form Events

        private void UserLogin_Load(object sender, EventArgs e)
        {
        }

        private void UserLogin_VisibleChanged(object sender, EventArgs e)
        {
            txtUsrName.Clear();
            txtPswd.Clear();
            HideError();
            // Vehicle / Spare Parts stays as it was chosen last time; nothing is pre-selected on first start.
            // Focus goes to the user name: a radio button that receives keyboard focus selects itself,
            // which would silently pre-select a module.
            if (this.Visible)
                this.ActiveControl = txtUsrName;
            txtUsrName.Focus();
        }

        #endregion

        #region Login

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string user = txtUsrName.Text.Trim();
            string pwd = txtPswd.Text;

            if (!rdoVehicle.Checked && !rdoParts.Checked)
            {
                ShowError("Please select Vehicle or Spare Parts.");
                return;
            }

            if (user.Length == 0 || pwd.Length == 0)
            {
                ShowError("Please enter your user name and password.");
                return;
            }

            // Everything after this line (login check included) uses the chosen module's
            // section of AppConfig.xml: its base company, SAP user and email recipients.
            AppSettings.Module = rdoParts.Checked ? AppModule.Parts : AppModule.Vehicle;

            try
            {
                // Parameterized query – prevents SQL injection.
                const string sql =
                    "select \"USER_CODE\", \"U_ItemCreationPwd\" from OUSR " +
                    "where \"USER_CODE\" = :user and \"U_ItemCreationPwd\" = :pwd";

                HanaParameter[] parameters =
                {
                    new HanaParameter(":user", user),
                    new HanaParameter(":pwd", pwd)
                };

                DataTable dt = SqlHelper.Hana_ADOConnectionNew(sql, parameters);

                if (dt != null && dt.Rows.Count > 0)
                {
                    HideError();
                    Form ic;
                    if (AppSettings.Module == AppModule.Parts)
                        ic = new Parts.FrmCompanySelection();      // spare parts screen
                    else
                        ic = new Vehicle.FrmCompanySelection();    // vehicle screen
                    ic.Show();
                    this.Hide();
                    ic.Focus();
                }
                else
                {
                    txtPswd.Clear();
                    txtUsrName.Clear();
                    txtUsrName.Focus();
                    ShowError("Invalid user name or password.");
                }
            }
            catch (Exception ex)
            {
                // A broken AppConfig.xml is worth saying out loud; anything else stays generic.
                ShowError(ex.Message.StartsWith("AppConfig.xml") ? ex.Message : "Unable to sign in. Please try again.");
                // TODO: log ex to the application log instead of swallowing it.
            }
        }

        #endregion

        #region UI Behaviour

        private void WireUpUiEvents()
        {
            // Focus accent on the input underline.
            txtUsrName.Enter += (s, e) => pnlUser.BackColor = Orange;
            txtUsrName.Leave += (s, e) => pnlUser.BackColor = LineIdle;
            txtPswd.Enter += (s, e) => pnlPass.BackColor = Orange;
            txtPswd.Leave += (s, e) => pnlPass.BackColor = LineIdle;

            // Hide the error message as soon as the user edits a field.
            txtUsrName.TextChanged += (s, e) => HideError();
            txtPswd.TextChanged += (s, e) => HideError();

            // Login button hover.
            btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = NavyHover;
            btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = Navy;
        }

        private void rdoModule_CheckedChanged(object sender, EventArgs e)
        {
            // Selected choice = navy with white text, the other one = white with navy text
            foreach (RadioButton rdo in new[] { rdoVehicle, rdoParts })
            {
                rdo.BackColor = rdo.Checked ? Navy : Color.White;
                rdo.ForeColor = rdo.Checked ? Color.White : Navy;
            }

            HideError();
            lblSubtitle.Text = rdoParts.Checked ? "Sign in to Spare Parts item creation"
                             : rdoVehicle.Checked ? "Sign in to Vehicle item creation"
                             : "Sign in to continue";
        }

        private void chkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPswd.UseSystemPasswordChar = !chkShowPassword.Checked;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }

        private void HideError()
        {
            if (lblError.Visible)
                lblError.Visible = false;
        }

        #endregion

        #region Borderless window drag

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private void Drag_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        #endregion
    }
}
