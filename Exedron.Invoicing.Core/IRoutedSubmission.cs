namespace Exedron.Invoicing.Core
{
    // Opaque, non-secret host metadata identifying the original route/configuration.
    // Stored with the immutable request; never reconstruct it from current settings.
    public interface IRoutedSubmission
    {
        string RoutingContext { get; set; }
    }
}
