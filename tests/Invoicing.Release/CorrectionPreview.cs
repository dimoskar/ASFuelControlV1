using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
class CorrectionPreview
{
    [STAThread] static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s,e) => Assembly.LoadFrom(System.IO.Path.Combine(args[0],new AssemblyName(e.Name).Name + ".dll"));
        Preview(args);
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Preview(string[] args)
    {
        // Load the EXE explicitly so the handler above can resolve its dependencies.
        var assembly = Assembly.LoadFrom(System.IO.Path.Combine(args[0],"MyDataCorrection.exe"));
        Application.EnableVisualStyles();
        using (var form = (Form)Activator.CreateInstance(assembly.GetType("MyDataCorrection.Form1")))
        {
            var grid = (DataGridView)form.GetType().GetField("evidenceGrid",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
            if (!grid.ReadOnly || grid.AllowUserToDeleteRows || grid.AllowUserToAddRows) throw new Exception("Report must be read-only");
            grid.DataSource = new[] {
                new ASFuelControl.Invoicing.Persistence.FiscalMigrationRow { InvoiceId=Guid.NewGuid(),Date=new DateTime(2026,10,16),Number=42,Series="TEST",Workflow=true,Attempts=3,Mark=900010 },
                new ASFuelControl.Invoicing.Persistence.FiscalMigrationRow { InvoiceId=Guid.NewGuid(),Date=new DateTime(2026,10,16),Number=41,Series="TEST",LegacyRows=1,Mark=900009,PrintedFlag=true },
                new ASFuelControl.Invoicing.Persistence.FiscalMigrationRow { InvoiceId=Guid.NewGuid(),Date=new DateTime(2026,10,16),Number=40,Series="TEST",PrintedFlag=true }
            };
            form.Show(); Application.DoEvents();
            using (var bitmap = new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size)); bitmap.Save(args[1]); }
            form.Close();
        }
        Console.WriteLine("PASS correction utility read-only controls and native render");
    }
}
