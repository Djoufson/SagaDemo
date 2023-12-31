using SagaDemo.PaymentService.Entities;

namespace SagaDemo.PaymentService.Services.Payments;

public interface IPaymentService
{
    Task<IReadOnlyList<Transaction>> GetAllTransactionsAsync();
    Task<Transaction?> GetTransactionAsync(Guid id);
    Task<Transaction?> MakeTransactionAsync(Transaction transaction);
    Task<bool> UndoTransaction(Guid transactionId);
}