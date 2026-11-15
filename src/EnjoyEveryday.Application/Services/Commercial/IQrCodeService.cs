using System;

namespace EnjoyEveryday.Application.Services;

public interface IQrCodeService
{
    /// <summary>
    /// Generates a clean, scalable SVG string representing the QR code.
    /// </summary>
    string GenerateSvg(string content, int border = 2);

    /// <summary>
    /// Generates a standard PNG image byte array for the QR code.
    /// </summary>
    byte[] GeneratePng(string content, int moduleSize = 6, int border = 2);

    /// <summary>
    /// Generates a Base64 data URI (data:image/png;base64,...) for embedding in HTML emails and web pages.
    /// </summary>
    string GeneratePngDataUri(string content, int moduleSize = 6, int border = 2);

    /// <summary>
    /// Builds a mobile payment payload for an invoice (UPI or web payment checkout link).
    /// </summary>
    string BuildInvoicePaymentPayload(string invoiceNumber, decimal amount, string currency, Guid invoiceId, string? paymentUrl = null);
}

