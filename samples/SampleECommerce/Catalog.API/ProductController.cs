using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Catalog.API.Controllers;

/// <summary>
/// Manages product catalog. Unrelated to payment or order creation flow.
/// </summary>
public sealed class ProductController
{
    private readonly IProductRepository _productRepository;

    public ProductController(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default) =>
        await _productRepository.GetAllAsync(cancellationToken);

    public async Task<Product?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _productRepository.GetByIdAsync(id, cancellationToken);

    public async Task CreateProductAsync(Product product, CancellationToken cancellationToken = default) =>
        await _productRepository.AddAsync(product, cancellationToken);
}

public sealed class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
}
