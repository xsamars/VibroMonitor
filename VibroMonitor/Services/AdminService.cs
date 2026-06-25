using LiveChartsCore.SkiaSharpView.WPF;
using System;
using System.Windows;
using VibroMonitor.Data;
using VibroMonitor.Views;

namespace VibroMonitor.Services;

public class AdminService
{
    private readonly AppDbContext _db;
    private bool _isAuthenticated = false;

    public AdminService(AppDbContext db)
    {
        _db = db;
    }

    public bool IsAuthenticated => _isAuthenticated;

    public event Action<bool>? AuthChanged;

    public bool EnsureAuthenticated(Window? owner)
    {
        if (_isAuthenticated) return true;
        var pw = App.Services?.GetService(typeof(PasswordPromptWindow)) as PasswordPromptWindow;
        if (pw == null) return false;
        if (owner != null) pw.Owner = owner;
        var res = pw.ShowDialog();
        if (res != true) return false;
        var ok = CheckPassword(pw.Password ?? string.Empty);
        if (ok)
        {
            AuthChanged?.Invoke(true);
        }
        return ok;
    }

    public bool CheckPassword(string password)
    {
        // compare with stored password in DB (Settings table) or default
        var setting = _db.Set<Models.Setting>().Find("admin_password");
        var stored = setting != null ? setting.Value : "Admin1qq";
        if (stored == password)
        {
            _isAuthenticated = true;
            AuthChanged?.Invoke(true);
            return true;
        }
        return false;
    }

    public void Logout()
    {
        _isAuthenticated = false;
        AuthChanged?.Invoke(false);
    }

    public void ChangePassword(string newPassword)
    {
        var setting = _db.Set<Models.Setting>().Find("admin_password");
        if (setting == null)
        {
            setting = new Models.Setting { Key = "admin_password", Value = newPassword };
            _db.Add(setting);
        }
        else
        {
            setting.Value = newPassword;
            _db.Update(setting);
        }
        _db.SaveChanges();
    }
}
