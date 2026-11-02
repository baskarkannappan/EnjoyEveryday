using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly IUserContext _userContext;

    public BillingController(IBillingService billingService, IUserContext userContext)
    {
        _billingService = billingService;
        _userContext = userContext;
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<List<Invoice>>> GetInvoices([FromQuery] Guid? organizationId, CancellationToken ct)
    {
        var targetOrgId = organizationId ?? _userContext.OrganizationId;
        if (!targetOrgId.HasValue)
        {
            return BadRequest("Organization ID is required.");
        }

        var invoices = await _billingService.GetInvoicesForOrganizationAsync(targetOrgId.Value, ct);
        return Ok(invoices);
    }

    [HttpGet("invoices/{id:guid}")]
    public async Task<ActionResult<Invoice>> GetInvoiceById(Guid id, CancellationToken ct)
    {
        var invoice = await _billingService.GetInvoiceByIdAsync(id, ct);
        if (invoice == null)
        {
            return NotFound();
        }
        return Ok(invoice);
    }

    [HttpGet("invoices/{id:guid}/payments")]
    public async Task<ActionResult<List<Payment>>> GetInvoicePayments(Guid id, CancellationToken ct)
    {
        var payments = await _billingService.GetInvoicePaymentsAsync(id, ct);
        return Ok(payments);
    }

    [HttpGet("settings")]
    public async Task<ActionResult<OrganizationBillingSettings>> GetBillingSettings([FromQuery] Guid? organizationId, CancellationToken ct)
    {
        var targetOrgId = organizationId ?? _userContext.OrganizationId;
        if (!targetOrgId.HasValue)
        {
            return BadRequest("Organization ID is required.");
        }

        var settings = await _billingService.GetBillingSettingsAsync(targetOrgId.Value, ct);
        return Ok(settings);
    }

    [HttpPost("orders/razorpay")]
    public async Task<ActionResult<RazorpayOrderResponse>> CreateRazorpayOrder([FromBody] CreateRazorpayOrderRequest req, CancellationToken ct)
    {
        var order = await _billingService.CreateRazorpayOrderAsync(req, ct);
        return Ok(order);
    }

    [HttpPost("orders/razorpay/verify")]
    public async Task<ActionResult<RazorpayVerificationResult>> VerifyRazorpayPayment([FromBody] VerifyRazorpayPaymentRequest req, CancellationToken ct)
    {
        var result = await _billingService.VerifyRazorpayPaymentAsync(req, ct);
        return Ok(result);
    }
}
