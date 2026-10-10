namespace AccessControl.Application.Common.Interfaces;

// Genera y valida los tokens de los AccessPass con el patrón selector+verifier:
// el selector es público (se usa para buscar el registro), el verifier es el secreto
// (solo se guarda su hash, nunca el valor real).
public interface ISecureTokenService
{
    (string SelectorId, string Verifier, string VerifierHash) GenerateNewToken();

    string HashVerifier(string verifier);

    bool TryParseToken(string rawToken, out string selectorId, out string verifier);

    // Comparación en tiempo constante para evitar timing attacks.
    bool VerifyHash(string verifier, string storedHash);
}
