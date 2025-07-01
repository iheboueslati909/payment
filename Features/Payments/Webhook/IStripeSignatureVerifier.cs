public interface IStripeSignatureVerifier
{
    bool Verify(string payload, string signatureHeader);
    //GetEndpointSecret
    string GetEndpointSecret(HttpRequest request);
}