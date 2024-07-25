using System;
using System.Linq;
using RefactorThis.Persistence;

namespace RefactorThis.Domain
{
    public class InvoiceService
    {
        private readonly InvoiceRepository _invoiceRepository;

        public InvoiceService(InvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public string ProcessPayment(Payment payment)
        {
            var inv = _invoiceRepository.GetInvoice(payment.Reference);

            var responseMessage = string.Empty;

            if (inv == null)
                throw new InvalidOperationException("There is no invoice matching this payment");

            if (inv.Amount == 0)
            {
                if (inv.Payments?.Any() == true)
                    throw new InvalidOperationException("The invoice is in an invalid state, it has an amount of 0 and it has payments.");

                responseMessage = "no payment needed";
            }
            else
            {
                if (inv.Payments?.Any() == true)
                {
                    var paymentSum = inv.Payments.Sum(x => x.Amount);
                    var amountRemaining = inv.Amount - inv.AmountPaid;

                    if (paymentSum != 0)
                    {
                        if (inv.Amount == paymentSum)
                            responseMessage = "invoice was already fully paid";
                        else if (payment.Amount > amountRemaining)
                            responseMessage = "the payment is greater than the partial amount remaining";
                    }
                    else
                    {
                        bool isFinalPayment = amountRemaining == payment.Amount;
                        responseMessage = isFinalPayment
                            ? "final partial payment received, invoice is now fully paid"
                            : "another partial payment received, still not fully paid";

                        inv.AmountPaid += payment.Amount;
                        inv.Payments.Add(payment);

                        if (inv.Type == InvoiceType.Commercial)
                            inv.TaxAmount += payment.Amount * 0.14m;
                        else if (inv.Type != InvoiceType.Standard)
                            throw new ArgumentOutOfRangeException(nameof(inv.Type));
                    }
                }
                else
                {
                    if (payment.Amount > inv.Amount)
                    {
                        responseMessage = "the payment is greater than the invoice amount";
                    }
                    else
                    {
                        bool isFullyPaid = inv.Amount == payment.Amount;
                        responseMessage = isFullyPaid ? "invoice is now fully paid" : "invoice is now partially paid";

                        inv.AmountPaid = payment.Amount;
                        inv.TaxAmount = payment.Amount * 0.14m;
                        inv.Payments.Add(payment);

                        if (inv.Type != InvoiceType.Standard && inv.Type != InvoiceType.Commercial)
                            throw new ArgumentOutOfRangeException(nameof(inv.Type));
                    }

                }
            }

            inv.Save();

            return responseMessage;
        }
    }
}