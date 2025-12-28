using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XtraSoft.Core;
using XtraSoft.Infrastructure;
using XtraSoft.Localization;

namespace XtraSoft.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LanguageController(IRepository<Language,ApplicationDbContext> repository) : Controller
    {
        private readonly IRepository<Language,ApplicationDbContext> _repository = repository;

        // GET: Admin/Language
        public async Task<IActionResult> Index()
        {
            var languages = await _repository.GetAllAsync();
            var viewModel = languages.Select(l => new LanguageViewModel(l)).ToList();
            return View(viewModel);
        }

        // Fix for CS8629: Nullable value type may be null.

        // In Details method
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var language = await _repository.GetByIdAsync(id.Value);
            if (language == null)
            {
                return NotFound();
            }

            return View(new LanguageViewModel(language));
        }

        // GET: Admin/Language/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Language/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LanguageViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var language = new Language { Name = viewModel.Name, Culture = viewModel.Culture };
                await _repository.AddAsync(language);
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: Admin/Language/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var language = await _repository.GetByIdAsync(id);
            if (language == null)
            {
                return NotFound();
            }
            return View(new LanguageViewModel(language));
        }

        // POST: Admin/Language/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Culture")] Language language)
        {
            if (id != language.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _repository.UpdateAsync(language);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await LanguageExists(language.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
                ;
            }
            return View(language);
        }


        // In Delete method
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var language = await _repository.GetByIdAsync(id.Value);
            if (language == null)
            {
                return NotFound();
            }

            return View(new LanguageViewModel(language));
        }

        // POST: Admin/Language/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var language = await _repository.GetByIdAsync(id);
            if (language != null)
            {
                await _repository.DeleteAsync(id);
            }

            return RedirectToAction(nameof(Index));
        }

        // In LanguageExists method
        private async Task<bool> LanguageExists(int id)
        {
            var language = await _repository.GetByIdAsync(id);
            return language != null;
        }
    }
}
