using System.Text.Json;
using Cinema.Application.Interfaces;
using Cinema.Application.Options;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cinema.Infrastructure.Payments;

public class MomoGateway(
    IOptions<MomoOptions> options,
    HttpClient httpClient,
    ILogger<MomoGateway> logger
) : IPaymentGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly MomoOptions _options = options.Value;

    public string MethodName => PaymentMethodNames.Momo;

    private const int OrderInfoMaxLength = 255;

    public async Task<CreatePaymentResult> CreatePaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var requestId = Guid.NewGuid().ToString();
        var orderId = request.TransactionId;
        var amount = (long)request.Amount;
        const string extraData = "";
        const string requestType = "captureWallet";
        var orderInfo = request.OrderInfo.Length > OrderInfoMaxLength
            ? request.OrderInfo[..OrderInfoMaxLength]
            : request.OrderInfo;
        var returnUrl = $"{_options.ReturnUrl.Trim()}?bookingId={request.BookingId}";

        var rawSignature =
            $"accessKey={_options.AccessKey}&amount={amount}&extraData={extraData}"
            + $"&ipnUrl={_options.IpnUrl}&orderId={orderId}&orderInfo={orderInfo}"
            + $"&partnerCode={_options.PartnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";

        var signature = HmacSigner.Sha256Hex(_options.SecretKey, rawSignature);

        var payload = new
        {
            partnerCode = _options.PartnerCode,
            requestId,
            amount,
            orderId,
            orderInfo,
            redirectUrl = returnUrl,
            ipnUrl = _options.IpnUrl,
            extraData,
            requestType,
            signature,
            lang = "vi",
        };

        MomoCreateResponse result;
        try
        {
            result = await GatewayHttp.PostJsonAsync<MomoCreateResponse>(
                httpClient,
                _options.Endpoint.Trim(),
                payload,
                JsonOptions,
                logger,
                "MoMo",
                cancellationToken
            );
        }
        catch (GatewayHttpException ex)
        {
            throw new BusinessRuleException(
                string.Format(Messages.Rule_MomoCreatePaymentFailed, ex.Message)
            );
        }

        if (result.ResultCode != 0)
            throw new BusinessRuleException(
                string.Format(Messages.Rule_MomoCreatePaymentFailed, result.Message)
            );

        return new CreatePaymentResult(result.PayUrl ?? string.Empty, orderId);
    }

    public GatewayCallbackResult VerifyCallback(
        IReadOnlyDictionary<string, string> queryOrFormParams
    )
    {
        var receivedSignature = queryOrFormParams.GetValueOrDefault("signature", string.Empty);

        var rawSignature =
            $"accessKey={_options.AccessKey}&amount={queryOrFormParams.GetValueOrDefault("amount")}"
            + $"&extraData={queryOrFormParams.GetValueOrDefault("extraData")}&message={queryOrFormParams.GetValueOrDefault("message")}"
            + $"&orderId={queryOrFormParams.GetValueOrDefault("orderId")}&orderInfo={queryOrFormParams.GetValueOrDefault("orderInfo")}"
            + $"&orderType={queryOrFormParams.GetValueOrDefault("orderType")}&partnerCode={queryOrFormParams.GetValueOrDefault("partnerCode")}"
            + $"&payType={queryOrFormParams.GetValueOrDefault("payType")}&requestId={queryOrFormParams.GetValueOrDefault("requestId")}"
            + $"&responseTime={queryOrFormParams.GetValueOrDefault("responseTime")}&resultCode={queryOrFormParams.GetValueOrDefault("resultCode")}"
            + $"&transId={queryOrFormParams.GetValueOrDefault("transId")}";

        var computedSignature = HmacSigner.Sha256Hex(_options.SecretKey, rawSignature);
        var signatureValid = string.Equals(
            computedSignature,
            receivedSignature,
            StringComparison.OrdinalIgnoreCase
        );

        var resultCode = queryOrFormParams.GetValueOrDefault("resultCode", "-1");
        var amount = decimal.TryParse(queryOrFormParams.GetValueOrDefault("amount"), out var amt)
            ? amt
            : 0m;
        var orderId = queryOrFormParams.GetValueOrDefault("orderId", string.Empty);
        var momoTransId = queryOrFormParams.GetValueOrDefault("transId", string.Empty);

        return new GatewayCallbackResult(
            signatureValid,
            resultCode == "0",
            orderId,
            resultCode,
            amount,
            BookingId: 0,
            GatewayTransactionId: string.IsNullOrEmpty(momoTransId) ? null : momoTransId
        );
    }

    public async Task<RefundResult> RefundAsync(
        RefundRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (!long.TryParse(request.GatewayTransactionId, out var transId) || transId <= 0)
            return new RefundResult(
                false,
                null,
                string.Format(Messages.Refund_MomoApiError, Messages.Refund_MomoInvalidTransId)
            );

        var requestId = Guid.NewGuid().ToString("N");
        var orderId = $"RF{Guid.NewGuid():N}";
        var amount = (long)request.Amount;
        var description = request.OrderInfo ?? string.Empty;

        var rawSignature =
            $"accessKey={_options.AccessKey}&amount={amount}&description={description}"
            + $"&orderId={orderId}&partnerCode={_options.PartnerCode}&requestId={requestId}&transId={transId}";
        var signature = HmacSigner.Sha256Hex(_options.SecretKey, rawSignature);

        var payload = new
        {
            partnerCode = _options.PartnerCode,
            orderId,
            requestId,
            amount,
            transId,
            lang = "vi",
            description,
            signature,
        };

        try
        {
            var result = await GatewayHttp.PostJsonAsync<MomoRefundResponse>(
                httpClient,
                _options.RefundEndpoint.Trim(),
                payload,
                JsonOptions,
                logger,
                "MoMo",
                cancellationToken
            );

            return result.ResultCode == 0
                ? new RefundResult(
                    true,
                    result.TransId is > 0 ? result.TransId.Value.ToString() : orderId,
                    null
                )
                : new RefundResult(
                    false,
                    null,
                    string.Format(Messages.Refund_MomoFailed, result.ResultCode, result.Message)
                );
        }
        catch (Exception ex)
        {
            if (ex is not GatewayHttpException)
                logger.LogError(ex, "MomoRefundApiError");
            return new RefundResult(
                false,
                null,
                string.Format(Messages.Refund_MomoApiError, ex.Message)
            );
        }
    }

    private record MomoCreateResponse(int ResultCode, string? Message, string? PayUrl);

    private record MomoRefundResponse(int ResultCode, string? Message, long? TransId);
}
