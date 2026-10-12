using ItemCreationInterCompanyUtilityVehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ItemCreationInterCompanyUtility
{
    static class Program
    {
        public static UserLogin usr;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
             usr = new UserLogin();
            Application.Run(Program.usr);
            // Application.Run(new FrmCompanySelection());

        }
    }
}
