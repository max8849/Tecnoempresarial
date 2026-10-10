namespace AccessControl.Application.Common.Interfaces;

public interface IQrImageGenerator
{
    // Devuelve un PNG codificado en base64, listo para usarse como <img src="data:image/png;base64,...">
    string GeneratePngBase64(string content);
}
