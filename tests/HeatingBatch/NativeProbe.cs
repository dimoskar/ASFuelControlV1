using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ASFuelControl.Data;
using ASFuelControl.Invoicing.Persistence;
using Telerik.WinControls.UI;

internal static class HeatingNativeProbe
{
    static int passed;
    static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static object Field(object value,string name)=>value.GetType().GetField(name,Private).GetValue(value);
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);passed++;Console.WriteLine("PASS "+message);}
    static void Sql(string cs,string text){using(var cn=new SqlConnection(cs))using(var cmd=new SqlCommand(text,cn)){cn.Open();cmd.CommandTimeout=120;cmd.ExecuteNonQuery();}}
    [STAThread]
    static int Main(string[] args)
    {
        if(args.Length!=1||!System.Text.RegularExpressions.Regex.IsMatch(args[0],"^ASFC_HeatingBatch_[a-f0-9]{32}$"))return 2;
        var builder=new SqlConnectionStringBuilder{DataSource="(localdb)\\"+args[0],InitialCatalog="master",IntegratedSecurity=true,Pooling=false};
        var master=builder.ConnectionString;var name="Heating_"+Guid.NewGuid().ToString("N");builder.InitialCatalog=name;var cs=builder.ConnectionString;
        try {
            Sql(master,"CREATE DATABASE ["+name+"]; ALTER DATABASE ["+name+"] SET COMPATIBILITY_LEVEL=100;");
            DatabaseModel.CurrentUserId=Guid.NewGuid(); // Avoid bootstrap user lookup before synthetic schema exists.
            using(var db=new DatabaseModel(cs)) {
                var schema=db.GetSchemaHandler();
                // The old metadata generator emits CREATE SCHEMA dbo even for a newly created SQL database.
                var ddl=System.Text.RegularExpressions.Regex.Replace(schema.CreateDDLScript(),@"(?im)^CREATE SCHEMA \[dbo\];?\s*$","");
                schema.ForceExecuteDDLScript(ddl);
            }
            var fiscal=new SqlFiscalDatabase(cs);fiscal.InstallSchema();
            var assembly=typeof(ASFuelControl.Windows.UI.Catalogs.InvoiceCatalog).Assembly;
            var settings=(ApplicationSettingsBase)assembly.GetType("ASFuelControl.Windows.Properties.Settings").GetProperty("Default").GetValue(null,null);
            var unused=settings["DBConnection"];settings.PropertyValues["DBConnection"].PropertyValue=cs;
            DatabaseModel.ConnectionString=cs;
            var runtime=assembly.GetType("ASFuelControl.Windows.Threads.SharedInvoiceRuntime");
            var admin=Guid.NewGuid();var operatorId=Guid.NewGuid();
            Sql(cs,"INSERT dbo.ApplicationUser(ApplicationUserId,UserLevel,UserName,Password) VALUES('"+admin+"',0,'admin','synthetic'),('"+operatorId+"',1,'operator','synthetic');");
            var require=runtime.GetMethod("RequireHeatingAdmin",BindingFlags.NonPublic|BindingFlags.Static);
            DatabaseModel.UserLoggedIn=operatorId;bool denied=false;try{require.Invoke(null,null);}catch(TargetInvocationException e){denied=e.InnerException is InvalidOperationException;}
            Check(denied,"backend rejects a non-administrator");DatabaseModel.UserLoggedIn=admin;DatabaseModel.CurrentUserId=admin;
            Check((Guid)require.Invoke(null,null)==admin,"backend accepts the logged-in administrator");
            Finance(cs,fiscal,admin);
            Layout(assembly);
            Console.WriteLine("RESULT: "+passed+" native/accounting checks passed.");return 0;
        } catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
        finally{SqlConnection.ClearAllPools();Sql(master,"IF DB_ID(N'"+name+"') IS NOT NULL BEGIN ALTER DATABASE ["+name+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+name+"]; END;");}
    }
    static void Finance(string cs,SqlFiscalDatabase fiscal,Guid user)
    {
        var original=Guid.NewGuid();var credit=Guid.NewGuid();var replacement=Guid.NewGuid();var trader=Guid.NewGuid();
        var store=new SqlHeatingCorrectionStore(fiscal);
        using(var lease=fiscal.EnterInvoice(original))store.Reserve(lease,new HeatingCorrectionRecord{OriginalInvoiceId=original,CreditInvoiceId=credit,ReplacementInvoiceId=replacement,BatchId=Guid.NewGuid(),CreatedBy=user,PlanJson="{}"});
        using(var db=new DatabaseModel(cs)) {
            var c=new Invoice{InvoiceId=credit,TraderId=trader,PaymentType=0,TotalAmount=124,TransactionDate=DateTime.Now,
                InvoiceType=new InvoiceType{InvoiceTypeId=Guid.NewGuid(),IsCancelation=true,HasFinancialTransactions=true,TransactionType=0}};
            var r=new Invoice{InvoiceId=replacement,TraderId=trader,PaymentType=0,TotalAmount=124,TransactionDate=DateTime.Now,
                InvoiceType=new InvoiceType{InvoiceTypeId=Guid.NewGuid(),IsCancelation=false,HasFinancialTransactions=true,TransactionType=0}};
            c.CreateFinTransactions(db);r.CreateFinTransactions(db);c.CreateFinTransactions(db);r.CreateFinTransactions(db);
            Check(c.FinTransactions.Count==1&&r.FinTransactions.Count==1,"batch credit and replacement each retain exactly one ledger entry after repeated projection");
            Check(c.FinTransactions.Single().CreditAmount==-124&&r.FinTransactions.Single().CreditAmount==124&&c.FinTransactions.Single().DebitAmount==0,
                "credit and replacement ledger entries offset without a cash transaction");
            var ordinary=new Invoice{InvoiceId=Guid.NewGuid(),TraderId=trader,PaymentType=0,TotalAmount=124,InvoiceType=c.InvoiceType};ordinary.CreateFinTransactions(db);
            Check(ordinary.FinTransactions.Count==0,"unrelated legacy credit behavior is unchanged");
        }
    }
    static void Layout(Assembly assembly)
    {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        var themeAssembly=Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Telerik.WinControls.Themes.TelerikMetroBlue.dll"));
        Activator.CreateInstance(themeAssembly.GetType("Telerik.WinControls.Themes.TelerikMetroBlueTheme"));
        Telerik.WinControls.ThemeResolutionService.ApplicationThemeName="TelerikMetroBlue";
        var creditType=assembly.GetType("ASFuelControl.Windows.Threads.HeatingCreditType");var types=Array.CreateInstance(creditType,2);
        for(int i=0;i<2;i++){var t=Activator.CreateInstance(creditType,true);creditType.GetProperty("Id").SetValue(t,Guid.NewGuid());creditType.GetProperty("Name").SetValue(t,i==0?"Πιστωτικό λιανικής / ΠΛ":"Πιστωτικό τιμολόγιο / ΠΤ");creditType.GetProperty("Retail").SetValue(t,i==0);types.SetValue(t,i);}
        var formType=assembly.GetType("ASFuelControl.Windows.UI.Forms.HeatingCorrectionBatchForm");
        using(var form=(Form)Activator.CreateInstance(formType,Private,null,new object[]{new DateTime(2026,10,1),new DateTime(2026,10,22),types},null)) {
            form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();Application.DoEvents();
            var grid=(RadGridView)Field(form,"grid");var start=(RadButton)Field(form,"start");
            Check(grid.Columns.Count==8&&grid.Columns["Selected"].ReadOnly==false,"batch table contains selection, both correction documents and exclusion reason");
            Check(!start.Enabled,"batch cannot start before a review");
            var rowType=assembly.GetType("ASFuelControl.Windows.Threads.HeatingBatchRow");var rows=(IList)Field(form,"rows");
            var row=Activator.CreateInstance(rowType,true);
            Action<string,object> set=(n,v)=>rowType.GetProperty(n).SetValue(row,v);
            set("Document","ΤΠ Β 142");set("Provider","iTimologia");set("Supply","123456789");set("Amount",124m);set("Eligible",true);set("Selected",true);set("Status","Έτοιμο — λείπει η ένδειξη 11");rows.Add(row);
            formType.GetField("scanned",Private).SetValue(form,true);formType.GetMethod("UpdateButtons",Private).Invoke(form,null);
            Check(start.Enabled,"reviewed eligible selection enables the batch");
            var from=(RadDateTimePicker)Field(form,"from");from.Value=from.Value.AddDays(1);Application.DoEvents();
            Check(!start.Enabled&&!(bool)rowType.GetProperty("Eligible").GetValue(row),"changing filters invalidates the reviewed selection");
            foreach(int width in new[]{1230,1024}) {
                form.ClientSize=new Size(width,770);Application.DoEvents();
                var buttons=new[]{"export","print","pause","resume","start"}.Select(n=>(Control)Field(form,n)).ToArray();
                Check(buttons.All(b=>b.Bottom<=b.Parent.ClientSize.Height&&b.Right<=b.Parent.ClientSize.Width),"all batch actions fit at width "+width);
                for(int i=0;i<buttons.Length;i++)for(int j=i+1;j<buttons.Length;j++)Check(!buttons[i].Bounds.IntersectsWith(buttons[j].Bounds),"actions do not overlap at width "+width+" ("+i+","+j+")");
                using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"heating-batch-"+width+".png"));}
            }
            form.Close();
        }
    }
}
