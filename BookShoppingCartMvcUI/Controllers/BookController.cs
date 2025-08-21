using BookShoppingCartMvcUI.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;

namespace BookShoppingCartMvcUI.Controllers;

[Authorize(Roles = nameof(Roles.Admin))]
public class BookController : Controller
{
    private readonly IBookRepository _bookRepo;
    private readonly IGenreRepository _genreRepo;
    private readonly IFileService _fileService;
    private readonly IMemoryCache _cache;
    private readonly IStockRepository _stockRepo; // ✅ إضافة مخزن الـ Stock

    public BookController(
        IBookRepository bookRepo,
        IGenreRepository genreRepo,
        IFileService fileService,
        IMemoryCache cache,
        IStockRepository stockRepo) // ✅ استقباله في الكونستركتور
    {
        _bookRepo = bookRepo;
        _genreRepo = genreRepo;
        _fileService = fileService;
        _cache = cache;
        _stockRepo = stockRepo;
    }

    private void ClearBookCache()
    {
        foreach (var key in CacheKeyTracker.Keys.Where(k => k.StartsWith(CacheKeys.BookPrefix)).ToList())
        {
            _cache.Remove(key);
            CacheKeyTracker.Keys.Remove(key);
        }
    }

    public async Task<IActionResult> Index()
    {
        var books = await _bookRepo.GetBooks();
        return View(books);
    }

    public async Task<IActionResult> AddBook()
    {
        var genreSelectList = (await _genreRepo.GetGenres()).Select(genre => new SelectListItem
        {
            Text = genre.GenreName,
            Value = genre.Id.ToString(),
        });
        BookDTO bookToAdd = new() { GenreList = genreSelectList };
        return View(bookToAdd);
    }

    [HttpPost]
    public async Task<IActionResult> AddBook(BookDTO bookToAdd)
    {
        var genreSelectList = (await _genreRepo.GetGenres()).Select(genre => new SelectListItem
        {
            Text = genre.GenreName,
            Value = genre.Id.ToString(),
        });
        bookToAdd.GenreList = genreSelectList;

        if (!ModelState.IsValid)
            return View(bookToAdd);

        try
        {
            if (bookToAdd.ImageFile != null)
            {
                if (bookToAdd.ImageFile.Length > 1 * 1024 * 1024)
                {
                    throw new InvalidOperationException("Image file can not exceed 1 MB");
                }
                string[] allowedExtensions = [".jpeg", ".jpg", ".png"];
                string imageName = await _fileService.SaveFile(bookToAdd.ImageFile, allowedExtensions);
                bookToAdd.Image = imageName;
            }

            Book book = new()
            {
                Id = bookToAdd.Id,
                BookName = bookToAdd.BookName,
                AuthorName = bookToAdd.AuthorName,
                Image = bookToAdd.Image,
                GenreId = bookToAdd.GenreId,
                Price = bookToAdd.Price
            };
            await _bookRepo.AddBook(book);

            ClearBookCache();

            TempData["successMessage"] = "Book is added successfully";
            return RedirectToAction(nameof(AddBook));
        }
        catch (Exception ex)
        {
            TempData["errorMessage"] = ex.Message;
            return View(bookToAdd);
        }
    }

    public async Task<IActionResult> UpdateBook(int id)
    {
        var book = await _bookRepo.GetBookById(id);
        if (book == null)
        {
            TempData["errorMessage"] = $"Book with the id: {id} does not found";
            return RedirectToAction(nameof(Index));
        }
        var genreSelectList = (await _genreRepo.GetGenres()).Select(genre => new SelectListItem
        {
            Text = genre.GenreName,
            Value = genre.Id.ToString(),
            Selected = genre.Id == book.GenreId
        });
        BookDTO bookToUpdate = new()
        {
            GenreList = genreSelectList,
            BookName = book.BookName,
            AuthorName = book.AuthorName,
            GenreId = book.GenreId,
            Price = book.Price,
            Image = book.Image
        };
        return View(bookToUpdate);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateBook(BookDTO bookToUpdate)
    {
        var genreSelectList = (await _genreRepo.GetGenres()).Select(genre => new SelectListItem
        {
            Text = genre.GenreName,
            Value = genre.Id.ToString(),
            Selected = genre.Id == bookToUpdate.GenreId
        });
        bookToUpdate.GenreList = genreSelectList;

        if (!ModelState.IsValid)
            return View(bookToUpdate);

        try
        {
            string oldImage = "";
            if (bookToUpdate.ImageFile != null)
            {
                if (bookToUpdate.ImageFile.Length > 1 * 1024 * 1024)
                {
                    throw new InvalidOperationException("Image file can not exceed 1 MB");
                }
                string[] allowedExtensions = [".jpeg", ".jpg", ".png"];
                string imageName = await _fileService.SaveFile(bookToUpdate.ImageFile, allowedExtensions);
                oldImage = bookToUpdate.Image;
                bookToUpdate.Image = imageName;
            }

            Book book = new()
            {
                Id = bookToUpdate.Id,
                BookName = bookToUpdate.BookName,
                AuthorName = bookToUpdate.AuthorName,
                GenreId = bookToUpdate.GenreId,
                Price = bookToUpdate.Price,
                Image = bookToUpdate.Image
            };
            await _bookRepo.UpdateBook(book);

            ClearBookCache();

            if (!string.IsNullOrWhiteSpace(oldImage))
            {
                _fileService.DeleteFile(oldImage);
            }
            TempData["successMessage"] = "Book is updated successfully";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["errorMessage"] = ex.Message;
            return View(bookToUpdate);
        }
    }

    public async Task<IActionResult> DeleteBook(int id)
    {
        try
        {
            var book = await _bookRepo.GetBookById(id);
            if (book == null)
            {
                TempData["errorMessage"] = $"Book with the id: {id} does not found";
            }
            else
            {
                // ✅ امسح كل الـ Stock المرتبط بالكتاب قبل ما تحذف الكتاب نفسه
                await _stockRepo.DeleteByBookId(id);

                await _bookRepo.DeleteBook(book);
                if (!string.IsNullOrWhiteSpace(book.Image))
                {
                    _fileService.DeleteFile(book.Image);
                }

                ClearBookCache();
                TempData["successMessage"] = "Book deleted successfully";
            }
        }
        catch (Exception ex)
        {
            TempData["errorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
