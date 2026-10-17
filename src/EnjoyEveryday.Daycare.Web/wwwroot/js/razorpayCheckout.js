// EnjoyEveryday Razorpay Checkout Integration
window.initiateRazorpayPayment = function (options, dotNetHelper) {
    if (typeof window.Razorpay === 'undefined') {
        // Fallback for offline/mock development or blocked external CDN
        if (options.orderId && options.orderId.startsWith("order_mock_")) {
            var simulated = confirm("Razorpay Mock Gateway (Offline/Test Mode)\n\nSimulate successful payment for " + options.invoiceNumber + " (" + (options.amount / 100).toFixed(2) + " " + options.currency + ")?");
            if (simulated) {
                var mockPaymentId = "pay_mock_" + Math.random().toString(36).substring(2, 12);
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnRazorpaySuccess', {
                        orderId: options.orderId,
                        paymentId: mockPaymentId,
                        signature: "mock_signature_valid"
                    });
                }
            } else {
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnRazorpayDismiss');
                }
            }
            return;
        }

        console.error("Razorpay SDK not loaded.");
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnRazorpayError', 'Razorpay checkout script is unavailable or blocked by browser.');
        }
        return;
    }

    var rzpOptions = {
        key: options.key,
        amount: options.amount,
        currency: options.currency,
        name: options.name || "EnjoyEveryday Daycare",
        description: options.description || ("Payment for Invoice " + options.invoiceNumber),
        order_id: options.orderId,
        prefill: {
            name: options.prefillName || "",
            email: options.prefillEmail || "",
            contact: options.prefillContact || ""
        },
        notes: {
            invoiceId: options.invoiceId,
            organizationId: options.organizationId
        },
        theme: {
            color: "#557c6c"
        },
        handler: function (response) {
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnRazorpaySuccess', {
                    orderId: response.razorpay_order_id,
                    paymentId: response.razorpay_payment_id,
                    signature: response.razorpay_signature
                });
            }
        },
        modal: {
            ondismiss: function () {
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnRazorpayDismiss');
                }
            }
        }
    };

    try {
        var rzp = new window.Razorpay(rzpOptions);
        rzp.on('payment.failed', function (response) {
            if (dotNetHelper) {
                var msg = (response && response.error && response.error.description) 
                    ? response.error.description 
                    : 'Payment failed or declined.';
                dotNetHelper.invokeMethodAsync('OnRazorpayError', msg);
            }
        });
        rzp.open();
    } catch (err) {
        console.error("Error opening Razorpay checkout:", err);
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnRazorpayError', err.message || 'Error opening payment dialog');
        }
    }
};
