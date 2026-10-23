using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Invoicing.Persistence
{
    public sealed class HeatingCorrectionRecord
    {
        public Guid OriginalInvoiceId, BatchId, CreditInvoiceId, ReplacementInvoiceId, CreatedBy;
        public string PlanJson, State, Message;
        public DateTime CreatedUtc;
    }

    public sealed class SqlHeatingCorrectionStore
    {
        private readonly SqlFiscalDatabase db;
        public SqlHeatingCorrectionStore(SqlFiscalDatabase database) { db = database; }
        // A single worker for the entire installation; session lock released on crash.
        public IDisposable EnterBatch()
        {
            var c = db.Open();
            try {
                using (var cmd = SqlFiscalDatabase.Command(c,null,SqlStatements.Acquire,"@resource","ASFuelControl.HeatingCorrection.Batch","@timeout",0))
                    if (Convert.ToInt32(cmd.ExecuteScalar()) < 0) throw new InvalidOperationException("Εκτελείται ήδη παρτίδα διόρθωσης σε άλλο παράθυρο ή υπολογιστή.");
                return c;
            } catch { c.Dispose(); throw; }
        }
        public HeatingCorrectionRecord[] Load()
        {
            using (var c = db.Open())
            using (var cmd = SqlFiscalDatabase.Command(c,null,"SELECT * FROM dbo.FiscalHeatingCorrection ORDER BY CreatedUtc,OriginalInvoiceId"))
            using (var r = cmd.ExecuteReader()) {
                var rows = new List<HeatingCorrectionRecord>();
                while (r.Read()) {
                    string json = (string)r["PlanJson"];
                    if (SqlFiscalDatabase.Hash(Encoding.UTF8.GetBytes(json)) != (string)r["PlanSha256"]) throw new InvalidOperationException("Το αποθηκευμένο σχέδιο διόρθωσης έχει αλλοιωθεί.");
                    rows.Add(new HeatingCorrectionRecord { OriginalInvoiceId=(Guid)r["OriginalInvoiceId"],BatchId=(Guid)r["BatchId"],
                        CreditInvoiceId=(Guid)r["CreditInvoiceId"],ReplacementInvoiceId=(Guid)r["ReplacementInvoiceId"],CreatedBy=(Guid)r["CreatedBy"],
                        PlanJson=json,State=(string)r["State"],Message=(string)r["Message"],CreatedUtc=(DateTime)r["CreatedUtc"] });
                }
                return rows.ToArray();
            }
        }
        public bool IsChild(Guid id, bool incompleteOnly = false)
        {
            using (var c=db.Open())
            using (var cmd=SqlFiscalDatabase.Command(c,null,@"IF OBJECT_ID(N'dbo.FiscalHeatingCorrection',N'U') IS NULL SELECT 0;
ELSE SELECT COUNT(*) FROM dbo.FiscalHeatingCorrection WHERE (CreditInvoiceId=@id OR ReplacementInvoiceId=@id) AND (@pending=0 OR State<>'Completed')",
                "@id",id,"@pending",incompleteOnly)) return Convert.ToInt32(cmd.ExecuteScalar()) != 0;
        }
        public void Reserve(IFiscalDocumentLease lease, HeatingCorrectionRecord row)
        {
            if (row.OriginalInvoiceId != lease.Document.DocumentId) throw new ArgumentException("Correction lease mismatch.");
            db.Locked(lease,(held,tx) => {
                using (var cmd=SqlFiscalDatabase.Command(held.Connection,tx,@"INSERT dbo.FiscalHeatingCorrection
(OriginalInvoiceId,BatchId,CreditInvoiceId,ReplacementInvoiceId,PlanJson,PlanSha256,State,Message,CreatedUtc,UpdatedUtc,CreatedBy)
VALUES(@id,@batch,@credit,@replacement,@plan,@hash,'Prepared',N'Αποθηκεύτηκε η παρτίδα.',@now,@now,@user)",
                    "@id",row.OriginalInvoiceId,"@batch",row.BatchId,"@credit",row.CreditInvoiceId,"@replacement",row.ReplacementInvoiceId,
                    "@plan",row.PlanJson,"@hash",SqlFiscalDatabase.Hash(Encoding.UTF8.GetBytes(row.PlanJson)),"@now",DateTime.UtcNow,"@user",row.CreatedBy)) cmd.ExecuteNonQuery();
                return true;
            });
        }
        public void Progress(IFiscalDocumentLease lease,string state,string message)
        {
            db.Locked(lease,(held,tx) => {
                using(var cmd=SqlFiscalDatabase.Command(held.Connection,tx,@"UPDATE dbo.FiscalHeatingCorrection SET State=@state,Message=@message,UpdatedUtc=@now
WHERE OriginalInvoiceId=@id AND State<>'Completed'","@state",state,"@message",message??"","@now",DateTime.UtcNow,"@id",lease.Document.DocumentId)) cmd.ExecuteNonQuery();
                return true;
            });
        }
        // Explicit persisted host columns: no navigation properties, computed fields or live pump data.
        private const string InvoiceColumns="InvoiceId,TraderId,Number,VehiclePlateNumber,InvoiceSignature,InvoiceTypeId,VehicleId,TransactionDate,NettoAmount,VatAmount,TotalAmount,Printer,InvoiceFormId,ApplicationUserId,Series,IsPrinted,Notes,PaymentType,DiscountAmount,IsEuromat,SupplyNumber,LastRestAmount,AllowEdit,VehicleOdometer,DeliveryAddress,QRCodeData,DiscountAmountWhole,DiscountAmountRetail,NettoAfterDiscount";
        private const string LineColumns="InvoiceLineId,InvoiceId,Volume,VolumeNormalized,Temperature,FuelDensity,UnitPrice,TotalPrice,VatAmount,VatPercentage,SaleTransactionId,TankFillingId,FuelTypeId,DiscountAmount,TankId,InvoiceRelationId,UnitPriceWhole,DiscountAmountWhole,UnitPriceRetail,DiscountAmountRetail,DiscountPercentage";
        public JObject ReadHost(Guid id)
        {
            using(var c=db.Open()) {
                var invoice=ReadRows(c,"Invoice",InvoiceColumns,id).Single();
                return new JObject { ["Invoice"]=invoice,["Lines"]=new JArray(ReadRows(c,"InvoiceLine",LineColumns,id)) };
            }
        }
        private static JObject[] ReadRows(SqlConnection c,string table,string columns,Guid id,SqlTransaction transaction=null)
        {
            using(var cmd=SqlFiscalDatabase.Command(c,transaction,"SELECT "+columns+" FROM dbo."+table+" WHERE InvoiceId=@id"+(table=="InvoiceLine"?" ORDER BY InvoiceLineId":""),"@id",id))
            using(var r=cmd.ExecuteReader()) {
                var values=new List<JObject>();
                while(r.Read()) {var row=new JObject();for(int i=0;i<r.FieldCount;i++)row[r.GetName(i)]=r.IsDBNull(i)?JValue.CreateNull():JToken.FromObject(r.GetValue(i)); values.Add(row);}return values.ToArray();
            }
        }
        // Clone and number atomically. Both records use credit/offset settlement and do not create physical fuel movements.
        public void CreateChild(IFiscalDocumentLease lease,JObject frozenHost,Guid typeId,string series,Guid userId,string note,DateTime issuedAt)
        {
            Guid id=lease.Document.DocumentId;
            db.Locked(lease,(held,tx) => {
                var existing=ReadRows(held.Connection,"Invoice",InvoiceColumns,id,tx).SingleOrDefault();
                if(existing!=null) {
                    var expected=(JObject)frozenHost["Invoice"].DeepClone();
                    expected["InvoiceTypeId"]=typeId;expected["Series"]=series;expected["PaymentType"]=0;
                    foreach(string field in new[]{"InvoiceTypeId","Series","PaymentType","TraderId","SupplyNumber","NettoAmount","VatAmount","TotalAmount","DiscountAmount","NettoAfterDiscount"})
                        if(!SameValue(existing[field],expected[field]))throw new InvalidOperationException("Το διορθωτικό παραστατικό διαφέρει από το αποθηκευμένο σχέδιο: "+field);
                    if((int)existing["Number"]<=0)throw new InvalidOperationException("Το διορθωτικό παραστατικό δεν έχει έγκυρη αρίθμηση.");
                    var expectedLines=frozenHost["Lines"].Select(l=>CleanLine((JObject)l)).ToList();
                    foreach(var line in ReadRows(held.Connection,"InvoiceLine",LineColumns,id,tx)) {
                        foreach(string field in new[]{"SaleTransactionId","TankFillingId","TankId","InvoiceRelationId"})
                            if(line[field].Type!=JTokenType.Null)throw new InvalidOperationException("Το διορθωτικό παραστατικό συνδέθηκε με φυσική διακίνηση.");
                        var clean=CleanLine(line);int index=expectedLines.FindIndex(l=>SameValue(l,clean));
                        if(index<0)throw new InvalidOperationException("Οι γραμμές του διορθωτικού παραστατικού άλλαξαν.");
                        expectedLines.RemoveAt(index);
                    }
                    if(expectedLines.Count!=0)throw new InvalidOperationException("Λείπουν γραμμές του διορθωτικού παραστατικού.");
                    return true;
                }
                int last;
                using(var cmd=SqlFiscalDatabase.Command(held.Connection,tx,"SELECT LastNumber FROM dbo.InvoiceType WITH(UPDLOCK,HOLDLOCK) WHERE InvoiceTypeId=@type","@type",typeId)) {
                    object value=cmd.ExecuteScalar(); if(value==null||value==DBNull.Value)throw new InvalidOperationException("Δεν υπάρχει μετρητής παραστατικού.");last=checked(Convert.ToInt32(value)+1);
                }
                using(var cmd=SqlFiscalDatabase.Command(held.Connection,tx,"SELECT COUNT(*) FROM dbo.Invoice WHERE InvoiceTypeId=@type AND Number>=@number","@type",typeId,"@number",last))
                    if(Convert.ToInt32(cmd.ExecuteScalar())>0)throw new InvalidOperationException("Ο μετρητής παραστατικών είναι πίσω από το ιστορικό.");
                var invoice=(JObject)frozenHost["Invoice"].DeepClone();
                invoice["InvoiceId"]=id;invoice["InvoiceTypeId"]=typeId;invoice["Number"]=last;invoice["Series"]=series;
                invoice["TransactionDate"]=issuedAt;invoice["ApplicationUserId"]=userId;
                invoice["InvoiceSignature"]=null;invoice["QRCodeData"]=null;invoice["IsPrinted"]=false;invoice["AllowEdit"]=false;
                invoice["IsEuromat"]=false;invoice["PaymentType"]=0;invoice["LastRestAmount"]=0;
                invoice["Notes"]=string.IsNullOrWhiteSpace((string)invoice["Notes"])?note:(string)invoice["Notes"]+"\r\n"+note;
                // Use the selected document's form/printer, not the original sale's credit layout.
                using(var cmd=SqlFiscalDatabase.Command(held.Connection,tx,"SELECT Printer,InvoiceFormId FROM dbo.InvoiceType WHERE InvoiceTypeId=@type","@type",typeId))
                using(var r=cmd.ExecuteReader()){r.Read();invoice["Printer"]=r.IsDBNull(0)?null:JToken.FromObject(r[0]);invoice["InvoiceFormId"]=r.IsDBNull(1)?null:JToken.FromObject(r[1]);}
                Insert(held.Connection,tx,"Invoice",InvoiceColumns,invoice);
                foreach(JObject old in frozenHost["Lines"]) {
                    var line=(JObject)old.DeepClone();line["InvoiceLineId"]=Guid.NewGuid();line["InvoiceId"]=id;
                    foreach(var field in new[]{"SaleTransactionId","TankFillingId","TankId","InvoiceRelationId"})line[field]=null;
                    Insert(held.Connection,tx,"InvoiceLine",LineColumns,line);
                }
                using(var cmd=SqlFiscalDatabase.Command(held.Connection,tx,"UPDATE dbo.InvoiceType SET LastNumber=@number WHERE InvoiceTypeId=@type","@number",last,"@type",typeId))cmd.ExecuteNonQuery();
                return true;
            });
        }
        private static bool SameValue(JToken left,JToken right) => JToken.DeepEquals(
            JToken.Parse((left??JValue.CreateNull()).ToString(Formatting.None)),JToken.Parse((right??JValue.CreateNull()).ToString(Formatting.None)));
        private static JObject CleanLine(JObject line)
        {
            var copy=(JObject)line.DeepClone();
            foreach(string field in new[]{"InvoiceLineId","InvoiceId","SaleTransactionId","TankFillingId","TankId","InvoiceRelationId"})copy.Remove(field);
            return copy;
        }
        private static void Insert(SqlConnection c,SqlTransaction tx,string table,string columns,JObject row)
        {
            var names=columns.Split(',');
            using(var cmd=new SqlCommand("INSERT dbo."+table+" ("+columns+") VALUES ("+string.Join(",",names.Select(n=>"@"+n))+")",c,tx)) {
                foreach(string name in names)cmd.Parameters.AddWithValue("@"+name,(row[name] as JValue)?.Value??DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
