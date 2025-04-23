namespace ASFuelControl.Reports
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using Telerik.Reporting;
    using Telerik.Reporting.Drawing;
    using System.Linq;

    /// <summary>
    /// Summary description for TankStausReport.
    /// </summary>
    public partial class TankStausReport : Telerik.Reporting.Report
    {
        public TankStausReport()
        {
            //
            // Required for telerik Reporting designer support
            //
            InitializeComponent();
            this.pictureBox1.Value = Properties.Resources.MainLogo;
            //
            // TODO: Add any constructor code after InitializeComponent call
            //
        }
    }

    public static class TankReportFunctions
    {
        private static Data.DatabaseModel database = new Data.DatabaseModel(Data.Implementation.OptionHandler.ConnectionString);

        [Telerik.Reporting.Expressions.Function(Category = "Tank Functions", Description = "Returns the Tank volume at the specified level")]
        public static decimal GetVolume(Guid tankId, decimal level)
        {
            Data.Tank tank = database.Tanks.Where(t => t.TankId == tankId).FirstOrDefault();
            if (tank == null)
                return 0;
            return tank.GetTankVolume(level);
        }

        [Telerik.Reporting.Expressions.Function(Category = "Tank Functions", Description = "Returns the Tank volume at 15 oC at the specified level")]
        public static decimal GetVolumeNormalized(Guid tankId, decimal level)
        {
            Data.Tank tank = database.Tanks.Where(t => t.TankId == tankId).FirstOrDefault();
            if (tank == null)
                return 0;
            return tank.GetTankVolumeNormalized(level);
        }
    }
}