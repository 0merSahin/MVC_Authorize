﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionTestProject.Concrete;
using SessionTestProject.Entities;

namespace SessionTestProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly SqlDbContext _context;

        public AccountController(SqlDbContext context)
        {
            _context = context;
        }


        [Authorize]
        [HttpGet]
        public ActionResult<IEnumerable<Account>> Index()
        {
            var accounts = _context.Accounts.ToList();

            try
            {
                var dataValue = _context.Accounts.FirstOrDefault(x => x.Email == User.FindFirst(ClaimTypes.Email).Value);
                if (dataValue != null) accounts.Remove(dataValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nBir hata oluştu:" + ex);
            }

            return View(accounts);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        public async Task<ActionResult<Account>> Create(Account account)
        {
            if (ModelState.IsValid)
            {
                // Var ise hali hazırdaki oturum kapatılıyor:
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }


                // Hali hazırda böyle bir kullanıcı var mı kontrol ediliyor:
                if (_context.Accounts.FirstOrDefault(x => x.Email == account.Email) != null)
                {
                    return RedirectToAction("CreateError", "Error", new { errorMessage = "Böyle bir kullanıcı zaten mevcut!" });
                }
                else
                {
                    // Yeni kullanıcı veritabanına kayıt ediliyor:
                    account.isActive = true;
                    _context.Accounts.Add(account);
                    _context.SaveChanges();

                    
                    // Yeni kullanıcı ile oturum açılıyor:
                    var dataValue = _context.Accounts.FirstOrDefault(x => x.Email == account.Email && x.Password == account.Password);
                    if (dataValue != null)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Email, account.Email)
                        };
                        var userIdentity = new ClaimsIdentity(claims, " ");
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true, // Oturumun kalıcı olmasını sağlar
                            ExpiresUtc = DateTimeOffset.UtcNow.AddMonths(1) // Opsiyonel: Çerezin ne kadar süreyle geçerli olacağını belirler
                        };
                        ClaimsPrincipal principal = new ClaimsPrincipal(userIdentity);
                        await HttpContext.SignInAsync(principal, authProperties);
                        return RedirectToAction("Index", "Account");
                    }
                    else
                    {
                        return RedirectToAction("CreateError", "Error", new { errorMessage = "Kullanıcı doğru bir şekilde oluşturulamadı!" });
                    }
                }
            }
            else
                return RedirectToAction("CreateError", "Error", new { errorMessage = "Form doğru formatta değil!" });
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(Account account)
        {
            if (ModelState.IsValid)
            {
                var dataValue = _context.Accounts.FirstOrDefault(x => x.Email == account.Email && x.Password == account.Password);
                if (dataValue != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, account.Email)
                    };
                    var userIdentity = new ClaimsIdentity(claims, " ");

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // Oturumun kalıcı olmasını sağlar
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMonths(1) // Opsiyonel: Çerezin ne kadar süreyle geçerli olacağını belirler
                    };

                    ClaimsPrincipal principal = new ClaimsPrincipal(userIdentity);
                    await HttpContext.SignInAsync(principal, authProperties);


                    return RedirectToAction("Index", "Account");
                }
                else
                {
                    return View();
                }
            }
            else
                return RedirectToAction("CreateError", "Error", new { errorMessage = "Form doğru formatta değil!" });
        }



        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

    }
}

