using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Wordify.Data;
using Wordify.Extensions;
using Wordify.Models;
using Wordify.Models.Interfaces;

namespace Wordify.Pages
{
    /// <summary>
    /// This makes sure the user is logged in to view this page.
    /// It also allows us to manage the profile page from here rather than a controller.
    /// </summary>
    [Authorize (Policy = "MemberOnly")]
    public class ProfileModel : PageModel
    {
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;
        private INote _notes;
        private IBlob _blob;

        [BindProperty]
        public string FirstName { get; set; }

        [BindProperty]
        public string LastName { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string UserName { get; set; }

        [BindProperty]
        public string Bio { get; set; }

        [BindProperty]
        public List<Note> Notes { get; set; }

        [BindProperty]
        public NoteCardViewModel Ncvm { get; set; }

        public ProfileModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
            INote notes, IBlob blob)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _notes = notes;
            _blob = blob;
        }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);

            UserName = user.UserName;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Bio = user.Bio;
            Email = user.Email;
            Notes = await _notes.GetNotesByUserID(user.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostUser()
        {
            var user = await _userManager.GetUserAsync(User);

            user.FirstName = FirstName;
            user.LastName = LastName;
            user.Bio = Bio;
            await _userManager.UpdateAsync(user);

            List<Claim> oldClaims = new List<Claim>();
            List<Claim> newClaims = new List<Claim>();

            oldClaims.Add(User.Claims.FirstOrDefault(c => c.Type == "FullName"));
            newClaims.Add(new Claim("FullName", $"{user.FirstName} {user.LastName}"));

            await _userManager.RemoveClaimsAsync(user, oldClaims);
            await _userManager.AddClaimsAsync(user, newClaims);

            await _signInManager.SignOutAsync();
            await _signInManager.SignInAsync(user, false);
            return RedirectToPage("/Index");
        }

        public void OnPostNote(int id)
        {
            var user = _userManager.GetUserAsync(User).Result;

            UserName = user.UserName;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Bio = user.Bio;
            Email = user.Email;
            Notes = _notes.GetNotesByUserID(user.Id).Result;
            try
            {
                Note note = _notes.GetNoteByID(id).Result;
                if (!Notes.Contains(note))
                {
                    throw new Exception("You are not the owner of that note");
                }
                string blobText = _blob.GetText(note).Result;
                Ncvm.Note = note;
                Ncvm.Text = blobText;
                Ncvm.Title = note.Title;
                byte[] blobImage = _blob.GetImage(note).Result;
                ImageDisplayExtensions.DisplayImage(blobImage);
            }
            catch (Exception)
            {
                TempData["Error"] = "Something went wrong with accessing that note";
            }
        }

        public IActionResult OnPostDeleteNote(int id)
        {
            var user = _userManager.GetUserAsync(User).Result;
            Note note = _notes.GetNoteByID(id).Result;
            if (note.UserID != user.Id)
            {
                TempData["Error"] = "You are not the owner of that note";
                return Page();
            }
            _blob.DeleteBlob(note);
            _notes.DestroyNote(id);
            return RedirectToPage("/Profile");
        }


        public IActionResult OnPostUpdate(int id)
        {
            var user = _userManager.GetUserAsync(User).Result;

            UserName = user.UserName;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Bio = user.Bio;
            Email = user.Email;
            Notes = _notes.GetNotesByUserID(user.Id).Result;
            try
            {
                Note note = _notes.GetNoteByID(id).Result;
                if (!Notes.Contains(note))
                {
                    throw new Exception("You are not the owner of that note");
                }
                note.Title = Ncvm.Title;
                _notes.UpdateNote(note, id);
                _blob.UpdateText(note, Ncvm.Text);
            }
            catch (Exception)
            {
                TempData["Error"] = "You are not the owner of that note";
                return Page();
            }

            return RedirectToPage();
        }
    }
}