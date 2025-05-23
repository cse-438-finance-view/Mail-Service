using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace MailService.Services
{
    public interface IFirebaseAuthService
    {
        Task<FirebaseToken> VerifyTokenAsync(string idToken);
    }

    public class FirebaseAuthService : IFirebaseAuthService
    {
        private readonly FirebaseAuth _firebaseAuth;

        public FirebaseAuthService()
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                var serviceAccountPath = "firebase-service-account.json";
                
                if (File.Exists(serviceAccountPath))
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(serviceAccountPath)
                    });
                }
                else
                {
                    throw new FileNotFoundException($"Firebase service account file not found: {serviceAccountPath}");
                }
            }

            _firebaseAuth = FirebaseAuth.DefaultInstance;
        }

        public async Task<FirebaseToken> VerifyTokenAsync(string idToken)
        {
            try
            {
                return await _firebaseAuth.VerifyIdTokenAsync(idToken);
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException($"Invalid or expired token: {ex.Message}");
            }
        }
    }
} 