using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BookShoppingCartMvcUI.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartRepository _cartRepo;
        private readonly IMemoryCache _cache;

        public CartController(ICartRepository cartRepo, IMemoryCache cache)
        {
            _cartRepo = cartRepo;
            _cache = cache;
        }

        public async Task<IActionResult> AddItem(int bookId, int qty = 1, int redirect = 0)
        {
            var cartCount = await _cartRepo.AddItem(bookId, qty);

            // امسح الكاش بعد إضافة كتاب (لأن المخزون بيتأثر)
            _cache.Remove("books_homepage");

            if (redirect == 0)
                return Ok(cartCount);

            return RedirectToAction("GetUserCart");
        }

        public async Task<IActionResult> RemoveItem(int bookId)
        {
            var cartCount = await _cartRepo.RemoveItem(bookId);

            // امسح الكاش بعد إزالة كتاب (لأن المخزون بيتأثر)
            _cache.Remove("books_homepage");

            return RedirectToAction("GetUserCart");
        }

        public async Task<IActionResult> GetUserCart()
        {
            var cart = await _cartRepo.GetUserCart();
            return View(cart);
        }

        public async Task<IActionResult> GetTotalItemInCart()
        {
            int cartItem = await _cartRepo.GetCartItemCount();
            return Ok(cartItem);
        }

        public IActionResult Checkout()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _cartRepo.DoCheckout(model);

                // امسح الكاش بعد إتمام عملية الشراء
                _cache.Remove("books_homepage");

                return RedirectToAction(nameof(OrderSuccess));
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(OrderFailure));
            }
        }

        public IActionResult OrderSuccess()
        {
            return View();
        }

        public IActionResult OrderFailure()
        {
            return View();
        }
    }
}
