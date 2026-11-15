using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using ASFuelControl.Common;
using ASFuelControl.Common.Enumerators;
using ASFuelControl.Common.Sales;
using ASFuelControl.VirtualDevices;
using ASFuelControl.Windows.Threads;
using ASFuelControl.WorkFlow;

internal static class Program
{
    private static string releaseDirectory;

    private static int Main(string[] args)
    {
        releaseDirectory = Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve += ResolveApplicationAssembly;
        try
        {
            Tests.Run();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static Assembly ResolveApplicationAssembly(object sender, ResolveEventArgs args)
    {
        AssemblyName name = new AssemblyName(args.Name);
        string fileName = name.Name + (name.Name == "ASFuelControl.Windows" ? ".exe" : ".dll");
        string path = Path.Combine(releaseDirectory, fileName);
        bool exists = File.Exists(path);
        return exists ? Assembly.LoadFrom(path) : null;
    }
}

internal static class Tests
{
    private static int dispatches;
    private static bool failDispatch;
    private static bool confirmDispatch;
    private static readonly HashSet<Guid> committed = new HashSet<Guid>();

    public static void Run()
    {
        ValuesSurviveStatusCoalescing();
        ValueQueueSupportsConcurrentReceipts();
        CallbackFailurePreservesControllerValue();
        ControllerRetainsWithoutApplicationSubscriber();
        CompletionFailureRetriesSameSnapshot();
        CompletionBeforeNozzleMappingIsRetained();
        DistinctSalesWithIdenticalTotalizers();
        DispatchFailureRetriesSameSale();
        UnconfirmedSaleCannotBeRemoved();
        WorkflowCompletionOnlyQueues();
        PendingSalesAreSharedAcrossInstances();
        PreparationRetryKeepsCapturedContext();
        PreparationFinishesWithoutExtraRetryDelay();
        FailedDispenserDoesNotBlockAnother();
        Console.WriteLine("PASS: 14 sale-retention scenarios.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }

    private static SaleData NewSale()
    {
        SaleData sale = new SaleData();
        sale.NozzleId = Guid.NewGuid();
        sale.TotalizerStart = 1000;
        sale.TotalizerEnd = 2000;
        sale.TotalVolume = 10;
        sale.UnitPrice = 2;
        sale.TotalPrice = 20;
        sale.TankData = new TankSaleData[0];
        return sale;
    }

    private static FuelPointValues SaleValues(SaleData sale)
    {
        FuelPointValues values = new FuelPointValues();
        values.Sale = sale;
        values.Status = FuelPointStatusEnum.Idle;
        return values;
    }

    private static void ValuesSurviveStatusCoalescing()
    {
        ValueHolder holder = new ValueHolder();
        FuelPointValues work = new FuelPointValues();
        work.Status = FuelPointStatusEnum.Work;
        holder.AddValues(work);
        SaleData first = NewSale();
        SaleData second = NewSale();
        second.NozzleId = first.NozzleId;
        FuelPointValues firstValues = SaleValues(first);
        FuelPointValues secondValues = SaleValues(second);
        holder.AddValues(firstValues);
        holder.AddValues(secondValues);
        holder.AddValues(firstValues);
        FuelPointValues nextWork = new FuelPointValues();
        nextWork.Status = FuelPointStatusEnum.Work;
        nextWork.CurrentVolume = 1;
        holder.AddValues(nextWork);
        holder.ClearValues(FuelPointStatusEnum.Work);
        FuelPointValues firstResult = holder.GetValues();
        FuelPointValues secondResult = holder.GetValues();
        FuelPointValues end = holder.GetValues();
        Assert(firstResult.Sale.SalesId == first.SalesId && secondResult.Sale.SalesId == second.SalesId && end == null,
            "Status replacement discarded or merged a completed sale.");
    }

    private sealed class Producer
    {
        public ValueHolder Holder;
        public void Produce()
        {
            for (int index = 0; index < 500; index++)
            {
                SaleData sale = NewSale();
                FuelPointValues values = SaleValues(sale);
                this.Holder.AddValues(values);
            }
        }
    }

    private static void ValueQueueSupportsConcurrentReceipts()
    {
        ValueHolder holder = new ValueHolder();
        Producer producer = new Producer();
        producer.Holder = holder;
        Thread first = new Thread(producer.Produce);
        Thread second = new Thread(producer.Produce);
        first.Start();
        second.Start();
        HashSet<Guid> received = new HashSet<Guid>();
        while (first.IsAlive || second.IsAlive)
        {
            FuelPointValues values = holder.GetValues();
            if (values != null)
            {
                received.Add(values.Sale.SalesId);
            }
            Thread.Yield();
        }
        FuelPointValues remaining;
        while ((remaining = holder.GetValues()) != null)
        {
            received.Add(remaining.Sale.SalesId);
        }
        Assert(received.Count == 1000, "Concurrent controller receipts lost sales.");
    }

    private sealed class TestController : FuelPumpControllerBase, IController
    {
        private readonly FuelPoint point = new FuelPoint();
        public TestController()
        {
            ValueHolder holder = new ValueHolder();
            holder.FuelPump = this.point;
            this.internalQueue.TryAdd(this.point, holder);
        }
        public void Receive(FuelPointValues values)
        {
            this.EnqueValues(this.point, values);
        }
        public FuelPointValues Take()
        {
            return this.internalQueue[this.point].GetValues();
        }
        public FuelPoint Point
        {
            get
            {
                return this.point;
            }
        }
        public void CompleteTotals(decimal total)
        {
            TotalsEventArgs args = new TotalsEventArgs(this.point, 1, total, 0);
            this.OnTransactionCompleted(args);
        }
        public void CompleteDirectSale(decimal total, decimal amount, decimal volume)
        {
            SaleEventArgs args = new SaleEventArgs(this.point, 1, total, 0, amount, volume);
            this.OnSaleRecieved(args);
        }
        ASFuelControl.Euromat.Transaction IController.GetEuromatTransaction(int channel, int address)
        {
            return null;
        }
    }

    private static void ThrowOnReceipt(object sender, FuelPointValuesArgs args)
    {
        throw new InvalidOperationException("Injected receipt failure");
    }

    private static void CallbackFailurePreservesControllerValue()
    {
        TestController controller = new TestController();
        controller.SaleRetentionRequired += ThrowOnReceipt;
        SaleData sale = NewSale();
        FuelPointValues values = SaleValues(sale);
        controller.Receive(values);
        FuelPointValues retained = controller.Take();
        Assert(retained != null && retained.Sale.SalesId == sale.SalesId, "Callback failure discarded the received sale.");
        PendingSaleRecord record;
        bool registered = PendingSaleRegistry.Instance.TryGetValue(sale.SalesId, out record);
        Assert(registered, "Subscriber failure prevented early registry capture.");
        ReleaseTestSale(sale);
    }

    private static void ReleaseTestSale(SaleData sale)
    {
        PendingSaleRecord record = PendingSaleRegistry.Instance[sale.SalesId];
        record.Prepared = true;
        sale.SaleProcessed = true;
        PendingSaleRegistry.Instance.ReleaseVerified(sale);
    }

    private static void ControllerRetainsWithoutApplicationSubscriber()
    {
        TestController controller = new TestController();
        SaleData sale = NewSale();
        FuelPointValues values = SaleValues(sale);
        controller.Receive(values);
        PendingSaleRecord record;
        bool retained = PendingSaleRegistry.Instance.TryGetValue(sale.SalesId, out record);
        Assert(retained && record.SourceController == controller && !record.Prepared,
            "Controller did not retain the sale before application polling/subscription.");
        FuelPointValues received = controller.Take();
        bool sameSale = object.ReferenceEquals(record.Sale, received.Sale);
        Assert(sameSale, "Controller/application handoff changed the canonical sale object.");
        ReleaseTestSale(sale);
    }

    private static void ThrowOnTotals(object sender, TotalsEventArgs args)
    {
        throw new InvalidOperationException("Injected totals subscriber failure");
    }

    private static void CompletionFailureRetriesSameSnapshot()
    {
        TestController controller = new TestController();
        FuelPoint point = controller.Point;
        point.NozzleCount = 1;
        point.TotalDecimalPlaces = 2;
        point.Status = FuelPointStatusEnum.Work;
        point.DispenserStatus = FuelPointStatusEnum.Idle;
        point.DispensedAmount = 20;
        point.DispensedVolume = 10;
        ASFuelControl.Common.Nozzle nozzle = point.Nozzles[0];
        nozzle.UnitPrice = 2;
        nozzle.TotalVolume = 1000;
        nozzle.TotalVolume = 2000;
        controller.CompleteTotals(2000);
        Assert(controller.PendingCompletionCount == 1, "Unconfirmed completion sample was discarded.");
        controller.TotalsRecieved += ThrowOnTotals;
        controller.CompleteTotals(2000);
        Assert(controller.PendingCompletionCount > 0, "Failed completion event was discarded.");
        FuelPointValues values = controller.Take();
        Assert(values != null && values.Sale != null, "Subscriber failure prevented sale handoff.");
        SaleData sale = values.Sale;
        Assert(sale.TotalizerStart == 1000 && sale.TotalizerEnd == 2000 && sale.TotalPrice == 20,
            "Initial completion snapshot was incorrect.");
        point.DispensedAmount = 999;
        point.DispensedVolume = 999;
        nozzle.TotalVolume = 9999;
        controller.TotalsRecieved -= ThrowOnTotals;
        Thread.Sleep(2100);
        controller.RetryPendingCompletions();
        Assert(controller.PendingCompletionCount == 0, "Completion was not released after successful retry.");
        FuelPointValues duplicate = controller.Take();
        Assert(duplicate == null && sale.TotalizerEnd == 2000 && sale.TotalPrice == 20,
            "Completion retry duplicated the handoff or used later mutable pump values.");
        ReleaseTestSale(sale);
    }

    private static void CompletionBeforeNozzleMappingIsRetained()
    {
        TestController controller = new TestController();
        FuelPoint point = controller.Point;
        point.NeedsInvoice = true;
        controller.CompleteDirectSale(2000, 20, 10);
        Assert(controller.PendingCompletionCount == 1, "Completion with missing nozzle mapping was lost.");
        point.NozzleCount = 1;
        point.Nozzles[0].TotalVolume = 1000;
        point.Nozzles[0].UnitPrice = 2;
        point.DispensedAmount = 999;
        point.DispensedVolume = 999;
        Thread.Sleep(2100);
        controller.RetryPendingCompletions();
        FuelPointValues received = controller.Take();
        Assert(controller.PendingCompletionCount == 0 && received != null && received.Sale.TotalPrice == 20 && received.Sale.TotalVolume == 10,
            "Completion retry lost its original event values.");
        ReleaseTestSale(received.Sale);
    }

    private static ControllerThread NewQueue()
    {
        object[] arguments = new object[] { false };
        ControllerThread queue = (ControllerThread)Activator.CreateInstance(typeof(ControllerThread), BindingFlags.Instance | BindingFlags.NonPublic, null, arguments, null);
        queue.SaleAvaliable += Dispatch;
        return queue;
    }

    private static void Dispatch(object sender, SaleCompletedArgs args)
    {
        ControllerThread queue = (ControllerThread)sender;
        bool retained = queue.IsQueuedSaleBeingProcessed(args.Sale);
        Assert(retained, "Invoicing was dispatched outside the retained queue.");
        dispatches++;
        if (failDispatch)
        {
            throw new InvalidOperationException("Injected database failure");
        }
        if (confirmDispatch)
        {
            committed.Add(args.Sale.SalesId);
            args.Sale.SaleProcessed = true;
        }
    }

    private static void Process(ControllerThread queue)
    {
        FieldInfo retryField = typeof(ControllerThread).GetField("saleRetryAfter", BindingFlags.Instance | BindingFlags.NonPublic);
        ConcurrentDictionary<Guid, DateTime> retries = (ConcurrentDictionary<Guid, DateTime>)retryField.GetValue(queue);
        retries.Clear();
        MethodInfo process = typeof(ControllerThread).GetMethod("ProcessNextRetainedSale", BindingFlags.Instance | BindingFlags.NonPublic);
        process.Invoke(queue, null);
    }

    private static void DistinctSalesWithIdenticalTotalizers()
    {
        ControllerThread queue = NewQueue();
        SaleData first = NewSale();
        SaleData second = NewSale();
        second.NozzleId = first.NozzleId;
        int before = dispatches;
        queue.AddSale(first);
        queue.AddSale(second);
        queue.AddSale(first);
        Assert(dispatches == before, "Receipt directly invoked invoicing.");
        confirmDispatch = true;
        Process(queue);
        Process(queue);
        bool firstCommitted = committed.Contains(first.SalesId);
        bool secondCommitted = committed.Contains(second.SalesId);
        SaleData remaining = queue.GetNextSale();
        Assert(firstCommitted && secondCommitted && dispatches == before + 2 && remaining == null,
            "Different sale IDs with identical totalizers did not invoice independently.");
    }

    private static void DispatchFailureRetriesSameSale()
    {
        ControllerThread queue = NewQueue();
        SaleData sale = NewSale();
        queue.AddSale(sale);
        failDispatch = true;
        Process(queue);
        Assert(!sale.SaleProcessed, "Failed sale was acknowledged.");
        failDispatch = false;
        int before = dispatches;
        Process(queue);
        bool saved = committed.Contains(sale.SalesId);
        Assert(saved && dispatches == before + 1, "Failed sale was not retried from the same retained entry.");
    }

    private static void UnconfirmedSaleCannotBeRemoved()
    {
        ControllerThread queue = NewQueue();
        SaleData sale = NewSale();
        queue.AddSale(sale);
        confirmDispatch = false;
        Process(queue);
        try
        {
            queue.RemoveSale(sale.SalesId);
            throw new Exception("Unconfirmed sale was removable.");
        }
        catch (InvalidOperationException)
        {
        }
        confirmDispatch = true;
        Process(queue);
        bool saved = committed.Contains(sale.SalesId);
        Assert(saved, "Unconfirmed sale did not remain available for retry.");
    }

    private static void WorkflowCompletionOnlyQueues()
    {
        ControllerThread queue = NewQueue();
        SaleData sale = NewSale();
        SaleCompletedArgs args = new SaleCompletedArgs(sale);
        MethodInfo completion = typeof(ControllerThread).GetMethod("workFlow_SaleCompleted", BindingFlags.Instance | BindingFlags.NonPublic);
        object[] arguments = new object[] { null, args };
        int before = dispatches;
        completion.Invoke(queue, arguments);
        Assert(dispatches == before, "Workflow completion bypassed the invoicing queue.");
        Process(queue);
        Assert(dispatches == before + 1, "Workflow sale did not reach queued invoicing.");
    }

    private static void PendingSalesAreSharedAcrossInstances()
    {
        ControllerThread first = NewQueue();
        ControllerThread second = NewQueue();
        SaleData sale = NewSale();
        first.AddSale(sale);
        SaleData retained = second.GetNextSale();
        Assert(object.ReferenceEquals(sale, retained), "Controller replacement lost the static retained sale.");
        Process(second);
    }

    private sealed class PreparationWorkflow : IFuelPumpWorkFlow
    {
        public event EventHandler QueryCheckState;
        public event EventHandler<SaleCompletedArgs> SaleCompleted;
        public VirtualDispenser Dispenser { get; set; }
        public IController Controller { get; set; }
        public WorkFlowProcess Process { get; set; }
        public Guid CurrentShiftId { get; set; }
        public int ContextCaptures;
        public bool FailPreparation = true;
        public ControllerThread Destination;
        public void SetValues(object values)
        {
        }
        public void ValidateTransitions()
        {
        }
        public void SetSale(SaleData sale)
        {
            this.ContextCaptures++;
            sale.TraderId = Guid.NewGuid();
        }
        public void CloseSale(SaleData sale)
        {
            if (this.FailPreparation)
            {
                throw new InvalidOperationException("Injected preparation failure");
            }
            this.Destination.AddSale(sale);
        }
    }

    private static void PreparationRetryKeepsCapturedContext()
    {
        ControllerThread queue = NewQueue();
        SaleData sale = NewSale();
        MethodInfo capture = typeof(ControllerThread).GetMethod("CaptureReceivedSale", BindingFlags.Instance | BindingFlags.NonPublic);
        VirtualDispenser dispenser = new VirtualDispenser();
        dispenser.DispenserId = Guid.NewGuid();
        VirtualNozzle nozzle = new VirtualNozzle();
        nozzle.NozzleId = sale.NozzleId;
        dispenser.Nozzles = new VirtualNozzle[] { nozzle };
        object[] captureArguments = new object[] { sale, dispenser, 1 };
        capture.Invoke(queue, captureArguments);
        PendingSaleRecord record = PendingSaleRegistry.Instance[sale.SalesId];
        PreparationWorkflow workflow = new PreparationWorkflow();
        workflow.Dispenser = dispenser;
        workflow.Controller = new TestController();
        MethodInfo prepare = typeof(ControllerThread).GetMethod("PrepareRetainedSale", BindingFlags.Instance | BindingFlags.NonPublic);
        object[] prepareArguments = new object[] { record, workflow };
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                prepare.Invoke(queue, prepareArguments);
                throw new Exception("Injected preparation failure was not exercised.");
            }
            catch (TargetInvocationException exception)
            {
                Assert(exception.InnerException is InvalidOperationException, "Unexpected preparation error.");
            }
        }
        bool retained = PendingSaleRegistry.Instance.ContainsKey(sale.SalesId);
        Assert(workflow.ContextCaptures == 1 && retained, "Preparation retry overwrote context or lost the sale.");
        queue.AddSale(sale);
        Process(queue);
    }

    private static void PreparationFinishesWithoutExtraRetryDelay()
    {
        ControllerThread queue = NewQueue();
        SaleData sale = NewSale();
        VirtualDispenser dispenser = new VirtualDispenser();
        dispenser.DispenserId = Guid.NewGuid();
        VirtualNozzle nozzle = new VirtualNozzle();
        nozzle.NozzleId = sale.NozzleId;
        dispenser.Nozzles = new VirtualNozzle[] { nozzle };
        PreparationWorkflow workflow = new PreparationWorkflow();
        workflow.Dispenser = dispenser;
        workflow.Controller = new TestController();
        workflow.FailPreparation = false;
        workflow.Destination = queue;
        FieldInfo workflowsField = typeof(ControllerThread).GetField("fpWorkFlows", BindingFlags.Instance | BindingFlags.NonPublic);
        List<IFuelPumpWorkFlow> workflows = (List<IFuelPumpWorkFlow>)workflowsField.GetValue(queue);
        workflows.Add(workflow);
        MethodInfo capture = typeof(ControllerThread).GetMethod("CaptureReceivedSale", BindingFlags.Instance | BindingFlags.NonPublic);
        object[] arguments = new object[] { sale, dispenser, 1 };
        capture.Invoke(queue, arguments);
        PendingSaleRecord record = PendingSaleRegistry.Instance[sale.SalesId];
        record.ReceivedForProcessing = true;
        int before = dispatches;
        Process(queue);
        bool retained = PendingSaleRegistry.Instance.ContainsKey(sale.SalesId);
        Assert(dispatches == before + 1 && !retained,
            "Successful preparation unnecessarily waited for another retry before invoicing.");
    }

    private static void FailedDispenserDoesNotBlockAnother()
    {
        ControllerThread queue = NewQueue();
        SaleData first = NewSale();
        SaleData second = NewSale();
        queue.AddSale(first);
        queue.AddSale(second);
        PendingSaleRegistry.Instance[first.SalesId].DispenserId = Guid.NewGuid();
        PendingSaleRegistry.Instance[second.SalesId].DispenserId = Guid.NewGuid();
        failDispatch = true;
        Process(queue);
        failDispatch = false;
        MethodInfo process = typeof(ControllerThread).GetMethod("ProcessNextRetainedSale", BindingFlags.Instance | BindingFlags.NonPublic);
        process.Invoke(queue, null);
        bool firstPending = PendingSaleRegistry.Instance.ContainsKey(first.SalesId);
        bool secondPending = PendingSaleRegistry.Instance.ContainsKey(second.SalesId);
        Assert(firstPending && !secondPending, "A deferred dispenser blocked another dispenser's ready sale.");
        Process(queue);
    }
}
