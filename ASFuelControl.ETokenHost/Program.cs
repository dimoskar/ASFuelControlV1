namespace ASFuelControl.ETokenHost
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return Communication.ETokenProcessHost.Run(args);
        }
    }
}
