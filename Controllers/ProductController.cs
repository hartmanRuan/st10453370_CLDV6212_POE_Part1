using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IAzureStorageService storageService, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imagefile)
        {
            if (Request.Form.TryGetValue("Price", out var priceFormValue))
            {
                _logger.LogInformation("Raw price from form: '{PriceFormValue}'", priceFormValue.ToString());

                if (double.TryParse(priceFormValue, out var parsedPrice))
                {
                    product.Price = parsedPrice;
                    _logger.LogInformation("Successfully parsed price: {Price}", parsedPrice);
                }
                else
                {
                    _logger.LogWarning("Failed to Parse Price: {PriceFormValue}", priceFormValue.ToString());
                }

            }

            

            _logger.LogInformation("Final product price: {Price}", product.Price);
            _logger.LogInformation("Final product productid: {Price}", product.ProductId);
            _logger.LogInformation("Final product Name: {Price}", product.ProductName);
            _logger.LogInformation("Final product Description: {Price}", product.Description);
            _logger.LogInformation("Final product Image: {Price}", product.ImageUrl); //PROBLEM IS HERE ITS EMPTY
            _logger.LogInformation("Final product Stock: {Price}", product.StockAvailable);


            if (ModelState.IsValid)
            {
                
                try
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price must be greater than $0.00");
                        return View(product);
                    }

                    if (imagefile != null && imagefile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imagefile, "product-images");
                        product.ImageUrl = imageUrl;

                    }

                    /* if (product.ImageFile != null && product.ImageFile.Length > 0)
                     {
                         // Save file
                         var fileName = Path.GetFileName(product.ImageFile.FileName);
                         var path = Path.Combine(_storageService.WebRootPath, "uploads", fileName);
                         using (var stream = new FileStream(path, FileMode.Create))
                         {
                             product.ImageFile.CopyTo(stream);
                         }

                         // Save relative path in ImageUrl
                         product.ImageUrl = "/uploads/" + fileName;
                     }*/

                    await _storageService.AddEntityAsync(product);
                    TempData["Success"] = $"Product '{product.ProductName}' created successfully with price {product.Price:c}!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                   
                }
            }
            
            return View(product);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _storageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (Request.Form.TryGetValue("Price", out var priceFormValue))
            {
                if (double.TryParse(priceFormValue, out var parsedPrice))
                {
                    product.Price = parsedPrice;
                    _logger.LogInformation("Edit: Successfully parsed price: {Price}", parsedPrice);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalProduct = await _storageService.GetEntityAsync<Product>("Product", product.RowKey);
                    if (originalProduct == null)
                    {
                        return NotFound();
                    }

                    originalProduct.ProductName = product.ProductName;
                    originalProduct.Description = product.Description;
                    originalProduct.Price = product.Price;
                    originalProduct.StockAvailable = product.StockAvailable;

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        originalProduct.ImageUrl = imageUrl;
                    }

                    await _storageService.UpdateEntityAsync(originalProduct);
                    TempData["Success"] = "Product uploaded successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                Console.WriteLine("ID = " + id);
                await _storageService.DeleteEntityAsync<Product>("Product", id);
                TempData["Success"] = "Product deleted successfully";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
