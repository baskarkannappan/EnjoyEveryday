// EnjoyEveryday Parent Portal Razorpay Checkout Integration
window.initiateRazorpayPayment = function (options, dotNetHelper) {
    function launch() {
        if (typeof window.Razorpay === 'undefined') {
            console.warn("Razorpay script not available. Ad-blocker or network restriction may be active.");
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnRazorpayError', 
                    'Razorpay checkout could not be loaded. A browser ad-blocker may be blocking checkout.razorpay.com. You can use Quick Test Simulation to complete payment.');
            }
            return;
        }

        var rzpOptions = {
            key: options.key,
            amount: options.amount,
            currency: options.currency || "INR",
            name: options.name || "EnjoyEveryday Daycare",
            description: options.description || ("Payment for Tuition Invoice " + (options.invoiceNumber || "")),
            order_id: options.orderId,
            prefill: {
                name: options.prefillName || "",
                email: options.prefillEmail || "",
                contact: options.prefillContact || ""
            },
            notes: {
                invoiceId: options.invoiceId,
                childId: options.childId
            },
            theme: {
                color: "#52745e"
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
                    dotNetHelper.invokeMethodAsync('OnRazorpayFailed', {
                        code: response.error ? response.error.code : 'UNKNOWN',
                        description: response.error ? response.error.description : 'Payment transaction failed',
                        source: response.error ? response.error.source : '',
                        step: response.error ? response.error.step : '',
                        reason: response.error ? response.error.reason : ''
                    });
                }
            });
            rzp.open();
        } catch (e) {
            console.error("Error opening Razorpay checkout:", e);
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnRazorpayError', e.message || 'Error opening payment dialog');
            }
        }
    }

    if (typeof window.Razorpay === 'undefined') {
        var existingScript = document.querySelector('script[src="https://checkout.razorpay.com/v1/checkout.js"]');
        if (!existingScript) {
            var script = document.createElement('script');
            script.src = 'https://checkout.razorpay.com/v1/checkout.js';
            script.async = true;
            script.onload = function () {
                launch();
            };
            script.onerror = function () {
                console.error("Failed to load Razorpay checkout script from CDN.");
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnRazorpayError', 'Failed to load Razorpay payment gateway script. Please check your internet connection.');
                }
            };
            document.head.appendChild(script);
        } else {
            existingScript.addEventListener('load', launch);
        }
    } else {
        launch();
    }
};

