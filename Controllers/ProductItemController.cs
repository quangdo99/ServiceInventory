using Microsoft.AspNetCore.Mvc;
using ServiceInventory.Models;
using ServiceInventory.Services;

namespace ServiceInventory.Controllers
{
    [Controller]
    [Route("api/[controller]")]
    public class ProductItemController : Controller
    {
        private readonly ProductInfoService _productInfoService;
        private readonly ProductItemService _productItemService;
        public ProductItemController(ProductInfoService productInfoService, ProductItemService productItemService)
        {
            _productInfoService = productInfoService;
            _productItemService = productItemService;
        }

        [HttpGet("{product_code}")]
        public async Task<List<ProductItem>> GetAsync(string product_code)
        {
            return await _productItemService.GetAsync(product_code);
        }

        [HttpPut("Import/{product_code}")]
        public async Task<IActionResult> UpdateProduct(string product_code, [FromBody] List<string> codes)
        {
            var productInfo = _productInfoService.GetProductInfo(product_code);
            if (productInfo == null)
            {
                throw new ArgumentException("Product Code Not Found!");
            }
            await _productItemService.CreateAsync(productInfo, codes);
            return NoContent();
        }

        [HttpPut("Export/{product_code}")]
        public async Task<IActionResult> ExportProduct(string product_code)
        {
            var productInfo = _productInfoService.GetProductInfo(product_code);
            if (productInfo == null)
            {
                throw new ArgumentException("Product Code Not Found!");
            }
            var productItem = _productItemService.GetProductItem(product_code);
            if (productItem == null)
            {
                throw new ArgumentException("Product Item Not Available!");
            }
            await _productItemService.UpdateStatus(productItem.Id);
            return NoContent();
        }
    }
}
