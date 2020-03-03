using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace BingLibrary.hjb.Metro
{
    public class Metro
    {
        public async Task<bool> ShowConfirm(string Title, string Msg, bool isAccented = true)
        {
            return await ((MetroWindow)Application.Current.MainWindow).ShowMessageAsync(Title,
                Msg,
                MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                {
                    AffirmativeButtonText = "确  认",
                    NegativeButtonText = "取  消",
                    ColorScheme = isAccented ? MetroDialogColorScheme.Accented : MetroDialogColorScheme.Theme,
                }) == MessageDialogResult.Affirmative;
        }

        public async Task<bool> ShowMessage(string Title, string Msg, bool isAccented = true)
        {
            var result = await ((MetroWindow)Application.Current.MainWindow).ShowMessageAsync(Title,
                Msg,
                MessageDialogStyle.Affirmative, new MetroDialogSettings()
                {
                    AffirmativeButtonText = "确  定",
                    ColorScheme = isAccented ? MetroDialogColorScheme.Accented : MetroDialogColorScheme.Theme,
                });
            return true;
        }

        public async Task<bool> ShowMessageAuto(string Msg, int Ms = 800, bool isAccented = true)
        {
            new CustomDialog().Title = Msg;
            await ((MetroWindow)Application.Current.MainWindow).ShowMetroDialogAsync(new CustomDialog());
            await Task.Delay(Ms);
            await ((MetroWindow)Application.Current.MainWindow).HideMetroDialogAsync(new CustomDialog());
            return true;
        }

        public async Task<List<string>> ShowLogin(string Title, string PasswordWatermark = "请输入你的密码", bool isAccented = true)
        {
            LoginDialogData result = await ((MetroWindow)Application.Current.MainWindow).ShowLoginAsync(Title, "输入你的凭证:", new LoginDialogSettings { ColorScheme = isAccented ? MetroDialogColorScheme.Accented : MetroDialogColorScheme.Theme, AffirmativeButtonText = "确  认", PasswordWatermark = PasswordWatermark, NegativeButtonText = "取  消", InitialUsername = "Administrator" });
            List<string> rst = new List<string>();
            rst.Add(result?.Username);
            rst.Add(result?.Password);
            return rst;
        }

        public async Task<string> ShowLoginOnlyPassword(string Title, string PasswordWatermark = "请输入你的密码", bool isAccented = true)
        {
            return (await ((MetroWindow)Application.Current.MainWindow).ShowLoginAsync(Title, "输入你的凭证:", new LoginDialogSettings { ColorScheme = isAccented ? MetroDialogColorScheme.Accented : MetroDialogColorScheme.Theme, PasswordWatermark = PasswordWatermark, ShouldHideUsername = true }))?.Password;
        }

        public async void ShowProgress(string Title, string Msg, Action Function)
        {
            (await ((MetroWindow)Application.Current.MainWindow).ShowProgressAsync(Title, Msg)).SetIndeterminate();
            //controller.SetCancelable(true);
            await ((Func<Task>)(() =>
            {
                return Task.Run(() =>
                {
                    Function();
                });
            }))();
            await (await ((MetroWindow)Application.Current.MainWindow).ShowProgressAsync(Title, Msg)).CloseAsync();
        }

        public async Task<string> ShowInput(string Title, string Msg, bool isAccented = true)
        {
            return await ((MetroWindow)Application.Current.MainWindow).ShowInputAsync(Title, Msg, new MetroDialogSettings { ColorScheme = isAccented ? MetroDialogColorScheme.Accented : MetroDialogColorScheme.Theme }) ?? await ((MetroWindow)Application.Current.MainWindow).ShowInputAsync(Title, Msg, new MetroDialogSettings { ColorScheme = isAccented ? MetroDialogColorScheme.Accented : MetroDialogColorScheme.Theme });
        }

        public List<string> GetThemes()
        {
            new List<string>().Add("BaseLight");
            new List<string>().Add("BaseDark");
            return new List<string>();
        }

        public List<string> GetAccents()
        {
            foreach (var act in ThemeManager.Accents)
            {
                new List<string>().Add(act.Name);
            }
            return new List<string>();
        }

        public void ChangeTheme(string Theme)
        {
            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.DetectAppStyle(Application.Current).Item2, ThemeManager.GetAppTheme(Theme));
        }

        public void ChangeAccent(string Accent)
        {
            //MetroWindow window = (MetroWindow)Application.Current.MainWindow;
            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(Accent), ThemeManager.DetectAppStyle(Application.Current).Item1);
        }
    }
}