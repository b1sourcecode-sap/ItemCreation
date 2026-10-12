using System.Drawing;
using System.Windows.Forms;

namespace ItemCreationInterCompanyUtility.Vehicle
{
    // Visual theming for the Item Master Creation screen.
    // Kept in a separate partial file so the generated designer code stays clean.
    // ApplyModernTheme() is invoked at the end of InitializeComponent().
    partial class FrmCompanySelection
    {
        // Brand palette (Hyundai navy / Laxmi orange).
        private static readonly Color ThemeNavy = Color.FromArgb(0, 44, 95);
        private static readonly Color ThemeOrange = Color.FromArgb(245, 130, 32);
        private static readonly Color ThemeBg = Color.FromArgb(240, 242, 245);
        private static readonly Color ThemeGrid = Color.FromArgb(228, 233, 239);
        private static readonly Color ThemeAltRow = Color.FromArgb(245, 247, 250);
        private static readonly Color ThemeBorder = Color.FromArgb(221, 227, 234);
        private static readonly Color ThemeDanger = Color.FromArgb(176, 0, 32);

        private void ApplyModernTheme()
        {
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = ThemeBg;

            BuildHeader();
            BuildFooterSeparator();

            StyleGrid(this.dataGridView1);
            StyleGrid(this.dataGridView2);

            // Section caption above the company/database grid.
            this.label1.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            this.label1.ForeColor = ThemeNavy;

            // Primary action.
            StylePrimaryButton(this.cmdNext);

            // Secondary actions.
            StyleOutlineButton(this.button1, ThemeNavy);   // Find
            StyleOutlineButton(this.button2, ThemeNavy);   // Refresh
            StyleOutlineButton(this.btnLogOut, ThemeDanger); // LogOut

            // Small browse button.
            this.btnBrowse.FlatStyle = FlatStyle.Flat;
            this.btnBrowse.FlatAppearance.BorderSize = 0;
            this.btnBrowse.BackColor = ThemeNavy;
            this.btnBrowse.ForeColor = Color.White;
            this.btnBrowse.Text = "...";
            this.btnBrowse.Cursor = Cursors.Hand;
        }

        private void BuildHeader()
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Width = this.ClientSize.Width, // set real width before placing the right-anchored logo
                BackColor = ThemeNavy
            };

            Label title = new Label
            {
                AutoSize = true,
                Text = "Item Master Creation",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(18, 9),
                BackColor = Color.Transparent
            };

            Label subtitle = new Label
            {
                AutoSize = true,
                Text = "Vehicle  \u00B7  inter-company utility",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(169, 192, 218),
                Location = new Point(20, 37),
                BackColor = Color.Transparent
            };

            Panel accent = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 3,
                BackColor = ThemeOrange
            };

            // White plate on the right holds the company logo (the logo image
            // has a white background, so it needs a light backing on the navy bar).
            System.Drawing.Image logoImage = LoadBrandLogo();
            if (logoImage != null)
            {
                Panel logoPlate = new Panel
                {
                    BackColor = Color.White,
                    Size = new Size(184, 44),
                    Location = new Point(this.ClientSize.Width - 184 - 14, 8),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Padding = new Padding(8, 4, 8, 4)
                };

                PictureBox logo = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = logoImage
                };
                logoPlate.Controls.Add(logo);
                header.Controls.Add(logoPlate);
            }

            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(accent);
            this.Controls.Add(header);
            header.BringToFront();
        }

        // Returns the first brand logo available in project resources, or null.
        private System.Drawing.Image LoadBrandLogo()
        {
            try
            {
                var r = global::ItemCreationInterCompanyUtility.Properties.Resources.ResourceManager;
                foreach (string name in new[] { "LaxmiLogo", "Logo", "LaxmiLogo1" })
                {
                    object obj = r.GetObject(name);
                    if (obj is System.Drawing.Image img)
                        return img;
                }
            }
            catch
            {
                // fall through – header simply shows without a logo
            }
            return null;
        }

        private void BuildFooterSeparator()
        {
            Panel line = new Panel
            {
                Height = 1,
                BackColor = ThemeBorder,
                Location = new Point(0, this.cmdNext.Top - 8),
                Width = this.ClientSize.Width,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            this.Controls.Add(line);
            line.SendToBack();
        }

        private void StyleGrid(DataGridView g)
        {
            g.BorderStyle = BorderStyle.None;
            g.BackgroundColor = Color.White;
            g.GridColor = ThemeGrid;
            g.EnableHeadersVisualStyles = false;
            g.RowHeadersVisible = false;
            g.AllowUserToResizeRows = false;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 32;
            g.ColumnHeadersDefaultCellStyle.BackColor = ThemeNavy;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = ThemeNavy;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            g.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 0, 0);

            g.RowTemplate.Height = 26;
            g.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            g.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            g.DefaultCellStyle.SelectionBackColor = ThemeNavy;
            g.DefaultCellStyle.SelectionForeColor = Color.White;
            g.AlternatingRowsDefaultCellStyle.BackColor = ThemeAltRow;
        }

        private void StylePrimaryButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = ThemeNavy;
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            b.MouseEnter += (s, e) => b.BackColor = Color.FromArgb(0, 62, 130);
            b.MouseLeave += (s, e) => b.BackColor = ThemeNavy;
        }

        private void StyleOutlineButton(Button b, Color accent)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.BackColor = Color.White;
            b.ForeColor = accent;
            b.FlatAppearance.BorderColor = accent;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(243, 246, 250);
            b.Font = new Font("Segoe UI", 8.25F);
            b.Cursor = Cursors.Hand;
        }
    }
}
