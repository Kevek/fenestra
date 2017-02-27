﻿using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using net.codingpanda.app.fenestra.utils;
using net.codingpanda.app.fenestra.wpf;


namespace net.codingpanda.app.fenestra {
  public class FenestraViewModel : NotifyPropertyChangedBase {
    private ForegroundWindowUtil.ForegroundWindowEventInfo windowEventInfo;

    private WindowManager WindowManager { get; }

    private Window mainWindow;

    public FenestraViewModel(WindowManager windowManager) {
      WindowManager=windowManager;
    }

    public void Init() {
      mainWindow=WindowManager.GetWindow(this);
      var fenestraProcessId=ForegroundWindowUtil.GetWindowThreadProcessId(new WindowInteropHelper(mainWindow).EnsureHandle());

      FenestraNotifyIconUtil.CreateNotifyIcon(() => {
        GlobalHotkeyUtil.RemoveHotkey(mainWindow);
        var windowArgs=WindowManagerWindowArgs.CreateDefault();
        windowArgs.Topmost=false;
        var settingsViewModel=new FenestraSettingsViewModel();
        var settingsWindow=WindowManager.CreateWindow(settingsViewModel, windowArgs);
        settingsWindow.Closing+=(s, e) => {
          var newArgs=FenestraSettingsUtil.LoadSettings();
          FenestraSettingsUtil.ApplySettings(newArgs, mainWindow);
        };
        settingsViewModel.SettingsSaved+=() => {
          WindowManager.CloseWindow(settingsViewModel);
          var newArgs=FenestraSettingsUtil.LoadSettings();
          FenestraSettingsUtil.ApplySettings(newArgs, mainWindow);
        };
      });

      LoadFenestraSettings();
      GlobalHotkeyUtil.OnHotkeyPressed+=() => {
        WindowManager.CloseAllSelectionWindows();
        var newForegroundWindowHandle=ForegroundWindowUtil.GetForegroundWindowHandle();
        if(ForegroundWindowUtil.IsHandleOwnedByFenestraProcess(fenestraProcessId, newForegroundWindowHandle)) {
          return;
        }
        foreach(var screen in Screen.AllScreens) {
          var vm=new FenestraResizeSelectionViewModel(screen, WindowManager);
          vm.LoadForegroundWindowInfo(newForegroundWindowHandle);
          WindowManager.CreateCenteredWindow(vm, screen);
        }
      };
      GlobalHotkeyUtil.OnEscapePressed+=() => { WindowManager.CloseAllSelectionWindows(); };

      windowEventInfo=ForegroundWindowUtil.AddHookToForegroundWindowEvent(() => {
        var newForegroundWindowHandle=ForegroundWindowUtil.GetForegroundWindowHandle();
        if(ForegroundWindowUtil.IsHandleOwnedByFenestraProcess(fenestraProcessId, newForegroundWindowHandle)) {
          return;
        }
        var selectionViewModels=WindowManager.GetWindowKeys().OfType<FenestraResizeSelectionViewModel>().ToArray();
        foreach(var selectionViewModel in selectionViewModels) {
          selectionViewModel.LoadForegroundWindowInfo(newForegroundWindowHandle);
        }
      });
    }

    public void LoadFenestraSettings() {
      FenestraSettingsUtil.ApplySettings(FenestraSettingsUtil.LoadSettings(), mainWindow);
    }
  }
}
