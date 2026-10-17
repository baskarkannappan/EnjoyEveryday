// EnjoyEveryday Razorpay Checkout Integration
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
            description: options.description || ("Payment for Order " + (options.invoiceNumber || "")),
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
    }

    if (typeof window.Razorpay !== 'undefined') {
        launch();
        return;
    }

    // Check if script tag is already in DOM
    var existingScript = document.querySelector('script[src*="checkout.razorpay.com"]');
    if (existingScript) {
        existingScript.addEventListener('load', launch);
        existingScript.addEventListener('error', function () {
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnRazorpayError', 
                    'Razorpay script failed to load. A browser ad-blocker may be active.');
            }
        });

        // Polling fallback up to 2.5s in case onload already fired
        var elapsed = 0;
        var interval = setInterval(function () {
            elapsed += 200;
            if (typeof window.Razorpay !== 'undefined') {
                clearInterval(interval);
                launch();
            } else if (elapsed >= 2500) {
                clearInterval(interval);
                launch();
            }
        }, 200);
        return;
    }

    // Dynamic injection
    var script = document.createElement('script');
    script.src = 'https://checkout.razorpay.com/v1/checkout.js';
    script.onload = launch;
    script.onerror = function () {
        if (dotNetHelper) {
            dotNetHelper.invokeMethodAsync('OnRazorpayError', 
                'Razorpay CDN is blocked or unavailable. You can use Quick Test Simulation to complete payment.');
        }
    };
    document.head.appendChild(script);
};
