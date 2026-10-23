using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Exedron.Invoicing.Adapters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static HeatingCorrectionPlan HeatingPlan(string provider, bool retail=false, int fuel=30)
    {
        var source=new Source();source.Invoice.SupplyNumber="123456789";
        var route=Route(provider);var request=source.Prepare(source.Id,route);
        request.Invoice.InvoiceSeries.InvoiceType.IsRetail=retail;
        request.Invoice.InvoiceLines[0].OfficialFuelCode=fuel;
        return new HeatingCorrectionPlan{OriginalId=source.Id,CreditId=Guid.NewGuid(),ReplacementId=Guid.NewGuid(),
            OriginalTypeId=Guid.NewGuid(),CreditTypeId=Guid.NewGuid(),OriginalAttemptId=Guid.NewGuid(),OriginalMark=900010,
            CreditSeries="C",ReplacementSeries="TEST",OriginalDescription="TEST 42",
            Original=new InvoiceWorkflowSnapshot{InvoiceId=source.Id,Invoice=request,Provider=route}};
    }
    static JObject HeatingPayload(HeatingCorrectionPlan p,bool? credit=null)
    {
        var model=new InvoiceBuilder(new InvoiceTypeResolver()).Build(credit.HasValue?p.Request(credit.Value,43,"TEST",new DateTime(2026,10,22)):p.Original.Invoice);
        return JObject.Parse(new InvoiceAdapterFactory(new InvoiceTypeResolver()).CreatePayloadMapper(p.Original.Provider.ProviderKey)
            .Serialize(model,credit.HasValue?(credit.Value?p.CreditId:p.ReplacementId):p.OriginalId));
    }
    static string MissingCategory(JObject payload)
    {
        var copy=(JObject)payload.DeepClone();
        (copy["invoice_header"] as JObject)?.Remove("special_invoice_category");
        (copy["aadeData"] as JObject)?.Remove("aadeSpecialInvoiceCategory");
        var doc=(copy["additionalSupportDocs"] as JArray)?.Single(d=>(string)d["description"]=="##InvoicesDoc##");
        if(doc!=null){var xml=XDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String((string)doc["reference"])));
            xml.Descendants().Where(e=>e.Name.LocalName=="specialInvoiceCategory").Remove();doc["reference"]=Convert.ToBase64String(Encoding.UTF8.GetBytes(xml.ToString()));}
        return copy.ToString();
    }
    static void HeatingBatchTests()
    {
        foreach(string provider in new[]{InvoiceProviderCatalog.ITimologiaKey,InvoiceProviderCatalog.DirectIlydaKey})
        foreach(bool retail in new[]{false,true})
        foreach(int fuel in new[]{30,31})
        Test("heating batch preserves original and creates correlated pair: "+provider+" retail="+retail+" fuel="+fuel,()=>{
            var plan=HeatingPlan(provider,retail,fuel);string before=JsonConvert.SerializeObject(plan);
            HeatingCorrectionPolicy.Validate(plan,MissingCategory(HeatingPayload(plan)));
            foreach(bool credit in new[]{true,false}) {
                var req=plan.Request(credit,43,"TEST",new DateTime(2026,10,22));var built=new InvoiceBuilder(new InvoiceTypeResolver()).Build(req);
                Equal(credit?(retail?"11.4":"5.1"):(retail?"11.1":"1.1"),built.InvoiceHeader.InvoiceType);
                Equal(InvoicePaymentType.Credit,req.Invoice.PaymentType);Equal(124m,req.Invoice.TotalAmount);
                Equal(11,MyDataHeaderFields.SpecialInvoiceCategory(built).Value);
                Equal(credit?1:0,req.Parents.Count());if(credit)Equal(plan.OriginalMark,req.Parents.First().Transmission.Mark.Value);
                var payload=HeatingPayload(plan,credit);
                if(provider==InvoiceProviderCatalog.ITimologiaKey){Equal(JTokenType.String,payload["invoice_header"]["special_invoice_category"].Type);Equal("11",(string)payload["invoice_header"]["special_invoice_category"]);}
                else{Equal(JTokenType.Integer,payload["aadeData"]["aadeSpecialInvoiceCategory"].Type);var xml=XDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String((string)payload["additionalSupportDocs"][0]["reference"])));Equal("11",xml.Descendants().Single(e=>e.Name.LocalName=="specialInvoiceCategory").Value);}
            }
            Equal(before,JsonConvert.SerializeObject(plan));
            Throws(()=>HeatingCorrectionPolicy.Validate(plan,HeatingPayload(plan).ToString()));
        });
        Test("heating batch excludes absent supply, other fuels, unsupported providers and POS",()=>{
            var p=HeatingPlan(InvoiceProviderCatalog.ITimologiaKey);string sent=MissingCategory(HeatingPayload(p));
            p.Original.Invoice.Invoice.SupplyCode=" ";Throws(()=>HeatingCorrectionPolicy.Validate(p,sent));
            p=HeatingPlan(InvoiceProviderCatalog.ITimologiaKey);p.Original.Invoice.Invoice.InvoiceLines[0].OfficialFuelCode=10;Throws(()=>HeatingCorrectionPolicy.Validate(p,sent));
            p=HeatingPlan(InvoiceProviderCatalog.MyDataKey);Throws(()=>HeatingCorrectionPolicy.Validate(p,sent));
            p=HeatingPlan(InvoiceProviderCatalog.ITimologiaKey);p.Original.Invoice.Invoice.PaymentType=InvoicePaymentType.Card;Throws(()=>HeatingCorrectionPolicy.Validate(p,sent));
        });
        Test("batch resume after uncertain credit performs lookup before replacement, without another credit POST",()=>{
            var plan=HeatingPlan(InvoiceProviderCatalog.ITimologiaKey);var creditHttp=new Http(plan.CreditId,InvoiceProviderCatalog.ITimologiaKey){InvoicePost=s=>throw new System.Net.Http.HttpRequestException("lost reply"),Lookup=()=>Reply("{}",503)};
            var replacementHttp=new Http(plan.ReplacementId,InvoiceProviderCatalog.ITimologiaKey);var printer=new Printer();string state="";
            Func<bool,InvoiceWorkflowResult> issue=c=>Workflow(c?creditHttp:replacementHttp,printer).Process(c?plan.CreditId:plan.ReplacementId,
                new WorkflowSource{Snapshot=new InvoiceWorkflowSnapshot{InvoiceId=c?plan.CreditId:plan.ReplacementId,Provider=plan.Original.Provider,
                    Invoice=plan.Request(c,c?43:44,c?"C":"TEST",new DateTime(2026,10,22)),PrintDestination="test",PrintFormat="test/v1",PrintContent=Encoding.UTF8.GetBytes("batch")}},false,retryRejected:true);
            HeatingCorrectionRunner.Run(issue,(s,m)=>state=s,()=>false);Equal("CreditPending",state);Equal(1,creditHttp.Posts);Equal(0,replacementHttp.Posts);
            creditHttp.Lookup=null;
            HeatingCorrectionRunner.Run(issue,(s,m)=>state=s,()=>false);Equal("Completed",state);Equal(1,creditHttp.Posts);Equal(1,replacementHttp.Posts);Check(creditHttp.Gets>0,"credit was not looked up");
            HeatingCorrectionRunner.Run(issue,(s,m)=>state=s,()=>false);Equal(1,creditHttp.Posts);Equal(1,replacementHttp.Posts);Equal(0,printer.Calls);Equal(0,creditHttp.Signatures);
        });
        Test("pause after accepted credit retains the pair and sends replacement only on resume",()=>{
            var plan=HeatingPlan(InvoiceProviderCatalog.ITimologiaKey);var http=new Http(plan.CreditId,InvoiceProviderCatalog.ITimologiaKey);var printer=new Printer();bool pause=false;int replacements=0;
            var credit=Workflow(http,printer).Process(plan.CreditId,new WorkflowSource{Snapshot=new InvoiceWorkflowSnapshot{InvoiceId=plan.CreditId,Provider=plan.Original.Provider,Invoice=plan.Request(true,43,"C",DateTime.Now),PrintDestination="test",PrintFormat="test",PrintContent=new byte[]{1}}},false);
            HeatingCorrectionRunner.Run(c=>{if(!c)replacements++;return credit;},(s,m)=>pause=true,()=>pause);Equal(0,replacements);
            pause=false;HeatingCorrectionRunner.Run(c=>{if(!c)replacements++;return credit;},(s,m)=>{},()=>pause);Equal(1,replacements);Equal(1,http.Posts);
        });
        foreach(string provider in new[]{InvoiceProviderCatalog.ITimologiaKey,InvoiceProviderCatalog.DirectIlydaKey})
        Test(provider+" replacement timeout cannot duplicate either document",()=>{
            var p=HeatingPlan(provider);var c=new Http(p.CreditId,provider);var r=new Http(p.ReplacementId,provider){InvoicePost=s=>throw new System.Net.Http.HttpRequestException("lost reply"),Lookup=()=>Reply("{}",503)};
            var printer=new Printer();string state="";
            Func<bool,InvoiceWorkflowResult> issue=credit=>Workflow(credit?c:r,printer).Process(credit?p.CreditId:p.ReplacementId,
                new WorkflowSource{Snapshot=new InvoiceWorkflowSnapshot{InvoiceId=credit?p.CreditId:p.ReplacementId,Provider=p.Original.Provider,
                    Invoice=p.Request(credit,credit?43:44,credit?"C":"TEST",DateTime.Now),PrintDestination="test",PrintFormat="test/v1",PrintContent=new byte[]{1}}},false,retryRejected:true);
            HeatingCorrectionRunner.Run(issue,(s,m)=>state=s,()=>false);Equal("ReplacementPending",state);Equal(1,c.Posts);Equal(1,r.Posts);
            r.Lookup=()=>Reply(provider==InvoiceProviderCatalog.ITimologiaKey?TimReply(p.ReplacementId):IlydaReply);
            HeatingCorrectionRunner.Run(issue,(s,m)=>state=s,()=>false);
            // Direct Ilyda has no documented lookup for a normal ambiguous submission.
            Equal(provider==InvoiceProviderCatalog.ITimologiaKey?"Completed":"ReplacementPending",state);
            Equal(1,c.Posts);Equal(1,r.Posts);Equal(0,printer.Calls);
        });
        Test("Ilyda offline credit blocks replacement until the queue confirms acceptance",()=>{
            var p=HeatingPlan(InvoiceProviderCatalog.DirectIlydaKey);
            var credit=new Http(p.CreditId,p.Original.Provider.ProviderKey){InvoicePost=b=>Reply("{\"invoiceMarking\":{\"invoiceIdentifier\":\"UID\",\"qrCode\":\"https://example.invalid/qr\",\"providerUrl\":\"https://example.invalid\"},\"errors\":[{\"code\":\"MQ002\",\"fatal\":false},{\"code\":\"I9999\",\"fatal\":true}]}")};
            var replacement=new Http(p.ReplacementId,p.Original.Provider.ProviderKey);var printer=new Printer();string state="";int projections=0;
            Func<bool,InvoiceWorkflowResult> issue=c=>Workflow(c?credit:replacement,printer,project:(id,result)=>{if(result.Accepted)projections++;}).Process(c?p.CreditId:p.ReplacementId,
                new WorkflowSource{Snapshot=new InvoiceWorkflowSnapshot{InvoiceId=c?p.CreditId:p.ReplacementId,Provider=p.Original.Provider,
                    Invoice=p.Request(c,c?43:44,c?"C":"TEST",DateTime.Now),PrintDestination="test",PrintFormat="test/v1",PrintContent=new byte[]{1}}},false,retryRejected:true);
            HeatingCorrectionRunner.Run(issue,(s,m)=>state=s,()=>false);Equal("CreditPending",state);Equal(0,replacement.Posts);Equal(0,projections);
            credit.Lookup=()=>Reply(new JObject{["uid"]="UID",["sellerVatIdentifier"]="000000000",["invoiceNumber"]=JObject.Parse(credit.LastPayload)["invoiceNumber"],
                ["invoiceState"]="SUBMITTED",["mark"]="900010",["verificationHash"]="HASH"}.ToString());
            HeatingCorrectionRunner.Run(issue,(s,m)=>state=s,()=>false);Equal("Completed",state);Equal(1,credit.Posts);Equal(1,replacement.Posts);Equal(0,printer.Calls);Equal(2,projections);
        });
        Test("batch rejects mismatched original provider identity and totals",()=>{
            var p=HeatingPlan(InvoiceProviderCatalog.ITimologiaKey);var body=JObject.Parse(MissingCategory(HeatingPayload(p)));
            body["invoice_header"]["aa"]="999";Throws(()=>HeatingCorrectionPolicy.Validate(p,body.ToString()));
            body=JObject.Parse(MissingCategory(HeatingPayload(p)));body["invoice_summary"]["total_gross_value"]="123.00";Throws(()=>HeatingCorrectionPolicy.Validate(p,body.ToString()));
        });
        HeatingStoreTests();
    }
    static void HeatingStoreTests()
    {
        // Extend the existing synthetic numbering fixture only; never open application settings/databases.
        Execute(connection,"DROP TRIGGER TestInvoiceLines; ALTER TABLE dbo.InvoiceType ADD Printer nvarchar(500),InvoiceFormId uniqueidentifier;");
        string ids="VehicleId,InvoiceFormId,ApplicationUserId";
        string decimals="NettoAmount,VatAmount,TotalAmount,DiscountAmount,LastRestAmount,VehicleOdometer,DiscountAmountWhole,DiscountAmountRetail,NettoAfterDiscount";
        foreach(string column in ids.Split(','))Execute(connection,"ALTER TABLE dbo.Invoice ADD "+column+" uniqueidentifier NULL;");
        foreach(string column in decimals.Split(','))Execute(connection,"ALTER TABLE dbo.Invoice ADD "+column+" decimal(18,4) NULL;");
        foreach(string column in "Printer,Notes,SupplyNumber,DeliveryAddress".Split(','))Execute(connection,"ALTER TABLE dbo.Invoice ADD "+column+" nvarchar(max) NULL;");
        Execute(connection,"ALTER TABLE dbo.Invoice ADD PaymentType int NULL,IsEuromat bit NULL,AllowEdit bit NULL;");
        foreach(string column in "InvoiceLineId,SaleTransactionId,TankFillingId,FuelTypeId,TankId,InvoiceRelationId".Split(','))Execute(connection,"ALTER TABLE dbo.InvoiceLine ADD "+column+" uniqueidentifier NULL;");
        foreach(string column in "Volume,VolumeNormalized,Temperature,FuelDensity,UnitPrice,TotalPrice,VatAmount,VatPercentage,DiscountAmount,UnitPriceWhole,DiscountAmountWhole,UnitPriceRetail,DiscountAmountRetail,DiscountPercentage".Split(','))Execute(connection,"ALTER TABLE dbo.InvoiceLine ADD "+column+" decimal(18,4) NULL;");
        Test("batch schema is idempotent and ownership protects reserved children before creation",()=>{
            database.InstallSchema();database.InstallSchema();var store=new SqlHeatingCorrectionStore(database);var p=HeatingPlan(InvoiceProviderCatalog.ITimologiaKey);
            using(var lease=database.EnterInvoice(p.OriginalId))store.Reserve(lease,new HeatingCorrectionRecord{OriginalInvoiceId=p.OriginalId,CreditInvoiceId=p.CreditId,ReplacementInvoiceId=p.ReplacementId,BatchId=Guid.NewGuid(),CreatedBy=Guid.NewGuid(),PlanJson=JsonConvert.SerializeObject(p)});
            Check(store.IsChild(p.CreditId,true),"reserved child not protected");using(var lease=database.EnterInvoice(p.CreditId))Check(new SqlFiscalOwnershipStore(database).Owns(lease),"ownership missed reservation");
            using(var lease=database.EnterInvoice(p.OriginalId)){store.Progress(lease,"Completed","done");store.Progress(lease,"Prepared","should not regress");}
            Check(!store.IsChild(p.CreditId,true)&&store.IsChild(p.CreditId),"completed pair protection");
            using(store.EnterBatch())Throws(()=>new SqlHeatingCorrectionStore(new SqlFiscalDatabase(connection)).EnterBatch());
            using(store.EnterBatch()){}
            Execute(connection,"UPDATE dbo.FiscalHeatingCorrection SET PlanJson=N'{}' WHERE OriginalInvoiceId='"+p.OriginalId+"';");Throws(()=>store.Load());
            Execute(connection,"DELETE dbo.FiscalHeatingCorrection WHERE OriginalInvoiceId='"+p.OriginalId+"';");
        });
        Test("child clone and number are atomic, repeatable and preserve the original physical and financial data",()=>{
            var store=new SqlHeatingCorrectionStore(database);var original=Guid.NewGuid();var type=Guid.NewGuid();var child=Guid.NewGuid();var user=Guid.NewGuid();
            Execute(connection,"INSERT dbo.InvoiceType(InvoiceTypeId,LastNumber,DefaultSeries) VALUES('"+type+"',20,'C'); INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate,TraderId,NettoAmount,VatAmount,TotalAmount,SupplyNumber,PaymentType,IsPrinted) VALUES('"+original+"','"+type+"',20,'C','20261019','"+Guid.NewGuid()+"',100,24,124,'123456789',1,1); INSERT dbo.InvoiceLine(InvoiceId,InvoiceLineId,SaleTransactionId,FuelTypeId,Volume,TotalPrice,VatAmount) VALUES('"+original+"','"+Guid.NewGuid()+"','"+Guid.NewGuid()+"','"+Guid.NewGuid()+"',50,124,24);");
            var frozen=store.ReadHost(original);string before=frozen.ToString();
            using(var lease=database.EnterInvoice(child)){store.CreateChild(lease,frozen,type,"C",user,"correction",DateTime.Now);store.CreateChild(lease,JObject.Parse(frozen.ToString()),type,"C",user,"correction",DateTime.Now);}
            var saved=store.ReadHost(child);Equal(21,(int)saved["Invoice"]["Number"]);Equal(0,(int)saved["Invoice"]["PaymentType"]);Equal(false,(bool)saved["Invoice"]["IsPrinted"]);Equal(124m,(decimal)saved["Invoice"]["TotalAmount"]);
            Equal(JTokenType.Null,saved["Lines"][0]["SaleTransactionId"].Type);Equal(before,store.ReadHost(original).ToString());
            Execute(connection,"UPDATE dbo.Invoice SET TotalAmount=999 WHERE InvoiceId='"+child+"';");using(var lease=database.EnterInvoice(child))Throws(()=>store.CreateChild(lease,frozen,type,"C",user,"correction",DateTime.Now));
            var failedId=Guid.NewGuid();Execute(connection,"CREATE TRIGGER RejectHeatingLine ON dbo.InvoiceLine AFTER INSERT AS BEGIN RAISERROR('test rollback',16,1); ROLLBACK TRANSACTION; END;");
            try{using(var lease=database.EnterInvoice(failedId))Throws(()=>store.CreateChild(lease,frozen,type,"C",user,"correction",DateTime.Now));}finally{Execute(connection,"DROP TRIGGER RejectHeatingLine;");}
            using(var lease=database.EnterInvoice(failedId))store.CreateChild(lease,frozen,type,"C",user,"correction",DateTime.Now);
            Equal(22,(int)store.ReadHost(failedId)["Invoice"]["Number"]);
        });
    }
}
