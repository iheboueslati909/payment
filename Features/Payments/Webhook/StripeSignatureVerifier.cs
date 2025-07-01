using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace PaymentGateway.Features.Payments.Webhook
{
    public class StripeSignatureVerifier : IStripeSignatureVerifier
    {
        // In a real system, fetch the secret from a secure store or DB per appId
        public string GetEndpointSecret(HttpRequest request)
        {
            // Example: extract appId from JWT or header, then fetch secret from DB/config
            // For demo, fallback to config or env variable
            // You should implement a proper lookup based on your multi-tenant model
            return Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") ?? "whsec_test_secret";
        }

        public bool Verify(string payload, string signatureHeader)
        {
            var secret = GetEndpointSecret(null); // In real use, pass HttpRequest if needed
            if (string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(secret))
                return false;

            // Stripe signature format: t=timestamp,v1=signature,...
            var sigItems = signatureHeader.Split(',');
            var timestamp = "";
            var signature = "";
            foreach (var item in sigItems)
            {
                var parts = item.Split('=');
                if (parts.Length == 2)
                {
                    if (parts[0] == "t") timestamp = parts[1];
                    if (parts[0] == "v1") signature = parts[1];
                }
            }

            if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signature))
                return false;

            var signedPayload = $"{timestamp}.{payload}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
            var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

            // Use constant time comparison
            return SlowEquals(computedSignature, signature);
        }

        private static bool SlowEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}