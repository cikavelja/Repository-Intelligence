using System;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Application.Services;

/// <summary>
/// Manages customer profile data. Unrelated to payment or order creation flow.
/// </summary>
public sealed class CustomerProfileService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerProfileService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerProfile?> GetProfileAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await _customerRepository.GetByIdAsync(customerId, cancellationToken);

    public async Task UpdateEmailAsync(Guid customerId, string newEmail, CancellationToken cancellationToken = default)
    {
        var profile = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (profile is null) throw new InvalidOperationException("Customer not found.");
        profile.Email = newEmail;
        await _customerRepository.SaveAsync(profile, cancellationToken);
    }
}

public sealed class CustomerProfile
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public interface ICustomerRepository
{
    Task<CustomerProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(CustomerProfile profile, CancellationToken cancellationToken = default);
}
