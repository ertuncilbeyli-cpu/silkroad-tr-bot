using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using RSBot.Core;
using RSBot.Core.Event;
using SDUI.Controls;

namespace RSBot.Log.Views;

[ToolboxItem(false)]
public partial class Main : DoubleBufferedControl
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Main" /> class.
    /// </summary>
    public Main()
    {
        CheckForIllegalCrossThreadCalls = false;
        InitializeComponent();
        LoadConfig();

        EventManager.SubscribeEvent("OnAddLog", new Action<string, LogLevel>(AppendLog));

        if (!Kernel.Debug)
        {
            checkDebug.Checked = false;
            checkError.Visible = false;
            checkNormal.Visible = false;
            checkWarning.Visible = false;
            checkDebug.Visible = false;
        }
    }

    /// <summary>
    ///     Appends the log.
    /// </summary>
    /// <param name="message">The message.</param>
    public void AppendLog(string message, LogLevel level = LogLevel.Notify)
    {
        if (!checkEnabled.Checked)
            return;

        var logFile = Path.Combine(
            Kernel.BasePath,
            "User",
            "Logs",
            Game.Player == null ? "Environment" : Game.Player.Name,
            $"{DateTime.Now:dd-MM-yyyy}.txt"
        );

        if (level == LogLevel.Debug && !checkDebug.Checked)
            return;

        if (level == LogLevel.Error && !checkError.Checked)
            return;

        if (level == LogLevel.Notify && !checkNormal.Checked)
            return;

        if (level == LogLevel.Warning && !checkWarning.Checked)
            return;

        if (WriteOnUIThread(message, level))
            return;

        txtLog.Write($"<{level}> \t{message}", true, Kernel.Debug, logFile);
    }

    /// <summary>
    ///     Silkroad TR: kutunun pencere tanıtıcısı (handle) henüz yoksa ve arka plan iş parçacığındaysak yazmayı açık bir
    ///     pencere üzerinden arayüz iş parçacığına aktarır. Aktarmazsa tanıtıcı bu iş parçacığında oluşur (InvokeRequired
    ///     tanıtıcı yokken false döner) ve arayüz iş parçacığının sonraki yazmaları Invoke'ta sonsuza dek bekler: oyun
    ///     verisi yüklenirken gelen günlükle bot açılış ekranında kilitleniyordu (Wine'da her açılışta).
    /// </summary>
    private bool WriteOnUIThread(string message, LogLevel level)
    {
        if (txtLog.IsHandleCreated)
            return false;

        try
        {
            var ui = Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;
            if (ui == null || !ui.IsHandleCreated || !ui.InvokeRequired)
                return false;

            ui.BeginInvoke(new Action(() => AppendLog(message, level)));
            return true;
        }
        catch (InvalidOperationException)
        {
            return false; // pencere o arada kapandıysa eskisi gibi doğrudan yaz
        }
    }

    /// <summary>
    ///     Loads the configuration.
    /// </summary>
    private void LoadConfig()
    {
        checkEnabled.Checked = GlobalConfig.Get("RSBot.Log.logEnabled", true);
    }

    /// <summary>
    ///     Handles the CheckedChanged event of the checkEnabled control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
    private void checkEnabled_CheckedChanged(object sender, EventArgs e)
    {
        GlobalConfig.Set("RSBot.Log.logEnabled", checkEnabled.Checked.ToString());
    }

    /// <summary>
    ///     Handles the Click event of the btnReset control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
    private void btnReset_Click(object sender, EventArgs e)
    {
        txtLog.Text = string.Empty;
    }
}
