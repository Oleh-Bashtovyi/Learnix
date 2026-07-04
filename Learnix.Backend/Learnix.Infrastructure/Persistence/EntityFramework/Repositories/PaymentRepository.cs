using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Payments.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class PaymentRepository(ApplicationDbContext context)
    : RepositoryBase<Payment>(context), IPaymentRepository
{
}
